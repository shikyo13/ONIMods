using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace OniProfiler.Timing
{
    /// <summary>
    /// All prefix/postfix Harmony pairs for timing instrumentation.
    /// Each prefix stores Stopwatch.GetTimestamp() in __state (20-50ns, no allocation).
    /// Each postfix computes elapsed and accumulates into FrameTimings.
    /// </summary>
    public static class SystemPatches
    {
        /// <summary>
        /// Returns the prefix MethodInfo for a given timing key.
        /// All timing keys share a common prefix (capture start timestamp).
        /// </summary>
        public static MethodInfo GetPrefix(TimingKey key)
        {
            return AccessTools.Method(typeof(SystemPatches), nameof(TimingPrefix));
        }

        /// <summary>
        /// Returns the postfix MethodInfo for a given timing key.
        /// Each key has a dedicated postfix that writes to the correct slot.
        /// </summary>
        public static MethodInfo GetPostfix(TimingKey key)
        {
            string methodName = "Postfix_" + key.ToString();
            return AccessTools.Method(typeof(SystemPatches), methodName);
        }

        /// <summary>
        /// Returns the special conduit postfix that distinguishes gas vs liquid by instance.
        /// </summary>
        public static MethodInfo GetConduitPostfix()
        {
            return AccessTools.Method(typeof(SystemPatches), nameof(Postfix_Conduit));
        }

        // ─── Allocation tracking (main thread only) ────────────────
        // GC.GetTotalMemory is process-wide; only measure on main thread
        // to avoid cross-thread contamination. Worker-thread systems get 0KB.
        private static int mainThreadId;
        private static bool mainThreadIdSet;
        private static long[] heapStack;
        private static int heapDepth;

        private static void RecordAlloc(TimingKey key)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId) return;
            long startHeap = heapStack[--heapDepth];
            long delta = GC.GetTotalMemory(false) - startHeap;
            if (delta > 0)
                FrameTimings.Instance.AddAlloc(key, delta);
        }

        // ─── Shared prefix ────────────────────────────────────────────
        public static void TimingPrefix(out long __state)
        {
            __state = Stopwatch.GetTimestamp();
            if (!mainThreadIdSet)
            {
                mainThreadId = Thread.CurrentThread.ManagedThreadId;
                mainThreadIdSet = true;
            }
            if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
            {
                if (heapStack == null) heapStack = new long[16];
                heapStack[heapDepth++] = GC.GetTotalMemory(false);
            }
        }

        // ─── Dedicated Game.Update prefix ────────────────────────────
        // Records inter-frame gap and GC check start in addition to normal timing.
        public static void TimingPrefix_GameUpdate(out long __state)
        {
            __state = Stopwatch.GetTimestamp();
            if (!mainThreadIdSet)
            {
                mainThreadId = Thread.CurrentThread.ManagedThreadId;
                mainThreadIdSet = true;
            }
            if (heapStack == null) heapStack = new long[16];
            heapStack[heapDepth++] = GC.GetTotalMemory(false);

            var ft = FrameTimings.Instance;
            ft.RecordUpdateStart(__state);
            ft.RecordGCCheckStart();
        }

        // ─── Frame level ──────────────────────────────────────────────
        public static void Postfix_GameUpdate(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.GameUpdate, __state);
            RecordAlloc(TimingKey.GameUpdate);
        }

        public static void Postfix_GameLateUpdate(long __state)
        {
            var ft = FrameTimings.Instance;
            ft.StopTiming(TimingKey.GameLateUpdate, __state);
            RecordAlloc(TimingKey.GameLateUpdate);
            ft.RecordGCCheckEnd();
            ft.RecordLateUpdateEnd();
        }

        // ─── Simulation ──────────────────────────────────────────────
        public static void Postfix_Sim200ms(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.Sim200ms, __state);
            RecordAlloc(TimingKey.Sim200ms);
        }

        /// <summary>
        /// ConduitFlow.Sim200ms is shared by gas and liquid instances.
        /// Determine which by comparing to Game.Instance fields.
        /// </summary>
        public static void Postfix_Conduit(ConduitFlow __instance, long __state)
        {
            var game = Game.Instance;
            bool isMainThread = Thread.CurrentThread.ManagedThreadId == mainThreadId;
            if (game == null)
            {
                if (isMainThread && heapStack != null && heapDepth > 0) heapDepth--;
                return;
            }

            if (__instance == game.gasConduitFlow)
            {
                FrameTimings.Instance.StopTiming(TimingKey.GasConduit, __state);
                RecordAlloc(TimingKey.GasConduit);
            }
            else if (__instance == game.liquidConduitFlow)
            {
                FrameTimings.Instance.StopTiming(TimingKey.LiquidConduit, __state);
                RecordAlloc(TimingKey.LiquidConduit);
            }
            else
            {
                if (isMainThread && heapStack != null && heapDepth > 0) heapDepth--;
            }
        }

        public static void Postfix_SolidConduit(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.SolidConduit, __state);
            RecordAlloc(TimingKey.SolidConduit);
        }

        public static void Postfix_CircuitFirst(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.CircuitFirst, __state);
            RecordAlloc(TimingKey.CircuitFirst);
        }

        public static void Postfix_CircuitLast(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.CircuitLast, __state);
            RecordAlloc(TimingKey.CircuitLast);
        }

        public static void Postfix_EnergySim(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.EnergySim, __state);
            RecordAlloc(TimingKey.EnergySim);
        }

        // ─── AI & Pathfinding ─────────────────────────────────────────
        public static void Postfix_PathProbe(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.PathProbe, __state);
            RecordAlloc(TimingKey.PathProbe);
        }

        public static void Postfix_PathProbe_Async(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.PathProbe_Async, __state);
            RecordAlloc(TimingKey.PathProbe_Async);
        }

        public static void Postfix_BrainAdvance(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.BrainAdvance, __state);
            RecordAlloc(TimingKey.BrainAdvance);
        }

        public static void Postfix_FindNextChore(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.FindNextChore, __state);
            RecordAlloc(TimingKey.FindNextChore);
        }

        public static void Postfix_FetchUpdatePickups(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.FetchUpdatePickups, __state);
            RecordAlloc(TimingKey.FetchUpdatePickups);
        }

        public static void Postfix_SensorUpdate(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.SensorUpdate, __state);
            RecordAlloc(TimingKey.SensorUpdate);
        }

        // ─── World systems ────────────────────────────────────────────
        public static void Postfix_RoomProber(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.RoomProber, __state);
            RecordAlloc(TimingKey.RoomProber);
        }

        public static void Postfix_DecorRecalc(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.DecorRecalc, __state);
            RecordAlloc(TimingKey.DecorRecalc);
        }

        public static void Postfix_GameScheduler(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.GameScheduler, __state);
            RecordAlloc(TimingKey.GameScheduler);
        }

        // ─── Rendering ───────────────────────────────────────────────
        public static void Postfix_SMRender(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.SMRender, __state);
            RecordAlloc(TimingKey.SMRender);
        }

        public static void Postfix_SMRenderEveryTick(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.SMRenderEveryTick, __state);
            RecordAlloc(TimingKey.SMRenderEveryTick);
        }

        public static void Postfix_OverlayRefresh(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.OverlayRefresh, __state);
            RecordAlloc(TimingKey.OverlayRefresh);
        }

        // ─── Unused keys get no-op postfixes (GasConduit/LiquidConduit
        //     handled by Postfix_Conduit above) ──────────────────────
        public static void Postfix_GasConduit(long __state) { RecordAlloc(TimingKey.GasConduit); }
        public static void Postfix_LiquidConduit(long __state) { RecordAlloc(TimingKey.LiquidConduit); }
    }
}
