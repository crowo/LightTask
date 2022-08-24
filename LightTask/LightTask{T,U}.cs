using System.Runtime.CompilerServices;

namespace LightTask;
internal struct VoidResult : System.Runtime.CompilerServices.IAsyncStateMachine
{
    public void MoveNext()
    {
        throw new NotImplementedException();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        throw new NotImplementedException();
    }
}
internal class LightTask<TStateMachine, T> : LightTask<T> where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine
{
    private static readonly System.Collections.Concurrent.ConcurrentQueue<LightTask<TStateMachine, T>> s_pool = new();

    public TStateMachine? _StateMachine;

    private LightTask()
    {
        _MoveNextDelegate = CallMoveNext;
    }

    private void CallMoveNext()
    {
        _StateMachine!.MoveNext();
    }

    public void SetStateMachine(in TStateMachine stateMachine)
    {
        _StateMachine = stateMachine;
        _StateMachineSet = true;
    }
    protected override void Return()
    {
        _continuation = null;
        _Exception = null;
        _ExecutionContext = null;
        _completed = false;
        _StateMachineSet = false;
        m_Result = default;
        _StateMachine = default;
        s_pool.Enqueue(this);
    }

    public static LightTask<TStateMachine, T> Rent()
    {
        if (s_pool.TryDequeue(out var runner))
            return runner;
        return new LightTask<TStateMachine, T>();
    }
}
