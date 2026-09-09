using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleMovement : MonoBehaviour
{
    public float speed = 5f;
    public float rotateSpeed = 120f;
    public int playerIndex = 0; // 0 = WASD, 1 = Arrow Keys

    CharacterController cc;
    Animator anim;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float forward, rotate;

        if (playerIndex == 0)
        {
            forward = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
            rotate  = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        }
        else
        {
            forward = (kb.upArrowKey.isPressed ? 1 : 0) - (kb.downArrowKey.isPressed ? 1 : 0);
            rotate  = (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0);
        }

        // Rotate then move forward
        transform.Rotate(0, rotate * rotateSpeed * Time.deltaTime, 0);
        Vector3 move = transform.forward * forward + Vector3.down;
        cc.Move(move * speed * Time.deltaTime);

        if (anim) anim.SetFloat("Speed", Mathf.Abs(forward));
    }
}
