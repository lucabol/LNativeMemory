using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using static System.Console;
using System.Threading;

internal sealed class GcEventListener : EventListener {
    Action _action;
    int _gcNumber = 0; // One GC is performed before starting NoGC region, must skip first one.
    bool _started = false;
    const int warmupGC = 3;

    internal void Start() { _started = true; }

    internal GcEventListener(Action action) {
        _action = action;
    }
    protected override void OnEventSourceCreated(EventSource eventSource) {
        if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime")) {
            EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)(-1));
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData) {
        var eventName = eventData.EventName;
        if (eventName.StartsWith("GC")) {
            WriteLine(eventName);
            for(int i = 0; i < eventData.PayloadNames.Count; i++) {
                WriteLine($"\t{eventData.PayloadNames[i]}\t {eventData.Payload[i]}");
            }
            WriteLine();
        }
        if (_started && eventName == "GCStart_V2" && _gcNumber < warmupGC) {
            WriteLine($"IN GCNUMBER < {warmupGC} {_gcNumber}");
            _gcNumber += 1;
        } else if (_started && eventName == "GCStart_V2" && _action != null) {
            WriteLine($"IN GCNUMBER >= {warmupGC} {_gcNumber}");
            _action();
        } else {
            // Do nothing. It's not one of the condition above.
        }
    }
    public override void Dispose() { _action = null; base.Dispose(); }
}

public static class GC2 {
    static private GcEventListener _evListener;

    public static bool TryStartNoGCRegion(long totalSize, Action actionWhenAllocatedMore) {

        _evListener = new GcEventListener(actionWhenAllocatedMore);
        var succeeded = GC.TryStartNoGCRegion(totalSize, false);
        _evListener.Start();
        return succeeded;
    }

    public static void EndNoGCRegion() {
        try {
            if (_evListener != null) {
                _evListener.Dispose();
                _evListener = null;
            }
            System.GC.EndNoGCRegion();
        } catch (InvalidOperationException) {
        }

    }
}

class Program {


    static void Main(string[] args) {

        var sleep = 1000;

        try {
            var triggered = false;
            var succeeded = GC2.TryStartNoGCRegion(32 * 1024 * 1024, () => {
                triggered = true;
            });
            Trace.Assert(succeeded == true, "Not entered NoGC Region");
            Thread.Sleep(sleep);
            Trace.Assert(triggered == false, "Here we have not allocated anything");

            var bytes = new Byte[99];
            Thread.Sleep(sleep);
            Trace.Assert(triggered ==  false, "No GC should have been triggered");
        } finally {
            GC2.EndNoGCRegion();
        }
    }
}
