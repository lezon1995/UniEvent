using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using UniEvent.Internal;

namespace UniEvent
{
    internal interface IHandlerMarker
    {
    }

    public class StackTraceInfo
    {
        static bool displayFileNames = true;
        static int idSeed;

        public int Id { get; }
        public DateTimeOffset Timestamp { get; }
        public StackTrace StackTrace { get; }
        public string Head { get; }

        // cache field for internal use(Unity Editor, etc...)
        internal string formattedStackTrace = default;

        public StackTraceInfo(StackTrace stackTrace)
        {
            Id = Interlocked.Increment(ref idSeed);
            Timestamp = DateTimeOffset.UtcNow;
            StackTrace = stackTrace;
            Head = GetGroupKey(stackTrace);
        }

        private static string GetGroupKey(StackTrace stackTrace)
        {
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                if (frame == null)
                {
                    continue;
                }

                var method = frame.GetMethod();
                if (method == null)
                {
                    continue;
                }

                if (method.DeclaringType == null)
                {
                    continue;
                }

                if (method.DeclaringType.Namespace == null || !method.DeclaringType.Namespace.StartsWith("UniEvent"))
                {
                    if (displayFileNames && frame.GetILOffset() != -1)
                    {
                        string fileName = null;
                        try
                        {
                            fileName = frame.GetFileName();
                        }
                        catch (NotSupportedException)
                        {
                            displayFileNames = false;
                        }
                        catch (SecurityException)
                        {
                            displayFileNames = false;
                        }

                        if (fileName != null)
                        {
                            return method.DeclaringType.FullName + "." + method.Name + " (at " + Path.GetFileName(fileName) + ":" + frame.GetFileLineNumber() + ")";
                        }

                        return method.DeclaringType.FullName + "." + method.Name + " (offset: " + frame.GetILOffset() + ")";
                    }

                    return method.DeclaringType.FullName + "." + method.Name;
                }
            }

            return "";
        }
    }

    /// <summary>
    /// Diagnostics info of in-memory(ISubscriber/IAsyncSubscriber) subscriptions.
    /// </summary>
    
    public sealed class DiagnosticsInfo
    {
        static ILookup<string, StackTraceInfo> EmptyLookup = Array.Empty<StackTraceInfo>().ToLookup(_ => "", x => x);

        int subCount;
        bool dirty;

        object gate = new object();
        Dictionary<IHandlerMarker, Dictionary<IDisposable, StackTraceInfo>> capturedStackTraces = new Dictionary<IHandlerMarker, Dictionary<IDisposable, StackTraceInfo>>();
        public int SubCount => subCount;
        internal Options Options { get; }

        internal bool CheckAndResetDirty()
        {
            var d = dirty;
            dirty = false;
            return d;
        }

        /// <summary>
        /// When Options.EnableCaptureStackTrace is enabled, list all stacktrace on subscribe.
        /// </summary>
        public StackTraceInfo[] GetCapturedStackTraces(bool ascending = true)
        {
            if (Options.EnableCaptureStackTrace)
            {
                lock (gate)
                {
                    var iter = capturedStackTraces.SelectMany(x => x.Value.Values);
                    iter = ascending ? iter.OrderBy(x => x.Id) : iter.OrderByDescending(x => x.Id);
                    return iter.ToArray();
                }
            }

            return Array.Empty<StackTraceInfo>();
        }

        /// <summary>
        /// When Options.EnableCaptureStackTrace is enabled, groped by caller of subscribe.
        /// </summary>
        public ILookup<string, StackTraceInfo> GetGroupedByCaller(bool ascending = true)
        {
            if (Options.EnableCaptureStackTrace)
            {
                lock (gate)
                {
                    var iter = capturedStackTraces.SelectMany(x => x.Value.Values);
                    iter = ascending ? iter.OrderBy(x => x.Id) : iter.OrderByDescending(x => x.Id);
                    return iter.ToLookup(x => x.Head);
                }
            }

            return EmptyLookup;
        }

        
        public DiagnosticsInfo(Options _options)
        {
            Options = _options;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementSub(IHandlerMarker marker, IDisposable subscription)
        {
            Interlocked.Increment(ref subCount);
            if (Options.EnableCaptureStackTrace)
            {
                AddStackTrace(marker, subscription);
            }

            dirty = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void AddStackTrace(IHandlerMarker marker, IDisposable subscription)
        {
            lock (gate)
            {
                if (!capturedStackTraces.TryGetValue(marker, out var dict))
                {
                    dict = new Dictionary<IDisposable, StackTraceInfo>();
                    capturedStackTraces[marker] = dict;
                }

                dict.Add(subscription, new StackTraceInfo(new StackTrace(true)));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementSub(IHandlerMarker marker, IDisposable subscription)
        {
            Interlocked.Decrement(ref subCount);
            if (Options.EnableCaptureStackTrace)
            {
                RemoveStackTrace(marker, subscription);
            }

            dirty = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void RemoveStackTrace(IHandlerMarker marker, IDisposable subscription)
        {
            lock (gate)
            {
                if (capturedStackTraces.TryGetValue(marker, out var dict))
                {
                    dict.Remove(subscription);
                }
            }
        }

        internal void RemoveTargetDiagnostics(IHandlerMarker target, int removeCount)
        {
            Interlocked.Add(ref subCount, -removeCount);
            if (Options.EnableCaptureStackTrace)
            {
                lock (gate)
                {
                    capturedStackTraces.Remove(target);
                }
            }

            dirty = true;
        }
    }
}