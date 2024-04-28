using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UniEvent
{
    // TopicRequester<string, T, R>
    public static class UTopic<T, R>
    {
        static ITopic<string, T, R> _topic = Events.NewTopic<string, T, R>();

        #region Sync

        static Tuple<Key, IDisposable> tuple;
        static Dictionary<Key, IDisposable> dict;

        public static bool TryPub(string topic, T e, out R result)
        {
            if (_topic.TryPublish(topic, e, out result))
                return true;

            return false;
        }

        public static void Sub(string topic, Func<T, R> handler)
        {
            var disposable = _topic.Subscribe(topic, handler);
            var key = new Key(topic, handler);
            if (tuple == null)
            {
                if (dict == null)
                {
                    tuple = new Tuple<Key, IDisposable>(key, disposable);
                }
                else
                {
                    dict[key] = disposable;
                }
            }
            else
            {
                if (dict == null)
                {
                    dict = new Dictionary<Key, IDisposable>();
                    var (key0, disposable0) = tuple;
                    dict[key0] = disposable0;
                    dict[key] = disposable;
                    tuple = null;
                }
            }
        }

        public static void UnSub(string topic, Func<T, R> handler)
        {
            if (dict == null)
                return;

            var key = new Key(topic, handler);
            if (tuple == null)
            {
                if (dict.Remove(key, out var disposable))
                {
                    disposable.Dispose();
                }
            }
            else
            {
                var (key0, disposable0) = tuple;
                if (key == key0)
                {
                    disposable0.Dispose();
                    tuple = null;
                }
            }
        }

        #endregion

        #region Async

        static Tuple<Key2, IDisposable> tuple2;
        static Dictionary<Key2, IDisposable> dict2;

        public static void SubTask(string topic, Func<T, UniTask<(bool, R)>> handler)
        {
            var disposable = _topic.Subscribe(topic, handler);
            var key = new Key2(topic, handler);
            if (tuple2 == null)
            {
                if (dict2 == null)
                {
                    tuple2 = new Tuple<Key2, IDisposable>(key, disposable);
                }
                else
                {
                    dict2[key] = disposable;
                }
            }
            else
            {
                if (dict2 == null)
                {
                    dict2 = new Dictionary<Key2, IDisposable>();
                    var (key0, disposable0) = tuple2;
                    dict2[key0] = disposable0;
                    dict2[key] = disposable;
                    tuple2 = null;
                }
            }
        }

        public static void UnSubTask(string topic, Func<T, UniTask<(bool, R)>> handler)
        {
            if (dict2 == null)
                return;

            var key = new Key2(topic, handler);
            if (tuple2 == null)
            {
                if (dict2.Remove(key, out var disposable))
                {
                    disposable.Dispose();
                }
            }
            else
            {
                var (key0, disposable0) = tuple2;
                if (key == key0)
                {
                    disposable0.Dispose();
                    tuple2 = null;
                }
            }
        }

        public static async UniTask<(bool, R)> TryPubAsync(string topic, T e)
        {
            return await _topic.TryPublishAsync(topic, e);
        }

        public static async UniTask<bool> TryPubAsync(string topic, T e, List<R> results)
        {
            return await _topic.TryPublishAsync(topic, e, results);
        }

        #endregion

        struct Key : IEquatable<Key>
        {
            string topic;
            Func<T, R> handler;

            public Key(string _topic, Func<T, R> _handler)
            {
                topic = _topic;
                handler = _handler;
            }

            public bool Equals(Key other)
            {
                return topic == other.topic && Equals(handler, other.handler);
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(topic, handler);
            }

            public static bool operator ==(Key a, Key b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(Key a, Key b)
            {
                return !(a == b);
            }
        }

        struct Key2 : IEquatable<Key2>
        {
            string topic;
            Func<T, UniTask<(bool, R)>> handler;

            public Key2(string _topic, Func<T, UniTask<(bool, R)>> _handler)
            {
                topic = _topic;
                handler = _handler;
            }

            public bool Equals(Key2 other)
            {
                return topic == other.topic && Equals(handler, other.handler);
            }

            public override bool Equals(object obj)
            {
                return obj is Key2 other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(topic, handler);
            }

            public static bool operator ==(Key2 a, Key2 b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(Key2 a, Key2 b)
            {
                return !(a == b);
            }
        }
    }
}