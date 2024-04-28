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
        public static IUniTaskAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEvent<T> subscriber, params HandlerDecorator<T>[] decorators)
        {
            return new AsyncEnumerableAsyncSubscriber<T>(subscriber, decorators);
        }

        public static IUniTaskAsyncEnumerable<T> AsAsyncEnumerable<K, T>(this ITopic<K, T> subscriber, K key, params HandlerDecorator<T>[] decorators)

        {
            return new AsyncEnumerableAsyncSubscriber<K, T>(key, subscriber, decorators);
        }
    }

    internal class AsyncEnumerableAsyncSubscriber<T> : IUniTaskAsyncEnumerable<T>
    {
        IEvent<T> subscriber;
        HandlerDecorator<T>[] decorators;

        public AsyncEnumerableAsyncSubscriber(IEvent<T> _subscriber, HandlerDecorator<T>[] _decorators)
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
        ITopic<K, T> subscriber;
        HandlerDecorator<T>[] decorators;

        public AsyncEnumerableAsyncSubscriber(K _key, ITopic<K, T> _subscriber, HandlerDecorator<T>[] _decorators)
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

    internal class HandlerEnumerator<T> : IUniTaskAsyncEnumerator<T>, IHandler<T>
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

        void IHandler<T>.Handle(T msg)
        {
            channel.Writer.TryWrite(msg);
        }

        UniTask IHandler<T>.HandleAsync(T msg)
        {
            channel.Writer.TryWrite(msg);
            return default;
        }

        UniTask IHandler<T>.HandleAsync(T msg, CancellationToken token)
        {
            channel.Writer.TryWrite(msg);
            return default;
        }

        UniTask IUniTaskAsyncDisposable.DisposeAsync()
        {
            // unsubscribe msg.
            singleAssignmentDisposable.Dispose();
            return default;
        }
    }
}