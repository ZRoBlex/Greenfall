using UnityEngine;

public enum MovementMode
{
    None = 0,
    Team = 1,
    Chase = 2,
    Wander = 3
}

public class MovementPriorityController : MonoBehaviour
{
    public MovementMode currentMode = MovementMode.None;

    EnemyPathAgent agent;
    MovementGrounded movement;

    void Awake()
    {
        agent = GetComponent<EnemyPathAgent>();
        movement = GetComponent<MovementGrounded>();
    }

    public void SetMode(MovementMode mode)
    {
        currentMode = mode;
    }

    public void Stop()
    {
        currentMode = MovementMode.None;
        agent.Stop();
        movement.StopInstantly();
    }

    public void MoveTo(Vector3 target, MovementMode mode)
    {
        // Solo cambiar si el nuevo modo tiene más prioridad
        if ((int)mode >= (int)currentMode)
        {
            currentMode = mode;
            agent.MoveTo(target);
        }
    }
}
