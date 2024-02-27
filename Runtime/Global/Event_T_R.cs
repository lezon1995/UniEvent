using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    // EventRequester<T, R>
    public static class Event<T, R>
    {
        static IEventRequester<T, R> requester;

        #region Sync

        static Tuple<Func<T, R>, IDisposable> tuple;
        static Dictionary<Func<T, R>, IDisposable> dict;

        public static void Sub(Func<T, R> handler)
        {
            requester ??= Events.EventRequester<T, R>();
            var disposable = requester.Subscribe(handler);
            if (tuple == null)
            {
                if (dict == null)
                {
                    tuple = new Tuple<Func<T, R>, IDisposable>(handler, disposable);
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
                    dict = new Dictionary<Func<T, R>, IDisposable>();
                    var (handler0, disposable0) = tuple;
                    dict[handler0] = disposable0;
                    dict[handler] = disposable;
                    tuple = null;
                }
            }
        }

        public static void UnSub(Func<T, R> handler)
        {
            if (requester == null || dict == null)
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

        public static bool TryPub(T e, out R result)
        {
            requester ??= Events.EventRequester<T, R>();
            if (requester.TryPublish(e, out result))
            {
                return true;
            }

            return false;
        }
        
        public static bool TryPub(T e, List<R> results)
        {
            requester ??= Events.EventRequester<T, R>();
            if (requester.TryPublish(e, results))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Async

        static Tuple<Func<T, UniTask<(bool, R)>>, IDisposable> tuple2;
        static Dictionary<Func<T, UniTask<(bool, R)>>, IDisposable> dict2;

        public static void SubTask(Func<T, UniTask<(bool, R)>> handler)
        {
            requester ??= Events.EventRequester<T, R>();
            var disposable = requester.Subscribe(handler);
            if (tuple2 == null)
            {
                if (dict2 == null)
                {
                    tuple2 = new Tuple<Func<T, UniTask<(bool, R)>>, IDisposable>(handler, disposable);
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
                    dict2 = new Dictionary<Func<T, UniTask<(bool, R)>>, IDisposable>();
                    var (handler0, disposable0) = tuple2;
                    dict2[handler0] = disposable0;
                    dict2[handler] = disposable;
                    tuple2 = null;
                }
            }
        }

        public static void UnSubTask(Func<T, UniTask<(bool, R)>> handler)
        {
            if (requester == null || dict2 == null)
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

        public static async UniTask<(bool, R)> TryPubAsync(T e)
        {
            requester ??= Events.EventRequester<T, R>();
            return await requester.TryPublishAsync(e);
        }

        public static async UniTask<bool> TryPubAsync(T e, List<R> results)
        {
            requester ??= Events.EventRequester<T, R>();
            return await requester.TryPublishAsync(e, results);
        }

        #endregion
    }
}