public static class ClientToServerSignifiers
{
    public const int CreateAccount = 1;
    public const int Login = 2;
    public const int Logout = 3;
    public const int JoinOrCreateGameRoom = 4;
    public const int LeaveGameRoom = 5;
    public const int SendChatMessage = 6;
    public const int PlaceMark = 7;
    public const int RequestRematch = 8;
    public const int WatchGameRoom = 9;
    public const int StopWatching = 10;
}

public static class ServerToClientSignifiers
{
    public const int CreateAccountResult = 1;
    public const int LoginResult = 2;
    public const int LogoutResult = 3;
    public const int GameRoomError = 4;
    public const int WaitingForOpponent = 5;
    public const int GameStarted = 6;
    public const int ChatMessage = 7;
    public const int LeftGameRoom = 8;
    public const int PlayerLeftGameRoom = 9;
    public const int BoardUpdate = 10;
    public const int MoveRejected = 11;
    public const int OpponentWantsRematch = 12;
    public const int ObservingStarted = 13;
    public const int StoppedWatching = 14;
}
