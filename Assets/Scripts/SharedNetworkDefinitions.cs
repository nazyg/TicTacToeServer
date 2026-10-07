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
}

public static class ServerToClientSignifiers
{
    public const int CreateAccountResult = 1;   
    public const int LoginResult = 2;           
    public const int LogoutResult = 3;          
}

public enum AccountResult
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
}

public static class AccountRules
{
    public const int MinNameLength = 3;
    public const int MaxNameLength = 16;
    public const int MinPasswordLength = 4;
    public const int MaxPasswordLength = 32;

    static readonly Regex validName = new Regex("^[A-Za-z0-9_]{" + MinNameLength + "," + MaxNameLength + "}$");

    public static AccountResult Validate(string name, string password)
    {
        if (string.IsNullOrWhiteSpace(name))
            return AccountResult.NameEmpty;
        if (string.IsNullOrEmpty(password))
            return AccountResult.PasswordEmpty;
        if (!validName.IsMatch(name))
            return AccountResult.NameInvalid;
        if (password.Length < MinPasswordLength || password.Length > MaxPasswordLength
            || password.IndexOf(NetworkMessageFormat.Separator) >= 0
            || password.IndexOf('\n') >= 0 || password.IndexOf('\r') >= 0)
            return AccountResult.PasswordInvalid;

        return AccountResult.Success;
    }
    public static string ToMessage(AccountResult result)
    {
        switch (result)
        {
            case AccountResult.Success: return "Success.";
            case AccountResult.NameEmpty: return "Please enter an account name.";
            case AccountResult.PasswordEmpty: return "Please enter a password.";
            case AccountResult.NameInvalid: return "Account names must be " + MinNameLength + "-" + MaxNameLength + " characters (letters, numbers or _ only).";
            case AccountResult.PasswordInvalid: return "Passwords must be " + MinPasswordLength + "-" + MaxPasswordLength + " characters and cannot contain commas.";
            case AccountResult.PasswordsDoNotMatch: return "The two passwords do not match.";
            case AccountResult.NameTaken: return "That account name is already taken.";
            case AccountResult.AccountNotFound: return "No account exists with that name.";
            case AccountResult.WrongPassword: return "Incorrect password.";
            case AccountResult.AlreadyLoggedIn: return "That account is already logged in on another client.";
            case AccountResult.AlreadyLoggedInOnThisConnection: return "You are already logged in.";
            case AccountResult.NotLoggedIn: return "You are not logged in.";
            case AccountResult.MalformedMessage: return "The server could not read the request.";
            default: return "Unknown error (" + (int)result + ").";
        }
    }
}
