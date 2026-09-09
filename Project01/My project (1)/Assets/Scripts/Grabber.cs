using UnityEngine;
using UnityEngine.InputSystem;

public class Grabber : MonoBehaviour
{
    public float grabRadius = 1.5f;
    public float throwForce = 8f;
    public int playerIndex = 0; // 0 = E key, 1 = Right Shift

    Animator anim;
    Transform heldObject;
    Transform rightHand;
    float ikWeight;
    bool isGrabbing;
    bool wasPressed;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
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
        isGrabbing = true; // arm will reach toward object's current position
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    void Throw()
    {
        heldObject.SetParent(null);
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            // Bug 02 fix: add upward arc to throw
            rb.AddForce((transform.forward + Vector3.up * 0.6f).normalized * throwForce, ForceMode.Impulse);
        }
        heldObject = null;
        isGrabbing = false;
    }

    void Update()
    {
        // Bug 01 fix: if object was stolen by another player, silently release
        if (heldObject != null && !isGrabbing && heldObject.parent != rightHand)
        {
            heldObject = null;
        }

        // Detect single press (not hold)
        var kb = Keyboard.current;
        bool pressed = playerIndex == 0 ? kb.eKey.isPressed : kb.rightShiftKey.isPressed;
        if (pressed && !wasPressed)
        {
            if (heldObject) Throw();
            else TryGrab();
        }
        wasPressed = pressed;

        ikWeight = Mathf.MoveTowards(ikWeight, heldObject ? 1f : 0f, Time.deltaTime * 5f);

        // Once arm has fully reached, snap object to hand
        if (isGrabbing && ikWeight >= 0.99f)
        {
            isGrabbing = false;
            heldObject.SetParent(rightHand);
            heldObject.localPosition = Vector3.zero;
            heldObject.localRotation = Quaternion.identity;
        }
    }

    void OnAnimatorIK(int layer)
    {
        if (heldObject == null && ikWeight <= 0) return;
        anim.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
        anim.SetIKPosition(AvatarIKGoal.RightHand, heldObject ? heldObject.position : rightHand.position);
        anim.SetIKRotation(AvatarIKGoal.RightHand, heldObject ? heldObject.rotation : rightHand.rotation);
    }
}
