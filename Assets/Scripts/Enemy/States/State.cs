using UnityEngine;

/// <summary>
/// Base para estados de la máquina de estados
/// </summary>
public abstract class State<T>
{
    public virtual void Enter(T owner) { }
    public virtual void Tick(T owner) { }
    public virtual void Exit(T owner) { }
}

/// <summary>
/// Máquina de estados genérica
/// </summary>
public class StateMachine<T>
{
    public State<T> CurrentState { get; private set; }

    readonly T owner;

    public StateMachine(T owner)
    {
        this.owner = owner;
    }

    public void ChangeState(State<T> newState)
    {
        if (newState == null)
        {
            Debug.LogWarning("[StateMachine] Intentando cambiar a estado null");
            return;
        }

        CurrentState?.Exit(owner);
        CurrentState = newState;
        CurrentState?.Enter(owner);
    }

    public void Tick()
    {
        CurrentState?.Tick(owner);
    }

    public bool IsInState<TState>() where TState : State<T>
    {
        return CurrentState != null && CurrentState.GetType() == typeof(TState);
    }
}