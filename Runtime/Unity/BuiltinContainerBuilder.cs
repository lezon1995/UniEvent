// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
//
// namespace UniEvent
// {
//     public interface IServiceCollection
//     {
//         void Add(Type serviceType, Type implementationType, InstanceLifetime lifetime);
//         void SetSingleton<T>(T instance);
//         void AddSingleton<T>();
//         void AddSingleton<T1, T2>();
//         void AddTransient(Type type);
//         void TryAddTransient<T>();
//         void TryAddTransient(Type type);
//     }
//
//     // DI Container builder.
//     public partial class BuiltinContainerBuilder : IServiceCollection
//     {
//         internal Dictionary<Type, object> singletonInstances = new Dictionary<Type, object>();
//         internal List<(Type serviceType, Type implementationType)> singleton = new List<(Type serviceType, Type implementationType)>();
//         internal List<(Type serviceType, Type implementationType)> transient = new List<(Type serviceType, Type implementationType)>();
//
//         #region IServiceCollection
//
//         public void Add(Type serviceType, Type implementationType, InstanceLifetime lifetime)
//         {
//             switch (lifetime)
//             {
//                 case InstanceLifetime.Scoped:
//                 case InstanceLifetime.Singleton:
//                     singleton.Add((serviceType, implementationType));
//                     break;
//                 case InstanceLifetime.Transient:
//                 default:
//                     transient.Add((serviceType, implementationType));
//                     break;
//             }
//         }
//
//         public void SetSingleton<T>(T instance)
//         {
//             singletonInstances[typeof(T)] = instance;
//         }
//
//         public void AddSingleton<T>()
//         {
//             var type = typeof(T);
//             singleton.Add((type, type));
//         }
//
//         public void AddSingleton<T1, T2>()
//         {
//             var serviceType = typeof(T1);
//             var implementationType = typeof(T2);
//             singleton.Add((serviceType, implementationType));
//         }
//
//         public void AddTransient(Type type)
//         {
//             transient.Add((type, type));
//         }
//
//         public void TryAddTransient<T>()
//         {
//             var type = typeof(T);
//             foreach (var (serviceType, implementationType) in transient)
//             {
//                 if (serviceType == type)
//                 {
//                     return;
//                 }
//             }
//
//             transient.Add((type, type));
//         }
//
//         public void TryAddTransient(Type type)
//         {
//             foreach (var (serviceType, implementationType) in transient)
//             {
//                 if (serviceType == type)
//                 {
//                     return;
//                 }
//             }
//
//             transient.Add((type, type));
//         }
//
//         #endregion
//     }
//
//     public partial class BuiltinContainerBuilder
//     {
//         public void AddMessagePipe(Action<Options> configure = null)
//         {
//             var options = new Options();
//             configure?.Invoke(options);
//             SetSingleton(options);
//             AddSingleton<DiagnosticsInfo>();
//             AddSingleton<AFactory>();
//             AddSingleton<HandlerFactory>();
//         }
//
//         public IServiceProvider BuildServiceProvider()
//         {
//             return new BuiltinContainerBuilderServiceProvider(this);
//         }
//     }
//
//
//     internal class BuiltinContainerBuilderServiceProvider : IServiceProvider
//     {
//         Dictionary<Type, Lazy<object>> singletonInstances;
//         Dictionary<Type, ServiceProviderType> transientTypes;
//
//         public BuiltinContainerBuilderServiceProvider(BuiltinContainerBuilder builder)
//         {
//             singletonInstances = new Dictionary<Type, Lazy<object>>(builder.singletonInstances.Count + builder.singleton.Count);
//             transientTypes = new Dictionary<Type, ServiceProviderType>(builder.transient.Count);
//
//             foreach (var (type, instance) in builder.singletonInstances)
//             {
//                 object ValueFactory()
//                 {
//                     return instance;
//                 }
//
//                 singletonInstances[type] = new Lazy<object>(ValueFactory);
//             }
//
//             foreach (var (serviceType, implementationType) in builder.singleton)
//             {
//                 object ValueFactory()
//                 {
//                     var providerType = new ServiceProviderType(implementationType);
//                     return providerType.Instantiate(this, 0);
//                 }
//
//                 singletonInstances[serviceType] = new Lazy<object>(ValueFactory); // memo: require to lazy with parameter(pass depth).
//             }
//
//             foreach (var (serviceType, implementationType) in builder.transient)
//             {
//                 transientTypes[serviceType] = new ServiceProviderType(implementationType);
//             }
//         }
//
//         object IServiceProvider.GetService(Type serviceType)
//         {
//             return GetService(serviceType, 0);
//         }
//
//         public object GetService(Type serviceType, int depth)
//         {
//             if (serviceType == typeof(IServiceProvider))
//             {
//                 // resolve self
//                 return this;
//             }
//
//             if (singletonInstances.TryGetValue(serviceType, out var lazy))
//             {
//                 // return Lazy<T>.Value
//                 return lazy.Value;
//             }
//
//             if (transientTypes.TryGetValue(serviceType, out var providerType))
//             {
//                 return providerType.Instantiate(this, depth);
//             }
//
//             return null;
//         }
//     }
//
//     class ServiceProviderType
//     {
//         Type type;
//         ConstructorInfo ctor;
//         ParameterInfo[] parameters;
//
//         public ServiceProviderType(Type _type)
//         {
//             var info = _type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
//                 .Select(x => new { ctor = x, parameters = x.GetParameters() })
//                 .OrderByDescending(x => x.parameters.Length) // MaxBy
//                 .FirstOrDefault();
//
//             if (info == null && !_type.IsValueType)
//             {
//                 throw new InvalidOperationException("ConstructorInfo is not found, is stripped? Type:" + _type.FullName);
//             }
//
//             type = _type;
//             ctor = info?.ctor;
//             parameters = info?.parameters;
//         }
//
//         public object Instantiate(BuiltinContainerBuilderServiceProvider provider, int depth)
//         {
//             if (ctor == null)
//             {
//                 return Activator.CreateInstance(type);
//             }
//
//             if (parameters.Length == 0)
//             {
//                 return ctor.Invoke(Array.Empty<object>());
//             }
//
//             if (depth > 15)
//             {
//                 throw new InvalidOperationException("Parameter too recursively: " + type.FullName);
//             }
//
//             var p = new object[parameters.Length];
//             for (var i = 0; i < p.Length; i++)
//             {
//                 p[i] = provider.GetService(parameters[i].ParameterType, depth + 1);
//             }
//
//             return ctor.Invoke(p);
//         }
//     }
// }