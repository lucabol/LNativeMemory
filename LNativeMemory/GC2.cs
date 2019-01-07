using System;
using System.Diagnostics.Tracing;

[assembly: CLSCompliant(false)]

namespace LNativeMemory
{

    internal sealed class GcEventListener : EventListener
    {
        Action _action;
        bool _isWarm = false; // One GC is performed before starting NoGC region, must skip first one.
        bool _started = false;
        EventSource _eventSource;

        internal void Start() { _started = true; }

        internal GcEventListener(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime", StringComparison.Ordinal))
            {
                _eventSource = eventSource;
                EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)(-1));
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var eventName = eventData.EventName;
            if (_started && _isWarm && eventName == "GCStart_V2")
            {
                if(_action != null) _action();
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
        public override void Dispose() {
            _action = null;
            DisableEvents(_eventSource);
            base.Dispose();
        }
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
            if (_evListener != null)
            {
                _evListener.Dispose();
                _evListener = null;
            }
            try
            {
                GC.EndNoGCRegion();
            } catch (Exception)
            {

            }
        }
    }

    public class OutOfGCHeapMemoryException : OutOfMemoryException {
        public OutOfGCHeapMemoryException(string message) : base(message) { }
        public OutOfGCHeapMemoryException(string message, Exception innerException) : base(message, innerException) { }
        public OutOfGCHeapMemoryException() : base() { }

    }

    public sealed class NoGCRegion: IDisposable
    {
        static readonly Action defaultErrorF = () => throw new OutOfGCHeapMemoryException();

        public NoGCRegion(int totalSize, Action actionWhenAllocatedMore)
        {
            var succeeded = GC2.TryStartNoGCRegion(totalSize, actionWhenAllocatedMore);
            if (!succeeded)
                throw new InvalidOperationException("Cannot enter NoGCRegion");
        }

        public NoGCRegion(int totalSize) : this(totalSize, defaultErrorF) { }
        public NoGCRegion() : this(16 * 1024 * 1024, defaultErrorF) { }

        public void Dispose() => GC2.EndNoGCRegion();
    }
}
