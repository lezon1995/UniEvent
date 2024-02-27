using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public static partial class SubscriberExtensions
    {
        public static IObservable<T> AsObservable<T>(this IEventBroker<T> subscriber, params BrokerHandlerDecorator<T>[] decorators)
        {
            return new ObservableSubscriber<T>(subscriber, decorators);
        }

        public static IObservable<T> AsObservable<K, T>(this ITopicBroker<K, T> subscriber, K key, params BrokerHandlerDecorator<T>[] decorators)

        {
            return new ObservableSubscriber<K, T>(key, subscriber, decorators);
        }
    }

    internal sealed class ObservableSubscriber<K, T> : IObservable<T>
    {
        K key;
        ITopicBroker<K, T> subscriber;
        BrokerHandlerDecorator<T>[] decorators;

        public ObservableSubscriber(K _key, ITopicBroker<K, T> _subscriber, BrokerHandlerDecorator<T>[] _decorators)
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
        IEventBroker<T> subscriber;
        BrokerHandlerDecorator<T>[] decorators;

        public ObservableSubscriber(IEventBroker<T> _subscriber, BrokerHandlerDecorator<T>[] _decorators)
        {
            subscriber = _subscriber;
            decorators = _decorators;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return subscriber.Subscribe(new ObserverHandler<T>(observer), false, decorators);
        }
    }


    internal sealed class ObserverHandler<T> : IBrokerHandler<T>
    {
        public SyncType Sync { get; set; }
        IObserver<T> observer;

        public ObserverHandler(IObserver<T> _observer)
        {
            observer = _observer;
            Sync = SyncType.Sync;
        }

        public void Handle(T message)
        {
            observer.OnNext(message);
        }

        public UniTask HandleAsync(T message)
        {
            throw new NotImplementedException();
        }

        public UniTask HandleAsync(T message, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}