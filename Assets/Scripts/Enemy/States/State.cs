public abstract class State<T>
{
    public virtual void Enter(T owner) { }
    public virtual void Tick(T owner) { }
    public virtual void Exit(T owner) { }
}
