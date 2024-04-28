using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    // EventRequester<T, R>
    public static class UEvent<T, R>
    {
        static IEvent<T, R> _event= Events.NewEvent<T, R>();

        #region Sync

        static Tuple<Func<T, R>, IDisposable> tuple;
        static Dictionary<Func<T, R>, IDisposable> dict;

        public static void Sub(Func<T, R> handler)
        {
            var disposable = _event.Sub(handler);
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
            if (dict == null)
                return;

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

        public static bool Pub(T e, out R result)
        {
            if (_event.Pub(e, out result))
                return true;

            return false;
        }
        
        public static bool Pub(T e, List<R> results)
        {
            if (_event.Pub(e, results))
                return true;

            return false;
        }

        #endregion

        #region Async

        static Tuple<Func<T, UniTask<(bool, R)>>, IDisposable> tuple2;
        static Dictionary<Func<T, UniTask<(bool, R)>>, IDisposable> dict2;

        public static void SubTask(Func<T, UniTask<(bool, R)>> handler)
        {
            var disposable = _event.Sub(handler);
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
            if (dict2 == null)
                return;

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

        public static async UniTask<(bool, R)> PubAsync(T e)
        {
            return await _event.PubAsync(e);
        }

        public static async UniTask<bool> PubAsync(T e, List<R> results)
        {
            return await _event.PubAsync(e, results);
        }

        #endregion
    }
}