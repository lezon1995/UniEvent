using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniEvent.Internal;

namespace UniEvent
{
    public interface ITopicBroker<in K, T>
    {
        void Publish(K key, T message);
        
        UniTask PublishAsync(K key, T message, CancellationToken token = default);
        UniTask PublishAsync(K key, T message, PublishAsyncStrategy strategy, CancellationToken token = default);
        
        IDisposable Subscribe(K key, IBrokerHandler<T> handler, params BrokerHandlerDecorator<T>[] decorators);
    }

    internal partial class Topic
    {
        
        public class Broker<K, T> : ITopicBroker<K, T>, IDisposable
        {
            Options options;
            HandlerFactory handlerFactory;
            DiagnosticsInfo diagnosticsInfo;

            Dictionary<K, HandlerHolder> handlerGroup;
            object gate;
            bool isDisposed;

            
            public Broker(Options _options, HandlerFactory _handlerFactory, DiagnosticsInfo _diagnosticsInfo)
            {
                options = _options;
                handlerFactory = _handlerFactory;
                diagnosticsInfo = _diagnosticsInfo;

                handlerGroup = new Dictionary<K, HandlerHolder>();
                gate = new object();
            }

            public void Publish(K key, T message)
            {
                List<IBrokerHandler<T>> handlers;
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
                    handler.Handle(message);
                    handler.HandleAsync(message).Forget();
                    handler.HandleAsync(message, default).Forget();
                }
            }

            public UniTask PublishAsync(K key, T message, CancellationToken token)
            {
                return PublishAsync(key, message, options.DefaultPublishAsyncStrategy, token);
            }

            public async UniTask PublishAsync(K key, T message, PublishAsyncStrategy strategy, CancellationToken token)
            {
                List<IBrokerHandler<T>> handlers;
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

                if (count <= 1 || strategy == PublishAsyncStrategy.Sequential)
                {
                    foreach (var handler in handlers)
                    {
                        if (token == default)
                            await handler.HandleAsync(message);
                        else
                            await handler.HandleAsync(message, token);
                    }
                }
                else
                {
                    await new AsyncHandlerWhenAll<T>(handlers, message, token);
                }
            }

            public IDisposable Subscribe(K key, IBrokerHandler<T> handler, params BrokerHandlerDecorator<T>[] decorators)
            {
                lock (gate)
                {
                    if (isDisposed)
                    {
                        return options.HandlingSubscribeDisposedPolicy.Handle(nameof(Broker<K, T>));
                    }

                    if (!handlerGroup.TryGetValue(key, out var holder))
                    {
                        handlerGroup[key] = holder = new HandlerHolder(this);
                    }

                    handler = handlerFactory.BuildHandler(handler, decorators);

                    return holder.Subscribe(key, handler);
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
            sealed class HandlerHolder : IDisposable, IHandlerHolderMarker
            {
                List<IBrokerHandler<T>> handlers;
                Broker<K, T> broker;

                public HandlerHolder(Broker<K, T> _broker)
                {
                    handlers = new List<IBrokerHandler<T>>();
                    broker = _broker;
                }

                public List<IBrokerHandler<T>> GetHandlers()
                {
                    return handlers;
                }

                public int GetCount()
                {
                    return handlers.Count;
                }

                public IDisposable Subscribe(K key, IBrokerHandler<T> handler)
                {
                    handlers.Add(handler);
                    var subscription = new Subscription(key, handler, this);
                    broker.diagnosticsInfo.IncrementSubscribe(this, subscription);
                    return subscription;
                }

                public void Dispose()
                {
                    lock (broker.gate)
                    {
                        var count = handlers.Count;
                        handlers.Clear();
                        broker.diagnosticsInfo.RemoveTargetDiagnostics(this, count);
                    }
                }

                sealed class Subscription : IDisposable
                {
                    bool isDisposed;
                    K key;
                    IBrokerHandler<T> subscriptionKey;
                    HandlerHolder holder;

                    public Subscription(K _key, IBrokerHandler<T> _subscriptionKey, HandlerHolder _holder)
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
                            lock (holder.broker.gate)
                            {
                                if (!holder.broker.isDisposed)
                                {
                                    holder.handlers.Remove(subscriptionKey);
                                    holder.broker.diagnosticsInfo.DecrementSubscribe(holder, this);
                                    if (holder.handlers.Count == 0)
                                    {
                                        holder.broker.handlerGroup.Remove(key);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}