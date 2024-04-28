using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniEvent.Internal;

namespace UniEvent
{
    public static partial class SubExtensions
    {
        #region Event

        // pub/sub-keyless-sync

        public static IDisposable Sub<T>(this IEvent<T> _event, Action<T> handler, params HandlerDecorator<T>[] decorators)
        {
            return _event.Sub(new Handler<T>(handler), false, decorators);
        }

        public static IDisposable Sub<T>(this IEvent<T> _event, Action<T> handler, Func<T, bool> predicate, params HandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return _event.Sub(new Handler<T>(handler), false, decorators);
        }

        // pub/sub-keyless-async

        public static IDisposable Sub<T>(this IEvent<T> _event, Func<T, UniTask> handler, params HandlerDecorator<T>[] decorators)
        {
            return _event.Sub(new Handler<T>(handler), false, decorators);
        }

        public static IDisposable Sub<T>(this IEvent<T> _event, Func<T, CancellationToken, UniTask> handler, params HandlerDecorator<T>[] decorators)
        {
            return _event.Sub(new Handler<T>(handler), false, decorators);
        }

        public static IDisposable Sub<T>(this IEvent<T> _event, Func<T, CancellationToken, UniTask> handler, Func<T, bool> predicate, params HandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return _event.Sub(new Handler<T>(handler), false, decorators);
        }

        #endregion

        #region Event Requester

        public static IDisposable Sub<T, R>(this IEvent<T, R> _event, Func<T, R> handler, params HandlerDecorator<T, R>[] decorators)
        {
            return _event.Sub(new Handler<T, R>(handler), decorators);
        }

        public static IDisposable Sub<T, R>(this IEvent<T, R> _event, Func<T, UniTask<(bool, R)>> handler, params HandlerDecorator<T, R>[] decorators)
        {
            return _event.Sub(new Handler<T, R>(handler), decorators);
        }

        public static IDisposable Sub<T, R>(this IEvent<T, R> _event, Func<T, CancellationToken, UniTask<(bool, R)>> handler, params HandlerDecorator<T, R>[] decorators)
        {
            return _event.Sub(new Handler<T, R>(handler), decorators);
        }

        #endregion
    }

    public static partial class SubExtensions
    {
        #region Topic

        // pub/sub-key-sync

        public static IDisposable Sub<K, T>(this ITopic<K, T> topic, K key, Action<T> handler, params HandlerDecorator<T>[] decorator)
        {
            return topic.Sub(key, new Handler<T>(handler), decorator);
        }

        public static IDisposable Sub<K, T>(this ITopic<K, T> topic, K key, Action<T> handler, Func<T, bool> predicate, params HandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return topic.Sub(key, new Handler<T>(handler), decorators);
        }

        // pub/sub-key-async

        public static IDisposable Sub<K, T>(this ITopic<K, T> topic, K key, Func<T, UniTask> handler, params HandlerDecorator<T>[] decorators)
        {
            return topic.Sub(key, new Handler<T>(handler), decorators);
        }

        public static IDisposable Sub<K, T>(this ITopic<K, T> topic, K key, Func<T, CancellationToken, UniTask> handler, params HandlerDecorator<T>[] decorators)
        {
            return topic.Sub(key, new Handler<T>(handler), decorators);
        }

        public static IDisposable Sub<K, T>(this ITopic<K, T> topic, K key, Func<T, CancellationToken, UniTask> handler, Func<T, bool> predicate, params HandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return topic.Sub(key, new Handler<T>(handler), decorators);
        }

        #endregion

        #region Topic Requester

        public static IDisposable Sub<K, T, R>(this ITopic<K, T, R> topic, K key, Func<T, R> handler, params HandlerDecorator<T, R>[] decorators)
        {
            return topic.Sub(key, new Handler<T, R>(handler), decorators);
        }

        public static IDisposable Sub<K, T, R>(this ITopic<K, T, R> topic, K key, Func<T, UniTask<(bool, R)>> handler, params HandlerDecorator<T, R>[] decorators)
        {
            return topic.Sub(key, new Handler<T, R>(handler), decorators);
        }

        public static IDisposable Sub<K, T, R>(this ITopic<K, T, R> topic, K key, Func<T, CancellationToken, UniTask<(bool, R)>> handler, params HandlerDecorator<T, R>[] decorators)
        {
            return topic.Sub(key, new Handler<T, R>(handler), decorators);
        }

        #endregion
    }
}