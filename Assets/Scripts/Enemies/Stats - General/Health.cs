using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public event Action OnDeath;
    public event Action<float> OnDamageTaken;

    void Awake() { currentHealth = maxHealth; }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;
        currentHealth -= amount;
        OnDamageTaken?.Invoke(amount);
        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    void Die()
    {
        OnDeath?.Invoke();
        // default: disable object; override if necessary
        gameObject.SetActive(false);
        currentHealth = maxHealth;
    }
}
