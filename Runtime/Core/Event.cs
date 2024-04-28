using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniEvent.Internal;

namespace UniEvent
{
    public interface IEvent<T>
    {
        void Pub(T msg, bool buffered = false);
        IDisposable Sub(IHandler<T> handler, bool handleBuffered = false, params HandlerDecorator<T>[] decorators);

        UniTask PubAsync(T msg, bool buffered = false, CancellationToken token = default);
        UniTask PubAsync(T msg, AsyncPubStrategy strategy, bool buffered = false, CancellationToken token = default);
        UniTask<IDisposable> SubAsync(IHandler<T> handler, bool handleBuffered = false, CancellationToken token = default, params HandlerDecorator<T>[] decorators);
    }

    public class Event<T> : IEvent<T>, IDisposable, IHandlerMarker
    {
        Options options;
        HandlerFactory handlerFactory;
        DiagnosticsInfo diagnosticsInfo;

        List<IHandler<T>> handlers;
        object gate;
        bool isDisposed;

        Queue<T> buffer => _buffer ??= new Queue<T>();
        Queue<T> _buffer;


        public Event(Options _options, HandlerFactory _handlerFactory, DiagnosticsInfo _diagnosticsInfo)
        {
            options = _options;
            handlerFactory = _handlerFactory;
            diagnosticsInfo = _diagnosticsInfo;

            handlers = new List<IHandler<T>>();
            gate = new object();
        }

        public void Pub(T msg, bool buffered = false)
        {
            if (buffered)
            {
                buffer.Enqueue(msg);
            }

            foreach (var handler in handlers)
            {
                handler.Handle(msg);
                handler.HandleAsync(msg).Forget();
                handler.HandleAsync(msg, default).Forget();
            }
        }

        public async UniTask PubAsync(T msg, bool buffered = false, CancellationToken token = default)
        {
            await PubAsync(msg, options.DefaultStrategy, buffered, token);
        }

