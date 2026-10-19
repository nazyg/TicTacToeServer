using System.Text.RegularExpressions;

public static class NetworkMessageFormat
{
    public const char Separator = ',';
}

public static class ClientToServerSignifiers
{
    public const int CreateAccount = 1;   
    public const int Login = 2;          
    public const int Logout = 3;          
    public const int JoinOrCreateGameRoom = 4;
    public const int LeaveGameRoom = 5;
    public const int SendMessageToOpponent = 6;
    public const int PlaceMark = 7;
    public const int RequestRematch = 8;
}

public static class ServerToClientSignifiers
{
    public const int CreateAccountResult = 1;   
    public const int LoginResult = 2;           
    public const int LogoutResult = 3;          
    public const int GameRoomError = 4;
    public const int WaitingForOpponent = 5;
    public const int GameStarted = 6;
    public const int OpponentMessage = 7;
    public const int LeftGameRoom = 8;
    public const int OpponentLeftGameRoom = 9;
    public const int BoardUpdate = 10;
    public const int MoveRejected = 11;
    public const int OpponentWantsRematch = 12;
}

public enum ResultCode
{
    Success = 0,
    NameEmpty = 1,
    PasswordEmpty = 2,
    NameInvalid = 3,
    PasswordInvalid = 4,
    PasswordsDoNotMatch = 5,
    NameTaken = 6,
    AccountNotFound = 7,
    WrongPassword = 8,
    AlreadyLoggedIn = 9,
    AlreadyLoggedInOnThisConnection = 10,
    NotLoggedIn = 11,
    MalformedMessage = 12,
    RoomNameInvalid = 13,
    RoomFull = 14,
    AlreadyInGameRoom = 15,
    NotInGameRoom = 16,
    NotYourTurn = 17,
    CellOccupied = 18,
    GameOver = 19,
    GameNotOver = 20,
}

public static class AccountRules
{
    public const int MinNameLength = 3;
    public const int MaxNameLength = 16;
    public const int MinPasswordLength = 4;
    public const int MaxPasswordLength = 32;

    static readonly Regex validName = new Regex("^[A-Za-z0-9_]{" + MinNameLength + "," + MaxNameLength + "}$");

    public static ResultCode Validate(string name, string password)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ResultCode.NameEmpty;
        if (string.IsNullOrEmpty(password))
            return ResultCode.PasswordEmpty;
        if (!validName.IsMatch(name))
            return ResultCode.NameInvalid;
        if (password.Length < MinPasswordLength || password.Length > MaxPasswordLength
            || password.IndexOf(NetworkMessageFormat.Separator) >= 0
            || password.IndexOf('\n') >= 0 || password.IndexOf('\r') >= 0)
            return ResultCode.PasswordInvalid;

        return ResultCode.Success;
    }
}

public static class GameRoomRules
{
    public const int MaxRoomNameLength = 20;
    public const int MaxPlayersPerRoom = 2;

    static readonly Regex validRoomName = new Regex("^[A-Za-z0-9_ ]{1," + MaxRoomNameLength + "}$");

    public static ResultCode Validate(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName) || !validRoomName.IsMatch(roomName))
            return ResultCode.RoomNameInvalid;
        return ResultCode.Success;
    }
}

public static class ResultCodeMessages
{
    public static string ToMessage(ResultCode result)
    {
        switch (result)
        {
            case ResultCode.Success: return "Success.";
            case ResultCode.NameEmpty: return "Please enter an account name.";
            case ResultCode.PasswordEmpty: return "Please enter a password.";
            case ResultCode.NameInvalid: return "Account names must be " + AccountRules.MinNameLength + "-" + AccountRules.MaxNameLength + " characters (letters, numbers or _ only).";
            case ResultCode.PasswordInvalid: return "Passwords must be " + AccountRules.MinPasswordLength + "-" + AccountRules.MaxPasswordLength + " characters and cannot contain commas.";
            case ResultCode.PasswordsDoNotMatch: return "The two passwords do not match.";
            case ResultCode.NameTaken: return "That account name is already taken.";
            case ResultCode.AccountNotFound: return "No account exists with that name.";
            case ResultCode.WrongPassword: return "Incorrect password.";
            case ResultCode.AlreadyLoggedIn: return "That account is already logged in on another client.";
            case ResultCode.AlreadyLoggedInOnThisConnection: return "You are already logged in.";
            case ResultCode.NotLoggedIn: return "You are not logged in.";
            case ResultCode.MalformedMessage: return "The server could not read the request.";
            case ResultCode.RoomNameInvalid: return "Room names must be 1-" + GameRoomRules.MaxRoomNameLength + " characters (letters, numbers, spaces or _ only).";
            case ResultCode.RoomFull: return "That game room is full.";
            case ResultCode.AlreadyInGameRoom: return "You are already in a game room.";
            case ResultCode.NotInGameRoom: return "You are not in a game room.";
            case ResultCode.NotYourTurn: return "It's not your turn.";
            case ResultCode.CellOccupied: return "That cell is already taken.";
            case ResultCode.GameOver: return "The game is over.";
            case ResultCode.GameNotOver: return "The game is not over yet.";
            default: return "Unknown error (" + (int)result + ").";
        }
    }
}
