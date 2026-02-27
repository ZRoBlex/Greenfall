using UnityEngine;

/// <summary>
/// Stunned State - Compatible with NonLethalHealth.cs
/// Called when enemy is knocked out
/// </summary>
public class StunnedState : State<OptimizedEnemyController>
{
    private NonLethalHealth nonLethalHealth;

    public override void Enter(OptimizedEnemyController o)
    {
        if (o == null) return;

        nonLethalHealth = o.GetComponent<NonLethalHealth>();

        // Set stunned animation
        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsStunned", true);
        }

        // Stop movement
        if (o.Motor != null)
        {
            o.Motor.enabled = false;
        }

        Debug.Log($"[{o.stats.displayName}] Entered StunnedState");
    }

    public override void Tick(OptimizedEnemyController o)
    {
        // Just stay stunned - NonLethalHealth handles recovery
        // When NonLethalHealth.Recover() is called, it will change state back to Wander
    }

    public override void Exit(OptimizedEnemyController o)
    {
        if (o == null) return;

        // Re-enable movement
        if (o.Motor != null)
        {
            o.Motor.enabled = true;
        }

        // Clear stunned animation
        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsStunned", false);
        }

        Debug.Log($"[{o.stats.displayName}] Exited StunnedState");
    }
}
