using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(DamageOnImpact))]
public class GrabbableProp : MonoBehaviour
{
    [Tooltip("Choose the object class (Small: 1-handed fast, Medium: 1-handed, Heavy: 2-handed massive damage)")]
    public PropClass propClass = PropClass.Small;

    [Header("Optional Custom Override")]
    [Tooltip("Check this if this specific object has unique stats (e.g., a special golden mug)")]
    public bool customOverride = false;
    public PropStats customStats = new PropStats();

    void Awake()
    {
        // Enforce tag at runtime in case it wasn't saved in editor
        if (!gameObject.CompareTag("Grabbable"))
        {
            gameObject.tag = "Grabbable";
        }
    }

    void Reset()
    {
        // Automatically ensure the tag is Grabbable when attached in editor
        if (!gameObject.CompareTag("Grabbable"))
        {
            gameObject.tag = "Grabbable";
        }
    }

    public PropStats GetStats()
    {
        if (customOverride) return customStats;
        return PropDatabase.Instance.GetStats(propClass);
    }
}
