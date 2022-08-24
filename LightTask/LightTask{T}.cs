using System.Runtime.CompilerServices;

namespace LightTask;

[AsyncMethodBuilder(typeof(AsyncLightTaskMethodBuilder<>))]
public abstract class LightTask<T> : LightTask
{
    public new readonly struct LightTaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly LightTask<T> _task;
        public LightTaskAwaiter(LightTask<T> task) => _task = task;
        public readonly void OnCompleted(Action continuation) => _task.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation) => _task.UnsafeOnCompleted(continuation);
        public readonly bool IsCompleted => _task.IsCompleted;
        public readonly T? GetResult() => _task.GetResult();
    }
    internal T? m_Result;
    public new LightTaskAwaiter GetAwaiter() => new (this);

    private T? GetResult()
    {
        Console.WriteLine("get result");
        if (_Exception != null)
        {
            var ex = _Exception;
            Return();
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
            return default;
        }
        if (_continuation != null && _completed)
        {
            var result = m_Result;
            Return();
            return result;
        }
        throw new InvalidOperationException(nameof(LightTask<T>) + " didn't complete");
    }

    public Task<T> AsTask()
    {
        var tcs = new TaskCompletionSource<T>();
        this.OnCompleted(() => tcs.SetResult(m_Result!));
        return tcs.Task;

    }
}