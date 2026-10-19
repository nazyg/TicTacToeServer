using System;
using System.Collections.Generic;
using UnityEngine;

public class GameRoom
{
    public string name;
    public List<int> playerConnectionIds = new List<int>();
    public TicTacToeBoard board = new TicTacToeBoard();
    public HashSet<int> rematchRequests = new HashSet<int>();

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

    public TicTacToeMark GetMark(int connectionId)
    {
        int index = playerConnectionIds.IndexOf(connectionId);
        if (index == 0)
            return TicTacToeMark.X;
        if (index == 1)
            return TicTacToeMark.O;
        return TicTacToeMark.None;
    }
}

public class GameRoomManager
{
    readonly Dictionary<string, GameRoom> roomsByName = new Dictionary<string, GameRoom>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<int, GameRoom> roomByConnectionId = new Dictionary<int, GameRoom>();

    const int MaxChatMessageLength = 100;

    readonly Action<string, int> sendMessageToClient;
    readonly Func<int, string> getAccountName;

    public GameRoomManager(Action<string, int> sendMessageToClient, Func<int, string> getAccountName)
    {
        this.sendMessageToClient = sendMessageToClient;
        this.getAccountName = getAccountName;
    }

    public void JoinOrCreate(int connectionId, string roomName)
    {
        ResultCode result = CheckCanJoin(connectionId, roomName);
        if (result != ResultCode.Success)
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
        room.board.Reset();
        room.rematchRequests.Clear();
        foreach (int playerId in room.playerConnectionIds)
        {
            string opponentName = getAccountName(room.GetOpponent(playerId));
            sendMessageToClient(ServerToClientSignifiers.GameStarted + "," + room.name + "," + opponentName + "," + (int)room.GetMark(playerId), playerId);
        }
        SendBoardToRoom(room);
    }

    public void PlaceMark(int connectionId, int cellIndex)
    {
        ResultCode result = CheckCanPlaceMark(connectionId, cellIndex, out GameRoom room);
        if (result != ResultCode.Success)
        {
            sendMessageToClient(ServerToClientSignifiers.MoveRejected + "," + (int)result, connectionId);
            return;
        }

        room.board.TryPlaceMark(cellIndex);
        Debug.Log(getAccountName(connectionId) + " placed " + room.GetMark(connectionId) + " on cell " + cellIndex + " in room '" + room.name + "'");

        if (room.board.Result != TicTacToeResult.InProgress)
            Debug.Log("Game in room '" + room.name + "' ended: " + room.board.Result);

        SendBoardToRoom(room);
    }

    ResultCode CheckCanPlaceMark(int connectionId, int cellIndex, out GameRoom room)
    {
        if (!roomByConnectionId.TryGetValue(connectionId, out room) || !room.IsFull)
            return ResultCode.NotInGameRoom;
        if (room.board.Result != TicTacToeResult.InProgress)
            return ResultCode.GameOver;
        if (room.board.CurrentTurn != room.GetMark(connectionId))
            return ResultCode.NotYourTurn;
        if (!room.board.CanPlaceMark(cellIndex))
            return ResultCode.CellOccupied;
        return ResultCode.Success;
    }

    public void RequestRematch(int connectionId)
    {
        if (!roomByConnectionId.TryGetValue(connectionId, out GameRoom room) || !room.IsFull)
        {
            sendMessageToClient(ServerToClientSignifiers.MoveRejected + "," + (int)ResultCode.NotInGameRoom, connectionId);
            return;
        }
        if (room.board.Result == TicTacToeResult.InProgress)
        {
            sendMessageToClient(ServerToClientSignifiers.MoveRejected + "," + (int)ResultCode.GameNotOver, connectionId);
            return;
        }

        room.rematchRequests.Add(connectionId);
        if (room.rematchRequests.Count < GameRoomRules.MaxPlayersPerRoom)
        {
            sendMessageToClient(ServerToClientSignifiers.OpponentWantsRematch + "," + getAccountName(connectionId), room.GetOpponent(connectionId));
            return;
        }

        Debug.Log("Rematch in room '" + room.name + "'");
        room.rematchRequests.Clear();
        room.board.Reset();
        SendBoardToRoom(room);
    }

    void SendBoardToRoom(GameRoom room)
    {
        string msg = ServerToClientSignifiers.BoardUpdate + "," + room.board.Serialize();
        foreach (int playerId in room.playerConnectionIds)
            sendMessageToClient(msg, playerId);
    }

    ResultCode CheckCanJoin(int connectionId, string roomName)
    {
        if (getAccountName(connectionId) == null)
            return ResultCode.NotLoggedIn;
        if (roomByConnectionId.ContainsKey(connectionId))
            return ResultCode.AlreadyInGameRoom;

        ResultCode validation = GameRoomRules.Validate(roomName == null ? null : roomName.Trim());
        if (validation != ResultCode.Success)
            return validation;

        if (roomsByName.TryGetValue(roomName.Trim(), out GameRoom room) && room.IsFull)
            return ResultCode.RoomFull;

        return ResultCode.Success;
    }

    public void Leave(int connectionId)
    {
        if (!roomByConnectionId.TryGetValue(connectionId, out GameRoom room))
        {
            sendMessageToClient(ServerToClientSignifiers.GameRoomError + "," + (int)ResultCode.NotInGameRoom, connectionId);
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
            sendMessageToClient(ServerToClientSignifiers.GameRoomError + "," + (int)ResultCode.NotInGameRoom, connectionId);
            return;
        }

        text = text.Replace("\n", " ").Replace("\r", " ").Trim();
        if (text.Length == 0)
            return;
        if (text.Length > MaxChatMessageLength)
            text = text.Substring(0, MaxChatMessageLength);

        int opponentId = room.GetOpponent(connectionId);
        string senderName = getAccountName(connectionId);
        Debug.Log("Piping msg from " + senderName + " to connection " + opponentId + ": " + text);
        sendMessageToClient(ServerToClientSignifiers.OpponentMessage + "," + senderName + "," + text, opponentId);
    }
}
