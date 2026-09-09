using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health))]
public class Grabber : MonoBehaviour
{
    public float grabRadius = 1.6f;
    public float defaultThrowForce = 15f;
    public int playerIndex = 0;

    [Header("Punch Settings")]
    public float punchCooldown = 1.0f; // 1 punch per second limit
    public int punchDamage = 10;        // Utility tool for disarming

    Animator anim;
    Transform heldObject;
    Transform rightHand;
    Transform leftHand;
    SimpleMovement movement;
    CharacterController cc;

    PropStats activePropStats;
    float ikWeight;
    bool isGrabbing;
    bool wasGrabPressed;
    bool wasPunchPressed;
    float punchTimer;
    float punchCooldownTimer;
    const float punchDuration = 0.35f;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim)
        {
            rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        }
        movement = GetComponent<SimpleMovement>();
        cc = GetComponent<CharacterController>();
    }

    // 1-Handed Carry: Raised near right shoulder
    Vector3 CarryPosOneHanded() => transform.position + transform.forward * 0.5f + transform.right * 0.4f + Vector3.up * 1.5f;
    Quaternion CarryRotOneHanded() => Quaternion.LookRotation(transform.forward + transform.right * 0.3f, Vector3.up);

    // 2-Handed Carry: Centered in front of chest
    Vector3 CarryPosTwoHanded() => transform.position + transform.forward * 0.65f + Vector3.up * 1.15f;

    Vector3 PunchPos(float progress)
    {
        float fwd = Mathf.Lerp(0.1f, 1.25f, Mathf.Sin(progress * Mathf.PI));
        float side = Mathf.Lerp(0.75f, -0.25f, progress);
        float up = 1.35f + Mathf.Sin(progress * Mathf.PI) * 0.15f;
        return transform.position + transform.forward * fwd + transform.right * side + Vector3.up * up;
    }

    void TryGrab()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, grabRadius);
        float best = Mathf.Infinity;
        Transform target = null;
        foreach (var h in hits)
        {
            if (!h.CompareTag("Grabbable")) continue;
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < best) { best = d; target = h.transform; }
        }
        if (target == null) return;

        heldObject = target;
        isGrabbing = true;

        // Fetch stats from GrabbableProp if present, otherwise default
        var prop = heldObject.GetComponent<GrabbableProp>();
        activePropStats = prop != null ? prop.GetStats() : new PropStats();

        // Apply movement speed penalty if heavy
        if (movement) movement.speedMultiplier = activePropStats.moveSpeedMultiplier;

        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
        var dmg = heldObject.GetComponent<DamageOnImpact>();
        if (dmg) dmg.isThrown = false;
    }

    void Throw()
    {
        heldObject.SetParent(null);
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            float force = activePropStats != null ? activePropStats.throwForce : defaultThrowForce;
            rb.AddForce((transform.forward + Vector3.up * 0.25f).normalized * force, ForceMode.Impulse);
        }
        var dmg = heldObject.GetComponent<DamageOnImpact>();
        if (dmg) { dmg.isThrown = true; dmg.thrower = transform; }

        heldObject = null;
        activePropStats = null;
        isGrabbing = false;
        if (movement) movement.speedMultiplier = 1f;
    }

    public void ForceRelease()
    {
        if (heldObject == null) return;
        heldObject.SetParent(null);
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            Vector3 popForce = (Vector3.up * 1.5f + Random.insideUnitSphere * 0.5f).normalized * 3.5f;
            rb.AddForce(popForce, ForceMode.Impulse);
        }
        heldObject = null;
        activePropStats = null;
        isGrabbing = false;
        if (movement) movement.speedMultiplier = 1f;
    }

    void Punch()
    {
        punchTimer = punchDuration;

        Vector3 punchCenter = transform.position + transform.forward * 1.0f + Vector3.up * 1.1f;
        var hits = Physics.OverlapSphere(punchCenter, 0.9f);
        foreach (var h in hits)
        {
            if (h.transform.root != transform.root)
            {
                // Disarm: knock whatever object the victim is holding out of their hands
                var victimGrabber = h.transform.root.GetComponent<Grabber>();
                if (victimGrabber != null)
                {
                    victimGrabber.ForceRelease();
                }

                var victimHealth = h.transform.root.GetComponent<Health>();
                if (victimHealth != null)
                {
                    Vector3 knockDir = (h.transform.position - transform.position).normalized;
                    knockDir.y = 0.35f;
                    victimHealth.TakeDamage(punchDamage, knockDir.normalized, 1.2f);
                    break;
                }
            }
        }
    }

    void Update()
    {
        // Stolen object check
        if (heldObject != null && !isGrabbing && (heldObject.parent != rightHand && heldObject.parent != transform))
        {
            heldObject = null;
            activePropStats = null;
            if (movement) movement.speedMultiplier = 1f;
        }

        var kb = Keyboard.current;

        // Grab / Throw
        bool grabPressed = playerIndex == 0 ? kb.eKey.isPressed : kb.rightShiftKey.isPressed;
        if (grabPressed && !wasGrabPressed)
        {
            if (heldObject) Throw();
            else TryGrab();
        }
        wasGrabPressed = grabPressed;

        // Punch cooldown
        if (punchCooldownTimer > 0) punchCooldownTimer -= Time.deltaTime;

        // Punch (1s cooldown)
        bool punchPressed = playerIndex == 0 ? kb.qKey.isPressed : kb.numpad0Key.isPressed;
        if (punchPressed && !wasPunchPressed && punchCooldownTimer <= 0)
        {
            Punch();
            punchCooldownTimer = punchCooldown;
        }
        wasPunchPressed = punchPressed;

        // Punch animation
        if (punchTimer > 0)
        {
            punchTimer -= Time.deltaTime;
            float p = 1f - Mathf.Clamp01(punchTimer / punchDuration);
            float curve = Mathf.Sin(p * Mathf.PI);

            if (movement)
            {
                movement.punchLeanPitch = curve * 22f;
                movement.punchLeanYaw = curve * 28f;
            }
        }
        else if (movement)
        {
            movement.punchLeanPitch = 0f;
            movement.punchLeanYaw = 0f;
        }

        // IK weight blending
        ikWeight = Mathf.MoveTowards(ikWeight, heldObject ? 1f : 0f, Time.deltaTime * 5f);
        if (isGrabbing && ikWeight >= 0.99f)
        {
            isGrabbing = false;
            bool isTwoHanded = activePropStats != null && activePropStats.holdStyle == HoldStyle.TwoHanded;

            if (isTwoHanded)
            {
                // Parent centrally in front of character
                heldObject.SetParent(transform);
                heldObject.localPosition = new Vector3(0, 1.15f, 0.65f);
                heldObject.localRotation = Quaternion.identity;
            }
            else
            {
                // Parent to right hand for one-handed throwing
                heldObject.SetParent(rightHand);
                heldObject.localPosition = Vector3.zero;
                heldObject.localRotation = Quaternion.identity;
            }
        }
    }

    void OnAnimatorIK(int layer)
    {
        if (heldObject != null)
        {
            bool isTwoHanded = activePropStats != null && activePropStats.holdStyle == HoldStyle.TwoHanded;

            if (isTwoHanded)
            {
                // TWO-HANDED CARRY (Chairs, Desks): Both hands clamp onto the object
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
                anim.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);

                Vector3 targetCenter = isGrabbing ? heldObject.position : CarryPosTwoHanded();
                anim.SetIKPosition(AvatarIKGoal.RightHand, targetCenter + transform.right * 0.35f);
                anim.SetIKPosition(AvatarIKGoal.LeftHand, targetCenter - transform.right * 0.35f);

                Quaternion holdRot = Quaternion.LookRotation(transform.forward, Vector3.up);
                anim.SetIKRotation(AvatarIKGoal.RightHand, holdRot);
                anim.SetIKRotation(AvatarIKGoal.LeftHand, holdRot);
            }
            else
            {
                // ONE-HANDED CARRY (Mugs, Keyboards): Right hand raised ready to throw
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
                anim.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
                Vector3 target = isGrabbing ? heldObject.position : CarryPosOneHanded();
                anim.SetIKPosition(AvatarIKGoal.RightHand, target);
                anim.SetIKRotation(AvatarIKGoal.RightHand, CarryRotOneHanded());

                // Release left hand
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            }
        }
        else if (punchTimer > 0)
        {
            float p = 1f - Mathf.Clamp01(punchTimer / punchDuration);
            float w = Mathf.Sin(p * Mathf.PI);

            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, w);
            anim.SetIKPosition(AvatarIKGoal.RightHand, PunchPos(p));
            anim.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(transform.forward - transform.right * 0.4f));

            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }
    }
}
