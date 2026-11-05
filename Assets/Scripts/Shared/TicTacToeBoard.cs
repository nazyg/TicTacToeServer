public enum TicTacToeMark
{
    None = 0,
    X = 1,
    O = 2,
}

public enum TicTacToeResult
{
    InProgress = 0,
    XWins = 1,
    OWins = 2,
    Draw = 3,
}

public static class TicTacToeMarkExtensions
{
    public static TicTacToeMark Opposite(this TicTacToeMark mark)
    {
        if (mark == TicTacToeMark.X)
            return TicTacToeMark.O;
        if (mark == TicTacToeMark.O)
            return TicTacToeMark.X;
        return TicTacToeMark.None;
    }
}

public class TicTacToeBoard
{
    public const int CellCount = 9;
    public const int RowLength = 3;

    static readonly int[][] WinningLines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 },
    };

    readonly TicTacToeMark[] cells = new TicTacToeMark[CellCount];

    public TicTacToeMark CurrentTurn { get; private set; }
    public TicTacToeResult Result { get; private set; }
    public int[] WinningLine { get; private set; }

    public bool IsInProgress => Result == TicTacToeResult.InProgress;

    public TicTacToeMark Winner
    {
        get
        {
            if (Result == TicTacToeResult.XWins)
                return TicTacToeMark.X;
            if (Result == TicTacToeResult.OWins)
                return TicTacToeMark.O;
            return TicTacToeMark.None;
        }
    }

    public TicTacToeBoard()
    {
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < CellCount; i++)
            cells[i] = TicTacToeMark.None;

        CurrentTurn = TicTacToeMark.X;
        Result = TicTacToeResult.InProgress;
        WinningLine = null;
    }

    public TicTacToeMark GetCell(int index)
    {
        return cells[index];
    }

    public bool IsInWinningLine(int index)
    {
        return WinningLine != null && System.Array.IndexOf(WinningLine, index) >= 0;
    }

    public bool CanPlaceMark(int index)
    {
        return IsInProgress && index >= 0 && index < CellCount && cells[index] == TicTacToeMark.None;
    }

    public bool TryPlaceMark(int index)
    {
        if (!CanPlaceMark(index))
            return false;

        cells[index] = CurrentTurn;
        UpdateResult();

        if (IsInProgress)
            CurrentTurn = CurrentTurn.Opposite();

        return true;
    }

    // Format: 9 digits for the cells (0 = empty, 1 = X, 2 = O) followed by 1 digit for whose turn it is.
    public string Serialize()
    {
        char[] state = new char[CellCount + 1];
        for (int i = 0; i < CellCount; i++)
            state[i] = MarkToDigit(cells[i]);
        state[CellCount] = MarkToDigit(CurrentTurn);
        return new string(state);
    }

    public bool Deserialize(string state)
    {
        if (!IsValidSerializedState(state))
            return false;

        for (int i = 0; i < CellCount; i++)
            cells[i] = DigitToMark(state[i]);
        CurrentTurn = DigitToMark(state[CellCount]);

        Result = TicTacToeResult.InProgress;
        WinningLine = null;
        UpdateResult();
        return true;
    }

    static bool IsValidSerializedState(string state)
    {
        if (state == null || state.Length != CellCount + 1)
            return false;

        foreach (char digit in state)
        {
            if (digit < '0' || digit > '2')
                return false;
        }
        return true;
    }

    static char MarkToDigit(TicTacToeMark mark)
    {
        return (char)('0' + (int)mark);
    }

    static TicTacToeMark DigitToMark(char digit)
    {
        return (TicTacToeMark)(digit - '0');
    }

    void UpdateResult()
    {
        foreach (int[] line in WinningLines)
        {
            TicTacToeMark first = cells[line[0]];
            if (first != TicTacToeMark.None && first == cells[line[1]] && first == cells[line[2]])
            {
                Result = first == TicTacToeMark.X ? TicTacToeResult.XWins : TicTacToeResult.OWins;
                WinningLine = line;
                return;
            }
        }

        if (System.Array.IndexOf(cells, TicTacToeMark.None) < 0)
            Result = TicTacToeResult.Draw;
    }
}
