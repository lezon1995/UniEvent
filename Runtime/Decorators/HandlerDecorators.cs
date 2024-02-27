using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public interface IHandlerDecorator
    {
        public int Order { get; set; }
    }

    public interface IBrokerHandlerDecorator : IHandlerDecorator
    {
    }

    public abstract class BrokerHandlerDecorator<T> : IBrokerHandlerDecorator
    {
        public int Order { get; set; }

        protected BrokerHandlerDecorator()
        {
        }

        protected BrokerHandlerDecorator(int order)
        {
            Order = order;
        }

        public virtual void Handle(T message, BrokerHandler1<T> next)
        {
        }

        public virtual UniTask HandleAsync(T message, BrokerHandler2<T> next)
        {
            return default;
        }

        public virtual UniTask HandleAsync(T message, CancellationToken token, BrokerHandler3<T> next)
        {
            return default;
        }
    }

    public interface IRequesterHandlerDecorator : IHandlerDecorator
    {
    }

    public abstract class RequesterHandlerDecorator<T, R> : IRequesterHandlerDecorator
    {
        public int Order { get; set; }

        protected RequesterHandlerDecorator()
        {
        }

        protected RequesterHandlerDecorator(int order)
        {
            Order = order;
        }

        public virtual bool TryHandle(T message, out R result, RequesterHandler1<T, R> next)
        {
            result = default;
            return default;
        }

        public virtual UniTask<(bool, R)> TryHandleAsync(T message, RequesterHandler2<T, R> next)
        {
            return default;
        }

        public virtual UniTask<(bool, R)> TryHandleAsync(T message, CancellationToken token, RequesterHandler3<T, R> next)
        {
            return default;
        }
    }
}