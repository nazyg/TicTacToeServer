using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetworkServer : MonoBehaviour
{
    const ushort NetworkPort = 9001;
    const int MaxNumberOfClientConnections = 1000;
    const string AccountsFileName = "Accounts.txt";

    NetworkDriver networkDriver;
    NativeList<NetworkConnection> networkConnections;
    NetworkPipeline reliableAndInOrderPipeline;

    readonly Dictionary<NetworkConnection, int> connectionIdByConnection = new Dictionary<NetworkConnection, int>();
    readonly Dictionary<int, NetworkConnection> connectionById = new Dictionary<int, NetworkConnection>();
    int nextConnectionId = 1;

    NetworkServerProcessing networkServerProcessing;

    void Start()
    {
        networkDriver = NetworkDriver.Create();
        reliableAndInOrderPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        networkConnections = new NativeList<NetworkConnection>(MaxNumberOfClientConnections, Allocator.Persistent);

        StartListening();

        string accountsFilePath = Path.Combine(Application.persistentDataPath, AccountsFileName);
        networkServerProcessing = new NetworkServerProcessing(SendMessageToClient, accountsFilePath);
    }

    void OnDestroy()
    {
        networkDriver.Dispose();
        networkConnections.Dispose();
    }

    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();
        RemoveClosedConnections();
        AcceptNewConnections();
        ProcessNetworkEvents();
    }

    public void SendMessageToClient(string message, int connectionId)
    {
        if (!connectionById.TryGetValue(connectionId, out NetworkConnection connection))
            return;

        NativeArray<byte> buffer = new NativeArray<byte>(Encoding.Unicode.GetBytes(message), Allocator.Temp);
        networkDriver.BeginSend(reliableAndInOrderPipeline, connection, out DataStreamWriter streamWriter);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);
        buffer.Dispose();
    }

    void StartListening()
    {
        NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = NetworkPort;

        if (networkDriver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind to port " + NetworkPort);
            return;
        }

        networkDriver.Listen();
        Debug.Log("Server listening on port " + NetworkPort);
    }

    void RemoveClosedConnections()
    {
        for (int i = networkConnections.Length - 1; i >= 0; i--)
        {
            if (!networkConnections[i].IsCreated)
                networkConnections.RemoveAtSwapBack(i);
        }
    }

    void AcceptNewConnections()
    {
        NetworkConnection connection;
        while ((connection = networkDriver.Accept()) != default(NetworkConnection))
        {
            networkConnections.Add(connection);

            int connectionId = nextConnectionId++;
            connectionIdByConnection.Add(connection, connectionId);
            connectionById.Add(connectionId, connection);
            Debug.Log("Accepted a client connection, connection ID = " + connectionId);
        }
    }

    void ProcessNetworkEvents()
    {
        for (int i = 0; i < networkConnections.Length; i++)
        {
            NetworkConnection connection = networkConnections[i];
            if (!connection.IsCreated)
                continue;

            NetworkEvent.Type eventType;
            while ((eventType = connection.PopEvent(networkDriver, out DataStreamReader streamReader)) != NetworkEvent.Type.Empty)
            {
                if (eventType == NetworkEvent.Type.Data)
                {
                    string message = ReadMessage(ref streamReader);
                    networkServerProcessing.ReceivedMessageFromClient(message, connectionIdByConnection[connection]);
                }
                else if (eventType == NetworkEvent.Type.Disconnect)
                {
                    OnClientDisconnected(connection);
                    networkConnections[i] = default(NetworkConnection);
                    break;
                }
            }
        }
    }

    static string ReadMessage(ref DataStreamReader streamReader)
    {
        int messageLength = streamReader.ReadInt();
        NativeArray<byte> buffer = new NativeArray<byte>(messageLength, Allocator.Temp);
        streamReader.ReadBytes(buffer);
        string message = Encoding.Unicode.GetString(buffer.ToArray());
        buffer.Dispose();
        return message;
    }

    void OnClientDisconnected(NetworkConnection connection)
    {
        if (!connectionIdByConnection.TryGetValue(connection, out int connectionId))
            return;

        Debug.Log("Client with connection ID " + connectionId + " has disconnected from server");
        networkServerProcessing.OnClientDisconnected(connectionId);
        connectionIdByConnection.Remove(connection);
        connectionById.Remove(connectionId);
    }
}
