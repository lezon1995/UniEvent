using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public static partial class SubscriberExtensions
    {
        public static IObservable<T> AsObservable<T>(this IEvent<T> subscriber, params HandlerDecorator<T>[] decorators)
        {
            return new ObservableSubscriber<T>(subscriber, decorators);
        }

        public static IObservable<T> AsObservable<K, T>(this ITopic<K, T> subscriber, K key, params HandlerDecorator<T>[] decorators)

        {
            return new ObservableSubscriber<K, T>(key, subscriber, decorators);
        }
    }

    internal sealed class ObservableSubscriber<K, T> : IObservable<T>
    {
        K key;
        ITopic<K, T> subscriber;
        HandlerDecorator<T>[] decorators;

        public ObservableSubscriber(K _key, ITopic<K, T> _subscriber, HandlerDecorator<T>[] _decorators)
        {
            key = _key;
            subscriber = _subscriber;
            decorators = _decorators;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return subscriber.Subscribe(key, new ObserverHandler<T>(observer), decorators);
        }
    }

    internal sealed class ObservableSubscriber<T> : IObservable<T>
    {
        IEvent<T> subscriber;
        HandlerDecorator<T>[] decorators;

        public ObservableSubscriber(IEvent<T> _subscriber, HandlerDecorator<T>[] _decorators)
        {
            subscriber = _subscriber;
            decorators = _decorators;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return subscriber.Subscribe(new ObserverHandler<T>(observer), false, decorators);
        }
    }


    internal sealed class ObserverHandler<T> : IHandler<T>
    {
        public SyncType Sync { get; set; }
        IObserver<T> observer;

        public ObserverHandler(IObserver<T> _observer)
        {
            observer = _observer;
            Sync = SyncType.Sync;
        }

        public void Handle(T msg)
        {
            observer.OnNext(msg);
        }

        public UniTask HandleAsync(T msg)
        {
            throw new NotImplementedException();
        }

        public UniTask HandleAsync(T msg, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}