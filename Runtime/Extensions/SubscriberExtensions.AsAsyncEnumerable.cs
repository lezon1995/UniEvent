using System;
using System.Threading;
using Cysharp.Threading.Tasks;
#if !UNITY_2018_3_OR_NEWER
using System.Threading.Channels;
#endif

namespace UniEvent
{
    public static partial class SubscriberExtensions
    {
        public static IUniTaskAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEventBroker<T> subscriber, params BrokerHandlerDecorator<T>[] decorators)
        {
            return new AsyncEnumerableAsyncSubscriber<T>(subscriber, decorators);
        }

        public static IUniTaskAsyncEnumerable<T> AsAsyncEnumerable<K, T>(this ITopicBroker<K, T> subscriber, K key, params BrokerHandlerDecorator<T>[] decorators)

        {
            return new AsyncEnumerableAsyncSubscriber<K, T>(key, subscriber, decorators);
        }
    }

    internal class AsyncEnumerableAsyncSubscriber<T> : IUniTaskAsyncEnumerable<T>
    {
        IEventBroker<T> subscriber;
        BrokerHandlerDecorator<T>[] decorators;

        public AsyncEnumerableAsyncSubscriber(IEventBroker<T> _subscriber, BrokerHandlerDecorator<T>[] _decorators)
        {
            subscriber = _subscriber;
            decorators = _decorators;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default)
        {
            var disposable = DisposableBag.CreateSingleAssignment();
            var e = new HandlerEnumerator<T>(disposable, token);
            disposable.Disposable = subscriber.Subscribe(e, false, decorators);
            return e;
        }
    }

    internal class AsyncEnumerableAsyncSubscriber<K, T> : IUniTaskAsyncEnumerable<T>
    {
        K key;
        ITopicBroker<K, T> subscriber;
        BrokerHandlerDecorator<T>[] decorators;

        public AsyncEnumerableAsyncSubscriber(K _key, ITopicBroker<K, T> _subscriber, BrokerHandlerDecorator<T>[] _decorators)
        {
            key = _key;
            subscriber = _subscriber;
            decorators = _decorators;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default)
        {
            var disposable = DisposableBag.CreateSingleAssignment();
            var e = new HandlerEnumerator<T>(disposable, token);
            disposable.Disposable = subscriber.Subscribe(key, e, decorators);
            return e;
        }
    }

    internal class HandlerEnumerator<T> : IUniTaskAsyncEnumerator<T>, IBrokerHandler<T>
    {
        public SyncType Sync { get; set; }
        Channel<T> channel;
        CancellationToken token;
        SingleAssignmentDisposable singleAssignmentDisposable;

        public HandlerEnumerator(SingleAssignmentDisposable _singleAssignmentDisposable, CancellationToken _token)
        {
            if (_token == default)
                Sync = SyncType.ASync;
            else
                Sync = SyncType.ASyncCancelable;
            
            singleAssignmentDisposable = _singleAssignmentDisposable;
            token = _token;
#if !UNITY_2018_3_OR_NEWER
            channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions()
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true
            });
#else
            channel = Channel.CreateSingleConsumerUnbounded<T>();
#endif
        }

        T IUniTaskAsyncEnumerator<T>.Current
        {
            get
            {
                if (channel.Reader.TryRead(out var msg))
                {
                    return msg;
                }

                throw new InvalidOperationException("Message is not buffered in Channel.");
            }
        }

        UniTask<bool> IUniTaskAsyncEnumerator<T>.MoveNextAsync()
        {
            return channel.Reader.WaitToReadAsync(token);
        }

        void IBrokerHandler<T>.Handle(T message)
        {
            channel.Writer.TryWrite(message);
        }

        UniTask IBrokerHandler<T>.HandleAsync(T message)
        {
            channel.Writer.TryWrite(message);
            return default;
        }

        UniTask IBrokerHandler<T>.HandleAsync(T message, CancellationToken token)
        {
            channel.Writer.TryWrite(message);
            return default;
        }

        UniTask IUniTaskAsyncDisposable.DisposeAsync()
        {
            // unsubscribe message.
            singleAssignmentDisposable.Dispose();
            return default;
        }
    }
}