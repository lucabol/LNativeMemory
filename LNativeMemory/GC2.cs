namespace LNativeMemory {
    using System;
    using System.Diagnostics.Tracing;

    internal sealed class GcEventListener : EventListener {
        Action _action;
        bool _isWarm = false; // One GC is performed before starting NoGC region, must skip first one.
        bool _started = false;

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
            if (_started && _isWarm && eventName == "GCStart_V2") {
                _action();
            } else if (_started && !_isWarm && eventName == "GCStart_V2") {
                _isWarm = true;
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
}
