using System.Runtime.CompilerServices;
using System.Text;

namespace LightTask;

public struct AsyncLightTaskMethodBuilder<T>
{
    private LightTask<T> m_Task;
    public static AsyncLightTaskMethodBuilder<T> Create() => default;
    public LightTask<T> Task => m_Task;

    public void SetException(Exception e)
    {
        m_Task._Exception = e;
        m_Task.SignalCompletion();
    }

    public void SetResult(T result)
    {
        m_Task.m_Result = result;
        m_Task.SignalCompletion();
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
   where TAwaiter : INotifyCompletion
   where TStateMachine : IAsyncStateMachine
    {
        if (!m_Task._StateMachineSet)
            Unsafe.As<LightTask<TStateMachine, T>>(m_Task).SetStateMachine(in stateMachine);
        awaiter.OnCompleted(m_Task._MoveNextDelegate);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (!m_Task._StateMachineSet)
            Unsafe.As<LightTask<TStateMachine, T>>(m_Task).SetStateMachine(in stateMachine);
        awaiter.UnsafeOnCompleted(m_Task._MoveNextDelegate);
    }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        m_Task = LightTask<TStateMachine, T>.Rent();
        stateMachine.MoveNext();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) => ArgumentNullException.ThrowIfNull(stateMachine);
}