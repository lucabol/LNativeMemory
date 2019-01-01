using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using static System.Console;
using System.Threading;
using System.Runtime.InteropServices;

namespace LNativeMemory
{
    using System;
    using System.Diagnostics.Tracing;

    internal sealed class GcEventListener : EventListener
    {
        Action _action;
        bool _isWarm = false; // One GC is performed before starting NoGC region, must skip first one.
        bool _started = false;

        internal void Start() { _started = true; }

        internal GcEventListener(Action action)
        {
            _action = action;
        }
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)(-1));
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var eventName = eventData.EventName;
            if (_started && _isWarm && eventName == "GCStart_V2")
            {
                _action();
            }
            else if (_started && !_isWarm && eventName == "GCStart_V2")
            {
                _isWarm = true;
            }
            else
            {
                // Do nothing. It's not one of the condition above.
            }
        }
        public override void Dispose() { _action = null; base.Dispose(); }
    }

    public static class GC2
    {
        static private GcEventListener _evListener;

        public static bool TryStartNoGCRegion(long totalSize, Action actionWhenAllocatedMore)
        {

            _evListener = new GcEventListener(actionWhenAllocatedMore);
            var succeeded = GC.TryStartNoGCRegion(totalSize, false);
            _evListener.Start();
            return succeeded;
        }

        public static void EndNoGCRegion()
        {
            try
            {
                if (_evListener != null)
                {
                    _evListener.Dispose();
                    _evListener = null;
                }
                System.GC.EndNoGCRegion();
            }
            catch (InvalidOperationException)
            {
            }

        }
    }
}

class Program
{
    static void TestGC()
    {
        var sleep = 200;

        try
        {
            var triggered = false;
            var succeeded = LNativeMemory.GC2.TryStartNoGCRegion(16 * 1024 * 1024, () =>
            {
                triggered = true;
            });
            Trace.Assert(succeeded == true, "Not entered NoGC Region");
            Thread.Sleep(sleep);
            Trace.Assert(triggered == false, "Here we have not allocated anything");

            var bytes = new Byte[99];
            Thread.Sleep(sleep);
            Trace.Assert(triggered == false, "No GC should have been triggered");
        }
        finally
        {
            LNativeMemory.GC2.EndNoGCRegion();
        }

    }

    unsafe static void TestAlignAlgos()
    {
        var r = new Random();
        for (int i = 0; i < 100; i++)
        {
            var b = Marshal.AllocHGlobal(r.Next(100,500));
            Trace.Assert(0 == b.ToInt64() % 16);
        }
    }

    static void Main(string[] args)
    {
        TestAlignAlgos();
    }
}
