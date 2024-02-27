using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public interface IRequesterHandler<in T, R>
    {
        SyncType Sync { get; set; }
        bool TryHandle(T message, out R result);
        UniTask<(bool, R)> TryHandleAsync(T message);
        UniTask<(bool, R)> TryHandleAsync(T message, CancellationToken token);
    }

    internal sealed class RequesterHandler<T, R> : IRequesterHandler<T, R>
    {
        public SyncType Sync { get; set; }

        Func<T, R> handler;
        Func<T, UniTask<(bool, R)>> handlerAsync;
        Func<T, CancellationToken, UniTask<(bool, R)>> handlerAsyncCancelable;

        public RequesterHandler(Func<T, R> _handler)
        {
            handler = _handler;
            Sync = SyncType.Sync;
        }

        public RequesterHandler(Func<T, UniTask<(bool, R)>> _handlerAsync)
        {
            handlerAsync = _handlerAsync;
            Sync = SyncType.ASync;
        }

        public RequesterHandler(Func<T, CancellationToken, UniTask<(bool, R)>> _handlerAsyncCancelable)
        {
            handlerAsyncCancelable = _handlerAsyncCancelable;
            Sync = SyncType.ASyncCancelable;
        }

        public bool TryHandle(T message, out R result)
        {
            if (handler != null)
            {
                result = handler.Invoke(message);
                return true;
            }

            result = default;
            return false;
        }

        public UniTask<(bool, R)> TryHandleAsync(T message)
        {
            if (handlerAsync != null)
            {
                return handlerAsync.Invoke(message);
            }

            return default;
        }

        public UniTask<(bool, R)> TryHandleAsync(T message, CancellationToken token)
        {
            if (handlerAsyncCancelable != null)
            {
                return handlerAsyncCancelable.Invoke(message, token);
            }

            return default;
        }
    }
}