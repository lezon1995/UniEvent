using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    // EventBroker<T>
    public static class Event<T>
    {
        static IEventBroker<T> broker;

        #region Sync

        static Tuple<Action<T>, IDisposable> tuple;
        static Dictionary<Action<T>, IDisposable> dict;

        public static void Sub(Action<T> handler)
        {
            broker ??= Events.EventBroker<T>();
            var disposable = broker.Subscribe(handler);
            if (tuple == null)
            {
                if (dict == null)
                {
                    tuple = new Tuple<Action<T>, IDisposable>(handler, disposable);
                }
                else
                {
                    dict[handler] = disposable;
                }
            }
            else
            {
                if (dict == null)
                {
                    dict = new Dictionary<Action<T>, IDisposable>();
                    var (handler0, disposable0) = tuple;
                    dict[handler0] = disposable0;
                    dict[handler] = disposable;
                    tuple = null;
                }
            }
        }

        public static void UnSub(Action<T> handler)
        {
            if (broker == null || dict == null)
            {
                return;
            }

            if (tuple == null)
            {
                if (dict.Remove(handler, out var disposable))
                {
                    disposable.Dispose();
                }
            }
            else
            {
                var (handler0, disposable0) = tuple;
                if (handler == handler0)
                {
                    disposable0.Dispose();
                    tuple = null;
                }
            }
        }

        public static void Pub(T e, bool buffered = false)
        {
            broker ??= Events.EventBroker<T>();
            broker.Publish(e, buffered);
        }

        #endregion

        #region Async

        static Tuple<Func<T, UniTask>, IDisposable> tuple2;
        static Dictionary<Func<T, UniTask>, IDisposable> dict2;

        public static void SubTask(Func<T, UniTask> handler)
        {
            broker ??= Events.EventBroker<T>();
            var disposable = broker.Subscribe(handler);
            if (tuple2 == null)
            {
                if (dict2 == null)
                {
                    tuple2 = new Tuple<Func<T, UniTask>, IDisposable>(handler, disposable);
                }
                else
                {
                    dict2[handler] = disposable;
                }
            }
            else
            {
                if (dict2 == null)
                {
                    dict2 = new Dictionary<Func<T, UniTask>, IDisposable>();
                    var (handler0, disposable0) = tuple2;
                    dict2[handler0] = disposable0;
                    dict2[handler] = disposable;
                    tuple2 = null;
                }
            }
        }

        public static void UnSubTask(Func<T, UniTask> handler)
        {
            if (broker == null || dict2 == null)
            {
                return;
            }

            if (tuple2 == null)
            {
                if (dict2.Remove(handler, out var disposable))
                {
                    disposable.Dispose();
                }
            }
            else
            {
                var (handler0, disposable0) = tuple2;
                if (handler == handler0)
                {
                    disposable0.Dispose();
                    tuple2 = null;
                }
            }
        }

        public static async UniTask PubAsync(T e, bool buffered = false)
        {
            broker ??= Events.EventBroker<T>();
            await broker.PublishAsync(e, buffered);
        }

        #endregion
    }
}