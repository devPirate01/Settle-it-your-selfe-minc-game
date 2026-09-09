using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleMovement : MonoBehaviour
{
    public float speed = 5f;
    CharacterController cc;
    Animator anim;
    Vector2 input;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    // Called automatically by PlayerInput component (Send Messages mode)
    void OnMove(UnityEngine.InputSystem.InputValue v) => input = v.Get<Vector2>();

    void Update()
    {
        Vector3 move = new Vector3(input.x, -1f, input.y);
        cc.Move(move * speed * Time.deltaTime);
        move.y = 0;
        if (move.sqrMagnitude > 0) transform.forward = move;
        if (anim) anim.SetFloat("Speed", input.magnitude);
    }
}
