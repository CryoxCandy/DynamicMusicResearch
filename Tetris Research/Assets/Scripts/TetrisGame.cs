using System;
using System.Collections.Generic;
using UnityEngine;

public enum TetrisState { Ready, Playing, Paused, GameOver }

[DisallowMultipleComponent]
public class TetrisGame : MonoBehaviour
{
    [Header("Board")]
    [SerializeField, Min(4)] int boardWidth = 10;
    [SerializeField, Min(4)] int boardHeight = 20;

    [Header("Speed")]
    [SerializeField, Range(1, 20)] int startLevel = 1;
    [SerializeField, Min(1)] int linesPerLevel = 10;
    [SerializeField] bool speedUpWithLevel = true;
    [SerializeField, Min(0.01f)] float softDropInterval = 0.05f;
    [SerializeField, Min(0f)] float lockDelay = 0.5f;
    [SerializeField, Min(0)] int maxLockResets = 15;

    [Header("Controls")]
    [SerializeField, Min(0f)] float autoRepeatDelay = 0.17f;
    [SerializeField, Min(0f)] float autoRepeatInterval = 0.05f;

    [Header("Preview")]
    [SerializeField, Range(0, 5)] int previewCount = 3;


    public TetrisBoard Board { get; private set; }
    public TetrisState State { get; private set; } = TetrisState.Ready;
    public int Score { get; private set; }
    public int Level { get; private set; }
    public int Lines { get; private set; }

    public int StackHeight { get; private set; }

    public float Danger => Board == null ? 0f : Mathf.Clamp01((float)StackHeight / Board.VisibleHeight);

    public bool HasActivePiece { get; private set; }
    public TetrominoType ActiveType { get; private set; }
    public int ActiveRotation { get; private set; }
    public Vector2Int ActivePosition { get; private set; }
    public int PreviewCount => previewCount;

    public event Action GameStarted;                     // a new game began (StateChanged also fires on resume)
    public event Action<TetrisState> StateChanged;       // Ready / Playing / Paused / GameOver
    public event Action<TetrominoType> PieceSpawned;
    public event Action PieceMoved;                      // one successful step left or right
    public event Action PieceRotated;
    public event Action<int> PieceHardDropped;           // rows fallen
    public event Action PieceLocked;
    public event Action<int> LinesCleared;               // 1-4 (4 = a Tetris)
    public event Action<int> LevelChanged;               // the new level
    public event Action<int> StackHeightChanged;         // the new height in rows (Danger is the 0..1 version)

    static readonly int[] LineClearPoints = { 0, 100, 300, 500, 800 };

    readonly List<TetrominoType> queue = new List<TetrominoType>();
    float fallTimer;
    float lockTimer;     // time spent resting on the stack at the current depth
    int lockResets;      // moves/rotations used since touching down at the current depth
    bool touchedDown;    // has the piece rested on the stack since reaching its current depth?
    int lowestY;
    int shiftDirection; // -1 left, 0 none, +1 right
    float shiftTimer;
    bool wasSoftDropping;

    void Awake()
    {
        Board = new TetrisBoard(boardWidth, boardHeight);
        Level = startLevel;
    }

    void Update()
    {
        switch (State)
        {
            case TetrisState.Ready:
            case TetrisState.GameOver:
                if (TetrisInput.Pressed(TetrisAction.Confirm) || TetrisInput.Pressed(TetrisAction.Restart))
                    StartGame();
                break;

            case TetrisState.Paused:
                if (TetrisInput.Pressed(TetrisAction.Pause) || TetrisInput.Pressed(TetrisAction.Confirm))
                    SetState(TetrisState.Playing);
                else if (TetrisInput.Pressed(TetrisAction.Restart))
                    StartGame();
                break;

            case TetrisState.Playing:
                if (TetrisInput.Pressed(TetrisAction.Pause))
                    SetState(TetrisState.Paused);
                else
                    Tick(Mathf.Min(Time.deltaTime, 0.1f)); // clamp so a frame hitch can't skip rows or lock instantly
                break;
        }
    }

