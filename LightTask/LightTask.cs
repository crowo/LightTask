using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace LightTask;

[AsyncMethodBuilder(typeof(AsyncLightTaskMethodBuilder))]
public abstract class LightTask
{
    private static readonly Action s_sentinel = () => { };
    private static readonly Action<Action> s_threadPoolCallback = (continuation) => continuation!.Invoke();

    protected bool _completed;
    internal bool _StateMachineSet;
    internal Exception? _Exception;
    protected Action? _continuation;
    protected ExecutionContext? _ExecutionContext;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    internal Action _MoveNextDelegate;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


    public static LightTask<(T?, Exception?)[]> WhenAll<T>(IEnumerable<LightTask<T>> tasks, bool takeEnumerablesSnapshot = false)
    {
        int total;
        if (takeEnumerablesSnapshot)
        {
            var ary = tasks.ToArray();
            total = ary.Length;
            tasks = ary;
        }
        else
            total = tasks.Count();
        var remaining = total;
        var promise = LightTask<VoidResult, (T?, Exception?)[]>.Rent();
        Action onComplete = () =>
        {
            if (Interlocked.Decrement(ref remaining) != 0)
                return;
            var result = new (T?, Exception?)[total];
            int i = 0;
            foreach (var task in tasks)
            {
                if (task._Exception != null)
                    result[i] = (default(T), task._Exception);
                else
                    result[i] = (task.m_Result, null);
                task.Return();
                i++;
            }
            promise.m_Result = result;
            promise.SignalCompletion();
        };
        foreach (var task in tasks)
        {
            task.OnCompleted(onComplete);
        }
        return promise;
    }

    static LightTask WhenAny<T>(IEnumerable<LightTask<T>> tasks, bool takeEnumerablesSnapshot = false)
    {
        var gate = -1;
        var promise = LightTask<VoidResult, VoidResult>.Rent();
        Action onComplete = () =>
        {
            if (Interlocked.Increment(ref gate) != 0)
                return;
        };
        return promise;
    }

    public static ContextAwaitable CaptureCurrentContext() => new ContextAwaitable(SynchronizationContext.Current);
    protected abstract void Return();

    public struct LightTaskAwaiter : ICriticalNotifyCompletion
    {
        private LightTask _task;
        public LightTaskAwaiter(LightTask task) => _task = task;

        public void OnCompleted(Action continuation) => _task.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation) => _task.UnsafeOnCompleted(continuation);
        public bool IsCompleted => _task.IsCompleted;
        public void GetResult() => _task.GetResult();
    }
    public LightTaskAwaiter GetAwaiter() => new LightTaskAwaiter(this);
    protected bool IsCompleted => _continuation != null && _completed || _Exception != null;
    private void GetResult()
    {
        if (_Exception != null)
        {
            var ex = _Exception;
            Return();
            ExceptionDispatchInfo.Capture(ex).Throw();
            return;
        }
        if (_continuation != null && _completed)
        {
            Return();
            return;
        }
        throw new InvalidOperationException(nameof(LightTask) + " didn't complete");
    }

    protected void OnCompleted(Action continuation)
    {
        var obj = _continuation;
        if (obj == null)
            obj = Interlocked.CompareExchange(ref _continuation, continuation, null);
        if (obj == null)
        {
            _ExecutionContext = ExecutionContext.Capture();
            return;
        }
        if (obj != s_sentinel)
            throw new InvalidOperationException();
        ThreadPool.QueueUserWorkItem(s_threadPoolCallback, continuation, true);
    }

    protected void UnsafeOnCompleted(Action continuation)
    {
        var obj = _continuation;
        if (obj == null)
            obj = Interlocked.CompareExchange(ref _continuation, continuation, null);
        if (obj == null)
            return;
        if (obj != s_sentinel)
            throw new InvalidOperationException();
        ThreadPool.UnsafeQueueUserWorkItem(s_threadPoolCallback, continuation, true);
    }

    internal void SignalCompletion()
    {
        if (_completed)
        {
            throw new InvalidOperationException();
        }
        _completed = true;
        if (_continuation == null && Interlocked.CompareExchange(ref _continuation, s_sentinel, null) == null)
            return;
        if (_ExecutionContext != null)
            ExecutionContext.Restore(_ExecutionContext);
        _continuation.Invoke();
    }
}