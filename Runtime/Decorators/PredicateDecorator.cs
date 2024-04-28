using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    internal sealed class PredicateDecorator<T> : HandlerDecorator<T>
    {
        Func<T, bool> predicate;

        public PredicateDecorator(Func<T, bool> _predicate)
        {
            predicate = _predicate;
            Order = int.MinValue;
        }

        public override void Handle(T msg, BrokerHandler1<T> next)
        {
            if (predicate(msg))
            {
                next(msg);
            }
        }

        public override UniTask HandleAsync(T msg, BrokerHandler2<T> next)
        {
            if (predicate(msg))
            {
                return next(msg);
            }

            return default;
        }

        public override UniTask HandleAsync(T msg, CancellationToken token, BrokerHandler3<T> next)
        {
            if (predicate(msg))
            {
                return next(msg, token);
            }

            return default;
        }
    }
}