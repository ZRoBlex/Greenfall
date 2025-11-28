using UnityEngine;

// StateMachine.cs
public class StateMachine<T>
{
    public State<T> CurrentState { get; private set; }
    T context;

    public StateMachine(T ctx) { context = ctx; }

    public void Initialize(State<T> startingState)
    {
        CurrentState = startingState;
        CurrentState?.Enter(context);
    }

    public void ChangeState(State<T> newState)
    {
        CurrentState?.Exit(context);
        CurrentState = newState;
        CurrentState?.Enter(context);
    }

    public void Tick()
    {
        CurrentState?.Tick(context);
    }
}