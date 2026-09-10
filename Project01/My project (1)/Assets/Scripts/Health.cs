using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public Slider healthBar; // Optional legacy Canvas slider

    int current;
    bool dead;
    int playerIndex = 0;

    public bool IsDead => dead;

    void Start()
    {
        current = maxHealth;
        var move = GetComponent<SimpleMovement>();
        if (move) playerIndex = move.playerIndex;

        if (healthBar) healthBar.value = 1f;
        GameUIController.Instance?.SetHealth(playerIndex, 1f);
    }

    public void TakeDamage(int amount, Vector3 hitDirection, float hitForce = 1f)
    {
        if (dead) return;

        current = Mathf.Max(0, current - amount);
        float normalized = (float)current / maxHealth;

        if (healthBar) healthBar.value = normalized;
        GameUIController.Instance?.SetHealth(playerIndex, normalized);

        if (current <= 0)
        {
            Die();
            return;
        }

        // Trigger real ragdoll knockdown
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

    public void SetSuddenDeath()
    {
        if (dead) return;
        current = 1;
        float normalized = (float)current / maxHealth;
        if (healthBar) healthBar.value = normalized;
        GameUIController.Instance?.SetHealth(playerIndex, normalized);
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

        string winner = (playerIndex == 0) ? "PLAYER 2 WINS!" : "PLAYER 1 WINS!";
        GameUIController.Instance?.ShowVictory(winner);
        GameUI.Instance?.ShowWinner(gameObject.name);

        AudioManager.Instance?.PlayDeath();
        AudioManager.Instance?.StopMusic(1.5f);
        GameIntroManager.Instance?.OnGameOver();
    }
}
