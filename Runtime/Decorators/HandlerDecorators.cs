using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    public interface IHandlerDecorator
    {
        public int Order { get; set; }
    }

    public interface IMsgHandlerDecorator : IHandlerDecorator
    {
    }

    public abstract class HandlerDecorator<T> : IMsgHandlerDecorator
    {
        public int Order { get; set; }

        protected HandlerDecorator()
        {
        }

        protected HandlerDecorator(int order)
        {
            Order = order;
        }

        public virtual void Handle(T msg, BrokerHandler1<T> next)
        {
        }

        public virtual UniTask HandleAsync(T msg, BrokerHandler2<T> next)
        {
            return default;
        }

        public virtual UniTask HandleAsync(T msg, CancellationToken token, BrokerHandler3<T> next)
        {
            return default;
        }
    }

    public interface IReqHandlerDecorator : IHandlerDecorator
    {
    }

    public abstract class HandlerDecorator<T, R> : IReqHandlerDecorator
    {
        public int Order { get; set; }

        protected HandlerDecorator()
        {
        }

        protected HandlerDecorator(int order)
        {
            Order = order;
        }

        public virtual bool TryHandle(T msg, out R result, RequesterHandler1<T, R> next)
        {
            result = default;
            return default;
        }

        public virtual UniTask<(bool, R)> TryHandleAsync(T msg, RequesterHandler2<T, R> next)
        {
            return default;
        }

        public virtual UniTask<(bool, R)> TryHandleAsync(T msg, CancellationToken token, RequesterHandler3<T, R> next)
        {
            return default;
        }
    }
}