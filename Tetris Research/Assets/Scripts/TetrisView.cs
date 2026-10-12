using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(TetrisGame))]
public class TetrisView : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] bool frameCamera = true;
    [SerializeField] Color backgroundColor = new Color(0.06f, 0.06f, 0.08f);

    [Header("Board")]
    [SerializeField] Color emptyCellColor = new Color(0.13f, 0.13f, 0.16f);
    [SerializeField] Color borderColor = new Color(0.45f, 0.45f, 0.5f);
    [SerializeField, Range(0f, 1f)] float ghostStrength = 0.3f;

    [Header("Panel")]
    [SerializeField] bool showControls = true;
    [SerializeField] bool showMusicInputs = true;

    // Layout in world units (1 unit = 1 cell), measured from the board's bottom left corner
    const float Gap = 1f;              // space between the board and each side panel
    const float SidePanelWidth = 6f;   // score panel
    const float PreviewWidth = 5f;     // next pieces
    const float PreviewHeader = 1.5f;  // room for "NEXT" above the first piece
    const float PreviewSlot = 3f;      // height given to each upcoming piece

    static readonly string[] ControlKeys = { "Left / Right", "Down", "Space", "Up / X", "Z / Ctrl", "P / Esc"};
    static readonly string[] ControlActions = { "Move", "Soft drop", "Hard drop", "Rotate right", "Rotate left", "Pause"};

    TetrisGame game;
    Texture2D blockTexture, solidTexture;
    Sprite blockSprite, solidSprite;
    Material spriteMaterial;
    SpriteRenderer[,] cells;
    SpriteRenderer[] previewBlocks;
    GUIStyle headingStyle, valueStyle, smallStyle, overlayStyle;

    void Start()
    {
        game = GetComponent<TetrisGame>();
        CreateSpritesAndMaterial();
        BuildBoard();
        BuildPreview();
    }

    void LateUpdate()
    {
        if (cells == null) return;
        if (frameCamera) FrameCamera();
        DrawBoard();
        DrawPreview();
    }

    void OnDestroy()
    {
        if (blockSprite != null) Destroy(blockSprite);
        if (solidSprite != null) Destroy(solidSprite);
        if (blockTexture != null) Destroy(blockTexture);
        if (solidTexture != null) Destroy(solidTexture);
        if (spriteMaterial != null) Destroy(spriteMaterial);
    }

    void CreateSpritesAndMaterial()
    {
        const int size = 16;
        blockTexture = MakeTexture(size, (x, y) =>
        {
            bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
            bool highlight = x == 1 || y == size - 2;
            return border ? (byte)120 : highlight ? (byte)255 : (byte)220;
        });
        blockSprite = Sprite.Create(blockTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);

        solidTexture = MakeTexture(4, (x, y) => 255);
        solidSprite = Sprite.Create(solidTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect);

        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlit != null) spriteMaterial = new Material(unlit);
        }
    }

    static Texture2D MakeTexture(int size, System.Func<int, int, byte> shade)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                byte value = shade(x, y);
                pixels[y * size + x] = new Color32(value, value, value, 255);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    void BuildBoard()
    {
        TetrisBoard board = game.Board;
        Transform root = CreateChild("Board");

        SpriteRenderer border = CreateRenderer("Border", solidSprite, borderColor, -1, root);
        border.transform.localPosition = new Vector3(board.Width / 2f, board.VisibleHeight / 2f, 0f);
        border.transform.localScale = new Vector3(board.Width + 0.4f, board.VisibleHeight + 0.4f, 1f);

        cells = new SpriteRenderer[board.Width, board.VisibleHeight];
        for (int y = 0; y < board.VisibleHeight; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                SpriteRenderer cell = CreateRenderer("Cell " + x + "," + y, blockSprite, emptyCellColor, 0, root);
                cell.transform.localPosition = new Vector3(x + 0.5f, y + 0.5f, 0f);
                cells[x, y] = cell;
            }
        }
    }

    void BuildPreview()
    {
        int count = game.PreviewCount;
        previewBlocks = new SpriteRenderer[count * 4];
        if (count == 0) return;

        TetrisBoard board = game.Board;
        Transform root = CreateChild("Preview");

        float height = PreviewHeader + count * PreviewSlot;
        var centre = new Vector3(board.Width + Gap + PreviewWidth / 2f, board.VisibleHeight - height / 2f, 0f);

        SpriteRenderer border = CreateRenderer("Border", solidSprite, borderColor, -2, root);
        border.transform.localPosition = centre;
        border.transform.localScale = new Vector3(PreviewWidth + 0.4f, height + 0.4f, 1f);

        SpriteRenderer panel = CreateRenderer("Panel", solidSprite, emptyCellColor, -1, root);
        panel.transform.localPosition = centre;
        panel.transform.localScale = new Vector3(PreviewWidth, height, 1f);

        for (int i = 0; i < previewBlocks.Length; i++)
        {
            previewBlocks[i] = CreateRenderer("Next Block", blockSprite, Color.white, 0, root);
            previewBlocks[i].enabled = false;
        }
    }

    Transform CreateChild(string childName)
    {
        Transform child = new GameObject(childName).transform;
        child.SetParent(transform, false);
        return child;
    }

    SpriteRenderer CreateRenderer(string objectName, Sprite sprite, Color color, int sortingOrder, Transform parent)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        if (spriteMaterial != null) spriteRenderer.sharedMaterial = spriteMaterial;
        return spriteRenderer;
    }

    void DrawBoard()
    {
        TetrisBoard board = game.Board;
        for (int y = 0; y < board.VisibleHeight; y++)
            for (int x = 0; x < board.Width; x++)
                cells[x, y].color = board.TryGetBlock(x, y, out TetrominoType type) ? Tetromino.GetColor(type) : emptyCellColor;

        if (!game.HasActivePiece) return;

        Color pieceColor = Tetromino.GetColor(game.ActiveType);
        Vector2Int[] shape = Tetromino.GetCells(game.ActiveType, game.ActiveRotation);

        if (game.State == TetrisState.Playing || game.State == TetrisState.Paused)
        {
            Vector2Int ghost = game.GhostPosition();
            Color ghostColor = Color.Lerp(emptyCellColor, pieceColor, ghostStrength);
            foreach (Vector2Int cell in shape) Paint(ghost + cell, ghostColor);
        }
        foreach (Vector2Int cell in shape) Paint(game.ActivePosition + cell, pieceColor);
    }

    void Paint(Vector2Int cell, Color color)
    {
        if (cell.x < 0 || cell.y < 0 || cell.x >= cells.GetLength(0) || cell.y >= cells.GetLength(1)) return;
        cells[cell.x, cell.y].color = color;
    }

    void DrawPreview()
    {
        TetrisBoard board = game.Board;
        float centreX = board.Width + Gap + PreviewWidth / 2f;
        int slots = Mathf.Min(game.PreviewCount, previewBlocks.Length / 4);

        for (int slot = 0; slot < slots; slot++)
        {
            if (game.State == TetrisState.Ready || !game.TryPeekNext(slot, out TetrominoType type))
            {
                for (int i = 0; i < 4; i++) previewBlocks[slot * 4 + i].enabled = false;
                continue;
            }

            Vector2Int[] shape = Tetromino.GetCells(type, 0);
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (Vector2Int cell in shape)
            {
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }
            float shapeCentreX = (minX + maxX + 1) / 2f;
            float shapeCentreY = (minY + maxY + 1) / 2f;
            float slotCentreY = board.VisibleHeight - PreviewHeader - (slot + 0.5f) * PreviewSlot;
            float scale = slot == 0 ? 1f : 0.75f;
            Color color = Tetromino.GetColor(type);

            for (int i = 0; i < 4; i++)
            {
                SpriteRenderer block = previewBlocks[slot * 4 + i];
                block.enabled = true;
                block.color = color;
                block.transform.localPosition = new Vector3(
                    centreX + (shape[i].x + 0.5f - shapeCentreX) * scale,
                    slotCentreY + (shape[i].y + 0.5f - shapeCentreY) * scale,
                    0f);
                block.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    void FrameCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        TetrisBoard board = game.Board;
        float left = -Gap - SidePanelWidth;
        float right = board.Width + Gap + PreviewWidth;
        float top = board.VisibleHeight;
        const float margin = 1f;

        Vector3 origin = transform.position;
        cam.orthographic = true;
        cam.transform.SetPositionAndRotation(
            new Vector3(origin.x + (left + right) / 2f, origin.y + top / 2f, origin.z - 10f),
            Quaternion.identity);
        cam.orthographicSize = Mathf.Max(top / 2f + margin, ((right - left) / 2f + margin) / cam.aspect);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
    }

    void OnGUI()
    {
        if (game == null || cells == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        float unit = cam.pixelHeight / (cam.orthographicSize * 2f);
        BuildStyles();
        headingStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(unit * 0.5f));
        valueStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(unit * 0.9f));
        smallStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(unit * 0.42f));
        overlayStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(unit * 0.6f));

        TetrisBoard board = game.Board;
        float left = -Gap - SidePanelWidth;
        float top = board.VisibleHeight;

        DrawStat(cam, "SCORE", game.Score.ToString("N0"), left, top);
        DrawStat(cam, "LEVEL", game.Level.ToString(), left, top - 3f);
        DrawStat(cam, "LINES", game.Lines.ToString(), left, top - 6f);

        if (showMusicInputs)
        {
            GUI.Label(WorldRect(cam, left, 9.8f, SidePanelWidth, 1f), "MUSIC INPUTS", headingStyle);
            GUI.Label(WorldRect(cam, left, 9.05f, SidePanelWidth, 0.75f), "State: " + game.State, smallStyle);
            GUI.Label(WorldRect(cam, left, 8.3f, SidePanelWidth, 0.75f), "Stack height: " + game.StackHeight + " / " + board.VisibleHeight, smallStyle);
            GUI.Label(WorldRect(cam, left, 7.55f, SidePanelWidth, 0.75f), "Danger: " + game.Danger.ToString("0.00"), smallStyle);
        }

        if (showControls)
        {
            const float rowHeight = 0.75f;
            float headingY = 0.25f + ControlKeys.Length * rowHeight;
            GUI.Label(WorldRect(cam, left, headingY, SidePanelWidth, 1f), "CONTROLS", headingStyle);
            for (int i = 0; i < ControlKeys.Length; i++)
            {
                float y = headingY - (i + 1) * rowHeight;
                GUI.Label(WorldRect(cam, left, y, 2.9f, rowHeight), ControlKeys[i], smallStyle);
                GUI.Label(WorldRect(cam, left + 2.9f, y, SidePanelWidth - 2.9f, rowHeight), ControlActions[i], smallStyle);
            }
        }

        if (game.PreviewCount > 0)
            GUI.Label(WorldRect(cam, board.Width + Gap + 0.4f, top - PreviewHeader, PreviewWidth, PreviewHeader), "NEXT", headingStyle);

        string message = null;
        switch (game.State)
        {
            case TetrisState.Ready:
                message = "<b>TETRIS</b>\n\nPress Enter to start";
                break;
            case TetrisState.Paused:
                message = "<b>PAUSED</b>\n\nP or Esc to resume\nR to restart";
                break;
            case TetrisState.GameOver:
                message = "<b>GAME OVER</b>\n\nScore " + game.Score.ToString("N0") + "\n\nEnter to play again";
                break;
        }
        if (message != null)
        {
            Rect box = WorldRect(cam, 0.5f, board.VisibleHeight / 2f - 3f, board.Width - 1f, 6f);
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(box, message, overlayStyle);
        }
    }

    void DrawStat(Camera cam, string label, string value, float x, float top)
    {
        GUI.Label(WorldRect(cam, x, top - 1f, SidePanelWidth, 1f), label, headingStyle);
        GUI.Label(WorldRect(cam, x, top - 2.4f, SidePanelWidth, 1.4f), value, valueStyle);
    }
    Rect WorldRect(Camera cam, float x, float y, float width, float height)
    {
        Vector3 origin = transform.position;
        Vector3 min = cam.WorldToScreenPoint(origin + new Vector3(x, y, 0f));
        Vector3 max = cam.WorldToScreenPoint(origin + new Vector3(x + width, y + height, 0f));
        return Rect.MinMaxRect(min.x, Screen.height - max.y, max.x, Screen.height - min.y);
    }

    void BuildStyles()
    {
        if (headingStyle != null) return;

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.LowerLeft,
            wordWrap = false,
            clipping = TextClipping.Overflow,
        };
        headingStyle.normal.textColor = new Color(0.6f, 0.6f, 0.68f);

        valueStyle = new GUIStyle(headingStyle) { alignment = TextAnchor.UpperLeft };
        valueStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            clipping = TextClipping.Overflow,
        };
        smallStyle.normal.textColor = new Color(0.78f, 0.78f, 0.82f);

        overlayStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            wordWrap = true,
        };
        overlayStyle.normal.textColor = Color.white;
    }
}
