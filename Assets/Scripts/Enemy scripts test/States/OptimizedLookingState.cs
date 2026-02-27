using UnityEngine;

/// <summary>
/// Optimized Looking State:
/// - Enemy stands still and looks around
/// - Transitions back to wandering after duration
/// - Smooth rotation animations
/// </summary>
public class OptimizedLookingState : State<OptimizedEnemyController>
{
    private float lookTimer;
    private Quaternion targetRotation;
    private bool hasPickedDirection;

    public override void Enter(OptimizedEnemyController o)
    {
        if (o == null) return;

        lookTimer = 0f;
        hasPickedDirection = false;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsIdle", true);
        }

        // Stop movement
        if (o.Motor != null)
        {
            Vector2Int currentCell = o.LocalGrid.WorldToCell(o.transform.position);
            o.Motor.SetDestination(currentCell);
        }

        // Pick random direction to look
        PickLookDirection(o);
    }

    public override void Tick(OptimizedEnemyController o)
    {
        if (o == null) return;

        lookTimer += Time.deltaTime;

        // Smoothly rotate toward look direction
        if (hasPickedDirection)
        {
            o.transform.rotation = Quaternion.Slerp(
                o.transform.rotation,
                targetRotation,
                o.stats.turnSpeed * 0.5f * Time.deltaTime
            );
        }

        // Finished looking - return to wander
        if (lookTimer >= o.stats.lookDuration)
        {
            o.FSM.ChangeState(new OptimizedWanderState());
        }
    }

    public override void Exit(OptimizedEnemyController o)
    {
        if (o != null && o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsIdle", false);
        }
    }

    /// <summary>
    /// Pick random direction to look at
    /// </summary>
    private void PickLookDirection(OptimizedEnemyController o)
    {
        // Random angle between 0-360
        float randomAngle = Random.Range(0f, 360f);

        // Create rotation
        targetRotation = Quaternion.Euler(0f, randomAngle, 0f);
        hasPickedDirection = true;
    }
}
