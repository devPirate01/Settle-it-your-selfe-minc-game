using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 1.8f, -3f);
    public float smoothSpeed = 10f;

    [HideInInspector] public bool isLocked = false; // Set to true during ragdoll to lock camera in place

    void LateUpdate()
    {
        if (!target || isLocked) return;

        Vector3 desired = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smoothSpeed);
        transform.LookAt(target.position + Vector3.up * 1.4f);
    }
}
