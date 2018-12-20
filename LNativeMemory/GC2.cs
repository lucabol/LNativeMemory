namespace LNativeMemory {
    using System;
    using System.Diagnostics.Tracing;

    public static class GC2 {
        static private GcEventListener _evListener;

        public static bool TryStartNoGCRegion(long totalSize, Action actionWhenAllocatedMore) {

            GC.Collect();
            _evListener = new GcEventListener(actionWhenAllocatedMore);
            var succeeded = GC.TryStartNoGCRegion(totalSize,true);
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
            if (eventData.EventName.StartsWith("GC"))
                Console.WriteLine(eventData.EventName);
            if (eventData.EventName.StartsWith("GCStart") && _action != null) {
                _action();
            }
        }

        public override void Dispose() { _action = null; base.Dispose(); }
    }
}
