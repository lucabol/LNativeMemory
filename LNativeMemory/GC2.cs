namespace LNativeMemory {
    using System;
    using System.Diagnostics.Tracing;

    public static class GC2 {
        static private GcEventListener _action;

        public static bool TryStartNoGCRegion(long totalSize, Action actionWhenAllocatedMore) {

            var succeeded = GC.TryStartNoGCRegion(totalSize);
            _action = new GcEventListener(actionWhenAllocatedMore);
            return succeeded;
        }

        public static void EndNoGCRegion() {
            try {
                _action = null;
                System.GC.EndNoGCRegion();
            } catch (InvalidOperationException) {
            }

        }
    }

    internal sealed class GcEventListener : EventListener {
        Action _action;

        internal GcEventListener(Action action) {
            _action = action;
        }
        protected override void OnEventSourceCreated(EventSource eventSource) {
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime")) {
                EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)(-1));
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData) {
            if (eventData.EventName.StartsWith("GCStart")) {
                _action();
            }
        }
    }
}
