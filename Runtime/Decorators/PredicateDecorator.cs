using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    internal sealed class PredicateDecorator<T> : BrokerHandlerDecorator<T>
    {
        Func<T, bool> predicate;

        public PredicateDecorator(Func<T, bool> _predicate)
        {
            predicate = _predicate;
            Order = int.MinValue;
        }

        public override void Handle(T message, BrokerHandler1<T> next)
        {
            if (predicate(message))
            {
                next(message);
            }
        }

        public override UniTask HandleAsync(T message, BrokerHandler2<T> next)
        {
            if (predicate(message))
            {
                return next(message);
            }

            return default;
        }

        public override UniTask HandleAsync(T message, CancellationToken token, BrokerHandler3<T> next)
        {
            if (predicate(message))
            {
                return next(message, token);
            }

            return default;
        }
    }
}