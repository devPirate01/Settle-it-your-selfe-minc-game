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

    [Header("Selection Outline")]
    public bool enableOutline = true;
    [Range(0.01f, 0.15f)]
    public float outlineWidth = 0.055f;
    public Color p1OutlineColor = new Color(0.1f, 0.85f, 1f, 1f);
    public Color p2OutlineColor = new Color(1f, 0.78f, 0.15f, 1f);

    Transform currentCandidate;
    static Material outlineMat;
    static MaterialPropertyBlock outlinePropertyBlock;

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

    Camera playerCam;
    float smoothedAimY;

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

        // Find camera following this player
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
    }

    // 1-Handed Carry: Raised near right shoulder, arms shift and pitch with aim angle
    Vector3 CarryPosOneHanded()
    {
        float upOffset = smoothedAimY * 0.55f;
        float fwdOffset = -smoothedAimY * 0.12f;
        return transform.position 
            + transform.forward * (0.5f + fwdOffset) 
            + transform.right * 0.4f 
            + Vector3.up * (1.5f + upOffset);
    }

    Quaternion CarryRotOneHanded()
    {
        float pitch = -smoothedAimY * 40f;
        return Quaternion.Euler(pitch, transform.eulerAngles.y + 18f, 0f);
    }

    // 2-Handed Carry: Lifts chair up high when aiming up, lowers towards waist when aiming down
    Vector3 CarryPosTwoHanded()
    {
        float upOffset = smoothedAimY * 0.45f;
        float fwdOffset = smoothedAimY * 0.15f;
        return transform.position 
            + transform.forward * (0.65f + fwdOffset) 
            + Vector3.up * (1.15f + upOffset);
    }

    Quaternion CarryRotTwoHanded()
    {
        float pitch = -smoothedAimY * 35f;
        return Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
    }

    Vector3 PunchPos(float progress)
    {
        float fwd = Mathf.Lerp(0.1f, 1.25f, Mathf.Sin(progress * Mathf.PI));
        float side = Mathf.Lerp(0.75f, -0.25f, progress);
        float up = 1.35f + Mathf.Sin(progress * Mathf.PI) * 0.15f;
        return transform.position + transform.forward * fwd + transform.right * side + Vector3.up * up;
    }

    public Transform GetBestGrabTarget()
    {
        if (heldObject != null) return null;

        Vector3 aimHeading = transform.forward;
        if (MatchInputManager.CurrentMode == InputMode.DualController && playerCam != null)
        {
            Vector3 camFwd = Vector3.ProjectOnPlane(playerCam.transform.forward, Vector3.up).normalized;
            if (camFwd.sqrMagnitude > 0.01f) aimHeading = camFwd;
        }

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Collider[] hits = Physics.OverlapSphere(origin, grabRadius);
        float bestScore = Mathf.Infinity;
        Transform target = null;

        foreach (var h in hits)
        {
            Transform propRoot = null;
            if (h.CompareTag("Grabbable")) propRoot = h.transform;
            else if (h.transform.parent != null && h.transform.parent.CompareTag("Grabbable")) propRoot = h.transform.parent;
            else
            {
                var gp = h.GetComponentInParent<GrabbableProp>();
                if (gp != null) propRoot = gp.transform;
            }

            if (propRoot == null) continue;

            // Don't target an object that is currently held by someone
            var rb = propRoot.GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic && propRoot.parent != null) continue;

            // Measure from player torso to prop collider center
            Vector3 toProp = h.bounds.center - origin;
            float dist = toProp.magnitude;
            if (dist < 0.05f) continue;

            Vector3 toPropHoriz = Vector3.ProjectOnPlane(toProp, Vector3.up).normalized;
            float dot = Vector3.Dot(aimHeading, toPropHoriz);

            // Ignore objects behind the player
            if (dot < 0f) continue;

            // Directional Aim Scoring: objects where the player/camera points get a major bonus
            float score = dist - (dot * 1.4f);

            if (score < bestScore)
            {
                bestScore = score;
                target = propRoot;
            }
        }

        return target;
    }

    void TryGrab()
    {
        Transform target = currentCandidate != null ? currentCandidate : GetBestGrabTarget();
        if (target == null) return;

        heldObject = target;
        currentCandidate = null;
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

            bool isTwoHanded = activePropStats != null && activePropStats.holdStyle == HoldStyle.TwoHanded;
            float baseForce = activePropStats != null ? activePropStats.throwForce : defaultThrowForce;

            // 1. Two-handed objects: reduced range for a heavy, short-distance heave
            if (isTwoHanded)
            {
                baseForce = Mathf.Min(baseForce, 6.5f);
            }

            Vector3 throwDir;
            float finalForce = baseForce;

            // 2. Controller Mode: Trajectory based on camera view angle
            if (MatchInputManager.CurrentMode == InputMode.DualController && playerCam != null)
            {
                Vector3 camFwd = playerCam.transform.forward;
                Vector3 camHoriz = Vector3.ProjectOnPlane(camFwd, Vector3.up).normalized;
                if (camHoriz.sqrMagnitude < 0.01f) camHoriz = transform.forward;

                // Turn character to face camera aim direction
                transform.rotation = Quaternion.LookRotation(camHoriz, Vector3.up);

                // Vertical look angle: positive = looking UP ("looking high"), negative = looking DOWN
                float verticalAim = camFwd.y;

                // Base loft so even horizontal aim has an arc
                float baseLoft = isTwoHanded ? 0.35f : 0.22f;

                // Loft increases when looking high, decreases when looking down
                float dynamicLoft = Mathf.Clamp(baseLoft + verticalAim * 0.75f, -0.15f, 0.85f);
                throwDir = (camHoriz + Vector3.up * dynamicLoft).normalized;

                // If looking high, trajectory increased (boost launch force for high arc)
                // If looking down, force is reduced for close-range slam
                float aimMultiplier = 1f + verticalAim * 0.4f;
                finalForce *= Mathf.Clamp(aimMultiplier, 0.7f, 1.4f);
            }
            else
            {
                // Keyboard Mode: Classic forward + upward arc along player facing
                float loft = isTwoHanded ? 0.35f : 0.25f;
                throwDir = (transform.forward + Vector3.up * loft).normalized;
            }

            rb.AddForce(throwDir * finalForce, ForceMode.Impulse);

            // Add realistic prop tumbling torque
            Vector3 spinAxis = Vector3.Cross(Vector3.up, throwDir).normalized;
            float torqueSpeed = isTwoHanded ? 7f : 15f;
            rb.AddTorque(spinAxis * torqueSpeed, ForceMode.Impulse);
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

        var dmg = heldObject.GetComponent<DamageOnImpact>();
        if (dmg) dmg.isThrown = false;

        heldObject.SetParent(null);
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            // Pop the dropped object with a physical tumble
            Vector3 popForce = (Vector3.up * 1.8f + Random.insideUnitSphere * 0.6f).normalized * 4.0f;
            rb.AddForce(popForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);
        }
        heldObject = null;
        activePropStats = null;
        isGrabbing = false;
        if (movement) movement.speedMultiplier = 1f;
    }

    void Punch()
    {
        if (heldObject != null) return; // Cannot punch while carrying an object
        punchTimer = punchDuration;

        Vector3 punchCenter = transform.position + transform.forward * 1.0f + Vector3.up * 1.1f;
        var hits = Physics.OverlapSphere(punchCenter, 0.9f);
        foreach (var h in hits)
        {
            if (h.transform.root != transform.root)
            {
                // Disarm: knock whatever object the victim is holding out of their hands
                var victimGrabber = h.GetComponentInParent<Grabber>();
                if (victimGrabber == null) victimGrabber = h.transform.root.GetComponentInChildren<Grabber>();
                if (victimGrabber != null)
                {
                    victimGrabber.ForceRelease();
                }

                var victimHealth = h.GetComponentInParent<Health>();
                if (victimHealth == null) victimHealth = h.transform.root.GetComponentInChildren<Health>();
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
        // Track camera viewing pitch for aim stance
        float targetAimY = (MatchInputManager.CurrentMode == InputMode.DualController && playerCam != null)
            ? playerCam.transform.forward.y
            : 0f;
        smoothedAimY = Mathf.Lerp(smoothedAimY, targetAimY, Time.deltaTime * 10f);

        // Stolen object check
        if (heldObject != null && !isGrabbing && (heldObject.parent != rightHand && heldObject.parent != transform))
        {
            heldObject = null;
            activePropStats = null;
            if (movement) movement.speedMultiplier = 1f;
        }

        // Update real-time aim candidate for selection outline
        currentCandidate = GetBestGrabTarget();

        // Grab ONLY: LT / A / B on controller; E / RightShift on keyboard
        if (MatchInputManager.GetGrabDown(playerIndex))
        {
            if (heldObject == null)
            {
                TryGrab();
            }
        }

        // Punch cooldown
        if (punchCooldownTimer > 0) punchCooldownTimer -= Time.deltaTime;

        // Throw / Punch: RT / X / Y on controller; Q / Num0 on keyboard
        // Throws if holding an object; punches if empty-handed
        if (MatchInputManager.GetThrowOrPunchDown(playerIndex))
        {
            if (heldObject != null)
            {
                Throw();
            }
            else if (punchCooldownTimer <= 0)
            {
                Punch();
                punchCooldownTimer = punchCooldown;
            }
        }

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
            // Head and spine aim towards camera look direction
            if (playerCam != null && MatchInputManager.CurrentMode == InputMode.DualController)
            {
                Vector3 lookTarget = transform.position + Vector3.up * 1.5f + playerCam.transform.forward * 8f;
                anim.SetLookAtWeight(ikWeight * 0.65f, 0.25f, 0.75f, 0.5f);
                anim.SetLookAtPosition(lookTarget);
            }

            bool isTwoHanded = activePropStats != null && activePropStats.holdStyle == HoldStyle.TwoHanded;

            if (isTwoHanded)
            {
                // TWO-HANDED CARRY (Chairs, Desks): Both hands clamp onto the object and shift with aim angle
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
                anim.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);

                Vector3 targetCenter = isGrabbing ? heldObject.position : CarryPosTwoHanded();
                anim.SetIKPosition(AvatarIKGoal.RightHand, targetCenter + transform.right * 0.35f);
                anim.SetIKPosition(AvatarIKGoal.LeftHand, targetCenter - transform.right * 0.35f);

                Quaternion holdRot = CarryRotTwoHanded();
                anim.SetIKRotation(AvatarIKGoal.RightHand, holdRot);
                anim.SetIKRotation(AvatarIKGoal.LeftHand, holdRot);

                // Update held object position to match lifted/lowered hands
                if (!isGrabbing && heldObject.parent == transform)
                {
                    float upOffset = smoothedAimY * 0.45f;
                    float fwdOffset = smoothedAimY * 0.15f;
                    heldObject.localPosition = new Vector3(0f, 1.15f + upOffset, 0.65f + fwdOffset);
                    heldObject.localRotation = Quaternion.Euler(-smoothedAimY * 35f, 0f, 0f);
                }
            }
            else
            {
                // ONE-HANDED CARRY (Mugs, Keyboards): Right hand raised ready to throw, pitches with aim
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
        else
        {
            anim.SetLookAtWeight(0);

            if (punchTimer > 0)
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

    void LateUpdate()
    {
        if (!enableOutline || currentCandidate == null || heldObject != null) return;

        if (outlineMat == null)
        {
            Shader s = Shader.Find("Custom/SelectionOutline");
            if (s) outlineMat = new Material(s);
        }

        if (outlineMat != null)
        {
            if (outlinePropertyBlock == null) outlinePropertyBlock = new MaterialPropertyBlock();

            // Dynamic breathing pulse
            float pulse = 0.88f + Mathf.PingPong(Time.time * 2.5f, 0.25f);
            Color baseCol = (playerIndex == 0) ? p1OutlineColor : p2OutlineColor;
            Color outlineColor = baseCol * pulse;

            outlinePropertyBlock.SetColor("_OutlineColor", outlineColor);
            outlinePropertyBlock.SetFloat("_OutlineWidth", outlineWidth);

            Camera camToRender = playerCam != null ? playerCam : Camera.main;

            var meshFilters = currentCandidate.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf && mf.sharedMesh)
                {
                    for (int sub = 0; sub < mf.sharedMesh.subMeshCount; sub++)
                    {
                        Graphics.DrawMesh(
                            mf.sharedMesh,
                            mf.transform.localToWorldMatrix,
                            outlineMat,
                            0,
                            camToRender,
                            sub,
                            outlinePropertyBlock
                        );
                    }
                }
            }
        }
    }
}
