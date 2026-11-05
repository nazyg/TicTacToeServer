using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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

    public ResultCode CreateAccount(string name, string password)
    {
        ResultCode validation = AccountRules.Validate(name, password);
        if (validation != ResultCode.Success)
            return validation;
        if (accountsByName.ContainsKey(name))
            return ResultCode.NameTaken;

        accountsByName.Add(name, Account.CreateWithPassword(name, password));
        SaveAccounts();

        Debug.Log("Account created: " + name);
        return ResultCode.Success;
    }

    public ResultCode Login(int connectionId, string name, string password, out string accountName)
    {
        accountName = name;

        ResultCode result = CheckCanLogin(connectionId, name, password, out Account account);
        if (result != ResultCode.Success)
            return result;

        accountNameByConnectionId.Add(connectionId, account.Name);
        accountName = account.Name;

        Debug.Log("Connection " + connectionId + " is now associated with account '" + account.Name + "'");
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

    // Returns null when the connection is not logged in.
    public string GetAccountName(int connectionId)
    {
        accountNameByConnectionId.TryGetValue(connectionId, out string name);
        return name;
    }

    ResultCode CheckCanLogin(int connectionId, string name, string password, out Account account)
    {
        account = null;

        ResultCode validation = AccountRules.Validate(name, password);
        if (validation != ResultCode.Success)
            return validation;
        if (accountNameByConnectionId.ContainsKey(connectionId))
            return ResultCode.AlreadyLoggedInOnThisConnection;
        if (!accountsByName.TryGetValue(name, out account))
            return ResultCode.AccountNotFound;
        if (!account.PasswordMatches(password))
            return ResultCode.WrongPassword;
        if (accountNameByConnectionId.ContainsValue(account.Name))
            return ResultCode.AlreadyLoggedIn;

        return ResultCode.Success;
    }

    void SaveAccounts()
    {
        File.WriteAllLines(saveFilePath, accountsByName.Values.Select(account => account.Serialize()));
    }

    void LoadAccounts()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("No account file yet, it will be created at: " + saveFilePath);
            return;
        }

        foreach (string line in File.ReadAllLines(saveFilePath))
        {
            if (Account.TryDeserialize(line, out Account account) && !accountsByName.ContainsKey(account.Name))
                accountsByName.Add(account.Name, account);
            else
                Debug.LogWarning("Skipping bad line in account file: " + line);
        }

        Debug.Log("Loaded " + accountsByName.Count + " account(s) from: " + saveFilePath);
    }
}
