using OniProfiler.Config;
using OniProfiler.Memory;
using PeterHan.PLib.Options;
using UnityEngine;

namespace OniProfiler.Timing
{
    public struct SpikeEvent
    {
        public float WallTime;     // Time.realtimeSinceStartup when spike occurred
        public double TotalMs;     // Stopwatch-based frame delta (correlates with SystemMs)
        public double WallClockCycleMs; // Time.unscaledDeltaTime * 1000 (previous frame's cycle, for reference)
        public double[] SystemMs;  // Per-TimingKey ms snapshot (length = TimingKey.COUNT)
        public double[] AllocKB;   // Per-TimingKey allocation on spike frame (KB)
        public bool Gen2GC;        // True if gen2 collection happened this frame
        public double InterFrameGapMs;  // Time between last LateUpdate end → this Update start
        public bool GCDuringGameLogic;  // True = GC fired during Update/LateUpdate
        public double[] PhaseMs;   // Per-LoopPhase ms snapshot (length = LoopPhase.COUNT)
        public string BulkTop5;   // Pipe-delimited "TypeA:142.3|TypeB:89.1|..."
        public string CoroutineTop5; // Pipe-delimited "MethodA:3|MethodB:1|..."
        public int CoroutineTotal;   // Total StartCoroutine calls this frame
    }

    /// <summary>
    /// Detects lag spikes by checking wall-clock frame delta against a threshold.
    /// On spike: captures a correlated snapshot of ALL per-system timings for that exact frame.
    /// Ring buffer of last 50 spikes, plus rolling spikes-per-minute counter.
    /// </summary>
    public static class SpikeTracker
    {
        private const int BUFFER_SIZE = 50;
        private const int MINUTE_BUFFER_SIZE = 300; // timestamps for spikes/min rolling window

        private static readonly int keyCount = (int)TimingKey.COUNT;
        private static readonly int phaseCount = (int)LoopPhase.COUNT;
        private static readonly SpikeEvent[] buffer = new SpikeEvent[BUFFER_SIZE];
        private static readonly double[][] systemMsPool;  // pre-allocated arrays, one per buffer slot
        private static readonly double[][] allocKBPool;   // pre-allocated alloc arrays, one per slot
        private static readonly double[][] phaseMsPool;   // pre-allocated phase arrays, one per slot
        private static int writeIndex;
        private static int count;
        private static bool hasNewSpike;

        // Reusable scratch buffer for GetTopContributors (UI thread only)
        private static readonly bool[] topContribUsed = new bool[keyCount];

        // Rolling spikes-per-minute: circular buffer of spike timestamps
        private static readonly float[] spikeTimestamps = new float[MINUTE_BUFFER_SIZE];
        private static int tsWriteIndex;
        private static int tsCount;

        private static float cachedThreshold = 33f;
        private static bool thresholdLoaded;

        static SpikeTracker()
        {
            systemMsPool = new double[BUFFER_SIZE][];
            allocKBPool = new double[BUFFER_SIZE][];
            phaseMsPool = new double[BUFFER_SIZE][];
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                systemMsPool[i] = new double[keyCount];
                allocKBPool[i] = new double[keyCount];
                phaseMsPool[i] = new double[phaseCount];
            }
        }

        public static bool HasNewSpike => hasNewSpike;
        public static int SpikeCount => count;
        public static float Threshold => cachedThreshold;

