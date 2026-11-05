public class Account
{
    const int SerializedValueCount = 3;

    public string Name { get; }
    public string Salt { get; }
    public string PasswordHash { get; }

    public Account(string name, string salt, string passwordHash)
    {
        Name = name;
        Salt = salt;
        PasswordHash = passwordHash;
    }

    public static Account CreateWithPassword(string name, string password)
    {
        string salt = PasswordHasher.CreateSalt();
        return new Account(name, salt, PasswordHasher.Hash(password, salt));
    }

    public bool PasswordMatches(string password)
    {
        return PasswordHasher.Hash(password, Salt) == PasswordHash;
    }

    // Format: name,salt,passwordHash
    public string Serialize()
    {
        return string.Join(NetworkMessage.Separator.ToString(), Name, Salt, PasswordHash);
    }

    public static bool TryDeserialize(string line, out Account account)
    {
        string[] values = line.Split(NetworkMessage.Separator);
        account = values.Length == SerializedValueCount ? new Account(values[0], values[1], values[2]) : null;
        return account != null;
    }
}
