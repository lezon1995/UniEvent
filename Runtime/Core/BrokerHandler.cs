using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public interface IBrokerHandler<in T>
    {
        SyncType Sync { get; set; }
        void Handle(T message);
        UniTask HandleAsync(T message);
        UniTask HandleAsync(T message, CancellationToken token);
    }

    internal sealed class BrokerHandler<T> : IBrokerHandler<T>
    {
        public SyncType Sync { get; set; }

        Action<T> handler;
        Func<T, UniTask> handlerAsync;
        Func<T, CancellationToken, UniTask> handlerAsyncCancelable;

        public BrokerHandler(Action<T> _handler)
        {
            handler = _handler;
            Sync = SyncType.Sync;
        }

        public BrokerHandler(Func<T, UniTask> _handlerAsync)
        {
            handlerAsync = _handlerAsync;
            Sync = SyncType.ASync;
        }

        public BrokerHandler(Func<T, CancellationToken, UniTask> _handlerAsyncCancelable)
        {
            handlerAsyncCancelable = _handlerAsyncCancelable;
            Sync = SyncType.ASyncCancelable;
        }

        public void Handle(T message)
        {
            handler?.Invoke(message);
        }

        public UniTask HandleAsync(T message)
        {
            if (handlerAsync != null)
            {
                return handlerAsync.Invoke(message);
            }

            return default;
        }

        public UniTask HandleAsync(T message, CancellationToken token)
        {
            if (handlerAsyncCancelable != null)
            {
                return handlerAsyncCancelable.Invoke(message, token);
            }

            return default;
        }
    }
}