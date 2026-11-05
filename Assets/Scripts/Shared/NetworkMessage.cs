using System.Linq;

public class NetworkMessage
{
    public const char Separator = ',';

    readonly string[] values;

    public int Signifier { get; }
    public bool IsValid { get; }

    NetworkMessage(string[] values, int signifier, bool isValid)
    {
        this.values = values;
        Signifier = signifier;
        IsValid = isValid;
    }

    public static string Build(int signifier, params object[] values)
    {
        if (values.Length == 0)
            return signifier.ToString();
        return signifier + Separator.ToString() + string.Join(Separator.ToString(), values.Select(FormatValue));
    }

    public static NetworkMessage Parse(string message)
    {
        string[] values = message.Split(Separator);
        bool isValid = int.TryParse(values[0], out int signifier);
        return new NetworkMessage(values, signifier, isValid);
    }

    public int ValueCount => values.Length - 1;

    public string GetString(int valueIndex)
    {
        int arrayIndex = valueIndex + 1;
        return arrayIndex < values.Length ? values[arrayIndex] : "";
    }

    public bool TryGetInt(int valueIndex, out int value)
    {
        return int.TryParse(GetString(valueIndex), out value);
    }

    public ResultCode GetResultCode(int valueIndex)
    {
        return TryGetInt(valueIndex, out int code) ? (ResultCode)code : ResultCode.MalformedMessage;
    }

    // Free text such as chat can contain separators, so everything from valueIndex on is joined back together.
    public string GetRemainingText(int valueIndex)
    {
        int arrayIndex = valueIndex + 1;
        if (arrayIndex >= values.Length)
            return "";
        return string.Join(Separator.ToString(), values, arrayIndex, values.Length - arrayIndex);
    }

    static string FormatValue(object value)
    {
        return value is System.Enum ? System.Convert.ToInt32(value).ToString() : value.ToString();
    }
}
