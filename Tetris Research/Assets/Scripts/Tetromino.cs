using UnityEngine;

public enum TetrominoType { I, O, T, S, Z, J, L }

public static class Tetromino
{
    public const int TypeCount = 7;

    // Bounding box size per piece, in TetrominoType order
    static readonly int[] BoxSizes = {4, 2, 3, 3, 3, 3, 3};

    // Rotation state 0,how they spawn, in TetrominoType order
    static readonly Vector2Int[][] SpawnCells =
    {
        Points(0, 2, 1, 2, 2, 2, 3, 2), // I
        Points(0, 0, 1, 0, 0, 1, 1, 1), // O
        Points(1, 2, 0, 1, 1, 1, 2, 1), // T
        Points(1, 2, 2, 2, 0, 1, 1, 1), // S
        Points(0, 2, 1, 2, 1, 1, 2, 1), // Z
        Points(0, 2, 0, 1, 1, 1, 2, 1), // J
        Points(2, 2, 0, 1, 1, 1, 2, 1), // L
    };

    static readonly Color[] Colors =
    {
        new Color(0.00f, 0.85f, 0.95f), // I
        new Color(0.97f, 0.85f, 0.10f), // O
        new Color(0.66f, 0.30f, 0.95f), // T
        new Color(0.30f, 0.85f, 0.30f), // S
        new Color(0.95f, 0.25f, 0.25f), // Z
        new Color(0.20f, 0.42f, 1.00f), // J 
        new Color(1.00f, 0.58f, 0.10f), // L
    };

    
    static readonly Vector2Int[][][] RotatedCells = BuildRotations();

    // Wall kicks. Offsets tried in order when a rotation is blocked, where the first fit will be usede
    // Values are the standard SRS tables, with +y = up.
    static readonly Vector2Int[][] KicksJLSTZ =
    {
        Points(0, 0, -1, 0, -1, 1, 0, -2, -1, -2),  // 0 -> R
        Points(0, 0, 1, 0, 1, -1, 0, 2, 1, 2),      // R -> 0
        Points(0, 0, 1, 0, 1, -1, 0, 2, 1, 2),      // R -> 2
        Points(0, 0, -1, 0, -1, 1, 0, -2, -1, -2),  // 2 -> R
        Points(0, 0, 1, 0, 1, 1, 0, -2, 1, -2),     // 2 -> L
        Points(0, 0, -1, 0, -1, -1, 0, 2, -1, 2),   // L -> 2
        Points(0, 0, -1, 0, -1, -1, 0, 2, -1, 2),   // L -> 0
        Points(0, 0, 1, 0, 1, 1, 0, -2, 1, -2),     // 0 -> L
    };

    static readonly Vector2Int[][] KicksI =
    {
        Points(0, 0, -2, 0, 1, 0, -2, -1, 1, 2), // 0 -> R
        Points(0, 0, 2, 0, -1, 0, 2, 1, -1, -2), // R -> 0
        Points(0, 0, -1, 0, 2, 0, -1, 2, 2, -1), // R -> 2
        Points(0, 0, 1, 0, -2, 0, 1, -2, -2, 1), // 2 -> R
        Points(0, 0, 2, 0, -1, 0, 2, 1, -1, -2), // 2 -> L
        Points(0, 0, -2, 0, 1, 0, -2, -1, 1, 2), // L -> 2
        Points(0, 0, 1, 0, -2, 0, 1, -2, -2, 1), // L -> 0
        Points(0, 0, -1, 0, 2, 0, -1, 2, 2, -1), // 0 -> L
    };

    public static Vector2Int[] GetCells(TetrominoType type, int rotation) => RotatedCells[(int)type][rotation & 3];

    public static Color GetColor(TetrominoType type) => Colors[(int)type];

    public static int GetBoxSize(TetrominoType type) => BoxSizes[(int)type];

    public static Vector2Int[] GetKicks(TetrominoType type, int fromRotation, bool clockwise)
    {
        int row = clockwise ? fromRotation * 2 : (fromRotation * 2 + 7) % 8;
        return type == TetrominoType.I ? KicksI[row] : KicksJLSTZ[row];
    }

    static Vector2Int[][][] BuildRotations()
    {
        var all = new Vector2Int[TypeCount][][];
        for (int t = 0; t < TypeCount; t++)
        {
            int n = BoxSizes[t];
            all[t] = new Vector2Int[4][];
            all[t][0] = SpawnCells[t];
            for (int r = 1; r < 4; r++)
            {
                Vector2Int[] previous = all[t][r - 1];
                var next = new Vector2Int[previous.Length];
                for (int i = 0; i < previous.Length; i++)
                    next[i] = new Vector2Int(previous[i].y, n - 1 - previous[i].x); // 90 degrees clockwise in an n x n box
                all[t][r] = next;
            }
        }
        return all;
    }

    static Vector2Int[] Points(params int[] xy)
    {
        var points = new Vector2Int[xy.Length / 2];
        for (int i = 0; i < points.Length; i++) points[i] = new Vector2Int(xy[i * 2], xy[i * 2 + 1]);
        return points;
    }
}
