using System;
using System.Reflection;
using UnityEngine;

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
        public float TimeSinceLastGen2;
        public float AvgGen2Interval;
    }

    /// <summary>
    /// Tracks GC collection counts (delta per frame), managed heap size, and allocation rate.
    /// No patches needed — reads from System.GC statics.
    /// </summary>
    public static class GCMonitor
    {
        public static GCData Current { get; private set; }

        // GC capabilities — populated once at mod load via LogCapabilities()
        public static bool IsIncrementalAvailable { get; private set; }
        public static string GCModeString { get; private set; } = "unknown";
        public static double IncrementalSliceMs { get; private set; }

        /// <summary>
        /// Probes Unity's GarbageCollector API via reflection for incremental GC support.
        /// Called once from OniProfilerMod.OnLoad. Safe if API is absent.
        /// </summary>
        public static void LogCapabilities()
        {
            try
            {
                var gcType = Type.GetType("UnityEngine.Scripting.GarbageCollector, UnityEngine.CoreModule");
                if (gcType == null)
                {
                    Debug.Log("[OniProfiler] GC capabilities: UnityEngine.Scripting.GarbageCollector not found");
                    return;
                }

                var isIncrProp = gcType.GetProperty("isIncremental", BindingFlags.Public | BindingFlags.Static);
                if (isIncrProp != null)
                    IsIncrementalAvailable = (bool)isIncrProp.GetValue(null);

                var modeProp = gcType.GetProperty("GCMode", BindingFlags.Public | BindingFlags.Static);
                if (modeProp != null)
                    GCModeString = modeProp.GetValue(null)?.ToString() ?? "null";

                var sliceProp = gcType.GetProperty("incrementalTimeSliceNanoseconds", BindingFlags.Public | BindingFlags.Static);
                if (sliceProp != null)
                {
                    ulong sliceNs = (ulong)sliceProp.GetValue(null);
                    IncrementalSliceMs = sliceNs / 1_000_000.0;
                }

                Debug.Log($"[OniProfiler] GC capabilities: incremental={IsIncrementalAvailable}, mode={GCModeString}, sliceMs={IncrementalSliceMs:F3}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OniProfiler] Failed to probe GC capabilities: {ex.Message}");
            }
        }

        private static int prevGen0;
        private static int prevGen1;
        private static int prevGen2;
        private static long prevHeapBytes;
        private static float prevTime;
        private static bool initialized;

        // Gen2 interval tracking
        private const int GEN2_RING_SIZE = 10;
        private static float lastGen2Time;
        private static readonly float[] gen2Intervals = new float[GEN2_RING_SIZE];
        private static int gen2WriteIndex;
        private static int gen2Count;
        private static float avgGen2Interval;

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

            // Track gen2 intervals
            if (data.Gen2Delta > 0)
            {
                if (lastGen2Time > 0f)
                {
                    float interval = currentTime - lastGen2Time;
                    gen2Intervals[gen2WriteIndex] = interval;
                    gen2WriteIndex = (gen2WriteIndex + 1) % GEN2_RING_SIZE;
                    if (gen2Count < GEN2_RING_SIZE) gen2Count++;

                    float sum = 0f;
                    for (int i = 0; i < gen2Count; i++)
                        sum += gen2Intervals[i];
                    avgGen2Interval = sum / gen2Count;
                }
                lastGen2Time = currentTime;
            }
            data.TimeSinceLastGen2 = lastGen2Time > 0f ? currentTime - lastGen2Time : 0f;
            data.AvgGen2Interval = avgGen2Interval;
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
