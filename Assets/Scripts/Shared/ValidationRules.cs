using System.Text.RegularExpressions;

public static class AccountRules
{
    public const int MinNameLength = 3;
    public const int MaxNameLength = 16;
    public const int MinPasswordLength = 4;
    public const int MaxPasswordLength = 32;

    static readonly Regex ValidName = new Regex($"^[A-Za-z0-9_]{{{MinNameLength},{MaxNameLength}}}$");

    public static ResultCode Validate(string name, string password)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ResultCode.NameEmpty;
        if (string.IsNullOrEmpty(password))
            return ResultCode.PasswordEmpty;
        if (!ValidName.IsMatch(name))
            return ResultCode.NameInvalid;
        if (!IsValidPassword(password))
            return ResultCode.PasswordInvalid;
        return ResultCode.Success;
    }

    static bool IsValidPassword(string password)
    {
        bool hasValidLength = password.Length >= MinPasswordLength && password.Length <= MaxPasswordLength;
        bool hasForbiddenCharacter = password.IndexOfAny(new[] { NetworkMessage.Separator, '\n', '\r' }) >= 0;
        return hasValidLength && !hasForbiddenCharacter;
    }
}

public static class GameRoomRules
{
    public const int MaxRoomNameLength = 20;
    public const int MaxPlayersPerRoom = 2;

    static readonly Regex ValidRoomName = new Regex($"^[A-Za-z0-9_ ]{{1,{MaxRoomNameLength}}}$");

    public static string Normalize(string roomName)
    {
        return roomName == null ? "" : roomName.Trim();
    }

    public static ResultCode Validate(string roomName)
    {
        string normalized = Normalize(roomName);
        return normalized.Length > 0 && ValidRoomName.IsMatch(normalized) ? ResultCode.Success : ResultCode.RoomNameInvalid;
    }
}

public static class ChatRules
{
    public const int MaxMessageLength = 60;

    public static string Sanitize(string text)
    {
        string singleLine = (text ?? "").Replace('\n', ' ').Replace('\r', ' ').Trim();
        return singleLine.Length > MaxMessageLength ? singleLine.Substring(0, MaxMessageLength) : singleLine;
    }
}
