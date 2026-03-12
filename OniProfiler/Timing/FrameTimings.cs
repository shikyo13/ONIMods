using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace OniProfiler.Timing
{
    /// <summary>
    /// Keys for all instrumented timing points.
    /// Grouped by category for display purposes.
    /// </summary>
    public enum TimingKey
    {
        // Frame level
        GameUpdate,
        GameLateUpdate,

        // Simulation
        Sim200ms,
        GasConduit,
        LiquidConduit,
        SolidConduit,
        CircuitFirst,
        CircuitLast,
        EnergySim,

        // AI & Pathfinding
        PathProbe,
        PathProbe_Async,
        NavGridUpdate,
        GroupProberRebuild,
        PathfindingUpdate,
        BrainAdvance,
        FindNextChore,
        FetchUpdatePickups,
        SensorUpdate,

        // World
        RoomProber,
        DecorRecalc,
        GameScheduler,

        // Rendering
        SMRender,
        SMRenderEveryTick,
        OverlayRefresh,

        // LateUpdate subsystems (PreLateUpdate phase)
        GlobalLateUpdate,    // Global.LateUpdate — wraps AnimBatchUpdate
        AnimBatchUpdate,     // KBatchedAnimUpdater.LateUpdate — sprite animation tick
        WorldLateUpdate,     // World.LateUpdate — KAnimBatchManager render
        PropertyTexUpdate,   // PropertyTextures.LateUpdate — shader property updates

        // Update subsystems (Update phase) — the 300ms Update-phase mystery
        KCompSpawnUpdate,    // KComponentSpawn.Update — component spawn + render dispatch
        GlobalUpdate,        // Global.Update — input + AnimEventManager
        OnDemandUpdate,      // OnDemandUpdater.Update — IUpdateOnDemand loop
        GridVisAreaUpdate,   // GridVisibleArea.Update — visible area callbacks

        COUNT
    }

    public enum TimingCategory
    {
        Frame,
        Simulation,
        AI,
        World,
        Rendering
    }

    /// <summary>
    /// Pre-allocated ring buffer storing per-frame timing data.
    /// Uses Stopwatch.GetTimestamp() for ~20-50ns overhead per pair.
    /// </summary>
    public sealed class FrameTimings
    {
        public static FrameTimings Instance { get; } = new FrameTimings();

        private readonly int keyCount = (int)TimingKey.COUNT;
        private readonly double ticksToMs;

        // Current frame accumulators (ticks)
        private readonly long[] currentTicks;

        // Per-frame allocation tracking (bytes → KB for display)
        private readonly long[] currentAllocBytes;
        private readonly double[] cachedAllocKB;
        private readonly double[] displayAllocKB;

        // Ring buffer: [keyIndex][frameIndex] = elapsed ticks
        private readonly long[][] history;
        private int historySize;
        private int writeIndex;
        private int sampleCount;

        // Inter-frame tracking (rendering/vsync gap between frames)
        private long lastLateUpdateEndTs;   // Stopwatch timestamp at end of LateUpdate
        private long thisUpdateStartTs;     // Stopwatch timestamp at start of Update
        private double interFrameGapMs;     // Computed gap between frames

        // GC location detection (in-frame vs inter-frame)
        private int gcCountAtUpdateStart;
        private long gcHeapAtStart;         // Heap size at frame start for heap-drop detection
        private bool gcDuringGameLogic;     // GC fired between Update prefix and LateUpdate postfix
        private bool lastFrameGCDuringLogic; // Snapshot consumed by SpikeTracker after RecordFrameEnd

        // Cached stats (updated every frame)
        private readonly double[] cachedCurrent;
        private readonly double[] cachedMin;
        private readonly double[] cachedAvg;
        private readonly double[] cachedMax;

        // Last non-zero value for periodic systems (prevents 0-flicker between ticks)
        private readonly double[] lastNonZero;

        // Display arrays — copied from cached arrays ~4 times/sec for readable UI
        private readonly double[] displayCurrent;
        private readonly double[] displayAvg;
        private readonly double[] displayMax;
        private float displayTimer;
        private int displayFrameCount;
        private double displayFps;

        public FrameTimings()
        {
            ticksToMs = 1000.0 / Stopwatch.Frequency;
            historySize = 300;
            currentTicks = new long[keyCount];
            history = new long[keyCount][];
            for (int i = 0; i < keyCount; i++)
                history[i] = new long[historySize];

            cachedCurrent = new double[keyCount];
            cachedMin = new double[keyCount];
            cachedAvg = new double[keyCount];
            cachedMax = new double[keyCount];
            lastNonZero = new double[keyCount];

            displayCurrent = new double[keyCount];
            displayAvg = new double[keyCount];
            displayMax = new double[keyCount];

            currentAllocBytes = new long[keyCount];
            cachedAllocKB = new double[keyCount];
            displayAllocKB = new double[keyCount];
        }

        /// <summary>
        /// Called from prefix patches. Returns timestamp for postfix.
        /// </summary>
        public static long StartTiming() => Stopwatch.GetTimestamp();

        /// <summary>
        /// Called from postfix patches. Accumulates elapsed time into current frame.
        /// Thread-safe: sim-thread patches call this concurrently with main thread reads.
        /// </summary>
        public void StopTiming(TimingKey key, long startTimestamp)
        {
            long elapsed = Stopwatch.GetTimestamp() - startTimestamp;
            Interlocked.Add(ref currentTicks[(int)key], elapsed);
        }

        /// <summary>
        /// Called from postfix patches. Accumulates allocation delta for this system.
        /// Thread-safe: sim-thread patches may call concurrently.
        /// </summary>
        public void AddAlloc(TimingKey key, long bytes)
        {
            Interlocked.Add(ref currentAllocBytes[(int)key], bytes);
        }

        // Inter-frame gap accessors
        public double InterFrameGapMs => interFrameGapMs;
        public bool GCDuringGameLogic => lastFrameGCDuringLogic;

        /// <summary>
        /// Called from TimingPrefix_GameUpdate at the very start of Game.Update.
        /// Records inter-frame gap (time spent in rendering/vsync since last LateUpdate).
        /// </summary>
        public void RecordUpdateStart(long timestamp)
        {
            thisUpdateStartTs = timestamp;
            if (lastLateUpdateEndTs > 0)
                interFrameGapMs = (timestamp - lastLateUpdateEndTs) * ticksToMs;
        }

        /// <summary>
        /// Called from Postfix_GameLateUpdate at the end of Game.LateUpdate.
        /// Marks the boundary between game logic and rendering pipeline.
        /// </summary>
        public void RecordLateUpdateEnd()
        {
            lastLateUpdateEndTs = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Called from TimingPrefix_GameUpdate. Captures GC count at frame start
        /// so we can detect if GC fired during game logic vs inter-frame.
        /// </summary>
        public void RecordGCCheckStart()
        {
            gcCountAtUpdateStart = GC.CollectionCount(0);
            gcHeapAtStart = GC.GetTotalMemory(false);
            // Don't reset gcDuringGameLogic here — it's consumed (read + reset) in RecordFrameEnd()
        }

        /// <summary>
        /// Called from Finalizer_GameUpdate after GCBudget's GC.Collect() has run.
        /// Detects GC via heap-drop (works even in Manual GC mode).
        /// </summary>
        public void NotifyGCDuringUpdate()
        {
            gcDuringGameLogic = true;
        }

        /// <summary>
        /// Called from Postfix_GameLateUpdate. Compares GC count to detect
        /// whether a collection happened during game logic execution.
        /// Secondary detection path for non-GCBudget GC events between Update and LateUpdate.
        /// </summary>
        public void RecordGCCheckEnd()
        {
            // CollectionCount check (works when GCMode is not Manual)
            bool countBased = GC.CollectionCount(0) > gcCountAtUpdateStart;
            // Heap-drop check (works in Manual mode where CollectionCount doesn't increment)
            bool heapDropBased = gcHeapAtStart - GC.GetTotalMemory(false) > 50L * 1024 * 1024;
            gcDuringGameLogic = countBased || heapDropBased;
        }

        /// <summary>
        /// Called once per frame from ProfilerOverlay.Update to commit current data to ring buffer.
        /// </summary>
        public void RecordFrameEnd()
        {
            // Snapshot GC flag for SpikeTracker (consumed on read)
            lastFrameGCDuringLogic = gcDuringGameLogic;
            gcDuringGameLogic = false;

            for (int i = 0; i < keyCount; i++)
            {
                long ticks = Interlocked.Exchange(ref currentTicks[i], 0);
                history[i][writeIndex] = ticks;
                double ms = ticks * ticksToMs;
                if (ms > 0.001)
                    lastNonZero[i] = ms;
                cachedCurrent[i] = ms > 0.001 ? ms : lastNonZero[i];
            }

            // Convert alloc bytes → KB and reset accumulators
            for (int i = 0; i < keyCount; i++)
            {
                long bytes = Interlocked.Exchange(ref currentAllocBytes[i], 0);
                cachedAllocKB[i] = bytes > 0 ? bytes / 1024.0 : 0;
            }

            writeIndex = (writeIndex + 1) % historySize;
            if (sampleCount < historySize)
                sampleCount++;

            ComputeStats();

            displayTimer += Time.unscaledDeltaTime;
            displayFrameCount++;
            if (displayTimer >= 0.25f)
            {
                displayFps = displayFrameCount / displayTimer;
                displayFrameCount = 0;
                displayTimer = 0f;
                for (int j = 0; j < keyCount; j++)
                {
                    displayCurrent[j] = cachedCurrent[j];
                    displayAvg[j] = cachedAvg[j];
                    displayMax[j] = cachedMax[j];
                    displayAllocKB[j] = cachedAllocKB[j];
                }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < keyCount; i++)
            {
                currentTicks[i] = 0;
                currentAllocBytes[i] = 0;
                cachedAllocKB[i] = 0;
                displayAllocKB[i] = 0;
                for (int j = 0; j < historySize; j++)
                    history[i][j] = 0;
            }
            writeIndex = 0;
            sampleCount = 0;
        }

        public double GetCurrentMs(TimingKey key) => cachedCurrent[(int)key];
        public double GetMinMs(TimingKey key) => cachedMin[(int)key];
        public double GetAvgMs(TimingKey key) => cachedAvg[(int)key];
        public double GetMaxMs(TimingKey key) => cachedMax[(int)key];

        // Display getters — throttled to ~4 Hz for readable UI
        public double GetDisplayCurrentMs(TimingKey key) => displayCurrent[(int)key];
        public double GetDisplayAvgMs(TimingKey key) => displayAvg[(int)key];
        public double GetDisplayMaxMs(TimingKey key) => displayMax[(int)key];
        public double GetDisplayFps() => displayFps;

        // Allocation getters
        public double GetCurrentAllocKB(TimingKey key) => cachedAllocKB[(int)key];
        public double GetDisplayAllocKB(TimingKey key) => displayAllocKB[(int)key];

        private void ComputeStats()
        {
            if (sampleCount == 0) return;

            for (int i = 0; i < keyCount; i++)
            {
                long min = long.MaxValue;
                long max = 0;
                long sum = 0;

                for (int j = 0; j < sampleCount; j++)
                {
                    long val = history[i][j];
                    if (val < min) min = val;
                    if (val > max) max = val;
                    sum += val;
                }

                cachedMin[i] = min * ticksToMs;
                cachedAvg[i] = (sum / (double)sampleCount) * ticksToMs;
                cachedMax[i] = max * ticksToMs;
            }
        }

        public static TimingCategory GetCategory(TimingKey key)
        {
            switch (key)
            {
                case TimingKey.GameUpdate:
                case TimingKey.GameLateUpdate:
                    return TimingCategory.Frame;

                case TimingKey.Sim200ms:
                case TimingKey.GasConduit:
                case TimingKey.LiquidConduit:
                case TimingKey.SolidConduit:
                case TimingKey.CircuitFirst:
                case TimingKey.CircuitLast:
                case TimingKey.EnergySim:
                    return TimingCategory.Simulation;

                case TimingKey.PathProbe:
                case TimingKey.PathProbe_Async:
                case TimingKey.BrainAdvance:
                case TimingKey.FindNextChore:
                case TimingKey.FetchUpdatePickups:
                case TimingKey.SensorUpdate:
                    return TimingCategory.AI;

                case TimingKey.RoomProber:
                case TimingKey.DecorRecalc:
                case TimingKey.GameScheduler:
                    return TimingCategory.World;

                case TimingKey.SMRender:
                case TimingKey.SMRenderEveryTick:
                case TimingKey.OverlayRefresh:
                case TimingKey.GlobalLateUpdate:
                case TimingKey.AnimBatchUpdate:
                case TimingKey.WorldLateUpdate:
                case TimingKey.PropertyTexUpdate:
                case TimingKey.KCompSpawnUpdate:
                case TimingKey.GlobalUpdate:
                case TimingKey.OnDemandUpdate:
                case TimingKey.GridVisAreaUpdate:
                    return TimingCategory.Rendering;

                default:
                    return TimingCategory.Frame;
            }
        }

        public static string GetDisplayName(TimingKey key)
        {
            switch (key)
            {
                case TimingKey.GameUpdate: return "Game.Update";
                case TimingKey.GameLateUpdate: return "Game.LateUpdate";
                case TimingKey.Sim200ms: return "Sim200ms (total)";
                case TimingKey.GasConduit: return "Gas Conduit";
                case TimingKey.LiquidConduit: return "Liquid Conduit";
                case TimingKey.SolidConduit: return "Solid Conduit";
                case TimingKey.CircuitFirst: return "Circuit (first)";
                case TimingKey.CircuitLast: return "Circuit (last)";
                case TimingKey.EnergySim: return "Energy Sim";
                case TimingKey.PathProbe: return "Path Probe";
                case TimingKey.PathProbe_Async: return "Path Probe (async)";
                case TimingKey.BrainAdvance: return "Brain Advance";
                case TimingKey.FindNextChore: return "Find Chore";
                case TimingKey.FetchUpdatePickups: return "Fetch Pickups";
                case TimingKey.SensorUpdate: return "Sensor Update";
                case TimingKey.RoomProber: return "Room Prober";
                case TimingKey.DecorRecalc: return "Decor Recalc";
                case TimingKey.GameScheduler: return "Scheduler";
                case TimingKey.SMRender: return "SM Render";
                case TimingKey.SMRenderEveryTick: return "SM RenderEveryTick";
                case TimingKey.OverlayRefresh: return "Overlay Refresh";
                case TimingKey.GlobalLateUpdate: return "Global.LateUpdate";
                case TimingKey.AnimBatchUpdate: return "Anim Batch Update";
                case TimingKey.WorldLateUpdate: return "World.LateUpdate";
                case TimingKey.PropertyTexUpdate: return "Property Textures";
                case TimingKey.KCompSpawnUpdate: return "KComp Spawn";
                case TimingKey.GlobalUpdate: return "Global.Update";
                case TimingKey.OnDemandUpdate: return "OnDemand Updater";
                case TimingKey.GridVisAreaUpdate: return "Grid VisArea";
                default: return key.ToString();
            }
        }
    }
}
