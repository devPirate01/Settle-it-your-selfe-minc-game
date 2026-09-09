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

    [HideInInspector] public bool isLocked = false;

    float currentDist;

    void Start()
    {
        currentDist = offset.magnitude;
    }

    void LateUpdate()
    {
        if (!target || isLocked) return;

        Vector3 headPos = target.position + Vector3.up * 1.4f;
        Vector3 desiredOffset = target.TransformDirection(offset);
        Vector3 fullPos = target.position + desiredOffset;

        Vector3 toCam = fullPos - headPos;
        float maxDist = toCam.magnitude;
        Vector3 camDir = toCam.normalized;

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
