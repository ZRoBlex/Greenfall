using UnityEngine;
using System;

public class NPCHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    float current;

    public Action OnDeath;

    void OnEnable()
    {
        current = maxHealth;
    }

    public void TakeDamage(float dmg)
    {
        current -= dmg;
        if (current <= 0)
            Die();
    }

    void Die()
    {
        OnDeath?.Invoke();
    }
}
