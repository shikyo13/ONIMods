using System;
using System.IO;
using System.Linq;
using System.Text;
using OniProfiler.Census;
using OniProfiler.Memory;
using OniProfiler.Timing;
using UnityEngine;

namespace OniProfiler.Recording
{
    public static class DataRecorder
    {
        public static bool IsRecording { get; private set; }

        private const float Interval = 1f;
        private static StreamWriter writer;
        private static StreamWriter spikeWriter;
        private static float accumulator;

        // FPS tracking — mirrors FrameTimings.displayFps approach
        private static int frameCount;
        private static float fpsTimer;
        private static double currentFps;

        // Per-interval spike detection
        private static double intervalMinFrameMs = double.MaxValue;
        private static double intervalMaxFrameMs;
        private static readonly double[] intervalMax = new double[(int)TimingKey.COUNT];

        // Per-interval allocation accumulator (KB summed across all frames in interval)
        private static readonly double[] intervalAllocKB = new double[(int)TimingKey.COUNT];

        public static void StartRecording()
        {
            if (IsRecording) return;

            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Klei", "OxygenNotIncluded", "mods", "local", "OniProfiler");
                Directory.CreateDirectory(dir);

                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string path = Path.Combine(dir, $"profiler_{timestamp}.csv");

                WriteSessionInfo(Path.Combine(dir, $"profiler_{timestamp}.txt"));

                writer = new StreamWriter(path, false, Encoding.UTF8);
                WriteColumnHeader();
                writer.Flush();

                string spikePath = Path.Combine(dir, $"profiler_{timestamp}_spikes.csv");
                spikeWriter = new StreamWriter(spikePath, false, Encoding.UTF8);
                WriteSpikeHeader();
                spikeWriter.Flush();

                accumulator = 0f;
                frameCount = 0;
                fpsTimer = 0f;
                currentFps = 0;
                intervalMinFrameMs = double.MaxValue;
                intervalMaxFrameMs = 0;
                for (int i = 0; i < (int)TimingKey.COUNT; i++)
                {
                    intervalMax[i] = 0;
                    intervalAllocKB[i] = 0;
                }
                IsRecording = true;
                Debug.Log($"[OniProfiler] Recording started: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OniProfiler] Failed to start recording: {ex.Message}");
                writer?.Dispose();
                writer = null;
                spikeWriter?.Dispose();
                spikeWriter = null;
            }
        }

