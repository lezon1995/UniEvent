using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniEvent.Internal;

namespace UniEvent
{
    public interface ITopic<in K, T>
    {
        void Pub(K key, T msg);
        IDisposable Sub(K key, IHandler<T> handler, params HandlerDecorator<T>[] decorators);

        UniTask PubAsync(K key, T msg, CancellationToken token = default);
        UniTask PubAsync(K key, T msg, AsyncPubStrategy strategy, CancellationToken token = default);
    }

    public class Topic<K, T> : ITopic<K, T>, IDisposable
    {
        Options options;
        HandlerFactory handlerFactory;
        DiagnosticsInfo diagnosticsInfo;

        Dictionary<K, HandlerHolder> handlerGroup;
        object gate;
        bool isDisposed;

        public Topic(Options _options, HandlerFactory _handlerFactory, DiagnosticsInfo _diagnosticsInfo)
        {
            options = _options;
            handlerFactory = _handlerFactory;
            diagnosticsInfo = _diagnosticsInfo;

            handlerGroup = new Dictionary<K, HandlerHolder>();
            gate = new object();
        }

        public void Pub(K key, T msg)
        {
            List<IHandler<T>> handlers;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    return;
                }

                handlers = holder.GetHandlers();
            }

            foreach (var handler in handlers)
            {
                handler.Handle(msg);
                handler.HandleAsync(msg).Forget();
                handler.HandleAsync(msg, default).Forget();
            }
        }

        public UniTask PubAsync(K key, T msg, CancellationToken token)
        {
            return PubAsync(key, msg, options.DefaultStrategy, token);
        }

        public async UniTask PubAsync(K key, T msg, AsyncPubStrategy strategy, CancellationToken token)
        {
            List<IHandler<T>> handlers;
            int count;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    return;
                }

                handlers = holder.GetHandlers();
                count = holder.GetCount();
            }

            if (count <= 1 || strategy == AsyncPubStrategy.Sequential)
            {
                foreach (var handler in handlers)
                {
                    if (token == default)
                        await handler.HandleAsync(msg);
                    else
                        await handler.HandleAsync(msg, token);
                }
            }
            else
            {
                await new AsyncHandlerWhenAll<T>(handlers, msg, token);
            }
        }

        public IDisposable Sub(K key, IHandler<T> handler, params HandlerDecorator<T>[] decorators)
        {
            lock (gate)
            {
                if (isDisposed)
                {
                    return options.HandleDisposedStrategy.Handle(nameof(Topic<K, T>));
                }

                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    handlerGroup[key] = holder = new HandlerHolder(this);
                }

                handler = handlerFactory.BuildHandler(handler, decorators);

                return holder.Sub(key, handler);
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    foreach (var handlers in handlerGroup.Values)
                    {
                        handlers.Dispose();
                    }
                }
            }
        }

        // similar as Keyless-MessageBrokerCore but require to remove when key is empty on Dispose
        sealed class HandlerHolder : IDisposable, IHandlerMarker
        {
            List<IHandler<T>> handlers;
            Topic<K, T> _topic;

            public HandlerHolder(Topic<K, T> topic)
            {
                handlers = new List<IHandler<T>>();
                _topic = topic;
            }

            public List<IHandler<T>> GetHandlers()
            {
                return handlers;
            }

            public int GetCount()
            {
                return handlers.Count;
            }

            public IDisposable Sub(K key, IHandler<T> handler)
            {
                handlers.Add(handler);
                var subscription = new Subscription(key, handler, this);
                _topic.diagnosticsInfo.IncrementSub(this, subscription);
                return subscription;
            }

            public void Dispose()
            {
                lock (_topic.gate)
                {
                    var count = handlers.Count;
                    handlers.Clear();
                    _topic.diagnosticsInfo.RemoveTargetDiagnostics(this, count);
                }
            }

            sealed class Subscription : IDisposable
            {
                bool isDisposed;
                K key;
                IHandler<T> subscriptionKey;
                HandlerHolder holder;

                public Subscription(K _key, IHandler<T> _subscriptionKey, HandlerHolder _holder)
                {
                    key = _key;
                    subscriptionKey = _subscriptionKey;
                    holder = _holder;
                }

                public void Dispose()
                {
                    if (!isDisposed)
                    {
                        isDisposed = true;
                        lock (holder._topic.gate)
                        {
                            if (!holder._topic.isDisposed)
                            {
                                holder.handlers.Remove(subscriptionKey);
                                holder._topic.diagnosticsInfo.DecrementSub(holder, this);
                                if (holder.handlers.Count == 0)
                                {
                                    holder._topic.handlerGroup.Remove(key);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public interface ITopic<in K, T, R>
    {
        bool Pub(K key, T msg, out R result);
        bool Pub(K key, T msg, List<R> results);

        UniTask<(bool, R)> PubAsync(K key, T msg, CancellationToken token = default);
        UniTask<(bool, R)> PubAsync(K key, T msg, AsyncPubStrategy strategy, CancellationToken token = default);

        UniTask<bool> PubAsync(K key, T msg, List<R> result, CancellationToken token = default);
        UniTask<bool> PubAsync(K key, T msg, List<R> result, AsyncPubStrategy strategy, CancellationToken token = default);

        IDisposable Sub(K key, IHandler<T, R> handler, params HandlerDecorator<T, R>[] decorators);
    }

    public class Topic<K, T, R> : ITopic<K, T, R>, IDisposable, IHandlerMarker
    {
        Options options;
        HandlerFactory handlerFactory;
        DiagnosticsInfo diagnosticsInfo;

        Dictionary<K, HandlerHolder> handlerGroup;
        object gate;
        bool isDisposed;

        public Topic(Options _options, HandlerFactory _handlerFactory, DiagnosticsInfo _diagnosticsInfo)
        {
            options = _options;
            handlerFactory = _handlerFactory;
            diagnosticsInfo = _diagnosticsInfo;

            handlerGroup = new Dictionary<K, HandlerHolder>();
            gate = new object();
        }

        public bool Pub(K key, T msg, out R result)
        {
            List<IHandler<T, R>> handlers;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    result = default;
                    return false;
                }

                handlers = holder.GetHandlers();
            }

            foreach (var handler in handlers)
            {
                if (handler.Handle(msg, out result))
                {
                    return true;
                }
            }

            result = default;
            return false;
        }

        public bool Pub(K key, T msg, List<R> results)
        {
            results.Clear();
            List<IHandler<T, R>> handlers;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    return false;
                }

                handlers = holder.GetHandlers();
            }

            foreach (var handler in handlers)
            {
                if (handler.Handle(msg, out var result))
                {
                    results.Add(result);
                    return true;
                }
            }

            return false;
        }

        public async UniTask<(bool, R)> PubAsync(K key, T msg, CancellationToken token = default)
        {
            return await PubAsync(key, msg, options.DefaultStrategy, token);
        }

        public async UniTask<(bool, R)> PubAsync(K key, T msg, AsyncPubStrategy strategy, CancellationToken token = default)
        {
            List<IHandler<T, R>> handlers;
            int count;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    return (false, default);
                }

                handlers = holder.GetHandlers();
                count = holder.GetCount();
            }

            if (count <= 1 || strategy == AsyncPubStrategy.Sequential)
            {
                foreach (var handler in handlers)
                {
                    bool success;
                    R result;
                    if (token == default)
                        (success, result) = await handler.HandleAsync(msg);
                    else
                        (success, result) = await handler.HandleAsync(msg, token);

                    if (success)
                    {
                        return (true, result);
                    }
                }
            }
            else
            {
                var results = await new AsyncHandlerWhenAll<T, R>(handlers, msg, token);
                if (results.Length > 0)
                {
                    return (true, results[0]);
                }
            }

            return (false, default);
        }

        public async UniTask<bool> PubAsync(K key, T msg, List<R> result, CancellationToken token = default)
        {
            return await PubAsync(key, msg, result, options.DefaultStrategy, token);
        }

        public async UniTask<bool> PubAsync(K key, T msg, List<R> list, AsyncPubStrategy strategy, CancellationToken token = default)
        {
            list.Clear();
            List<IHandler<T, R>> handlers;
            int count;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    return false;
                }

                handlers = holder.GetHandlers();
                count = holder.GetCount();
            }

            bool hasResult = false;
            if (count <= 1 || strategy == AsyncPubStrategy.Sequential)
            {
                foreach (var handler in handlers)
                {
                    bool success;
                    R result;
                    if (token == default)
                        (success, result) = await handler.HandleAsync(msg);
                    else
                        (success, result) = await handler.HandleAsync(msg, token);

                    if (success)
                    {
                        list.Add(result);
                        hasResult = true;
                    }
                }
            }
            else
            {
                var results = await new AsyncHandlerWhenAll<T, R>(handlers, msg, token);
                list.AddRange(results);
                hasResult = true;
            }

            return hasResult;
        }


        public IDisposable Sub(K key, IHandler<T, R> handler, params HandlerDecorator<T, R>[] decorators)
        {
            lock (gate)
            {
                if (isDisposed)
                {
                    return options.HandleDisposedStrategy.Handle(nameof(Topic<K, T, R>));
                }

                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    handlerGroup[key] = holder = new HandlerHolder(this);
                }

                handler = handlerFactory.BuildHandler(handler, decorators);

                return holder.Sub(key, handler);
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    foreach (var handlers in handlerGroup.Values)
                    {
                        handlers.Dispose();
                    }
                }
            }
        }

        sealed class HandlerHolder : IDisposable, IHandlerMarker
        {
            List<IHandler<T, R>> handlers;
            Topic<K, T, R> _topic;

            public HandlerHolder(Topic<K, T, R> topic)
            {
                handlers = new List<IHandler<T, R>>();
                _topic = topic;
            }

            public List<IHandler<T, R>> GetHandlers()
            {
                return handlers;
            }

            public int GetCount()
            {
                return handlers.Count;
            }

            public IDisposable Sub(K key, IHandler<T, R> handler)
            {
                handlers.Add(handler);
                var subscription = new Subscription(key, handler, this);
                _topic.diagnosticsInfo.IncrementSub(this, subscription);
                return subscription;
            }

            public void Dispose()
            {
                lock (_topic.gate)
                {
                    var count = handlers.Count;
                    handlers.Clear();
                    _topic.diagnosticsInfo.RemoveTargetDiagnostics(this, count);
                }
            }

            sealed class Subscription : IDisposable
            {
                bool isDisposed;
                K key;
                IHandler<T, R> subscriptionKey;
                HandlerHolder holder;

                public Subscription(K _key, IHandler<T, R> _subscriptionKey, HandlerHolder _holder)
                {
                    key = _key;
                    subscriptionKey = _subscriptionKey;
                    holder = _holder;
                }

                public void Dispose()
                {
                    if (!isDisposed)
                    {
                        isDisposed = true;
                        lock (holder._topic.gate)
                        {
                            if (!holder._topic.isDisposed)
                            {
                                holder.handlers.Remove(subscriptionKey);
                                holder._topic.diagnosticsInfo.DecrementSub(holder, this);
                                if (holder.handlers.Count == 0)
                                {
                                    holder._topic.handlerGroup.Remove(key);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}