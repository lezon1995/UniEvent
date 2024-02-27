using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public delegate void BrokerHandler1<in T>(T message);

    public delegate UniTask BrokerHandler2<in T>(T message);

    public delegate UniTask BrokerHandler3<in T>(T message, CancellationToken token);
    
    public sealed partial class HandlerFactory
    {
        Options options;
        
        public HandlerFactory(Options _options)
        {
            options = _options;
        }

        public IBrokerHandler<T> BuildHandler<T>(IBrokerHandler<T> handler, BrokerHandlerDecorator<T>[] decorators)
        {
            var hasG = options.TryGetBrokerDecorators<T>(out var enumerable);
            if (decorators.Length != 0 || hasG)
            {
                var brokerDecorators = enumerable.Concat(decorators).Cast<BrokerHandlerDecorator<T>>();
                handler = new HandlerWrapper<T>(handler, brokerDecorators);
            }

            return handler;
        }

        private sealed class HandlerWrapper<T> : IBrokerHandler<T>
        {
            public SyncType Sync { get; set; }
            BrokerHandler1<T> handler;
            BrokerHandler2<T> handlerAsync;
            BrokerHandler3<T> handlerAsyncCancelable;

            public HandlerWrapper(IBrokerHandler<T> body, IEnumerable<BrokerHandlerDecorator<T>> decorators)
            {
                BrokerHandler1<T> next = null;
                BrokerHandler2<T> nextAsync = null;
                BrokerHandler3<T> nextAsyncCancelable = null;

                switch (body.Sync)
                {
                    case SyncType.Sync:
                        next = body.Handle;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = next;
                            next = message => decorator.Handle(message, pre);
                        }

                        break;
                    case SyncType.ASync:
                        nextAsync = body.HandleAsync;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = nextAsync;
                            nextAsync = async message => await decorator.HandleAsync(message, pre);
                        }

                        break;
                    case SyncType.ASyncCancelable:
                        nextAsyncCancelable = body.HandleAsync;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = nextAsyncCancelable;
                            nextAsyncCancelable = async (message, token) => await decorator.HandleAsync(message, token, pre);
                        }

                        break;
                }

                handler = next;
                handlerAsync = nextAsync;
                handlerAsyncCancelable = nextAsyncCancelable;
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

    public delegate bool RequesterHandler1<in T, R>(T message, out R result);

    public delegate UniTask<(bool, R)> RequesterHandler2<in T, R>(T message);

    public delegate UniTask<(bool, R)> RequesterHandler3<in T, R>(T message, CancellationToken token);

    public sealed partial class HandlerFactory
    {
        public IRequesterHandler<T, R> BuildHandler<T, R>(IRequesterHandler<T, R> handler, RequesterHandlerDecorator<T, R>[] decorators)
        {
            var hasG = options.TryGetRequesterDecorators<T, R>(out var enumerable);
            if (decorators.Length != 0 || hasG)
            {
                var requesterDecorators = enumerable.Concat(decorators).Cast<RequesterHandlerDecorator<T, R>>();
                handler = new HandlerWrapper<T, R>(handler, requesterDecorators);
            }

            return handler;
        }

        // 自定义带有返回值和 out 参数的委托类型

        private sealed class HandlerWrapper<T, R> : IRequesterHandler<T, R>
        {
            public SyncType Sync { get; set; }

            RequesterHandler1<T, R> handler;
            RequesterHandler2<T, R> handlerAsync;
            RequesterHandler3<T, R> handlerAsyncCancelable;

            public HandlerWrapper(IRequesterHandler<T, R> body, IEnumerable<RequesterHandlerDecorator<T, R>> decorators)
            {
                RequesterHandler1<T, R> next = null;
                RequesterHandler2<T, R> nextAsync = null;
                RequesterHandler3<T, R> nextAsyncCancelable = null;

                switch (body.Sync)
                {
                    case SyncType.Sync:
                        next = body.TryHandle;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = next;
                            next = (T message, out R result) => decorator.TryHandle(message, out result, pre);
                        }

                        break;
                    case SyncType.ASync:
                        nextAsync = body.TryHandleAsync;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = nextAsync;
                            nextAsync = async message => await decorator.TryHandleAsync(message, pre);
                        }

                        break;
                    case SyncType.ASyncCancelable:
                        nextAsyncCancelable = body.TryHandleAsync;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = nextAsyncCancelable;
                            nextAsyncCancelable = async (message, token) => await decorator.TryHandleAsync(message, token, pre);
                        }

                        break;
                }

                handler = next;
                handlerAsync = nextAsync;
                handlerAsyncCancelable = nextAsyncCancelable;
            }

            public bool TryHandle(T message, out R result)
            {
                return handler(message, out result);
            }

            public UniTask<(bool, R)> TryHandleAsync(T message)
            {
                return handlerAsync(message);
            }

            public UniTask<(bool, R)> TryHandleAsync(T message, CancellationToken token)
            {
                return handlerAsyncCancelable(message, token);
            }
        }
    }
}