        public static void StopRecording()
        {
            if (!IsRecording) return;

            IsRecording = false;
            try
            {
                writer?.Flush();
                writer?.Dispose();
                spikeWriter?.Flush();
                spikeWriter?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OniProfiler] Error closing recording: {ex.Message}");
            }
            writer = null;
            spikeWriter = null;
            Debug.Log("[OniProfiler] Recording stopped.");
        }

        /// <summary>
        /// Called every visible frame (even before recording starts) to track FPS and frame extremes.
        /// </summary>
        public static void RecordFrame(float deltaTime)
        {
            frameCount++;
            fpsTimer += deltaTime;

            var timings = FrameTimings.Instance;

            // Track per-system max and alloc for the current interval
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
            {
                double ms = timings.GetCurrentMs((TimingKey)i);
                if (ms > intervalMax[i]) intervalMax[i] = ms;
                intervalAllocKB[i] += timings.GetCurrentAllocKB((TimingKey)i);
            }

            // Overall frame time min/max
            double frameMs = timings.GetCurrentMs(TimingKey.GameUpdate);
            if (frameMs < intervalMinFrameMs) intervalMinFrameMs = frameMs;
            if (frameMs > intervalMaxFrameMs) intervalMaxFrameMs = frameMs;

            if (fpsTimer >= 0.25f)
            {
                currentFps = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
            }

            // Write spike row if recording and a new spike was detected this frame
            if (IsRecording && SpikeTracker.HasNewSpike)
                WriteSpikeRow();
        }

        public static void Tick(float deltaTime)
        {
            if (!IsRecording) return;

            accumulator += deltaTime;
            if (accumulator < Interval) return;
            accumulator -= Interval;

            WriteRow();
        }

        private static void WriteSessionInfo(string path)
        {
            try
            {
                File.WriteAllText(path,
                    $"OniProfiler Recording\n" +
                    $"Date: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Game: {Application.version}\n" +
                    $"Cycle: {GameClock.Instance?.GetCycle() ?? 0}\n" +
                    $"Mods: {GetActiveModList()}\n" +
                    $"GC_Incremental: {GCMonitor.IsIncrementalAvailable}\n" +
                    $"GC_Mode: {GCMonitor.GetCurrentGCMode()}\n" +
                    $"GC_SliceMs: {GCMonitor.IncrementalSliceMs:F3}\n");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OniProfiler] Failed to write session info: {ex.Message}");
            }
        }

        private static string GetActiveModList()
        {
            try
            {
                var manager = Global.Instance?.modManager;
                if (manager == null) return "(unavailable)";

                var names = manager.mods
                    .Where(m => m.IsActive())
                    .Select(m => m.title)
                    .Where(t => t != "OniProfiler");

                var list = string.Join(", ", names);
                return string.IsNullOrEmpty(list) ? "(none)" : list;
            }
            catch
            {
                return "(unavailable)";
            }
        }

        private static void WriteColumnHeader()
        {
            var sb = new StringBuilder();
            sb.Append("Timestamp,Cycle,FPS,FrameMs_min,FrameMs_max");

            // timing keys: current value + worst-case spike per interval
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
            {
                sb.Append($",{(TimingKey)i}_ms");
                sb.Append($",{(TimingKey)i}_max");
            }

            // 11 census fields
            sb.Append(",Dupes,Critters,Debris,Buildings");
            sb.Append(",GasPipes,LiquidPipes,Conveyors");
            sb.Append(",JetSuits,Walkers");
            sb.Append(",Sim200msSubs,Sim1000msSubs");

            // per-system alloc (KB summed over interval)
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
                sb.Append($",{(TimingKey)i}_alloc_kb");

            // 5 GC fields
            sb.Append(",HeapMB,AllocRateKBps,GC_Gen0,GC_Gen1,GC_Gen2");

            writer.WriteLine(sb.ToString());
        }

        private static void WriteSpikeHeader()
        {
            var sb = new StringBuilder();
            sb.Append("WallTime,FrameMs,GC_Gen2,Unaccounted_ms,InterFrame_ms,GC_InGameLogic");
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
                sb.Append($",{(TimingKey)i}_ms");
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
                sb.Append($",{(TimingKey)i}_alloc_kb");
            for (int i = 0; i < (int)LoopPhase.COUNT; i++)
                sb.Append($",Phase_{PlayerLoopTimings.GetPhaseName((LoopPhase)i)}_ms");
            sb.Append(",BulkUpdateTop5,CoroutineStarts,CoroutineTop5");
            spikeWriter.WriteLine(sb.ToString());
        }

        private static void WriteSpikeRow()
        {
            var spike = SpikeTracker.GetLastSpike();
            if (!spike.HasValue || spikeWriter == null) return;

            var s = spike.Value;
            var sb = new StringBuilder();
            sb.Append(System.DateTime.Now.ToString("HH:mm:ss.fff"));
            sb.Append(',').Append(s.TotalMs.ToString("F3"));
            sb.Append(',').Append(s.Gen2GC ? 1 : 0);

            // Unaccounted = total - sum of leaf systems only.
            // Wrapper systems (GameUpdate, GameLateUpdate, BrainAdvance, SMRender,
            // SMRenderEveryTick) enclose other measured systems — including them
            // would double-count nested timings (e.g. SMRenderEveryTick wraps FindNextChore).
            double accounted = 0;
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
            {
                if (!IsWrapper((TimingKey)i))
                    accounted += s.SystemMs[i];
            }
            sb.Append(',').Append((s.TotalMs - accounted).ToString("F3"));
            sb.Append(',').Append(s.InterFrameGapMs.ToString("F3"));
            sb.Append(',').Append(s.GCDuringGameLogic ? 1 : 0);

            for (int i = 0; i < (int)TimingKey.COUNT; i++)
                sb.Append(',').Append(s.SystemMs[i].ToString("F3"));

            for (int i = 0; i < (int)TimingKey.COUNT; i++)
                sb.Append(',').Append(s.AllocKB != null ? s.AllocKB[i].ToString("F1") : "0");

            for (int i = 0; i < (int)LoopPhase.COUNT; i++)
                sb.Append(',').Append(s.PhaseMs != null ? s.PhaseMs[i].ToString("F3") : "0");

            sb.Append(',').Append(s.BulkTop5 ?? "");
            sb.Append(',').Append(s.CoroutineTotal);
            sb.Append(',').Append(s.CoroutineTop5 ?? "");

            try
            {
                spikeWriter.WriteLine(sb.ToString());
                spikeWriter.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OniProfiler] Spike write failed: {ex.Message}");
            }
        }

        private static bool IsWrapper(TimingKey k) =>
            k == TimingKey.GameUpdate ||
            k == TimingKey.GameLateUpdate ||
            k == TimingKey.BrainAdvance ||
            k == TimingKey.SMRender ||
            k == TimingKey.SMRenderEveryTick ||
            k == TimingKey.GlobalLateUpdate ||
            k == TimingKey.KCompSpawnUpdate;

        private static void WriteRow()
        {
            var timings = FrameTimings.Instance;
            var census = EntityCensus.Current;
            var gc = GCMonitor.Current;

            var sb = new StringBuilder();

            // Timestamp, Cycle, FPS, per-interval spike columns
            sb.Append(System.DateTime.Now.ToString("HH:mm:ss"));
            sb.Append(',').Append(GameClock.Instance?.GetCycle() ?? 0);
            sb.Append(',').Append(currentFps.ToString("F1"));
            sb.Append(',').Append(intervalMinFrameMs < double.MaxValue
                ? intervalMinFrameMs.ToString("F3") : "0");
            sb.Append(',').Append(intervalMaxFrameMs.ToString("F3"));

            // timing values: current + max spike per interval
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
            {
                sb.Append(',').Append(timings.GetCurrentMs((TimingKey)i).ToString("F3"));
                sb.Append(',').Append(intervalMax[i].ToString("F3"));
            }

            // 11 census fields
            sb.Append(',').Append(census.DupeCount);
            sb.Append(',').Append(census.CritterCount);
            sb.Append(',').Append(census.DebrisCount);
            sb.Append(',').Append(census.BuildingCount);
            sb.Append(',').Append(census.GasPipeSegments);
            sb.Append(',').Append(census.LiquidPipeSegments);
            sb.Append(',').Append(census.ConveyorSegments);
            sb.Append(',').Append(census.JetSuitCount);
            sb.Append(',').Append(census.WalkerCount);
            sb.Append(',').Append(census.Sim200msSubscribers);
            sb.Append(',').Append(census.Sim1000msSubscribers);

            // per-system alloc (KB summed over interval)
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
                sb.Append(',').Append(intervalAllocKB[i].ToString("F1"));

            // 5 GC fields
            sb.Append(',').Append(gc.HeapSizeMB.ToString("F1"));
            sb.Append(',').Append(gc.AllocationRateKBps.ToString("F0"));
            sb.Append(',').Append(gc.Gen0Delta);
            sb.Append(',').Append(gc.Gen1Delta);
            sb.Append(',').Append(gc.Gen2Delta);

            // Reset per-interval tracking
            intervalMinFrameMs = double.MaxValue;
            intervalMaxFrameMs = 0;
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
            {
                intervalMax[i] = 0;
                intervalAllocKB[i] = 0;
            }

            try
            {
                writer.WriteLine(sb.ToString());
                writer.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OniProfiler] Write failed, stopping: {ex.Message}");
                StopRecording();
            }
        }
    }
}
