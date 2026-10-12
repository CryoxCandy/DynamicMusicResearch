using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using KeyId = UnityEngine.InputSystem.Key;
#else
using KeyId = UnityEngine.KeyCode;
#endif

public enum TetrisAction
{
    MoveLeft,
    MoveRight,
    SoftDrop,
    HardDrop,
    RotateClockwise,
    RotateCounterClockwise,
    Pause,
    Confirm, // start, resume, play again
    Restart,
}

public static class TetrisInput
{
#if ENABLE_INPUT_SYSTEM
    const KeyId Enter = KeyId.Enter, NumpadEnter = KeyId.NumpadEnter, LeftCtrl = KeyId.LeftCtrl, RightCtrl = KeyId.RightCtrl;
#else
    const KeyId Enter = KeyId.Return, NumpadEnter = KeyId.KeypadEnter, LeftCtrl = KeyId.LeftControl, RightCtrl = KeyId.RightControl;
#endif

    static readonly KeyId[][] Bindings =
    {
        new[] { KeyId.LeftArrow, KeyId.A },            // MoveLeft
        new[] { KeyId.RightArrow, KeyId.D },           // MoveRight
        new[] { KeyId.DownArrow, KeyId.S },            // SoftDrop
        new[] { KeyId.Space },                         // HardDrop
        new[] { KeyId.UpArrow, KeyId.X, KeyId.W },     // RotateClockwise
        new[] { KeyId.Z, LeftCtrl, RightCtrl },        // RotateCounterClockwise
        new[] { KeyId.Escape, KeyId.P },               // Pause
        new[] { Enter, NumpadEnter, KeyId.Space },     // Confirm
        new[] { KeyId.R },                             // Restart
    };

    public static bool Pressed(TetrisAction action)
    {
        foreach (KeyId key in Bindings[(int)action])
            if (WasPressed(key)) return true;
        return false;
    }
    public static bool Held(TetrisAction action)
    {
        foreach (KeyId key in Bindings[(int)action])
            if (IsHeld(key)) return true;
        return false;
    }

#if ENABLE_INPUT_SYSTEM
    static bool WasPressed(KeyId key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].wasPressedThisFrame;
    }

    static bool IsHeld(KeyId key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].isPressed;
    }
#else
    static bool WasPressed(KeyId key) => Input.GetKeyDown(key);

    static bool IsHeld(KeyId key) => Input.GetKey(key);
#endif
}
