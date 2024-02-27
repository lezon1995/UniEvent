using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniEvent.Internal;

namespace UniEvent
{
    public static partial class SubscriberExtensions
    {
        #region Event Broker

        // pub/sub-keyless-sync

        public static IDisposable Subscribe<T>(this IEventBroker<T> subscriber, Action<T> handler, params BrokerHandlerDecorator<T>[] decorators)
        {
            return subscriber.Subscribe(new BrokerHandler<T>(handler), false, decorators);
        }

        public static IDisposable Subscribe<T>(this IEventBroker<T> subscriber, Action<T> handler, Func<T, bool> predicate, params BrokerHandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return subscriber.Subscribe(new BrokerHandler<T>(handler), false, decorators);
        }

        // pub/sub-keyless-async

        public static IDisposable Subscribe<T>(this IEventBroker<T> subscriber, Func<T, UniTask> handler, params BrokerHandlerDecorator<T>[] decorators)
        {
            return subscriber.Subscribe(new BrokerHandler<T>(handler), false, decorators);
        }

        public static IDisposable Subscribe<T>(this IEventBroker<T> subscriber, Func<T, CancellationToken, UniTask> handler, params BrokerHandlerDecorator<T>[] decorators)
        {
            return subscriber.Subscribe(new BrokerHandler<T>(handler), false, decorators);
        }

        public static IDisposable Subscribe<T>(this IEventBroker<T> subscriber, Func<T, CancellationToken, UniTask> handler, Func<T, bool> predicate, params BrokerHandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return subscriber.Subscribe(new BrokerHandler<T>(handler), false, decorators);
        }

        #endregion

        #region Topic Broker

        // pub/sub-key-sync

        public static IDisposable Subscribe<K, T>(this ITopicBroker<K, T> subscriber, K key, Action<T> handler, params BrokerHandlerDecorator<T>[] decorator)
        {
            return subscriber.Subscribe(key, new BrokerHandler<T>(handler), decorator);
        }

        public static IDisposable Subscribe<K, T>(this ITopicBroker<K, T> subscriber, K key, Action<T> handler, Func<T, bool> predicate, params BrokerHandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return subscriber.Subscribe(key, new BrokerHandler<T>(handler), decorators);
        }

        // pub/sub-key-async

        public static IDisposable Subscribe<K, T>(this ITopicBroker<K, T> subscriber, K key, Func<T, UniTask> handler, params BrokerHandlerDecorator<T>[] decorators)
        {
            return subscriber.Subscribe(key, new BrokerHandler<T>(handler), decorators);
        }

        public static IDisposable Subscribe<K, T>(this ITopicBroker<K, T> subscriber, K key, Func<T, CancellationToken, UniTask> handler, params BrokerHandlerDecorator<T>[] decorators)
        {
            return subscriber.Subscribe(key, new BrokerHandler<T>(handler), decorators);
        }

        public static IDisposable Subscribe<K, T>(this ITopicBroker<K, T> subscriber, K key, Func<T, CancellationToken, UniTask> handler, Func<T, bool> predicate, params BrokerHandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return subscriber.Subscribe(key, new BrokerHandler<T>(handler), decorators);
        }

        #endregion
    }

    public static partial class SubscriberExtensions
    {
        #region Event Requester

        public static IDisposable Subscribe<T, R>(this IEventRequester<T, R> subscriber, Func<T, R> handler, params RequesterHandlerDecorator<T, R>[] decorators)
        {
            return subscriber.Subscribe(new RequesterHandler<T, R>(handler), decorators);
        }

        public static IDisposable Subscribe<T, R>(this IEventRequester<T, R> subscriber, Func<T, UniTask<(bool, R)>> handler, params RequesterHandlerDecorator<T, R>[] decorators)
        {
            return subscriber.Subscribe(new RequesterHandler<T, R>(handler), decorators);
        }

        public static IDisposable Subscribe<T, R>(this IEventRequester<T, R> subscriber, Func<T, CancellationToken, UniTask<(bool, R)>> handler, params RequesterHandlerDecorator<T, R>[] decorators)
        {
            return subscriber.Subscribe(new RequesterHandler<T, R>(handler), decorators);
        }

        #endregion

        #region Topic Requester

        public static IDisposable Subscribe<K, T, R>(this ITopicRequester<K, T, R> subscriber, K key, Func<T, R> handler, params RequesterHandlerDecorator<T, R>[] decorators)
        {
            return subscriber.Subscribe(key, new RequesterHandler<T, R>(handler), decorators);
        }

        public static IDisposable Subscribe<K, T, R>(this ITopicRequester<K, T, R> subscriber, K key, Func<T, UniTask<(bool, R)>> handler, params RequesterHandlerDecorator<T, R>[] decorators)
        {
            return subscriber.Subscribe(key, new RequesterHandler<T, R>(handler), decorators);
        }

        public static IDisposable Subscribe<K, T, R>(this ITopicRequester<K, T, R> subscriber, K key, Func<T, CancellationToken, UniTask<(bool, R)>> handler, params RequesterHandlerDecorator<T, R>[] decorators)
        {
            return subscriber.Subscribe(key, new RequesterHandler<T, R>(handler), decorators);
        }

        #endregion
    }


}