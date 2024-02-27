using System;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;

namespace UniEvent.Internal
{
    internal partial class AsyncHandlerWhenAll<T>
    {
        internal class AwaiterNode : IPoolStackNode<AwaiterNode>
        {
            AwaiterNode nextNode;
            public ref AwaiterNode NextNode => ref nextNode;

            AsyncHandlerWhenAll<T> parent;
            UniTask.Awaiter awaiter;

            Action continuation;

            static PoolStack<AwaiterNode> pool;

            private AwaiterNode()
            {
                continuation = OnCompleted;
            }

            public static void RegisterUnsafeOnCompleted(AsyncHandlerWhenAll<T> parent, UniTask.Awaiter awaiter)
            {
                if (!pool.TryPop(out var result))
                {
                    result = new AwaiterNode();
                }

                result.parent = parent;
                result.awaiter = awaiter;

                result.awaiter.UnsafeOnCompleted(result.continuation);
            }

            void OnCompleted()
            {
                var p = parent;
                var a = awaiter;
                parent = null;
                awaiter = default;

                pool.TryPush(this);

                try
                {
                    a.GetResult();
                }
                catch (Exception ex)
                {
                    p.exception = ExceptionDispatchInfo.Capture(ex);
                    p.TryInvokeContinuation();
                    return;
                }

                p.IncrementSuccessfully();
            }
        }
    }

    internal partial class AsyncHandlerWhenAll<T, R>
    {
        internal class AwaiterNode : IPoolStackNode<AwaiterNode>
        {
            AwaiterNode nextNode;
            public ref AwaiterNode NextNode => ref nextNode;

            AsyncHandlerWhenAll<T, R> parent;
            UniTask<(bool, R)>.Awaiter awaiter;
            int index = -1;

            Action continuation;

            static PoolStack<AwaiterNode> pool;

            private AwaiterNode()
            {
                continuation = OnCompleted;
            }

            public static void RegisterUnsafeOnCompleted(AsyncHandlerWhenAll<T, R> parent, UniTask<(bool, R)>.Awaiter awaiter, int index)
            {
                if (!pool.TryPop(out var result))
                {
                    result = new AwaiterNode();
                }

                result.parent = parent;
                result.awaiter = awaiter;
                result.index = index;

                result.awaiter.UnsafeOnCompleted(result.continuation);
            }

            void OnCompleted()
            {
                var p = parent;
                var a = awaiter;
                var i = index;
                parent = null;
                awaiter = default;
                index = -1;

                pool.TryPush(this);

                try
                {
                    var (success, result) = a.GetResult();
                    if (success)
                    {
                        p.results[i] = result;
                    }
                }
                catch (Exception ex)
                {
                    p.exception = ExceptionDispatchInfo.Capture(ex);
                    p.TryInvokeContinuation();
                    return;
                }

                p.IncrementSuccessfully();
            }
        }
    }
}