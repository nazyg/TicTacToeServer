using System.Collections.Generic;
using System.Linq;

public class GameRoom
{
    readonly List<int> playerIds = new List<int>();
    readonly List<int> observerIds = new List<int>();
    readonly HashSet<int> rematchRequests = new HashSet<int>();

    public string Name { get; }
    public TicTacToeBoard Board { get; } = new TicTacToeBoard();

    public IReadOnlyList<int> PlayerIds => playerIds;
    public IReadOnlyList<int> ObserverIds => observerIds;
    public IEnumerable<int> EveryoneInRoom => playerIds.Concat(observerIds);

    public bool IsFull => playerIds.Count >= GameRoomRules.MaxPlayersPerRoom;
    public int PlayerXId => playerIds[0];
    public int PlayerOId => playerIds[1];

    public GameRoom(string name)
    {
        Name = name;
    }

    public void AddPlayer(int connectionId) => playerIds.Add(connectionId);
    public void RemovePlayer(int connectionId) => playerIds.Remove(connectionId);
    public void AddObserver(int connectionId) => observerIds.Add(connectionId);
    public void RemoveObserver(int connectionId) => observerIds.Remove(connectionId);

    public void StartNewGame()
    {
        Board.Reset();
        rematchRequests.Clear();
    }

    // Returns true when every player has asked for a rematch.
    public bool AddRematchRequest(int connectionId)
    {
        rematchRequests.Add(connectionId);
        return rematchRequests.Count >= GameRoomRules.MaxPlayersPerRoom;
    }

    public int GetOpponent(int connectionId)
    {
        return playerIds.FirstOrDefault(id => id != connectionId);
    }

    public TicTacToeMark GetMark(int connectionId)
    {
        int index = playerIds.IndexOf(connectionId);
        if (index == 0)
            return TicTacToeMark.X;
        if (index == 1)
            return TicTacToeMark.O;
        return TicTacToeMark.None;
    }
}
