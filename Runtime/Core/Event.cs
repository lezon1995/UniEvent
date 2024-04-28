using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniEvent.Internal;

namespace UniEvent
{
    public interface IEvent<T>
    {
        void Publish(T msg, bool buffered = false);

        UniTask PublishAsync(T msg, bool buffered = false, CancellationToken token = default);
        UniTask PublishAsync(T msg, PublishAsyncStrategy strategy, bool buffered = false, CancellationToken token = default);

        IDisposable Subscribe(IHandler<T> handler, bool handleBuffered = false, params HandlerDecorator<T>[] decorators);
        UniTask<IDisposable> SubscribeAsync(IHandler<T> handler, bool handleBuffered = false, CancellationToken token = default, params HandlerDecorator<T>[] decorators);
    }

    public class Event<T> : IEvent<T>, IDisposable, IHandlerHolderMarker
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

        public void Publish(T msg, bool buffered = false)
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

        public async UniTask PublishAsync(T msg, bool buffered = false, CancellationToken token = default)
        {
            await PublishAsync(msg, options.DefaultPublishAsyncStrategy, buffered, token);
        }

        public async UniTask PublishAsync(T msg, PublishAsyncStrategy strategy, bool buffered = false, CancellationToken token = default)
        {
            if (buffered)
            {
                buffer.Enqueue(msg);
            }

            if (handlers.Count <= 1 || strategy == PublishAsyncStrategy.Sequential)
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

        public IDisposable Subscribe(IHandler<T> handler, bool handleBuffered = false, params HandlerDecorator<T>[] decorators)
        {
            if (handleBuffered)
            {
                while (buffer.Count > 0)
                {
                    handler.Handle(buffer.Dequeue());
                }
            }

            return InternalSubscribe(handler, decorators);
        }

        public async UniTask<IDisposable> SubscribeAsync(IHandler<T> handler, bool handleBuffered = false, CancellationToken token = default, params HandlerDecorator<T>[] decorators)
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

            return InternalSubscribe(handler, decorators);
        }

        IDisposable InternalSubscribe(IHandler<T> handler, params HandlerDecorator<T>[] decorators)
        {
            lock (gate)
            {
                if (isDisposed)
                {
                    return options.HandlingSubscribeDisposedPolicy.Handle(nameof(Event<T>));
                }

                handler = handlerFactory.BuildHandler(handler, decorators);
                handlers.Add(handler);
                var subscription = new Subscription(this, handler);
                diagnosticsInfo.IncrementSubscribe(this, subscription);
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
                        _event.diagnosticsInfo.DecrementSubscribe(_event, this);
                    }
                }
            }
        }
    }

    public interface IEvent<T, R>
    {
        bool TryPublish(T msg, out R result);
        bool TryPublish(T msg, List<R> results);

        UniTask<(bool, R)> TryPublishAsync(T msg, CancellationToken token = default);
        UniTask<(bool, R)> TryPublishAsync(T msg, PublishAsyncStrategy strategy, CancellationToken token = default);

        UniTask<bool> TryPublishAsync(T msg, List<R> result, CancellationToken token = default);
        UniTask<bool> TryPublishAsync(T msg, List<R> result, PublishAsyncStrategy strategy, CancellationToken token = default);

        IDisposable Subscribe(IHandler<T, R> handler, params HandlerDecorator<T, R>[] decorators);
    }

    public class Event<T, R> : IEvent<T, R>, IDisposable, IHandlerHolderMarker
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

        public bool TryPublish(T msg, out R result)
        {
            foreach (var handler in handlers)
            {
                if (handler.TryHandle(msg, out result))
                {
                    return true;
                }
            }

            result = default;
            return false;
        }

        public bool TryPublish(T msg, List<R> results)
        {
            results.Clear();
            bool hasResult = false;
            foreach (var handler in handlers)
            {
                if (handler.TryHandle(msg, out var result))
                {
                    results.Add(result);
                    hasResult = true;
                }
            }

            return hasResult;
        }

        public async UniTask<(bool, R)> TryPublishAsync(T msg, CancellationToken token = default)
        {
            return await TryPublishAsync(msg, options.DefaultPublishAsyncStrategy, token);
        }

        public async UniTask<(bool, R)> TryPublishAsync(T msg, PublishAsyncStrategy strategy, CancellationToken token = default)
        {
            if (handlers.Count <= 1 || strategy == PublishAsyncStrategy.Sequential)
            {
                foreach (var handler in handlers)
                {
                    bool success;
                    R result;
                    if (token == default)
                        (success, result) = await handler.TryHandleAsync(msg);
                    else
                        (success, result) = await handler.TryHandleAsync(msg, token);

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

        public async UniTask<bool> TryPublishAsync(T msg, List<R> result, CancellationToken token = default)
        {
            return await TryPublishAsync(msg, result, options.DefaultPublishAsyncStrategy, token);
        }

        public async UniTask<bool> TryPublishAsync(T msg, List<R> list, PublishAsyncStrategy strategy, CancellationToken token = default)
        {
            list.Clear();
            bool hasResult = false;
            if (handlers.Count <= 1 || strategy == PublishAsyncStrategy.Sequential)
            {
                foreach (var handler in handlers)
                {
                    bool success;
                    R result;
                    if (token == default)
                        (success, result) = await handler.TryHandleAsync(msg);
                    else
                        (success, result) = await handler.TryHandleAsync(msg, token);

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

        public IDisposable Subscribe(IHandler<T, R> handler, params HandlerDecorator<T, R>[] decorators)
        {
            lock (gate)
            {
                if (isDisposed)

                {
                    return options.HandlingSubscribeDisposedPolicy.Handle(nameof(Event<T, R>));
                }

                handler = handlerFactory.BuildHandler(handler, decorators);
                handlers.Add(handler);
                var subscription = new Subscription(this, handler);
                diagnosticsInfo.IncrementSubscribe(this, subscription);
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
                        _event.diagnosticsInfo.DecrementSubscribe(_event, this);
                    }
                }
            }
        }
    }
}