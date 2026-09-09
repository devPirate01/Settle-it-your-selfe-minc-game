using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public Slider healthBar;

    int current;
    bool dead;
    public bool IsDead => dead;

    void Start()
    {
        current = maxHealth;
        if (healthBar) healthBar.value = 1f;
    }

    public void TakeDamage(int amount, Vector3 hitDirection, float hitForce = 1f)
    {
        if (dead) return;

        current = Mathf.Max(0, current - amount);
        if (healthBar) healthBar.value = (float)current / maxHealth;

        if (current <= 0)
        {
            Die();
            return;
        }

        // TRIGGER THE REAL RAGDOLL KNOCKDOWN!
        var ragdoll = GetComponent<RagdollController>();
        if (ragdoll != null)
        {
            ragdoll.Knockdown(hitDirection * (8f * hitForce));
        }
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, -transform.forward + Vector3.up * 0.3f, 1f);
    }

    void Die()
    {
        dead = true;

        GetComponent<SimpleMovement>().enabled = false;
        GetComponent<Grabber>().enabled = false;

        var ragdoll = GetComponent<RagdollController>();
        if (ragdoll != null)
        {
            ragdoll.TriggerDeathRagdoll((-transform.forward + Vector3.up * 0.5f) * 8f);
        }
        else
        {
            var cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            var anim = GetComponentInChildren<Animator>();
            if (anim) anim.enabled = false;
        }

        GameUI.Instance?.ShowWinner(gameObject.name);
    }
}
