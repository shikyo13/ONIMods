using System;

namespace OniProfiler.Memory
{
    /// <summary>
    /// Snapshot of GC and memory state for the current frame.
    /// </summary>
    public struct GCData
    {
        public double HeapSizeMB;
        public double AllocationRateKBps;
        public int Gen0Delta;
        public int Gen1Delta;
        public int Gen2Delta;
    }

    /// <summary>
    /// Tracks GC collection counts (delta per frame), managed heap size, and allocation rate.
    /// No patches needed — reads from System.GC statics.
    /// </summary>
    public static class GCMonitor
    {
        public static GCData Current { get; private set; }

        private static int prevGen0;
        private static int prevGen1;
        private static int prevGen2;
        private static long prevHeapBytes;
        private static float prevTime;
        private static bool initialized;

        public static void Update()
        {
            var data = new GCData();
            long heapBytes = GC.GetTotalMemory(false);
            data.HeapSizeMB = heapBytes / (1024.0 * 1024.0);

            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);

            if (!initialized)
            {
                prevGen0 = gen0;
                prevGen1 = gen1;
                prevGen2 = gen2;
                prevHeapBytes = heapBytes;
                prevTime = UnityEngine.Time.realtimeSinceStartup;
                initialized = true;
                Current = data;
                return;
            }

            data.Gen0Delta = gen0 - prevGen0;
            data.Gen1Delta = gen1 - prevGen1;
            data.Gen2Delta = gen2 - prevGen2;

            float currentTime = UnityEngine.Time.realtimeSinceStartup;
            float dt = currentTime - prevTime;
            if (dt > 0.001f)
            {
                long byteDelta = heapBytes - prevHeapBytes;
                // Only track positive allocation (heap can shrink after GC)
                data.AllocationRateKBps = byteDelta > 0
                    ? (byteDelta / 1024.0) / dt
                    : 0;
            }

            prevGen0 = gen0;
            prevGen1 = gen1;
            prevGen2 = gen2;
            prevHeapBytes = heapBytes;
            prevTime = currentTime;
            Current = data;
        }
    }
}
