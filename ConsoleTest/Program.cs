using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;

internal sealed class GcEventListener : EventListener {

    private bool _startTracking = false;

    protected override void OnEventSourceCreated(EventSource eventSource) {
        if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime")) {
            EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)(-1));
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData) {
        if (eventData.EventName.StartsWith("GC") && _startTracking) {
            Console.WriteLine(eventData.EventName);
            for (int i = 0; i < eventData.Payload.Count; i++) {
                Console.WriteLine($"{eventData.PayloadNames[i]}\t{eventData.Payload[i]}");
            }
            Console.WriteLine();
        }
    }

    internal void StartTracking() {
        _startTracking = true;
    }
}

class Program {
    static void Main(string[] args) {

        // This lines allocates plenty, hence doing it before NoGC, but start tracking after it
        var ev = new GcEventListener();

        var startBytes = GC.GetAllocatedBytesForCurrentThread();
        var inGc = GC.TryStartNoGCRegion(16 * 1024 * 1024, true);
        Trace.Assert(inGc, "Can't get into NoGC Region");
        ev.StartTracking();

        Thread.Sleep(200);
        var endBytes = GC.GetAllocatedBytesForCurrentThread();
        Trace.Assert(endBytes - startBytes == 0);
    }
}
