using System;
using System.Collections.Generic;
using UnityEngine;

public class GameRoom
{
    public string name;
    public List<int> playerConnectionIds = new List<int>();

    public bool IsFull => playerConnectionIds.Count >= GameRoomRules.MaxPlayersPerRoom;

    public int GetOpponent(int connectionId)
    {
        foreach (int id in playerConnectionIds)
        {
            if (id != connectionId)
                return id;
        }
        return -1;
    }
}

public class GameRoomManager
{
    readonly Dictionary<string, GameRoom> roomsByName = new Dictionary<string, GameRoom>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<int, GameRoom> roomByConnectionId = new Dictionary<int, GameRoom>();

    readonly Action<string, int> sendMessageToClient;
    readonly Func<int, string> getAccountName;

    public GameRoomManager(Action<string, int> sendMessageToClient, Func<int, string> getAccountName)
    {
        this.sendMessageToClient = sendMessageToClient;
        this.getAccountName = getAccountName;
    }

    public void JoinOrCreate(int connectionId, string roomName)
    {
        AccountResult result = CheckCanJoin(connectionId, roomName);
        if (result != AccountResult.Success)
        {
            sendMessageToClient(ServerToClientSignifiers.GameRoomError + "," + (int)result, connectionId);
            return;
        }

        roomName = roomName.Trim();

        if (!roomsByName.TryGetValue(roomName, out GameRoom room))
        {
            room = new GameRoom { name = roomName };
            roomsByName.Add(roomName, room);
            Debug.Log("Game room '" + roomName + "' created by connection " + connectionId);
        }

        room.playerConnectionIds.Add(connectionId);
        roomByConnectionId.Add(connectionId, room);

        if (!room.IsFull)
        {
            sendMessageToClient(ServerToClientSignifiers.WaitingForOpponent + "," + room.name, connectionId);
            return;
        }

        Debug.Log("Game room '" + room.name + "' is full, starting game");
        foreach (int playerId in room.playerConnectionIds)
        {
            string opponentName = getAccountName(room.GetOpponent(playerId));
            sendMessageToClient(ServerToClientSignifiers.GameStarted + "," + room.name + "," + opponentName, playerId);
        }
    }

    AccountResult CheckCanJoin(int connectionId, string roomName)
    {
        if (getAccountName(connectionId) == null)
            return AccountResult.NotLoggedIn;
        if (roomByConnectionId.ContainsKey(connectionId))
            return AccountResult.AlreadyInGameRoom;

        AccountResult validation = GameRoomRules.Validate(roomName == null ? null : roomName.Trim());
        if (validation != AccountResult.Success)
            return validation;

        if (roomsByName.TryGetValue(roomName.Trim(), out GameRoom room) && room.IsFull)
            return AccountResult.RoomFull;

        return AccountResult.Success;
    }

    public void Leave(int connectionId)
    {
        if (!roomByConnectionId.TryGetValue(connectionId, out GameRoom room))
        {
            sendMessageToClient(ServerToClientSignifiers.GameRoomError + "," + (int)AccountResult.NotInGameRoom, connectionId);
            return;
        }

        RemoveFromRoom(connectionId, room);
        sendMessageToClient(ServerToClientSignifiers.LeftGameRoom.ToString(), connectionId);
    }

    public void OnClientDisconnectedOrLoggedOut(int connectionId)
    {
        if (roomByConnectionId.TryGetValue(connectionId, out GameRoom room))
            RemoveFromRoom(connectionId, room);
    }

    void RemoveFromRoom(int connectionId, GameRoom room)
    {
        string leavingPlayerName = getAccountName(connectionId);

        room.playerConnectionIds.Remove(connectionId);
        roomByConnectionId.Remove(connectionId);
        Debug.Log("Connection " + connectionId + " left game room '" + room.name + "'");

        foreach (int remainingId in room.playerConnectionIds)
        {
            roomByConnectionId.Remove(remainingId);
            sendMessageToClient(ServerToClientSignifiers.OpponentLeftGameRoom + "," + leavingPlayerName, remainingId);
        }

        roomsByName.Remove(room.name);
        Debug.Log("Game room '" + room.name + "' closed");
    }

    public void SendMessageToOpponent(int connectionId, string text)
    {
        if (!roomByConnectionId.TryGetValue(connectionId, out GameRoom room) || !room.IsFull)
        {
            sendMessageToClient(ServerToClientSignifiers.GameRoomError + "," + (int)AccountResult.NotInGameRoom, connectionId);
            return;
        }

        int opponentId = room.GetOpponent(connectionId);
        string senderName = getAccountName(connectionId);
        Debug.Log("Piping msg from " + senderName + " to connection " + opponentId + ": " + text);
        sendMessageToClient(ServerToClientSignifiers.OpponentMessage + "," + senderName + "," + text, opponentId);
    }
}
