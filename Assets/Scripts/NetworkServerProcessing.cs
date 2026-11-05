using System;
using UnityEngine;

public class NetworkServerProcessing
{
    readonly Action<string, int> sendMessageToClient;
    readonly AccountManager accountManager;
    readonly GameRoomManager gameRoomManager;

    public NetworkServerProcessing(Action<string, int> sendMessageToClient, string accountsFilePath)
    {
        this.sendMessageToClient = sendMessageToClient;
        accountManager = new AccountManager(accountsFilePath);
        gameRoomManager = new GameRoomManager(sendMessageToClient, accountManager.GetAccountName);
    }

    public void ReceivedMessageFromClient(string rawMessage, int connectionId)
    {
        Debug.Log("Msg received from connection " + connectionId + " = " + rawMessage);

        NetworkMessage message = NetworkMessage.Parse(rawMessage);
        if (!message.IsValid)
        {
            Debug.LogWarning("Could not read signifier of msg: " + rawMessage);
            return;
        }

        switch (message.Signifier)
        {
            case ClientToServerSignifiers.CreateAccount:
                HandleCreateAccount(message, connectionId);
                break;
            case ClientToServerSignifiers.Login:
                HandleLogin(message, connectionId);
                break;
            case ClientToServerSignifiers.Logout:
                HandleLogout(connectionId);
                break;
            case ClientToServerSignifiers.JoinOrCreateGameRoom:
                gameRoomManager.JoinOrCreate(connectionId, ReadRoomName(message));
                break;
            case ClientToServerSignifiers.LeaveGameRoom:
                gameRoomManager.Leave(connectionId);
                break;
            case ClientToServerSignifiers.PlaceMark:
                HandlePlaceMark(message, connectionId);
                break;
            case ClientToServerSignifiers.RequestRematch:
                gameRoomManager.RequestRematch(connectionId);
                break;
            case ClientToServerSignifiers.WatchGameRoom:
                gameRoomManager.Watch(connectionId, ReadRoomName(message));
                break;
            case ClientToServerSignifiers.StopWatching:
                gameRoomManager.StopWatching(connectionId);
                break;
            case ClientToServerSignifiers.SendChatMessage:
                gameRoomManager.SendChatMessage(connectionId, message.GetRemainingText(0));
                break;
            default:
                Debug.LogWarning("Unknown signifier " + message.Signifier + " from connection " + connectionId);
                break;
        }
    }

    public void OnClientDisconnected(int connectionId)
    {
        gameRoomManager.RemoveConnection(connectionId);
        accountManager.Logout(connectionId);
    }

    void HandleCreateAccount(NetworkMessage message, int connectionId)
    {
        string name = message.GetString(0);
        ResultCode result = message.ValueCount == 2
            ? accountManager.CreateAccount(name, message.GetString(1))
            : ResultCode.MalformedMessage;

        Send(connectionId, ServerToClientSignifiers.CreateAccountResult, result, name);
    }

    void HandleLogin(NetworkMessage message, int connectionId)
    {
        string accountName = message.GetString(0);
        ResultCode result = message.ValueCount == 2
            ? accountManager.Login(connectionId, accountName, message.GetString(1), out accountName)
            : ResultCode.MalformedMessage;

        Send(connectionId, ServerToClientSignifiers.LoginResult, result, accountName);
    }

    void HandleLogout(int connectionId)
    {
        gameRoomManager.RemoveConnection(connectionId);
        ResultCode result = accountManager.Logout(connectionId);
        Send(connectionId, ServerToClientSignifiers.LogoutResult, result);
    }

    void HandlePlaceMark(NetworkMessage message, int connectionId)
    {
        if (message.TryGetInt(0, out int cellIndex))
            gameRoomManager.PlaceMark(connectionId, cellIndex);
        else
            Send(connectionId, ServerToClientSignifiers.MoveRejected, ResultCode.MalformedMessage);
    }

    // Room names can't contain the separator, so a message with extra values is treated as an invalid name.
    static string ReadRoomName(NetworkMessage message)
    {
        return message.ValueCount == 1 ? message.GetString(0) : null;
    }

    void Send(int connectionId, int signifier, params object[] values)
    {
        sendMessageToClient(NetworkMessage.Build(signifier, values), connectionId);
    }
}
