using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleMovement : MonoBehaviour
{
    [Header("General Settings")]
    public float speed = 5f;
    public float rotateSpeed = 120f;
    public int playerIndex = 0; // 0 = P1, 1 = P2

    [Header("Controller Directional Movement")]
    [Tooltip("Turn speed in degrees per second when rotating towards analog stick direction")]
    public float controllerTurnSpeed = 720f;

    CharacterController cc;
    Animator anim;
    Transform chest;
    Camera playerCam;

    float forwardInput;
    float rotateInput;
    float previousYaw;

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

        // Find the camera assigned to follow this player
        var allCams = FindObjectsByType<CameraFollow>(FindObjectsSortMode.None);
        foreach (var c in allCams)
        {
            if (c.target == transform)
            {
                playerCam = c.GetComponent<Camera>();
                break;
            }
        }
        if (!playerCam) playerCam = Camera.main;

        previousYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (MatchInputManager.CurrentMode == InputMode.DualController)
        {
            // CONTROLLER MODE: Modern 360-degree camera-relative movement
            Vector2 stick = MatchInputManager.GetControllerMoveInput(playerIndex);
            float magnitude = Mathf.Clamp01(stick.magnitude);

            if (magnitude > 0.05f)
            {
                // Calculate movement direction relative to player's camera horizontal heading
                Transform camT = playerCam != null ? playerCam.transform : Camera.main.transform;
                Vector3 camFwd = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(camT.right, Vector3.up).normalized;
                Vector3 moveDir = (camFwd * stick.y + camRight * stick.x).normalized;

                // Snappily rotate character to face the direction of the analog stick
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float currentAngle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, controllerTurnSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);

                // Move forward in facing direction with analog speed
                float moveSpeed = speed * speedMultiplier * magnitude;
                Vector3 move = moveDir * moveSpeed + Vector3.down;
                cc.Move(move * Time.deltaTime);

                forwardInput = magnitude;

                // Calculate angular velocity delta for procedural torso banking
                float angularDelta = Mathf.DeltaAngle(previousYaw, currentAngle);
                rotateInput = Mathf.Clamp(angularDelta / (Time.deltaTime * 360f), -1f, 1f);
                previousYaw = currentAngle;
            }
            else
            {
                forwardInput = 0f;
                rotateInput = 0f;
                cc.Move(Vector3.down * Time.deltaTime);
                previousYaw = transform.eulerAngles.y;
            }

            if (anim) anim.SetFloat("Speed", forwardInput);
        }
        else
        {
            // KEYBOARD MODE: Tank movement for 2 players sharing 1 keyboard
            MatchInputManager.GetMovement(playerIndex, out forwardInput, out rotateInput);

            transform.Rotate(0, rotateInput * rotateSpeed * Time.deltaTime, 0);
            Vector3 move = transform.forward * forwardInput + Vector3.down;
            cc.Move(move * (speed * speedMultiplier) * Time.deltaTime);

            if (anim) anim.SetFloat("Speed", Mathf.Abs(forwardInput));
        }
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
