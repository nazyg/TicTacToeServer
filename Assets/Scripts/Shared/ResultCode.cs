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
    RoomNotFound = 21,
    GameNotStarted = 22,
    NotWatching = 23,
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
            case ResultCode.NameInvalid: return $"Account names must be {AccountRules.MinNameLength}-{AccountRules.MaxNameLength} characters (letters, numbers or _ only).";
            case ResultCode.PasswordInvalid: return $"Passwords must be {AccountRules.MinPasswordLength}-{AccountRules.MaxPasswordLength} characters and cannot contain commas.";
            case ResultCode.PasswordsDoNotMatch: return "The two passwords do not match.";
            case ResultCode.NameTaken: return "That account name is already taken.";
            case ResultCode.AccountNotFound: return "No account exists with that name.";
            case ResultCode.WrongPassword: return "Incorrect password.";
            case ResultCode.AlreadyLoggedIn: return "That account is already logged in on another client.";
            case ResultCode.AlreadyLoggedInOnThisConnection: return "You are already logged in.";
            case ResultCode.NotLoggedIn: return "You are not logged in.";
            case ResultCode.MalformedMessage: return "The server could not read the request.";
            case ResultCode.RoomNameInvalid: return $"Room names must be 1-{GameRoomRules.MaxRoomNameLength} characters (letters, numbers, spaces or _ only).";
            case ResultCode.RoomFull: return "That game room is full.";
            case ResultCode.AlreadyInGameRoom: return "You are already in a game room.";
            case ResultCode.NotInGameRoom: return "You are not in a game room.";
            case ResultCode.NotYourTurn: return "It's not your turn.";
            case ResultCode.CellOccupied: return "That cell is already taken.";
            case ResultCode.GameOver: return "The game is over.";
            case ResultCode.GameNotOver: return "The game is not over yet.";
            case ResultCode.RoomNotFound: return "No game room exists with that name.";
            case ResultCode.GameNotStarted: return "That game hasn't started yet. Try again when two players are in the room.";
            case ResultCode.NotWatching: return "You are not watching a game room.";
            default: return $"Unknown error ({(int)result}).";
        }
    }
}
