using System.Runtime.CompilerServices;

namespace LightTask
{
    public struct ContextAwaitable
    {
        private SynchronizationContext? _Context;
        internal ContextAwaitable(SynchronizationContext? context) => _Context = context;
        public ContextAwaiter GetAwaiter()
        {
            return new ContextAwaiter(_Context);
        }

        public struct ContextAwaiter : INotifyCompletion
        {
            private SynchronizationContext? _Context;
            internal ContextAwaiter(SynchronizationContext? context) => _Context = context;
            public bool IsCompleted => false;
            public void GetResult() { }
            public void OnCompleted(Action continuation)
            {
                if (_Context != null)
                    _Context.Post((_) => continuation(), null);
                else
                    continuation();
            }
        }
    }
}