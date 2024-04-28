using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public interface IHandler<in T>
    {
        SyncType Sync { get; set; }
        void Handle(T msg);
        UniTask HandleAsync(T msg);
        UniTask HandleAsync(T msg, CancellationToken token);
    }

    internal sealed class Handler<T> : IHandler<T>
    {
        public SyncType Sync { get; set; }

        Action<T> handler;
        Func<T, UniTask> handlerAsync;
        Func<T, CancellationToken, UniTask> handlerAsyncCancelable;

        public Handler(Action<T> _handler)
        {
            handler = _handler;
            Sync = SyncType.Sync;
        }

        public Handler(Func<T, UniTask> _handlerAsync)
        {
            handlerAsync = _handlerAsync;
            Sync = SyncType.ASync;
        }

        public Handler(Func<T, CancellationToken, UniTask> _handlerAsyncCancelable)
        {
            handlerAsyncCancelable = _handlerAsyncCancelable;
            Sync = SyncType.ASyncCancelable;
        }

        public void Handle(T msg)
        {
            handler?.Invoke(msg);
        }

        public UniTask HandleAsync(T msg)
        {
            if (handlerAsync != null)
            {
                return handlerAsync.Invoke(msg);
            }

            return default;
        }

        public UniTask HandleAsync(T msg, CancellationToken token)
        {
            if (handlerAsyncCancelable != null)
            {
                return handlerAsyncCancelable.Invoke(msg, token);
            }

            return default;
        }
    }
    
    public interface IHandler<in T, R>
    {
        SyncType Sync { get; set; }
        bool Handle(T msg, out R result);
        UniTask<(bool, R)> HandleAsync(T msg);
        UniTask<(bool, R)> HandleAsync(T msg, CancellationToken token);
    }

    internal sealed class Handler<T, R> : IHandler<T, R>
    {
        public SyncType Sync { get; set; }

        Func<T, R> handler;
        Func<T, UniTask<(bool, R)>> handlerAsync;
        Func<T, CancellationToken, UniTask<(bool, R)>> handlerAsyncCancelable;

        public Handler(Func<T, R> _handler)
        {
            handler = _handler;
            Sync = SyncType.Sync;
        }

        public Handler(Func<T, UniTask<(bool, R)>> _handlerAsync)
        {
            handlerAsync = _handlerAsync;
            Sync = SyncType.ASync;
        }

        public Handler(Func<T, CancellationToken, UniTask<(bool, R)>> _handlerAsyncCancelable)
        {
            handlerAsyncCancelable = _handlerAsyncCancelable;
            Sync = SyncType.ASyncCancelable;
        }

        public bool Handle(T msg, out R result)
        {
            if (handler != null)
            {
                result = handler.Invoke(msg);
                return true;
            }

            result = default;
            return false;
        }

        public UniTask<(bool, R)> HandleAsync(T msg)
        {
            if (handlerAsync != null)
            {
                return handlerAsync.Invoke(msg);
            }

            return default;
        }

        public UniTask<(bool, R)> HandleAsync(T msg, CancellationToken token)
        {
            if (handlerAsyncCancelable != null)
            {
                return handlerAsyncCancelable.Invoke(msg, token);
            }

            return default;
        }
    }
}