        /// <summary>
        /// Called every frame after RecordFrameEnd(). One float comparison — negligible cost.
        /// </summary>
        public static void CheckFrame(double frameDeltaMs, double wallClockMs)
        {
            hasNewSpike = false;

            float threshold = GetThreshold();
            if (frameDeltaMs <= threshold) return;

            var timings = FrameTimings.Instance;
            var systemMs = systemMsPool[writeIndex];
            var allocKB = allocKBPool[writeIndex];
            for (int i = 0; i < keyCount; i++)
            {
                systemMs[i] = timings.GetCurrentMs((TimingKey)i);
                allocKB[i] = timings.GetCurrentAllocKB((TimingKey)i);
            }

            var phaseMs = phaseMsPool[writeIndex];
            for (int i = 0; i < phaseCount; i++)
                phaseMs[i] = PlayerLoopTimings.GetPhaseMs((LoopPhase)i);

            var spike = new SpikeEvent
            {
                WallTime = Time.realtimeSinceStartup,
                TotalMs = frameDeltaMs,
                WallClockCycleMs = wallClockMs,
                SystemMs = systemMs,
                AllocKB = allocKB,
                Gen2GC = GCMonitor.Current.Gen2Delta > 0,
                InterFrameGapMs = timings.InterFrameGapMs,
                GCDuringGameLogic = timings.GCDuringGameLogic,
                PhaseMs = phaseMs,
                BulkTop5 = BulkUpdateTimings.Top5,
                CoroutineTop5 = CoroutineTimings.Top5,
                CoroutineTotal = CoroutineTimings.FrameTotal
            };

            buffer[writeIndex] = spike;
            writeIndex = (writeIndex + 1) % BUFFER_SIZE;
            if (count < BUFFER_SIZE) count++;

            // Record timestamp for spikes/min
            spikeTimestamps[tsWriteIndex] = spike.WallTime;
            tsWriteIndex = (tsWriteIndex + 1) % MINUTE_BUFFER_SIZE;
            if (tsCount < MINUTE_BUFFER_SIZE) tsCount++;

            hasNewSpike = true;
        }

        /// <summary>
        /// Returns the most recent spike event, or null if none recorded.
        /// </summary>
        public static SpikeEvent? GetLastSpike()
        {
            if (count == 0) return null;
            int idx = (writeIndex - 1 + BUFFER_SIZE) % BUFFER_SIZE;
            return buffer[idx];
        }

        /// <summary>
        /// Returns top N contributors from a spike, sorted descending by ms.
        /// Reuses a static array to avoid per-call allocation.
        /// </summary>
        public static int GetTopContributors(SpikeEvent spike, TimingKey[] outKeys,
            double[] outMs, int maxCount)
        {
            // Simple selection sort for top N — keyCount is ~21, maxCount is ~3
            int found = 0;
            for (int i = 0; i < keyCount; i++)
                topContribUsed[i] = false;

            for (int n = 0; n < maxCount && n < keyCount; n++)
            {
                double best = -1;
                int bestIdx = -1;
                for (int i = 0; i < keyCount; i++)
                {
                    if (topContribUsed[i]) continue;
                    // Skip frame-level keys (GameUpdate wraps everything)
                    var key = (TimingKey)i;
                    if (key == TimingKey.GameUpdate || key == TimingKey.GameLateUpdate) continue;
                    if (spike.SystemMs[i] > best)
                    {
                        best = spike.SystemMs[i];
                        bestIdx = i;
                    }
                }

                if (bestIdx < 0 || best < 0.01) break;
                topContribUsed[bestIdx] = true;
                outKeys[found] = (TimingKey)bestIdx;
                outMs[found] = best;
                found++;
            }

            return found;
        }

        /// <summary>
        /// Rolling count of spikes in the last 60 seconds.
        /// </summary>
        public static int SpikesPerMinute
        {
            get
            {
                if (tsCount == 0) return 0;
                float cutoff = Time.realtimeSinceStartup - 60f;
                int result = 0;
                for (int i = 0; i < tsCount; i++)
                    if (spikeTimestamps[i] >= cutoff)
                        result++;
                return result;
            }
        }

        public static void Reset()
        {
            writeIndex = 0;
            count = 0;
            tsWriteIndex = 0;
            tsCount = 0;
            hasNewSpike = false;
        }

        private static float GetThreshold()
        {
            if (!thresholdLoaded)
            {
                var opts = POptions.ReadSettings<OniProfilerOptions>();
                cachedThreshold = opts?.SpikeThresholdMs ?? 33f;
                thresholdLoaded = true;
            }
            return cachedThreshold;
        }

        /// <summary>
        /// Reload threshold from options. Called when profiler opens.
        /// </summary>
        public static void RefreshThreshold()
        {
            thresholdLoaded = false;
        }
    }
}
