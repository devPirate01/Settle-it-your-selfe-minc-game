using UnityEngine;

public class DamageOnImpact : MonoBehaviour
{
    [HideInInspector] public bool isThrown;
    [HideInInspector] public Transform thrower; // set by Grabber, ignored on collision

    void OnCollisionEnter(Collision col)
    {
        if (!isThrown) return;
        if (!col.gameObject.CompareTag("Player")) return;
        if (col.transform.root == thrower) return; // ignore self-hit

        var health = col.gameObject.GetComponentInParent<Health>();
        if (health)
        {
            // Read damage and stats from GrabbableProp if present
            int damage = 35;
            float hitForce = 1.2f;

            if (TryGetComponent<GrabbableProp>(out var prop))
            {
                var stats = prop.GetStats();
                damage = stats.damage;
                hitForce = stats.holdStyle == HoldStyle.TwoHanded ? 2.0f : Mathf.Clamp(stats.throwForce * 0.12f, 0.8f, 2.5f);
            }
            else
            {
                damage = Mathf.Clamp(Mathf.RoundToInt(col.relativeVelocity.magnitude * 3.5f), 20, 50);
            }

            // Play prop impact SFX
            AudioManager.Instance?.PlayPropHit(hitForce);

            // Deals class-based damage + triggers real physics ragdoll knockdown
            health.TakeDamage(damage, col.relativeVelocity.normalized, hitForce);
        }
        isThrown = false;
    }
}
