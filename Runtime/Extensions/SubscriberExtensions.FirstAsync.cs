using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniEvent.Internal;

namespace UniEvent
{
    public static partial class SubscriberExtensions
    {
        public static UniTask<T> FirstAsync<T>(this IEventBroker<T> subscriber, params BrokerHandlerDecorator<T>[] decorators)
        {
            return new UniTask<T>(new FirstHandler<T>(subscriber, decorators), 0);
        }

        public static UniTask<T> FirstAsync<T>(this IEventBroker<T> subscriber, CancellationToken token, params BrokerHandlerDecorator<T>[] decorators)
        {
            return new UniTask<T>(new FirstHandler<T>(subscriber, token, decorators), 0);
        }

        public static UniTask<T> FirstAsync<T>(this IEventBroker<T> subscriber, Func<T, bool> predicate, params BrokerHandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = (decorators.Length == 0)
                ? new[] { decorator }
                : ArrayUtil.ImmutableAdd(decorators, decorator);

            return new UniTask<T>(new FirstHandler<T>(subscriber, decorators), 0);
        }

        public static UniTask<T> FirstAsync<T>(this IEventBroker<T> subscriber, CancellationToken token, Func<T, bool> predicate, params BrokerHandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = (decorators.Length == 0)
                ? new[] { decorator }
                : ArrayUtil.ImmutableAdd(decorators, decorator);

            return new UniTask<T>(new FirstHandler<T>(subscriber, token, decorators), 0);
        }

        public static UniTask<T> FirstAsync<K, T>(this ITopicBroker<K, T> subscriber, K key, params BrokerHandlerDecorator<T>[] decorators)
        {
            return new UniTask<T>(new FirstHandler<K, T>(subscriber, key, decorators), 0);
        }

        public static UniTask<T> FirstAsync<K, T>(this ITopicBroker<K, T> subscriber, K key, CancellationToken token, params BrokerHandlerDecorator<T>[] decorators)
        {
            return new UniTask<T>(new FirstHandler<K, T>(subscriber, key, token, decorators), 0);
        }

        public static UniTask<T> FirstAsync<K, T>(this ITopicBroker<K, T> subscriber, K key, Func<T, bool> predicate, params BrokerHandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);

            return new UniTask<T>(new FirstHandler<K, T>(subscriber, key, decorators), 0);
        }

