using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 1.8f, -3f);
    public float smoothSpeed = 10f;

    [Header("Wall Collision Avoidance")]
    [Tooltip("Buffer around camera to prevent clipping through wall geometry")]
    public float collisionRadius = 0.25f;
    [Tooltip("Layers to collide with (Walls, Environment, Props)")]
    public LayerMask collisionLayers = ~0;

    [Header("Controller Camera Orbit Tuning")]
    [Tooltip("Speed in deg/sec when rotating camera with Right Stick")]
    public float camYawSpeed = 140f;
    [Tooltip("Speed in deg/sec when tilting camera pitch with Right Stick")]
    public float camPitchSpeed = 90f;
    public float minPitch = -10f;
    public float maxPitch = 60f;
    [Tooltip("Speed in deg/sec camera auto-aligns behind player while running")]
    public float autoFollowSpeed = 60f;

    [HideInInspector] public bool isLocked = false;

    float currentDist;
    float currentYaw;
    float currentPitch = 25f;
    int playerIndex = -1;

    void Start()
    {
        currentDist = offset.magnitude;

        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
            var move = target.GetComponent<SimpleMovement>();
            if (move != null) playerIndex = move.playerIndex;
        }
    }

    void LateUpdate()
    {
        if (!target || isLocked) return;

        Vector3 headPos = target.position + Vector3.up * 1.4f;
        Vector3 camDir;
        float maxDist;

        if (MatchInputManager.CurrentMode == InputMode.DualController && playerIndex >= 0)
        {
            // CONTROLLER MODE: Right Stick orbits camera freely
            Vector2 rightStick = MatchInputManager.GetControllerCameraInput(playerIndex);

            if (rightStick.sqrMagnitude > 0.04f)
            {
                currentYaw += rightStick.x * camYawSpeed * Time.deltaTime;
                currentPitch -= rightStick.y * camPitchSpeed * Time.deltaTime;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }
            else
            {
                // Auto-center camera behind player when moving if right stick is untouched
                Vector2 leftStick = MatchInputManager.GetControllerMoveInput(playerIndex);
                if (leftStick.sqrMagnitude > 0.2f)
                {
                    currentYaw = Mathf.MoveTowardsAngle(currentYaw, target.eulerAngles.y, autoFollowSpeed * Time.deltaTime);
                }
            }

            // Snap camera behind player immediately on R3 click or Left Bumper (LB)
            if (MatchInputManager.GetCameraSnapDown(playerIndex))
            {
                currentYaw = target.eulerAngles.y;
                currentPitch = 25f;
            }

            Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);
            camDir = rot * -Vector3.forward;
            maxDist = offset.magnitude;
        }
        else
        {
            // KEYBOARD MODE: Classic fixed rear-follow
            Vector3 desiredOffset = target.TransformDirection(offset);
            Vector3 toCam = (target.position + desiredOffset) - headPos;
            maxDist = toCam.magnitude;
            camDir = toCam.normalized;
            currentYaw = target.eulerAngles.y;
        }

        float targetDist = maxDist;

        // Check for walls between target head and camera
        RaycastHit[] hits = Physics.SphereCastAll(headPos, collisionRadius, camDir, maxDist, collisionLayers, QueryTriggerInteraction.Ignore);
        float closestHitDist = maxDist;

        foreach (var hit in hits)
        {
            // Ignore the character itself
            if (hit.transform.root != target.root)
            {
                if (hit.distance < closestHitDist)
                {
                    closestHitDist = hit.distance;
                }
            }
        }

        if (closestHitDist < maxDist)
        {
            // Pull camera forward in front of the wall
            targetDist = Mathf.Max(closestHitDist - 0.1f, 0.4f);
        }

        // Fast zoom-in when hitting a wall (instant protection), smooth zoom-out when clearing
        float zoomSpeed = targetDist < currentDist ? 30f : smoothSpeed;
        currentDist = Mathf.Lerp(currentDist, targetDist, Time.deltaTime * zoomSpeed);

        Vector3 finalPos = headPos + camDir * currentDist;
        transform.position = finalPos;
        transform.LookAt(headPos);
    }
}
