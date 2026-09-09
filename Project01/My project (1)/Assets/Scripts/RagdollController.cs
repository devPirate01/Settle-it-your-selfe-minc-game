using System.Collections;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [Header("Tuning")]
    [Tooltip("How long the player stays down when punched (lower = snappy recovery)")]
    public float knockdownDuration = 0.35f; 

    Rigidbody[] ragdollRigidbodies;
    Collider[] ragdollColliders;
    Animator anim;
    CharacterController cc;
    Health health;
    CameraFollow playerCam;
    Coroutine knockdownRoutine;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();
        health = GetComponent<Health>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        // Ensure characters start animated and upright
        SetRagdollState(false);
    }

    void Start()
    {
        // Automatically find the camera following this character so we can lock it during ragdoll
        var allCams = FindObjectsByType<CameraFollow>(FindObjectsSortMode.None);
        foreach (var c in allCams)
        {
            if (c.target == transform)
            {
                playerCam = c;
                break;
            }
        }
    }

    // Calculates exact height offset so CharacterController capsule bottom rests flush on ground
    float GetCapsuleBottomOffset()
    {
        if (cc == null) return 0f;
        return (cc.height * 0.5f) - cc.center.y;
    }

    // Finds the real floor height, strictly ignoring all of this character's own limbs and colliders
    float FindGroundY(Vector3 checkPos)
    {
        RaycastHit[] hits = Physics.RaycastAll(checkPos + Vector3.up * 2f, Vector3.down, 10f);
        foreach (var h in hits)
        {
            // Ignore own body parts and triggers
            if (h.transform.root != transform.root && !h.collider.isTrigger)
            {
                return h.point.y;
            }
        }
        return transform.position.y;
    }

    public void SetRagdollState(bool active)
    {
        if (anim) anim.enabled = !active;
        if (cc) cc.enabled = !active;

        if (ragdollRigidbodies != null)
        {
            foreach (var rb in ragdollRigidbodies)
            {
                if (rb.gameObject == gameObject) continue; // skip root
                rb.isKinematic = !active;
                rb.useGravity = active;
            }
        }

        if (ragdollColliders != null)
        {
            foreach (var col in ragdollColliders)
            {
                if (col.gameObject == gameObject) continue; // skip root CharacterController
                col.enabled = active;
            }
        }
    }

    // Temporary ragdoll when punched or hit by heavy object
    public void Knockdown(Vector3 impactForce, float duration = -1f)
    {
        if (health != null && health.IsDead) return;

        if (ragdollRigidbodies == null || ragdollRigidbodies.Length <= 1)
        {
            Debug.LogWarning($"{name} does not have Ragdoll bone Rigidbodies yet! Run GameObject > 3D Object > Ragdoll... on this character.", this);
            return;
        }

        if (duration < 0) duration = knockdownDuration;

        if (knockdownRoutine != null) StopCoroutine(knockdownRoutine);
        knockdownRoutine = StartCoroutine(KnockdownCo(impactForce, duration));
    }

    IEnumerator KnockdownCo(Vector3 impactForce, float duration)
    {
        var move = GetComponent<SimpleMovement>();
        var grab = GetComponent<Grabber>();
        if (move) move.enabled = false;
        if (grab) grab.enabled = false;

        // 1. Lock camera in place so viewport doesn't jerk or drop
        if (playerCam) playerCam.isLocked = true;

        // 2. Ensure root is flush on floor before enabling ragdoll
        float groundY = FindGroundY(transform.position);
        Vector3 startPos = transform.position;
        startPos.y = groundY + GetCapsuleBottomOffset();
        transform.position = startPos;

        // 3. Activate REAL ragdoll
        SetRagdollState(true);

        // 4. Apply punch force to chest or hips
        if (anim)
        {
            Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (chest && chest.TryGetComponent<Rigidbody>(out var chestRb))
            {
                chestRb.AddForce(impactForce, ForceMode.Impulse);
            }
            else if (anim.GetBoneTransform(HumanBodyBones.Hips) is Transform hips && hips.TryGetComponent<Rigidbody>(out var hipsRb))
            {
                hipsRb.AddForce(impactForce, ForceMode.Impulse);
            }
        }

        // Wait while player stumbles/flops on ground
        yield return new WaitForSeconds(duration);

        if (health != null && health.IsDead) yield break;

        // 5. Disable bone colliders first so they don't block anything
        SetRagdollState(false);

        // 6. Reposition root cleanly to where hips landed, flush on the real floor
        if (anim)
        {
            Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips)
            {
                Vector3 newPos = hips.position;
                newPos.y = FindGroundY(newPos) + GetCapsuleBottomOffset();
                transform.position = newPos;
            }
        }

        // 7. Stand back up, unlock camera & re-enable control
        if (move) move.enabled = true;
        if (grab) grab.enabled = true;
        if (playerCam) playerCam.isLocked = false;
        knockdownRoutine = null;
    }

    // Permanent death ragdoll
    public void TriggerDeathRagdoll(Vector3 impactForce)
    {
        if (knockdownRoutine != null) StopCoroutine(knockdownRoutine);

        SetRagdollState(true);

        if (anim)
        {
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips && hips.TryGetComponent<Rigidbody>(out var hipsRb))
            {
                hipsRb.AddForce(impactForce, ForceMode.Impulse);
            }
        }
    }
}