        public static UniTask<T> FirstAsync<K, T>(this ITopicBroker<K, T> subscriber, K key, CancellationToken token, Func<T, bool> predicate, params BrokerHandlerDecorator<T>[] decorators)
        {
            var decorator = new PredicateDecorator<T>(predicate);
            decorators = decorators.Length == 0 ? new[] { decorator } : ArrayUtil.ImmutableAdd(decorators, decorator);
            return new UniTask<T>(new FirstHandler<K, T>(subscriber, key, token, decorators), 0);
        }
    }

    internal sealed class FirstHandler<K, T> : IBrokerHandler<T>, IUniTaskSource<T>
    {
        public SyncType Sync { get; set; }

        int handleCalled;
        IDisposable subscription;
        CancellationToken token;
        CancellationTokenRegistration cancellationTokenRegistration;
        UniTaskCompletionSourceCore<T> core;

        static Action<object> cancelCallback = Cancel;

        public FirstHandler(ITopicBroker<K, T> subscriber, K key, BrokerHandlerDecorator<T>[] decorators)
        {
            Sync = SyncType.ASync;

            CancellationToken _token;
            if (_token.IsCancellationRequested)
            {
                core.TrySetException(new OperationCanceledException(_token));
                return;
            }

            try
            {
                subscription = subscriber.Subscribe(key, this, decorators);
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                subscription?.Dispose();
                return;
            }

            if (_token.CanBeCanceled)
            {
                token = _token;
                cancellationTokenRegistration = _token.Register(cancelCallback, this, false);
            }
        }

        public FirstHandler(ITopicBroker<K, T> subscriber, K key, CancellationToken _token, BrokerHandlerDecorator<T>[] decorators)
        {
            Sync = SyncType.ASyncCancelable;
            if (_token.IsCancellationRequested)
            {
                core.TrySetException(new OperationCanceledException(_token));
                return;
            }

            try
            {
                subscription = subscriber.Subscribe(key, this, decorators);
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                subscription?.Dispose();
                return;
            }

            if (_token.CanBeCanceled)
            {
                token = _token;
                cancellationTokenRegistration = _token.Register(cancelCallback, this, false);
            }
        }

        static void Cancel(object state)
        {
            var self = (FirstHandler<K, T>)state;
            self.subscription?.Dispose();
            self.core.TrySetException(new OperationCanceledException(self.token));
        }


        public void Handle(T message)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    core.TrySetResult(message);
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }
        }

        public UniTask HandleAsync(T message)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    // if (token.IsCancellationRequested)
                    // {
                    //     core.TrySetException(new OperationCanceledException(token));
                    // }
                    // else
                    {
                        core.TrySetResult(message);
                    }
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }

            return default;
        }


        public UniTask HandleAsync(T message, CancellationToken _token)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    if (_token.IsCancellationRequested)
                    {
                        core.TrySetException(new OperationCanceledException(_token));
                    }
                    else
                    {
                        core.TrySetResult(message);
                    }
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }

            return default;
        }

        void IUniTaskSource.GetResult(short _token) => GetResult(_token);
        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();

        public /*replaced*/ UniTaskStatus GetStatus(short _token)
        {
            return core.GetStatus(_token);
        }

        public void OnCompleted(Action<object> continuation, object state, short _token)
        {
            core.OnCompleted(continuation, state, _token);
        }

        public T GetResult(short _token)
        {
            return core.GetResult(_token);
        }
    }

    internal sealed class FirstHandler<T> : IBrokerHandler<T>, IUniTaskSource<T>
    {
        public SyncType Sync { get; set; }

        int handleCalled;
        IDisposable subscription;
        CancellationToken token;
        CancellationTokenRegistration cancellationTokenRegistration;
        UniTaskCompletionSourceCore<T> core;

        static Action<object> cancelCallback = Cancel;

        public FirstHandler(IEventBroker<T> subscriber, BrokerHandlerDecorator<T>[] decorators)
        {
            Sync = SyncType.ASync;

            CancellationToken _token = default;
            if (_token.IsCancellationRequested)
            {
                core.TrySetException(new OperationCanceledException(_token));
                return;
            }

            try
            {
                subscription = subscriber.Subscribe(this, false, decorators);
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                subscription?.Dispose();
                return;
            }

            if (_token.CanBeCanceled)
            {
                token = _token;
                cancellationTokenRegistration = _token.Register(cancelCallback, this, false);
            }
        }

        public FirstHandler(IEventBroker<T> subscriber, CancellationToken _token, BrokerHandlerDecorator<T>[] decorators)
        {
            Sync = SyncType.ASyncCancelable;

            if (_token.IsCancellationRequested)
            {
                core.TrySetException(new OperationCanceledException(_token));
                return;
            }

            try
            {
                subscription = subscriber.Subscribe(this, false, decorators);
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                subscription?.Dispose();
                return;
            }

            if (_token.CanBeCanceled)
            {
                token = _token;
                cancellationTokenRegistration = _token.Register(cancelCallback, this, false);
            }
        }

        static void Cancel(object state)
        {
            var self = (FirstHandler<T>)state;
            self.subscription?.Dispose();
            self.core.TrySetException(new OperationCanceledException(self.token));
        }

        public void Handle(T message)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    core.TrySetResult(message);
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }
        }


        public UniTask HandleAsync(T message)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    // if (token.IsCancellationRequested)
                    // {
                    //     core.TrySetException(new OperationCanceledException(token));
                    // }
                    // else
                    {
                        core.TrySetResult(message);
                    }
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }

            return default;
        }

        public UniTask HandleAsync(T message, CancellationToken token)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        core.TrySetException(new OperationCanceledException(token));
                    }
                    else
                    {
                        core.TrySetResult(message);
                    }
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }

            return default;
        }

        void IUniTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public UniTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public /*replaced*/ UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public T GetResult(short token)
        {
            return core.GetResult(token);
        }
    }
}