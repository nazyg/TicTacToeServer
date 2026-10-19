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

public class TicTacToeBoard
{
    public const int CellCount = 9;

    static readonly int[][] winningLines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 },
    };

    readonly TicTacToeMark[] cells = new TicTacToeMark[CellCount];

    public TicTacToeMark CurrentTurn { get; private set; }
    public TicTacToeResult Result { get; private set; }
    public int[] WinningLine { get; private set; }

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

    public bool CanPlaceMark(int index)
    {
        return Result == TicTacToeResult.InProgress
            && index >= 0 && index < CellCount
            && cells[index] == TicTacToeMark.None;
    }

    public bool TryPlaceMark(int index)
    {
        if (!CanPlaceMark(index))
            return false;

        cells[index] = CurrentTurn;
        UpdateResult();

        if (Result == TicTacToeResult.InProgress)
            CurrentTurn = CurrentTurn == TicTacToeMark.X ? TicTacToeMark.O : TicTacToeMark.X;

        return true;
    }

    public string Serialize()
    {
        char[] state = new char[CellCount + 1];
        for (int i = 0; i < CellCount; i++)
            state[i] = (char)('0' + (int)cells[i]);
        state[CellCount] = (char)('0' + (int)CurrentTurn);
        return new string(state);
    }

    public bool Deserialize(string state)
    {
        if (state == null || state.Length != CellCount + 1)
            return false;

        for (int i = 0; i <= CellCount; i++)
        {
            if (state[i] < '0' || state[i] > '2')
                return false;
        }

        for (int i = 0; i < CellCount; i++)
            cells[i] = (TicTacToeMark)(state[i] - '0');
        CurrentTurn = (TicTacToeMark)(state[CellCount] - '0');

        Result = TicTacToeResult.InProgress;
        WinningLine = null;
        UpdateResult();
        return true;
    }

    void UpdateResult()
    {
        foreach (int[] line in winningLines)
        {
            TicTacToeMark first = cells[line[0]];
            if (first != TicTacToeMark.None && first == cells[line[1]] && first == cells[line[2]])
            {
                Result = first == TicTacToeMark.X ? TicTacToeResult.XWins : TicTacToeResult.OWins;
                WinningLine = line;
                return;
            }
        }

        foreach (TicTacToeMark cell in cells)
        {
            if (cell == TicTacToeMark.None)
                return;
        }

        Result = TicTacToeResult.Draw;
    }
}
