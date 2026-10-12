using UnityEngine;

public class TetrisBoard
{
    public int Width { get; }
    public int VisibleHeight { get; }
    public int Height { get; } // visible rows + hidden buffer rows

    // 0 = empty, otherwise (int)TetrominoType + 1 so the view knows which colour to draw
    readonly int[,] cells;

    public TetrisBoard(int width, int visibleHeight, int bufferRows = 4)
    {
        Width = width;
        VisibleHeight = visibleHeight;
        Height = visibleHeight + bufferRows;
        cells = new int[Width, Height];
    }

    public void Clear() => System.Array.Clear(cells, 0, cells.Length);

    public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public bool TryGetBlock(int x, int y, out TetrominoType type)
    {
        int value = cells[x, y];
        type = value == 0 ? default(TetrominoType) : (TetrominoType)(value - 1);
        return value != 0;
    }

    public bool Fits(TetrominoType type, int rotation, Vector2Int position)
    {
        foreach (Vector2Int cell in Tetromino.GetCells(type, rotation))
        {
            int x = position.x + cell.x;
            int y = position.y + cell.y;
            if (!IsInside(x, y) || cells[x, y] != 0) return false;
        }
        return true;
    }

    public void Place(TetrominoType type, int rotation, Vector2Int position)
    {
        foreach (Vector2Int cell in Tetromino.GetCells(type, rotation))
            cells[position.x + cell.x, position.y + cell.y] = (int)type + 1;
    }

    public int ClearFullRows()
    {
        int writeRow = 0;
        for (int readRow = 0; readRow < Height; readRow++)
        {
            if (IsRowFull(readRow)) continue; // skipped rows get overwritten by the rows above
            if (writeRow != readRow)
                for (int x = 0; x < Width; x++) cells[x, writeRow] = cells[x, readRow];
            writeRow++;
        }

        int cleared = Height - writeRow;
        for (int y = writeRow; y < Height; y++)
            for (int x = 0; x < Width; x++) cells[x, y] = 0;
        return cleared;
    }

    public int StackHeight()
    {
        for (int y = Height - 1; y >= 0; y--)
            for (int x = 0; x < Width; x++)
                if (cells[x, y] != 0) return y + 1;
        return 0;
    }

    bool IsRowFull(int y)
    {
        for (int x = 0; x < Width; x++)
            if (cells[x, y] == 0) return false;
        return true;
    }
}
