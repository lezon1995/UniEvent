using System;
using System.Collections.Generic;

namespace UniEvent
{
    public enum PublishAsyncStrategy
    {
        Parallel,
        Sequential
    }

    public enum HandlingSubscribeDisposedPolicy
    {
        Ignore,
        Throw
    }

    internal static class HandlingSubscribeDisposedPolicyExtensions
    {
        public static IDisposable Handle(this HandlingSubscribeDisposedPolicy policy, string name)
        {
            if (policy == HandlingSubscribeDisposedPolicy.Throw)
            {
                throw new ObjectDisposedException(name);
            }

            return DisposableBag.Empty;
        }
    }

    public sealed class Options
    {
        /// <summary>AsyncPublisher.PublishAsync's concurrent strategy, default is Parallel.</summary>
        public PublishAsyncStrategy DefaultPublishAsyncStrategy { get; set; }

        /// <summary>For diagnostics usage, enable MessagePipeDiagnosticsInfo.CapturedStackTraces; default is false.</summary>
        public bool EnableCaptureStackTrace { get; set; }

        /// <summary>Choose how work on subscriber.Subscribe when after disposed, default is Ignore.</summary>
        public HandlingSubscribeDisposedPolicy HandlingSubscribeDisposedPolicy { get; set; }

        public Options()
        {
            DefaultPublishAsyncStrategy = PublishAsyncStrategy.Parallel;
            EnableCaptureStackTrace = true;
            HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore;
        }

        Dictionary<Type, List<IBrokerHandlerDecorator>> brokerDecorators = new Dictionary<Type, List<IBrokerHandlerDecorator>>();

        public void AddBrokerDecorator<T>(T decorator) where T : IBrokerHandlerDecorator
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
                    list = new List<IBrokerHandlerDecorator>();
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

        internal bool TryGetBrokerDecorators<T>(out IEnumerable<IBrokerHandlerDecorator> results)
        {
            if (brokerDecorators.TryGetValue(typeof(T), out var list) && list.Count > 0)
            {
                results = list;
                return true;
            }

            results = Array.Empty<IBrokerHandlerDecorator>();
            return false;
        }

        Dictionary<(Type, Type), List<IRequesterHandlerDecorator>> requesterDecorators = new Dictionary<(Type, Type), List<IRequesterHandlerDecorator>>();

        public void AddRequesterDecorator<T>(T decorator) where T : IRequesterHandlerDecorator
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
                    list = new List<IRequesterHandlerDecorator>();
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

        internal bool TryGetRequesterDecorators<T, R>(out IEnumerable<IRequesterHandlerDecorator> results)
        {
            var key = (typeof(T), typeof(R));
            if (requesterDecorators.TryGetValue(key, out var list) && list.Count > 0)
            {
                results = list;
                return true;
            }

            results = Array.Empty<IRequesterHandlerDecorator>();
            return false;
        }
    }
}