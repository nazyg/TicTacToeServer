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

    public void ReceivedMessageFromClient(string msg, int connectionId)
    {
        Debug.Log("Msg received from connection " + connectionId + " = " + msg);

        string[] values = msg.Split(NetworkMessageFormat.Separator);
        if (!int.TryParse(values[0], out int signifier))
        {
            Debug.LogWarning("Could not read signifier of msg: " + msg);
            return;
        }

        switch (signifier)
        {
            case ClientToServerSignifiers.CreateAccount:
            {
                ResultCode result = values.Length == 3
                    ? accountManager.CreateAccount(values[1], values[2])
                    : ResultCode.MalformedMessage;
                string name = values.Length > 1 ? values[1] : "";
                sendMessageToClient(ServerToClientSignifiers.CreateAccountResult + "," + (int)result + "," + name, connectionId);
                break;
            }
            case ClientToServerSignifiers.Login:
            {
                string accountName = values.Length > 1 ? values[1] : "";
                ResultCode result = values.Length == 3
                    ? accountManager.Login(connectionId, values[1], values[2], out accountName)
                    : ResultCode.MalformedMessage;
                sendMessageToClient(ServerToClientSignifiers.LoginResult + "," + (int)result + "," + accountName, connectionId);
                break;
            }
            case ClientToServerSignifiers.Logout:
            {
                gameRoomManager.OnClientDisconnectedOrLoggedOut(connectionId);
                ResultCode result = accountManager.Logout(connectionId);
                sendMessageToClient(ServerToClientSignifiers.LogoutResult + "," + (int)result, connectionId);
                break;
            }
            case ClientToServerSignifiers.JoinOrCreateGameRoom:
                gameRoomManager.JoinOrCreate(connectionId, values.Length == 2 ? values[1] : null);
                break;
            case ClientToServerSignifiers.LeaveGameRoom:
                gameRoomManager.Leave(connectionId);
                break;
            case ClientToServerSignifiers.PlaceMark:
                if (values.Length == 2 && int.TryParse(values[1], out int cellIndex))
                    gameRoomManager.PlaceMark(connectionId, cellIndex);
                else
                    sendMessageToClient(ServerToClientSignifiers.MoveRejected + "," + (int)ResultCode.MalformedMessage, connectionId);
                break;
            case ClientToServerSignifiers.WatchGameRoom:
                gameRoomManager.Watch(connectionId, values.Length == 2 ? values[1] : null);
                break;
            case ClientToServerSignifiers.StopWatching:
                gameRoomManager.StopWatching(connectionId);
                break;
            case ClientToServerSignifiers.RequestRematch:
                gameRoomManager.RequestRematch(connectionId);
                break;
            case ClientToServerSignifiers.SendMessageToOpponent:
                gameRoomManager.SendMessageToOpponent(connectionId, values.Length > 1 ? string.Join(",", values, 1, values.Length - 1) : "");
                break;
            default:
                Debug.LogWarning("Unknown signifier " + signifier + " from connection " + connectionId);
                break;
        }
    }

    public void OnClientDisconnected(int connectionId)
    {
        gameRoomManager.OnClientDisconnectedOrLoggedOut(connectionId);
        accountManager.OnClientDisconnected(connectionId);
    }
}
