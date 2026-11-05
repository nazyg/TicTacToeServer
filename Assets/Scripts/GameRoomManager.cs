using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameRoomManager
{
    readonly Dictionary<string, GameRoom> roomsByName = new Dictionary<string, GameRoom>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<int, GameRoom> roomByPlayerId = new Dictionary<int, GameRoom>();
    readonly Dictionary<int, GameRoom> roomByObserverId = new Dictionary<int, GameRoom>();

    readonly Action<string, int> sendMessageToClient;
    readonly Func<int, string> getAccountName;

    public GameRoomManager(Action<string, int> sendMessageToClient, Func<int, string> getAccountName)
    {
        this.sendMessageToClient = sendMessageToClient;
        this.getAccountName = getAccountName;
    }

    #region Joining and Leaving

    public void JoinOrCreate(int connectionId, string roomName)
    {
        roomName = GameRoomRules.Normalize(roomName);
        ResultCode result = CheckCanJoin(connectionId, roomName);
        if (result != ResultCode.Success)
        {
            Send(connectionId, ServerToClientSignifiers.GameRoomError, result);
            return;
        }

        GameRoom room = GetOrCreateRoom(roomName, connectionId);
        room.AddPlayer(connectionId);
        roomByPlayerId.Add(connectionId, room);

        if (room.IsFull)
            StartGame(room);
        else
            Send(connectionId, ServerToClientSignifiers.WaitingForOpponent, room.Name);
    }

    public void Leave(int connectionId)
    {
        if (!roomByPlayerId.TryGetValue(connectionId, out GameRoom room))
        {
            Send(connectionId, ServerToClientSignifiers.GameRoomError, ResultCode.NotInGameRoom);
            return;
        }

        CloseRoomBecausePlayerLeft(room, connectionId);
        Send(connectionId, ServerToClientSignifiers.LeftGameRoom);
    }

    public void RemoveConnection(int connectionId)
    {
        if (roomByPlayerId.TryGetValue(connectionId, out GameRoom room))
            CloseRoomBecausePlayerLeft(room, connectionId);
        else if (roomByObserverId.TryGetValue(connectionId, out GameRoom watchedRoom))
            RemoveObserver(watchedRoom, connectionId);
    }

    ResultCode CheckCanJoin(int connectionId, string roomName)
    {
        ResultCode result = CheckCanEnterAnyRoom(connectionId, roomName);
        if (result != ResultCode.Success)
            return result;
        if (roomsByName.TryGetValue(roomName, out GameRoom room) && room.IsFull)
            return ResultCode.RoomFull;
        return ResultCode.Success;
    }

    ResultCode CheckCanEnterAnyRoom(int connectionId, string roomName)
    {
        if (getAccountName(connectionId) == null)
            return ResultCode.NotLoggedIn;
        if (IsInAnyRoom(connectionId))
            return ResultCode.AlreadyInGameRoom;
        return GameRoomRules.Validate(roomName);
    }

    bool IsInAnyRoom(int connectionId)
    {
        return roomByPlayerId.ContainsKey(connectionId) || roomByObserverId.ContainsKey(connectionId);
    }

    GameRoom GetOrCreateRoom(string roomName, int creatorId)
    {
        if (roomsByName.TryGetValue(roomName, out GameRoom room))
            return room;

        room = new GameRoom(roomName);
        roomsByName.Add(roomName, room);
        Debug.Log("Game room '" + roomName + "' created by connection " + creatorId);
        return room;
    }

    void CloseRoomBecausePlayerLeft(GameRoom room, int leavingPlayerId)
    {
        string leavingPlayerName = getAccountName(leavingPlayerId);
        room.RemovePlayer(leavingPlayerId);
        roomByPlayerId.Remove(leavingPlayerId);
        Debug.Log("Connection " + leavingPlayerId + " left game room '" + room.Name + "'");

        foreach (int remainingId in room.EveryoneInRoom.ToList())
        {
            roomByPlayerId.Remove(remainingId);
            roomByObserverId.Remove(remainingId);
            Send(remainingId, ServerToClientSignifiers.PlayerLeftGameRoom, leavingPlayerName);
        }

        roomsByName.Remove(room.Name);
        Debug.Log("Game room '" + room.Name + "' closed");
    }

    #endregion

    #region Playing

    public void PlaceMark(int connectionId, int cellIndex)
    {
        ResultCode result = CheckCanPlaceMark(connectionId, cellIndex, out GameRoom room);
        if (result != ResultCode.Success)
        {
            Send(connectionId, ServerToClientSignifiers.MoveRejected, result);
            return;
        }

        room.Board.TryPlaceMark(cellIndex);
        Debug.Log(getAccountName(connectionId) + " placed " + room.GetMark(connectionId) + " on cell " + cellIndex + " in room '" + room.Name + "'");
        if (!room.Board.IsInProgress)
            Debug.Log("Game in room '" + room.Name + "' ended: " + room.Board.Result);

        SendBoardToEveryone(room);
    }

    public void RequestRematch(int connectionId)
    {
        ResultCode result = CheckCanRequestRematch(connectionId, out GameRoom room);
        if (result != ResultCode.Success)
        {
            Send(connectionId, ServerToClientSignifiers.MoveRejected, result);
            return;
        }

        bool everyoneAgreed = room.AddRematchRequest(connectionId);
        if (!everyoneAgreed)
        {
            Send(room.GetOpponent(connectionId), ServerToClientSignifiers.OpponentWantsRematch, getAccountName(connectionId));
            return;
        }

        Debug.Log("Rematch in room '" + room.Name + "'");
        room.StartNewGame();
        SendBoardToEveryone(room);
    }

    public void SendChatMessage(int connectionId, string text)
    {
        if (!TryGetFullRoom(connectionId, out GameRoom room))
        {
            Send(connectionId, ServerToClientSignifiers.GameRoomError, ResultCode.NotInGameRoom);
            return;
        }

        text = ChatRules.Sanitize(text);
        if (text.Length == 0)
            return;

        int opponentId = room.GetOpponent(connectionId);
        string senderName = getAccountName(connectionId);
        Debug.Log("Piping msg from " + senderName + " to connection " + opponentId + ": " + text);
        Send(opponentId, ServerToClientSignifiers.ChatMessage, senderName, text);
    }

    void StartGame(GameRoom room)
    {
        Debug.Log("Game room '" + room.Name + "' is full, starting game");
        room.StartNewGame();

        foreach (int playerId in room.PlayerIds)
        {
            string opponentName = getAccountName(room.GetOpponent(playerId));
            Send(playerId, ServerToClientSignifiers.GameStarted, room.Name, opponentName, room.GetMark(playerId));
        }
        SendBoardToEveryone(room);
    }

    ResultCode CheckCanPlaceMark(int connectionId, int cellIndex, out GameRoom room)
    {
        if (!TryGetFullRoom(connectionId, out room))
            return ResultCode.NotInGameRoom;
        if (!room.Board.IsInProgress)
            return ResultCode.GameOver;
        if (room.Board.CurrentTurn != room.GetMark(connectionId))
            return ResultCode.NotYourTurn;
        if (!room.Board.CanPlaceMark(cellIndex))
            return ResultCode.CellOccupied;
        return ResultCode.Success;
    }

    ResultCode CheckCanRequestRematch(int connectionId, out GameRoom room)
    {
        if (!TryGetFullRoom(connectionId, out room))
            return ResultCode.NotInGameRoom;
        if (room.Board.IsInProgress)
            return ResultCode.GameNotOver;
        return ResultCode.Success;
    }

    bool TryGetFullRoom(int playerId, out GameRoom room)
    {
        return roomByPlayerId.TryGetValue(playerId, out room) && room.IsFull;
    }

    void SendBoardToEveryone(GameRoom room)
    {
        foreach (int connectionId in room.EveryoneInRoom)
            SendBoard(connectionId, room);
    }

    void SendBoard(int connectionId, GameRoom room)
    {
        Send(connectionId, ServerToClientSignifiers.BoardUpdate, room.Board.Serialize());
    }

    #endregion

    #region Observing

    public void Watch(int connectionId, string roomName)
    {
        roomName = GameRoomRules.Normalize(roomName);
        ResultCode result = CheckCanWatch(connectionId, roomName, out GameRoom room);
        if (result != ResultCode.Success)
        {
            Send(connectionId, ServerToClientSignifiers.GameRoomError, result);
            return;
        }

        room.AddObserver(connectionId);
        roomByObserverId.Add(connectionId, room);
        Debug.Log(getAccountName(connectionId) + " is now watching game room '" + room.Name + "'");

        Send(connectionId, ServerToClientSignifiers.ObservingStarted, room.Name, getAccountName(room.PlayerXId), getAccountName(room.PlayerOId));
        SendBoard(connectionId, room);
    }

    public void StopWatching(int connectionId)
    {
        if (!roomByObserverId.TryGetValue(connectionId, out GameRoom room))
        {
            Send(connectionId, ServerToClientSignifiers.GameRoomError, ResultCode.NotWatching);
            return;
        }

        RemoveObserver(room, connectionId);
        Send(connectionId, ServerToClientSignifiers.StoppedWatching);
    }

    ResultCode CheckCanWatch(int connectionId, string roomName, out GameRoom room)
    {
        room = null;
        ResultCode result = CheckCanEnterAnyRoom(connectionId, roomName);
        if (result != ResultCode.Success)
            return result;
        if (!roomsByName.TryGetValue(roomName, out room))
            return ResultCode.RoomNotFound;
        if (!room.IsFull)
            return ResultCode.GameNotStarted;
        return ResultCode.Success;
    }

    void RemoveObserver(GameRoom room, int observerId)
    {
        room.RemoveObserver(observerId);
        roomByObserverId.Remove(observerId);
        Debug.Log("Connection " + observerId + " stopped watching game room '" + room.Name + "'");
    }

    #endregion

    void Send(int connectionId, int signifier, params object[] values)
    {
        sendMessageToClient(NetworkMessage.Build(signifier, values), connectionId);
    }
}
