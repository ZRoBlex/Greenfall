using UnityEngine;
using System;

public class NPCHealth : MonoBehaviour
{
    public int maxHealth = 30;
    [SerializeField] int currentHealth;

    public Action OnDeath;

    NPCDropperAdvanced dropper;

    void Awake()
    {
        dropper = GetComponent<NPCDropperAdvanced>();
        ResetHealth();
    }

    void OnEnable()
    {
        ResetHealth();
    }

    void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        dropper?.Drop();
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
