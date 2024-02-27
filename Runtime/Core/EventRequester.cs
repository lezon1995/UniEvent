using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniEvent.Internal;

namespace UniEvent
{
    public interface IEventRequester<T, R>
    {
        bool TryPublish(T message, out R result);
        bool TryPublish(T message, List<R> results);
        
        UniTask<(bool, R)> TryPublishAsync(T message, CancellationToken token = default);
        UniTask<(bool, R)> TryPublishAsync(T message, PublishAsyncStrategy strategy, CancellationToken token = default);
        
        UniTask<bool> TryPublishAsync(T message, List<R> result, CancellationToken token = default);
        UniTask<bool> TryPublishAsync(T message, List<R> result, PublishAsyncStrategy strategy, CancellationToken token = default);
        
        IDisposable Subscribe(IRequesterHandler<T, R> handler, params RequesterHandlerDecorator<T, R>[] decorators);
    }

    internal partial class Event
    {
        
        public class Requester<T, R> : IEventRequester<T, R>, IDisposable, IHandlerHolderMarker
        {
            Options options;
            HandlerFactory handlerFactory;
            DiagnosticsInfo diagnosticsInfo;

            List<IRequesterHandler<T, R>> handlers;
            object gate;
            bool isDisposed;

            
            public Requester(Options _options, HandlerFactory _handlerFactory, DiagnosticsInfo _diagnosticsInfo)
            {
                options = _options;
                handlerFactory = _handlerFactory;
                diagnosticsInfo = _diagnosticsInfo;

                handlers = new List<IRequesterHandler<T, R>>();
                gate = new object();
            }

            public bool TryPublish(T message, out R result)
            {
                foreach (var handler in handlers)
                {
                    if (handler.TryHandle(message, out result))
                    {
                        return true;
                    }
                }

                result = default;
                return false;
            }

            public bool TryPublish(T message, List<R> results)
            {
                results.Clear();
                bool hasResult = false;
                foreach (var handler in handlers)
                {
                    if (handler.TryHandle(message, out var result))
                    {
                        results.Add(result);
                        hasResult = true;
                    }
                }

                return hasResult;
            }

            public async UniTask<(bool, R)> TryPublishAsync(T message, CancellationToken token = default)
            {
                return await TryPublishAsync(message, options.DefaultPublishAsyncStrategy, token);
            }

            public async UniTask<(bool, R)> TryPublishAsync(T message, PublishAsyncStrategy strategy, CancellationToken token = default)
            {
                if (handlers.Count <= 1 || strategy == PublishAsyncStrategy.Sequential)
                {
                    foreach (var handler in handlers)
                    {
                        bool success;
                        R result;
                        if (token == default)
                            (success, result) = await handler.TryHandleAsync(message);
                        else
                            (success, result) = await handler.TryHandleAsync(message, token);

                        if (success)
                        {
                            return (true, result);
                        }
                    }
                }
                else
                {
                    var results = await new AsyncHandlerWhenAll<T, R>(handlers, message, token);
                    if (results.Length > 0)
                    {
                        return (true, results[0]);
                    }
                }

                return (false, default);
            }

            public async UniTask<bool> TryPublishAsync(T message, List<R> result, CancellationToken token = default)
            {
                return await TryPublishAsync(message, result, options.DefaultPublishAsyncStrategy, token);
            }

            public async UniTask<bool> TryPublishAsync(T message, List<R> list, PublishAsyncStrategy strategy, CancellationToken token = default)
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
                            (success, result) = await handler.TryHandleAsync(message);
                        else
                            (success, result) = await handler.TryHandleAsync(message, token);

                        if (success)
                        {
                            list.Add(result);
                            hasResult = true;
                        }
                    }
                }
                else
                {
                    var results = await new AsyncHandlerWhenAll<T, R>(handlers, message, token);
                    list.AddRange(results);
                    hasResult = true;
                }

                return hasResult;
            }

            public IDisposable Subscribe(IRequesterHandler<T, R> handler, params RequesterHandlerDecorator<T, R>[] decorators)
            {
                lock (gate)
                {
                    if (isDisposed)

                    {
                        return options.HandlingSubscribeDisposedPolicy.Handle(nameof(Requester<T, R>));
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

                Requester<T, R> requester;
                IRequesterHandler<T, R> subscriptionKey;

                public Subscription(Requester<T, R> _requester, IRequesterHandler<T, R> _subscriptionKey)
                {
                    requester = _requester;
                    subscriptionKey = _subscriptionKey;
                }

                public void Dispose()
                {
                    if (!isDisposed)
                    {
                        isDisposed = true;
                        lock (requester.gate)
                        {
                            requester.handlers.Remove(subscriptionKey);
                            requester.diagnosticsInfo.DecrementSubscribe(requester, this);
                        }
                    }
                }
            }
        }
    }
}