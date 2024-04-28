using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public delegate void MsgHandler1<in T>(T msg);
    public delegate UniTask MsgHandler2<in T>(T msg);
    public delegate UniTask MsgHandler3<in T>(T msg, CancellationToken token);
    
    public sealed partial class HandlerFactory
    {
        Options options;
        
        public HandlerFactory(Options _options)
        {
            options = _options;
        }

        public IHandler<T> BuildHandler<T>(IHandler<T> handler, HandlerDecorator<T>[] decorators)
        {
            var hasG = options.TryGetBrokerDecorators<T>(out var enumerable);
            if (decorators.Length != 0 || hasG)
            {
                var brokerDecorators = enumerable.Concat(decorators).Cast<HandlerDecorator<T>>();
                handler = new HandlerWrapper<T>(handler, brokerDecorators);
            }

            return handler;
        }

        private sealed class HandlerWrapper<T> : IHandler<T>
        {
            public SyncType Sync { get; set; }
            MsgHandler1<T> handler;
            MsgHandler2<T> handlerAsync;
            MsgHandler3<T> handlerAsyncCancelable;

            public HandlerWrapper(IHandler<T> body, IEnumerable<HandlerDecorator<T>> decorators)
            {
                MsgHandler1<T> next = null;
                MsgHandler2<T> nextAsync = null;
                MsgHandler3<T> nextAsyncCancelable = null;

                switch (body.Sync)
                {
                    case SyncType.Sync:
                        next = body.Handle;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = next;
                            next = msg => decorator.Handle(msg, pre);
                        }

                        break;
                    case SyncType.ASync:
                        nextAsync = body.HandleAsync;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = nextAsync;
                            nextAsync = async msg => await decorator.HandleAsync(msg, pre);
                        }

                        break;
                    case SyncType.ASyncCancelable:
                        nextAsyncCancelable = body.HandleAsync;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = nextAsyncCancelable;
                            nextAsyncCancelable = async (msg, token) => await decorator.HandleAsync(msg, token, pre);
                        }

                        break;
                }

                handler = next;
                handlerAsync = nextAsync;
                handlerAsyncCancelable = nextAsyncCancelable;
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
    }

    public delegate bool ReqHandler1<in T, R>(T msg, out R result);
    public delegate UniTask<(bool, R)> ReqHandler2<in T, R>(T msg);
    public delegate UniTask<(bool, R)> ReqHandler3<in T, R>(T msg, CancellationToken token);

    public sealed partial class HandlerFactory
    {
        public IHandler<T, R> BuildHandler<T, R>(IHandler<T, R> handler, HandlerDecorator<T, R>[] decorators)
        {
            var hasG = options.TryGetRequesterDecorators<T, R>(out var enumerable);
            if (decorators.Length != 0 || hasG)
            {
                var requesterDecorators = enumerable.Concat(decorators).Cast<HandlerDecorator<T, R>>();
                handler = new HandlerWrapper<T, R>(handler, requesterDecorators);
            }

            return handler;
        }

        // 自定义带有返回值和 out 参数的委托类型

        private sealed class HandlerWrapper<T, R> : IHandler<T, R>
        {
            public SyncType Sync { get; set; }

            ReqHandler1<T, R> handler;
            ReqHandler2<T, R> handlerAsync;
            ReqHandler3<T, R> handlerAsyncCancelable;

            public HandlerWrapper(IHandler<T, R> body, IEnumerable<HandlerDecorator<T, R>> decorators)
            {
                ReqHandler1<T, R> next = null;
                ReqHandler2<T, R> nextAsync = null;
                ReqHandler3<T, R> nextAsyncCancelable = null;

                switch (body.Sync)
                {
                    case SyncType.Sync:
                        next = body.Handle;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = next;
                            next = (T msg, out R result) => decorator.TryHandle(msg, out result, pre);
                        }

                        break;
                    case SyncType.ASync:
                        nextAsync = body.HandleAsync;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = nextAsync;
                            nextAsync = async msg => await decorator.TryHandleAsync(msg, pre);
                        }

                        break;
                    case SyncType.ASyncCancelable:
                        nextAsyncCancelable = body.HandleAsync;
                        foreach (var decorator in decorators.OrderByDescending(x => x.Order))
                        {
                            var pre = nextAsyncCancelable;
                            nextAsyncCancelable = async (msg, token) => await decorator.TryHandleAsync(msg, token, pre);
                        }

                        break;
                }

                handler = next;
                handlerAsync = nextAsync;
                handlerAsyncCancelable = nextAsyncCancelable;
            }

            public bool Handle(T msg, out R result)
            {
                return handler(msg, out result);
            }

            public UniTask<(bool, R)> HandleAsync(T msg)
            {
                return handlerAsync(msg);
            }

            public UniTask<(bool, R)> HandleAsync(T msg, CancellationToken token)
            {
                return handlerAsyncCancelable(msg, token);
            }
        }
    }
}