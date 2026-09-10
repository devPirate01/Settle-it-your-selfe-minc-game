using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode
{
    Keyboard,
    DualController
}

public class MatchInputManager : MonoBehaviour
{
    public static MatchInputManager Instance;

    public static InputMode CurrentMode = InputMode.DualController;

    const float DEADZONE = 0.18f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    public static int ConnectedGamepadCount => Gamepad.all.Count;

    public static void SetMode(InputMode mode)
    {
        CurrentMode = mode;
    }

    /// <summary>
    /// Gets directional 2D stick input for modern 3D camera-relative movement.
    /// </summary>
    public static Vector2 GetControllerMoveInput(int playerIndex)
    {
        if (playerIndex < Gamepad.all.Count)
        {
            Gamepad gp = Gamepad.all[playerIndex];
            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                Vector2 move = Vector2.zero;
                if (Mathf.Abs(stick.x) > DEADZONE) move.x = stick.x;
                if (Mathf.Abs(stick.y) > DEADZONE) move.y = stick.y;

                // D-Pad backup
                if (Mathf.Approximately(move.x, 0f))
                {
                    if (gp.dpad.right.isPressed) move.x = 1f;
                    else if (gp.dpad.left.isPressed) move.x = -1f;
                }
                if (Mathf.Approximately(move.y, 0f))
                {
                    if (gp.dpad.up.isPressed) move.y = 1f;
                    else if (gp.dpad.down.isPressed) move.y = -1f;
                }

                return move;
            }
        }
        return Vector2.zero;
    }

    /// <summary>
    /// Gets right stick input for 3D camera look/orbit.
    /// </summary>
    public static Vector2 GetControllerCameraInput(int playerIndex)
    {
        if (playerIndex < Gamepad.all.Count)
        {
            Gamepad gp = Gamepad.all[playerIndex];
            if (gp != null)
            {
                Vector2 stick = gp.rightStick.ReadValue();
                Vector2 look = Vector2.zero;
                if (Mathf.Abs(stick.x) > DEADZONE) look.x = stick.x;
                if (Mathf.Abs(stick.y) > DEADZONE) look.y = stick.y;
                return look;
            }
        }
        return Vector2.zero;
    }

    /// <summary>
    /// Snaps camera behind player (R3 click or Left Bumper LB).
    /// </summary>
    public static bool GetCameraSnapDown(int playerIndex)
    {
        if (playerIndex < Gamepad.all.Count)
        {
            Gamepad gp = Gamepad.all[playerIndex];
            if (gp != null)
            {
                return gp.rightStickButton.wasPressedThisFrame || gp.leftShoulder.wasPressedThisFrame;
            }
        }
        return false;
    }

    /// <summary>
    /// Tank movement for keyboard (or fallback).
    /// </summary>
    public static void GetMovement(int playerIndex, out float forward, out float rotate)
    {
        forward = 0f;
        rotate = 0f;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (playerIndex == 0)
        {
            forward = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            rotate  = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        }
        else
        {
            forward = (kb.upArrowKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed ? 1f : 0f);
            rotate  = (kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed ? 1f : 0f);
        }
    }

    public static bool GetGrabDown(int playerIndex)
    {
        if (CurrentMode == InputMode.DualController)
        {
            if (playerIndex < Gamepad.all.Count)
            {
                Gamepad gp = Gamepad.all[playerIndex];
                if (gp != null)
                {
                    // A button (South) or Right Trigger (RT)
                    return gp.buttonSouth.wasPressedThisFrame || gp.rightTrigger.wasPressedThisFrame;
                }
            }
        }

        var kb = Keyboard.current;
        if (kb == null) return false;

        return playerIndex == 0 ? kb.eKey.wasPressedThisFrame : kb.rightShiftKey.wasPressedThisFrame;
    }

    public static bool GetPunchDown(int playerIndex)
    {
        if (CurrentMode == InputMode.DualController)
        {
            if (playerIndex < Gamepad.all.Count)
            {
                Gamepad gp = Gamepad.all[playerIndex];
                if (gp != null)
                {
                    // X button (West) or Right Bumper (RB)
                    return gp.buttonWest.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame;
                }
            }
        }

        var kb = Keyboard.current;
        if (kb == null) return false;

        return playerIndex == 0 ? kb.qKey.wasPressedThisFrame : kb.numpad0Key.wasPressedThisFrame;
    }

    public static bool GetStartDown()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            return true;

        foreach (var gp in Gamepad.all)
        {
            if (gp != null && (gp.startButton.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame))
                return true;
        }

        return false;
    }

    public static bool GetRestartDown()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
            return true;

        foreach (var gp in Gamepad.all)
        {
            if (gp != null && (gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame))
                return true;
        }

        return false;
    }
}
