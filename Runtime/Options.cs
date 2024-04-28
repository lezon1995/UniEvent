using System;
using System.Collections.Generic;

namespace UniEvent
{
    public enum AsyncPubStrategy
    {
        Parallel,
        Sequential
    }

    public enum HandleDisposedStrategy
    {
        Ignore,
        Throw
    }

    internal static class HandleDisposedStrategyExtensions
    {
        public static IDisposable Handle(this HandleDisposedStrategy strategy, string name)
        {
            if (strategy == HandleDisposedStrategy.Throw)
            {
                throw new ObjectDisposedException(name);
            }

            return DisposableBag.Empty;
        }
    }

    public sealed class Options
    {
        /// <summary>AsyncPublisher.PublishAsync's concurrent strategy, default is Parallel.</summary>
        public AsyncPubStrategy DefaultStrategy { get; set; }

        /// <summary>For diagnostics usage, enable MessagePipeDiagnosticsInfo.CapturedStackTraces; default is false.</summary>
        public bool EnableCaptureStackTrace { get; set; }

        /// <summary>Choose how work on subscriber.Subscribe when after disposed, default is Ignore.</summary>
        public HandleDisposedStrategy HandleDisposedStrategy { get; set; }

        public Options()
        {
            DefaultStrategy = AsyncPubStrategy.Parallel;
            EnableCaptureStackTrace = true;
            HandleDisposedStrategy = HandleDisposedStrategy.Ignore;
        }

        Dictionary<Type, List<IMsgHandlerDecorator>> brokerDecorators = new Dictionary<Type, List<IMsgHandlerDecorator>>();

        public void AddBrokerDecorator<T>(T decorator) where T : IMsgHandlerDecorator
        {
            var type = typeof(T);

            // 获取父类的Type
            Type baseType = type.BaseType;

            if (baseType is { IsGenericType: true })
            {
                // 获取泛型类型的定义
                // 获取泛型类型参数的数组
                Type[] genericArgTypes = baseType.GetGenericArguments();

                var argType = genericArgTypes[0];

                if (!brokerDecorators.TryGetValue(argType, out var list))
                {
                    list = new List<IMsgHandlerDecorator>();
                    brokerDecorators.Add(argType, list);
                }

                list.Add(decorator);
                list.Sort((d1, d2) =>
                {
                    if (d1.Order > d2.Order) return 1;
                    if (d1.Order < d2.Order) return -1;
                    return 0;
                });
            }
        }

        internal bool TryGetBrokerDecorators<T>(out IEnumerable<IMsgHandlerDecorator> results)
        {
            if (brokerDecorators.TryGetValue(typeof(T), out var list) && list.Count > 0)
            {
                results = list;
                return true;
            }

            results = Array.Empty<IMsgHandlerDecorator>();
            return false;
        }

        Dictionary<(Type, Type), List<IReqHandlerDecorator>> requesterDecorators = new Dictionary<(Type, Type), List<IReqHandlerDecorator>>();

        public void AddRequesterDecorator<T>(T decorator) where T : IReqHandlerDecorator
        {
            var type = typeof(T);

            // 获取父类的Type
            Type baseType = type.BaseType;

            if (baseType is { IsGenericType: true })
            {
                // 获取泛型类型的定义
                // 获取泛型类型参数的数组
                Type[] genericArgTypes = baseType.GetGenericArguments();

                var argType = genericArgTypes[0];
                var returnType = genericArgTypes[1];

                var key = (argType, returnType);
                if (!requesterDecorators.TryGetValue(key, out var list))
                {
                    list = new List<IReqHandlerDecorator>();
                    requesterDecorators.Add(key, list);
                }

                list.Add(decorator);
                list.Sort((d1, d2) =>
                {
                    if (d1.Order > d2.Order) return 1;
                    if (d1.Order < d2.Order) return -1;
                    return 0;
                });
            }
        }

        internal bool TryGetRequesterDecorators<T, R>(out IEnumerable<IReqHandlerDecorator> results)
        {
            var key = (typeof(T), typeof(R));
            if (requesterDecorators.TryGetValue(key, out var list) && list.Count > 0)
            {
                results = list;
                return true;
            }

            results = Array.Empty<IReqHandlerDecorator>();
            return false;
        }
    }
}