    public void StartGame()
    {
        Board.Clear();
        Score = 0;
        Lines = 0;
        Level = startLevel;
        queue.Clear();
        shiftDirection = 0;
        wasSoftDropping = false;
        UpdateStackHeight();

        GameStarted?.Invoke();
        SetState(TetrisState.Playing);
        LevelChanged?.Invoke(Level);
        SpawnNextPiece();
    }

    public void SetPaused(bool paused)
    {
        if (paused && State == TetrisState.Playing) SetState(TetrisState.Paused);
        else if (!paused && State == TetrisState.Paused) SetState(TetrisState.Playing);
    }

    public Vector2Int GhostPosition()
    {
        Vector2Int position = ActivePosition;
        while (Board.Fits(ActiveType, ActiveRotation, position + Vector2Int.down))
            position += Vector2Int.down;
        return position;
    }

    public bool TryPeekNext(int index, out TetrominoType type)
    {
        bool exists = index >= 0 && index < queue.Count;
        type = exists ? queue[index] : default(TetrominoType);
        return exists;
    }

    void Tick(float deltaTime)
    {
        HandleSideways(deltaTime);
        if (TetrisInput.Pressed(TetrisAction.RotateClockwise)) TryRotate(true);
        if (TetrisInput.Pressed(TetrisAction.RotateCounterClockwise)) TryRotate(false);
        if (TetrisInput.Pressed(TetrisAction.HardDrop))
        {
            HardDrop();
            return;
        }
        ApplyGravity(deltaTime);
        UpdateLockDelay(deltaTime);
    }

    void HandleSideways(float deltaTime)
    {
        bool leftHeld = TetrisInput.Held(TetrisAction.MoveLeft);
        bool rightHeld = TetrisInput.Held(TetrisAction.MoveRight);

        if (TetrisInput.Pressed(TetrisAction.MoveLeft)) BeginSideways(-1);
        else if (TetrisInput.Pressed(TetrisAction.MoveRight)) BeginSideways(1);
        else if (shiftDirection == -1 && !leftHeld) { shiftDirection = 0; if (rightHeld) BeginSideways(1); }
        else if (shiftDirection == 1 && !rightHeld) { shiftDirection = 0; if (leftHeld) BeginSideways(-1); }
        else if (shiftDirection != 0)
        {
            shiftTimer -= deltaTime;
            while (shiftTimer <= 0f)
            {
                if (!TryMove(new Vector2Int(shiftDirection, 0)))
                {
                    shiftTimer = 0f; // blocked: keep trying each frame in case the way opens up
                    break;
                }
                shiftTimer += autoRepeatInterval;
            }
        }
    }

    void BeginSideways(int direction)
    {
        shiftDirection = direction;
        shiftTimer = autoRepeatDelay;
        TryMove(new Vector2Int(direction, 0));
    }

    void ApplyGravity(float deltaTime)
    {
        bool softDrop = TetrisInput.Held(TetrisAction.SoftDrop);
        float interval = GravityInterval(speedUpWithLevel ? Level : startLevel);
        if (softDrop) interval = Mathf.Min(interval, softDropInterval);

        fallTimer += deltaTime;
        if (softDrop && !wasSoftDropping) fallTimer = interval; // first soft-drop row happens straight away
        wasSoftDropping = softDrop;

        while (fallTimer >= interval)
        {
            fallTimer -= interval;
            if (!TryMove(Vector2Int.down))
            {
                fallTimer = 0f; // landed: lock delay takes over
                break;
            }
            if (softDrop) Score += 1;
        }
    }
    void UpdateLockDelay(float deltaTime)
    {
        if (!IsGrounded()) return;
        touchedDown = true;
        lockTimer += deltaTime;
        if (lockTimer >= lockDelay) LockPiece();
    }

    static float GravityInterval(int level)
    {
        int n = Mathf.Clamp(level, 1, 20) - 1;
        return Mathf.Pow(0.8f - n * 0.007f, n);
    }

    bool IsGrounded() => !Board.Fits(ActiveType, ActiveRotation, ActivePosition + Vector2Int.down);

    bool TryMove(Vector2Int offset)
    {
        Vector2Int target = ActivePosition + offset;
        if (!Board.Fits(ActiveType, ActiveRotation, target)) return false;

        ActivePosition = target;
        NoteDepth(target.y);
        if (offset.x != 0)
        {
            RestartLockDelay();
            PieceMoved?.Invoke();
        }
        return true;
    }

