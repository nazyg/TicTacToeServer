using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class Account
{
    public string name;
    public string salt;
    public string passwordHash;
}


public class AccountManager
{
    readonly string saveFilePath;

    readonly Dictionary<string, Account> accountsByName = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);

    readonly Dictionary<int, string> accountNameByConnectionId = new Dictionary<int, string>();

    public AccountManager(string saveFilePath)
    {
        this.saveFilePath = saveFilePath;
        LoadAccounts();
    }

    #region Account Creation / Login / Logout

    public ResultCode CreateAccount(string name, string password)
    {
        ResultCode validation = AccountRules.Validate(name, password);
        if (validation != ResultCode.Success)
            return validation;

        if (accountsByName.ContainsKey(name))
            return ResultCode.NameTaken;

        string salt = CreateSalt();
        Account account = new Account { name = name, salt = salt, passwordHash = HashPassword(password, salt) };
        accountsByName.Add(name, account);
        SaveAccounts();

        Debug.Log("Account created: " + name);
        return ResultCode.Success;
    }

    public ResultCode Login(int connectionId, string name, string password, out string accountName)
    {
        accountName = name;

        ResultCode validation = AccountRules.Validate(name, password);
        if (validation != ResultCode.Success)
            return validation;

        if (accountNameByConnectionId.ContainsKey(connectionId))
            return ResultCode.AlreadyLoggedInOnThisConnection;

        if (!accountsByName.TryGetValue(name, out Account account))
            return ResultCode.AccountNotFound;

        if (HashPassword(password, account.salt) != account.passwordHash)
            return ResultCode.WrongPassword;

        if (accountNameByConnectionId.ContainsValue(account.name))
            return ResultCode.AlreadyLoggedIn;

        // Associate the client's connection ID with the account.
        accountNameByConnectionId.Add(connectionId, account.name);
        accountName = account.name;

        Debug.Log("Connection " + connectionId + " is now associated with account '" + account.name + "'");
        return ResultCode.Success;
    }

    public ResultCode Logout(int connectionId)
    {
        if (!accountNameByConnectionId.TryGetValue(connectionId, out string name))
            return ResultCode.NotLoggedIn;

        accountNameByConnectionId.Remove(connectionId);
        Debug.Log("Connection " + connectionId + " logged out of account '" + name + "'");
        return ResultCode.Success;
    }

    public void OnClientDisconnected(int connectionId)
    {
        if (accountNameByConnectionId.ContainsKey(connectionId))
            Logout(connectionId);
    }

    // Returns null when the connection is not logged in.
    public string GetAccountName(int connectionId)
    {
        accountNameByConnectionId.TryGetValue(connectionId, out string name);
        return name;
    }

    #endregion

    #region Save / Load

    // File format, one account per line:  name,salt,passwordHash
    void SaveAccounts()
    {
        List<string> lines = new List<string>();
        foreach (Account account in accountsByName.Values)
            lines.Add(account.name + NetworkMessageFormat.Separator + account.salt + NetworkMessageFormat.Separator + account.passwordHash);

        File.WriteAllLines(saveFilePath, lines);
    }

    void LoadAccounts()
    {
        accountsByName.Clear();

        if (!File.Exists(saveFilePath))
        {
            Debug.Log("No account file yet, it will be created at: " + saveFilePath);
            return;
        }

        foreach (string line in File.ReadAllLines(saveFilePath))
        {
            string[] values = line.Split(NetworkMessageFormat.Separator);
            if (values.Length != 3 || accountsByName.ContainsKey(values[0]))
            {
                Debug.LogWarning("Skipping bad line in account file: " + line);
                continue;
            }

            accountsByName.Add(values[0], new Account { name = values[0], salt = values[1], passwordHash = values[2] });
        }

        Debug.Log("Loaded " + accountsByName.Count + " account(s) from: " + saveFilePath);
    }

    #endregion

    #region Password Hashing

    // Passwords are never stored as plain text: we store a salted SHA-256 hash.
    static string CreateSalt()
    {
        byte[] saltBytes = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    static string HashPassword(string password, string salt)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
            return Convert.ToBase64String(hash);
        }
    }

    #endregion
}
