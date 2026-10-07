using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Text;
using System.Collections.Generic;
using System.IO;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver networkDriver;
    private NativeList<NetworkConnection> networkConnections;

    NetworkPipeline reliableAndInOrderPipeline;
    NetworkPipeline nonReliableNotInOrderedPipeline;

    const ushort NetworkPort = 9001;

    const int MaxNumberOfClientConnections = 1000;

    // Every accepted connection gets its own connection ID.
    Dictionary<NetworkConnection, int> connectionIdByConnection = new Dictionary<NetworkConnection, int>();
    Dictionary<int, NetworkConnection> connectionById = new Dictionary<int, NetworkConnection>();
    int nextConnectionId = 1;

    AccountManager accountManager;

    void Start()
    {
        networkDriver = NetworkDriver.Create();
        reliableAndInOrderPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        nonReliableNotInOrderedPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage));
        NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = NetworkPort;

        int error = networkDriver.Bind(endpoint);
        if (error != 0)
            Debug.Log("Failed to bind to port " + NetworkPort);
        else
            networkDriver.Listen();

        networkConnections = new NativeList<NetworkConnection>(MaxNumberOfClientConnections, Allocator.Persistent);

        accountManager = new AccountManager(Path.Combine(Application.persistentDataPath, "Accounts.txt"));
    }

    void OnDestroy()
    {
        networkDriver.Dispose();
        networkConnections.Dispose();
    }

    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        #region Remove Unused Connections

        for (int i = 0; i < networkConnections.Length; i++)
        {
            if (!networkConnections[i].IsCreated)
            {
                networkConnections.RemoveAtSwapBack(i);
                i--;
            }
        }

        #endregion

        #region Accept New Connections

        while (AcceptIncomingConnection())
        {
        }

        #endregion

        #region Manage Network Events

        DataStreamReader streamReader;
        NetworkPipeline pipelineUsedToSendEvent;
        NetworkEvent.Type networkEventType;

        for (int i = 0; i < networkConnections.Length; i++)
        {
            if (!networkConnections[i].IsCreated)
                continue;

            while (PopNetworkEventAndCheckForData(networkConnections[i], out networkEventType, out streamReader, out pipelineUsedToSendEvent))
            {
                if (pipelineUsedToSendEvent == reliableAndInOrderPipeline)
                    Debug.Log("Network event from: reliableAndInOrderPipeline");
                else if (pipelineUsedToSendEvent == nonReliableNotInOrderedPipeline)
                    Debug.Log("Network event from: nonReliableNotInOrderedPipeline");

                switch (networkEventType)
                {
                    case NetworkEvent.Type.Data:
                        int sizeOfDataBuffer = streamReader.ReadInt();
                        NativeArray<byte> buffer = new NativeArray<byte>(sizeOfDataBuffer, Allocator.Persistent);
                        streamReader.ReadBytes(buffer);
                        byte[] byteBuffer = buffer.ToArray();
                        string msg = Encoding.Unicode.GetString(byteBuffer);
                        ProcessReceivedMsg(msg, connectionIdByConnection[networkConnections[i]]);
                        buffer.Dispose();
                        break;
                    case NetworkEvent.Type.Disconnect:
                        OnClientDisconnected(networkConnections[i]);
                        networkConnections[i] = default(NetworkConnection);
                        break;
                }
            }
        }

        #endregion
    }

    private bool AcceptIncomingConnection()
    {
        NetworkConnection connection = networkDriver.Accept();
        if (connection == default(NetworkConnection))
            return false;

        networkConnections.Add(connection);

        int connectionId = nextConnectionId++;
        connectionIdByConnection.Add(connection, connectionId);
        connectionById.Add(connectionId, connection);
        Debug.Log("Accepted a client connection, connection ID = " + connectionId);
        return true;
    }

    private bool PopNetworkEventAndCheckForData(NetworkConnection networkConnection, out NetworkEvent.Type networkEventType, out DataStreamReader streamReader, out NetworkPipeline pipelineUsedToSendEvent)
    {
        networkEventType = networkConnection.PopEvent(networkDriver, out streamReader, out pipelineUsedToSendEvent);

        if (networkEventType == NetworkEvent.Type.Empty)
            return false;
        return true;
    }

    private void OnClientDisconnected(NetworkConnection connection)
    {
        if (!connectionIdByConnection.TryGetValue(connection, out int connectionId))
            return;

        Debug.Log("Client with connection ID " + connectionId + " has disconnected from server");
        accountManager.OnClientDisconnected(connectionId);
        connectionIdByConnection.Remove(connection);
        connectionById.Remove(connectionId);
    }

    private void ProcessReceivedMsg(string msg, int connectionId)
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
                AccountResult result = values.Length == 3
                    ? accountManager.CreateAccount(values[1], values[2])
                    : AccountResult.MalformedMessage;
                string name = values.Length > 1 ? values[1] : "";
                SendMessageToClient(ServerToClientSignifiers.CreateAccountResult + "," + (int)result + "," + name, connectionId);
                break;
            }
            case ClientToServerSignifiers.Login:
            {
                string accountName = values.Length > 1 ? values[1] : "";
                AccountResult result = values.Length == 3
                    ? accountManager.Login(connectionId, values[1], values[2], out accountName)
                    : AccountResult.MalformedMessage;
                SendMessageToClient(ServerToClientSignifiers.LoginResult + "," + (int)result + "," + accountName, connectionId);
                break;
            }
            case ClientToServerSignifiers.Logout:
            {
                AccountResult result = accountManager.Logout(connectionId);
                SendMessageToClient(ServerToClientSignifiers.LogoutResult + "," + (int)result, connectionId);
                break;
            }
            default:
                Debug.LogWarning("Unknown signifier " + signifier + " from connection " + connectionId);
                break;
        }
    }

    public void SendMessageToClient(string msg, int connectionId)
    {
        if (connectionById.TryGetValue(connectionId, out NetworkConnection connection))
            SendMessageToClient(msg, connection);
    }

    public void SendMessageToClient(string msg, NetworkConnection networkConnection)
    {
        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(msg);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);


        //Driver.BeginSend(m_Connection, out var writer);
        DataStreamWriter streamWriter;
        //networkConnection.
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }

}
