public class StateMachine<T>
{
    public State<T> CurrentState { get; private set; }
    T owner;

    public StateMachine(T owner)
    {
        this.owner = owner;
    }

    public void ChangeState(State<T> newState)
    {
        CurrentState?.Exit(owner);
        CurrentState = newState;
        CurrentState?.Enter(owner);
    }

    public void Tick()
    {
        CurrentState?.Tick(owner);
    }
}
