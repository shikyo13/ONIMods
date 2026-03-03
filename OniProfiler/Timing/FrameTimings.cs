using System.Diagnostics;
using System.Threading;

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
        BrainAdvance,
        FindNextChore,
        FetchUpdatePickups,
        SensorUpdate,

        // World
        RoomProber,
        DecorRecalc,
        GameScheduler,
        WorldContainer,

        // Rendering
        SMRender,
        SMRenderEveryTick,
        OverlayRefresh,

        // Scheduler buckets
        Sim33ms,
        Sim200msBucket,
        Sim1000ms,
        Sim4000ms,

        COUNT
    }

    public enum TimingCategory
    {
        Frame,
        Simulation,
        AI,
        World,
        Rendering,
        Scheduler
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

        // Ring buffer: [keyIndex][frameIndex] = elapsed ticks
        private readonly long[][] history;
        private int historySize;
        private int writeIndex;
        private int sampleCount;

        // Cached stats
        private readonly double[] cachedCurrent;
        private readonly double[] cachedMin;
        private readonly double[] cachedAvg;
        private readonly double[] cachedMax;

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
        /// Called once per frame from ProfilerOverlay.Update to commit current data to ring buffer.
        /// </summary>
        public void RecordFrameEnd()
        {
            for (int i = 0; i < keyCount; i++)
            {
                long ticks = Interlocked.Exchange(ref currentTicks[i], 0);
                history[i][writeIndex] = ticks;
                cachedCurrent[i] = ticks * ticksToMs;
            }

            writeIndex = (writeIndex + 1) % historySize;
            if (sampleCount < historySize)
                sampleCount++;

            ComputeStats();
        }

        public void Reset()
        {
            for (int i = 0; i < keyCount; i++)
            {
                currentTicks[i] = 0;
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
                case TimingKey.BrainAdvance:
                case TimingKey.FindNextChore:
                case TimingKey.FetchUpdatePickups:
                case TimingKey.SensorUpdate:
                    return TimingCategory.AI;

                case TimingKey.RoomProber:
                case TimingKey.DecorRecalc:
                case TimingKey.GameScheduler:
                case TimingKey.WorldContainer:
                    return TimingCategory.World;

                case TimingKey.SMRender:
                case TimingKey.SMRenderEveryTick:
                case TimingKey.OverlayRefresh:
                    return TimingCategory.Rendering;

                case TimingKey.Sim33ms:
                case TimingKey.Sim200msBucket:
                case TimingKey.Sim1000ms:
                case TimingKey.Sim4000ms:
                    return TimingCategory.Scheduler;

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
                case TimingKey.BrainAdvance: return "Brain Advance";
                case TimingKey.FindNextChore: return "Find Chore";
                case TimingKey.FetchUpdatePickups: return "Fetch Pickups";
                case TimingKey.SensorUpdate: return "Sensor Update";
                case TimingKey.RoomProber: return "Room Prober";
                case TimingKey.DecorRecalc: return "Decor Recalc";
                case TimingKey.GameScheduler: return "Scheduler";
                case TimingKey.WorldContainer: return "World Container";
                case TimingKey.SMRender: return "SM Render";
                case TimingKey.SMRenderEveryTick: return "SM RenderEveryTick";
                case TimingKey.OverlayRefresh: return "Overlay Refresh";
                case TimingKey.Sim33ms: return "ISim33ms bucket";
                case TimingKey.Sim200msBucket: return "ISim200ms bucket";
                case TimingKey.Sim1000ms: return "ISim1000ms bucket";
                case TimingKey.Sim4000ms: return "ISim4000ms bucket";
                default: return key.ToString();
            }
        }
    }
}