    bool TryRotate(bool clockwise)
    {
        if (ActiveType == TetrominoType.O) return false; // looks the same in every rotation

        int from = ActiveRotation;
        int to = (from + (clockwise ? 1 : 3)) % 4;
        foreach (Vector2Int kick in Tetromino.GetKicks(ActiveType, from, clockwise))
        {
            Vector2Int target = ActivePosition + kick;
            if (!Board.Fits(ActiveType, to, target)) continue;

            ActiveRotation = to;
            ActivePosition = target;
            NoteDepth(target.y);
            RestartLockDelay();
            PieceRotated?.Invoke();
            return true;
        }
        return false;
    }

    void HardDrop()
    {
        int rows = 0;
        while (TryMove(Vector2Int.down)) rows++;
        Score += rows * 2;
        PieceHardDropped?.Invoke(rows);
        LockPiece();
    }

    void RestartLockDelay()
    {
        if (!touchedDown || lockResets >= maxLockResets) return;
        lockTimer = 0f;
        lockResets++;
    }

    void NoteDepth(int y)
    {
        if (y >= lowestY) return;
        lowestY = y;
        lockTimer = 0f;
        lockResets = 0;
        touchedDown = false;
    }


    void LockPiece()
    {
        Board.Place(ActiveType, ActiveRotation, ActivePosition);
        HasActivePiece = false;
        PieceLocked?.Invoke();

        int cleared = Board.ClearFullRows();
        if (cleared > 0)
        {
            Lines += cleared;
            Score += LineClearPoints[Mathf.Min(cleared, 4)] * Level;
            LinesCleared?.Invoke(cleared);

            int newLevel = startLevel + Lines / linesPerLevel;
            if (newLevel != Level)
            {
                Level = newLevel;
                LevelChanged?.Invoke(Level);
            }
        }

        UpdateStackHeight();

        if (StackHeight > Board.VisibleHeight)
        {
            SetState(TetrisState.GameOver);
            return;
        }
        SpawnNextPiece();
    }

    void SpawnNextPiece()
    {
        RefillQueue();
        ActiveType = queue[0];
        queue.RemoveAt(0);
        ActiveRotation = 0;
        ActivePosition = SpawnPosition(ActiveType);
        HasActivePiece = true;
        fallTimer = 0f;
        lockTimer = 0f;
        lockResets = 0;
        touchedDown = false;
        lowestY = ActivePosition.y;

        // No room for the new piece = game over
        if (!Board.Fits(ActiveType, ActiveRotation, ActivePosition))
        {
            SetState(TetrisState.GameOver);
            return;
        }
        PieceSpawned?.Invoke(ActiveType);
    }

    // 7-bag randomiser: deal a shuffled set of all seven pieces, then another, and so on
    // Every seven pieces contain one of each, so there are no long droughts
    void RefillQueue()
    {
        while (queue.Count <= previewCount)
        {
            int start = queue.Count;
            for (int i = 0; i < Tetromino.TypeCount; i++) queue.Add((TetrominoType)i);
            for (int i = queue.Count - 1; i > start; i--)
            {
                int j = UnityEngine.Random.Range(start, i + 1);
                TetrominoType swap = queue[i];
                queue[i] = queue[j];
                queue[j] = swap;
            }
        }
    }

    // Centred horizontally, with the piece's top row on the top visible row
    Vector2Int SpawnPosition(TetrominoType type)
    {
        int top = 0;
        foreach (Vector2Int cell in Tetromino.GetCells(type, 0)) top = Mathf.Max(top, cell.y);
        int x = (Board.Width - Tetromino.GetBoxSize(type)) / 2;
        return new Vector2Int(x, Board.VisibleHeight - 1 - top);
    }

    void UpdateStackHeight()
    {
        int height = Board.StackHeight();
        if (height == StackHeight) return;
        StackHeight = height;
        StackHeightChanged?.Invoke(height);
    }

    void SetState(TetrisState next)
    {
        if (State == next) return;
        if (next != TetrisState.Playing) shiftDirection = 0;
        State = next;
        StateChanged?.Invoke(next);
    }
}
