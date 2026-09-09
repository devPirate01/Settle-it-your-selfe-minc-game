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
    Transform chest;

    float forwardInput;
    float rotateInput;

    // Smoothed torso lean angles
    float currentPitch;
    float currentRoll;
    float currentYaw;

    // External offsets (e.g. from punch lunges or heavy objects)
    [HideInInspector] public float punchLeanPitch;
    [HideInInspector] public float punchLeanYaw;
    [HideInInspector] public float speedMultiplier = 1f;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        if (anim)
        {
            chest = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (!chest) chest = anim.GetBoneTransform(HumanBodyBones.Spine);
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (playerIndex == 0)
        {
            forwardInput = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
            rotateInput  = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        }
        else
        {
            forwardInput = (kb.upArrowKey.isPressed ? 1 : 0) - (kb.downArrowKey.isPressed ? 1 : 0);
            rotateInput  = (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0);
        }

        // Rotate character then move forward (affected by speedMultiplier)
        transform.Rotate(0, rotateInput * rotateSpeed * Time.deltaTime, 0);
        Vector3 move = transform.forward * forwardInput + Vector3.down;
        cc.Move(move * (speed * speedMultiplier) * Time.deltaTime);

        if (anim) anim.SetFloat("Speed", Mathf.Abs(forwardInput));
    }

    void LateUpdate()
    {
        if (!chest) return;

        // Human: Fall Flat style torso bending:
        // Leans forward when moving forward (+22 deg), backwards on reverse (-15 deg)
        float targetPitch = forwardInput * 22f + punchLeanPitch;
        // Banks into turns (-22 deg)
        float targetRoll = -rotateInput * 22f;
        // Torso lags/swings with rotation (-15 deg)
        float targetYaw = -rotateInput * 15f + punchLeanYaw;

        // Floppy noodle waddle when moving
        if (Mathf.Abs(forwardInput) > 0.1f)
        {
            targetRoll += Mathf.Sin(Time.time * 8f) * 6f;
            targetPitch += Mathf.Cos(Time.time * 8f) * 3f;
        }

        // Smooth physics-like rubber spring
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * 8f);
        currentRoll  = Mathf.Lerp(currentRoll,  targetRoll,  Time.deltaTime * 8f);
        currentYaw   = Mathf.Lerp(currentYaw,   targetYaw,   Time.deltaTime * 8f);

        // Apply additive rotation in player-relative space
        chest.rotation = Quaternion.AngleAxis(currentPitch, transform.right)
                       * Quaternion.AngleAxis(currentRoll, transform.forward)
                       * Quaternion.AngleAxis(currentYaw, transform.up)
                       * chest.rotation;
    }
}
