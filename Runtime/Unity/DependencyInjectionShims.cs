using System;

namespace UniEvent
{
    public static class DependencyInjectionShims
    {
        public static T GetService<T>(this IServiceProvider provider)
        {
            var service = provider.GetService(typeof(T));
            if (service == null)
            {
                throw new InvalidOperationException($"{typeof(T).FullName} is not registered.");
            }

            return (T)service;
        }

        public static T GetService<T>(this IServiceProvider provider, Type type)
        {
            var service = provider.GetService(type);
            if (service == null)
            {
                throw new InvalidOperationException($"{type.FullName} is not registered.");
            }

            return (T)service;
        }
    }

}