        public async UniTask PubAsync(T msg, AsyncPubStrategy strategy, bool buffered = false, CancellationToken token = default)
        {
            if (buffered)
            {
                buffer.Enqueue(msg);
            }

            if (handlers.Count <= 1 || strategy == AsyncPubStrategy.Sequential)
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

        public IDisposable Sub(IHandler<T> handler, bool handleBuffered = false, params HandlerDecorator<T>[] decorators)
        {
            if (handleBuffered)
            {
                while (buffer.Count > 0)
                {
                    handler.Handle(buffer.Dequeue());
                }
            }

            return InternalSub(handler, decorators);
        }

        public async UniTask<IDisposable> SubAsync(IHandler<T> handler, bool handleBuffered = false, CancellationToken token = default, params HandlerDecorator<T>[] decorators)
        {
            if (handleBuffered)
            {
                while (buffer.Count > 0)
                {
                    var msg = buffer.Dequeue();
                    if (token == default)
                        await handler.HandleAsync(msg);
                    else
                        await handler.HandleAsync(msg, token);
                }
            }

            return InternalSub(handler, decorators);
        }

        IDisposable InternalSub(IHandler<T> handler, params HandlerDecorator<T>[] decorators)
        {
            lock (gate)
            {
                if (isDisposed)
                {
                    return options.HandleDisposedStrategy.Handle(nameof(Event<T>));
                }

                handler = handlerFactory.BuildHandler(handler, decorators);
                handlers.Add(handler);
                var subscription = new Subscription(this, handler);
                diagnosticsInfo.IncrementSub(this, subscription);
                return subscription;
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                // Dispose is called when scope is finished.
                var count = handlers.Count;
                handlers.Clear();
                if (!isDisposed)
                {
                    isDisposed = true;
                    diagnosticsInfo.RemoveTargetDiagnostics(this, count);
                }
            }
        }


        sealed class Subscription : IDisposable
        {
            bool isDisposed;
            Event<T> _event;
            IHandler<T> subscriptionKey;

            public Subscription(Event<T> @event, IHandler<T> _subscriptionKey)
            {
                _event = @event;
                subscriptionKey = _subscriptionKey;
            }

            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    lock (_event.gate)
                    {
                        _event.handlers.Remove(subscriptionKey);
                        _event.diagnosticsInfo.DecrementSub(_event, this);
                    }
                }
            }
        }
    }

    public interface IEvent<T, R>
    {
        bool Pub(T msg, out R result);
        bool Pub(T msg, List<R> results);

        UniTask<(bool, R)> PubAsync(T msg, CancellationToken token = default);
        UniTask<(bool, R)> PubAsync(T msg, AsyncPubStrategy strategy, CancellationToken token = default);

        UniTask<bool> PubAsync(T msg, List<R> result, CancellationToken token = default);
        UniTask<bool> PubAsync(T msg, List<R> result, AsyncPubStrategy strategy, CancellationToken token = default);

        IDisposable Sub(IHandler<T, R> handler, params HandlerDecorator<T, R>[] decorators);
    }

    public class Event<T, R> : IEvent<T, R>, IDisposable, IHandlerMarker
    {
        Options options;
        HandlerFactory handlerFactory;
        DiagnosticsInfo diagnosticsInfo;

        List<IHandler<T, R>> handlers;
        object gate;
        bool isDisposed;

        public Event(Options _options, HandlerFactory _handlerFactory, DiagnosticsInfo _diagnosticsInfo)
        {
            options = _options;
            handlerFactory = _handlerFactory;
            diagnosticsInfo = _diagnosticsInfo;

            handlers = new List<IHandler<T, R>>();
            gate = new object();
        }

        public bool Pub(T msg, out R result)
        {
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

        public bool Pub(T msg, List<R> results)
        {
            results.Clear();
            bool hasResult = false;
            foreach (var handler in handlers)
            {
                if (handler.Handle(msg, out var result))
                {
                    results.Add(result);
                    hasResult = true;
                }
            }

            return hasResult;
        }

        public async UniTask<(bool, R)> PubAsync(T msg, CancellationToken token = default)
        {
            return await PubAsync(msg, options.DefaultStrategy, token);
        }

        public async UniTask<(bool, R)> PubAsync(T msg, AsyncPubStrategy strategy, CancellationToken token = default)
        {
            if (handlers.Count <= 1 || strategy == AsyncPubStrategy.Sequential)
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

        public async UniTask<bool> PubAsync(T msg, List<R> result, CancellationToken token = default)
        {
            return await PubAsync(msg, result, options.DefaultStrategy, token);
        }

        public async UniTask<bool> PubAsync(T msg, List<R> list, AsyncPubStrategy strategy, CancellationToken token = default)
        {
            list.Clear();
            bool hasResult = false;
            if (handlers.Count <= 1 || strategy == AsyncPubStrategy.Sequential)
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

        public IDisposable Sub(IHandler<T, R> handler, params HandlerDecorator<T, R>[] decorators)
        {
            lock (gate)
            {
                if (isDisposed)
                {
                    return options.HandleDisposedStrategy.Handle(nameof(Event<T, R>));
                }

                handler = handlerFactory.BuildHandler(handler, decorators);
                handlers.Add(handler);
                var subscription = new Subscription(this, handler);
                diagnosticsInfo.IncrementSub(this, subscription);
                return subscription;
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                // Dispose is called when scope is finished.
                var count = handlers.Count;
                handlers.Clear();
                if (!isDisposed)
                {
                    isDisposed = true;
                    diagnosticsInfo.RemoveTargetDiagnostics(this, count);
                }
            }
        }


        sealed class Subscription : IDisposable
        {
            bool isDisposed;

            Event<T, R> _event;
            IHandler<T, R> subscriptionKey;

            public Subscription(Event<T, R> @event, IHandler<T, R> _subscriptionKey)
            {
                _event = @event;
                subscriptionKey = _subscriptionKey;
            }

            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    lock (_event.gate)
                    {
                        _event.handlers.Remove(subscriptionKey);
                        _event.diagnosticsInfo.DecrementSub(_event, this);
                    }
                }
            }
        }
    }
}