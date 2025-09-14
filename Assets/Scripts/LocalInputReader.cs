using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputReader : MonoBehaviour
{
    public Vector2 Move;
    public bool JumpPressedThisFrame;
    public bool SprintHeld;

    void OnMove(InputValue v) {
        Move = v.Get<Vector2>();
    }

    void OnJump(InputValue v) {
        if (!v.isPressed)
            return;
        JumpPressedThisFrame = true;
        Debug.Log("[InputReader] OnJump PRESSED");
    }

    void OnSprint(InputValue v) {
        SprintHeld = v.isPressed;
        Debug.Log($"[InputReader] OnSprint → {(SprintHeld ? "HELD" : "RELEASED")}");
    }

    void LateUpdate() {
        // reset edge del salto al final del frame
        if (JumpPressedThisFrame)
            Debug.Log("[InputReader] LateUpdate → reset JumpPressedThisFrame");
        JumpPressedThisFrame = false;
    }
}