using System.Diagnostics;
using System.Reflection;
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

        // ─── Shared prefix ────────────────────────────────────────────
        public static void TimingPrefix(out long __state)
        {
            __state = Stopwatch.GetTimestamp();
        }

        // ─── Frame level ──────────────────────────────────────────────
        public static void Postfix_GameUpdate(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.GameUpdate, __state);
        }

        public static void Postfix_GameLateUpdate(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.GameLateUpdate, __state);
        }

        // ─── Simulation ──────────────────────────────────────────────
        public static void Postfix_Sim200ms(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.Sim200ms, __state);
        }

        /// <summary>
        /// ConduitFlow.Sim200ms is shared by gas and liquid instances.
        /// Determine which by comparing to Game.Instance fields.
        /// </summary>
        public static void Postfix_Conduit(ConduitFlow __instance, long __state)
        {
            var game = Game.Instance;
            if (game == null) return;

            if (__instance == game.gasConduitFlow)
                FrameTimings.Instance.StopTiming(TimingKey.GasConduit, __state);
            else if (__instance == game.liquidConduitFlow)
                FrameTimings.Instance.StopTiming(TimingKey.LiquidConduit, __state);
        }

        public static void Postfix_SolidConduit(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.SolidConduit, __state);
        }

        public static void Postfix_CircuitFirst(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.CircuitFirst, __state);
        }

        public static void Postfix_CircuitLast(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.CircuitLast, __state);
        }

        public static void Postfix_EnergySim(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.EnergySim, __state);
        }

        // ─── AI & Pathfinding ─────────────────────────────────────────
        public static void Postfix_PathProbe(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.PathProbe, __state);
        }

        public static void Postfix_BrainAdvance(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.BrainAdvance, __state);
        }

        public static void Postfix_FindNextChore(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.FindNextChore, __state);
        }

        public static void Postfix_FetchUpdatePickups(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.FetchUpdatePickups, __state);
        }

        public static void Postfix_SensorUpdate(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.SensorUpdate, __state);
        }

        // ─── World systems ────────────────────────────────────────────
        public static void Postfix_RoomProber(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.RoomProber, __state);
        }

        public static void Postfix_DecorRecalc(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.DecorRecalc, __state);
        }

        public static void Postfix_GameScheduler(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.GameScheduler, __state);
        }

        public static void Postfix_WorldContainer(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.WorldContainer, __state);
        }

        // ─── Rendering ───────────────────────────────────────────────
        public static void Postfix_SMRender(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.SMRender, __state);
        }

        public static void Postfix_SMRenderEveryTick(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.SMRenderEveryTick, __state);
        }

        public static void Postfix_OverlayRefresh(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.OverlayRefresh, __state);
        }

        // ─── Scheduler buckets ───────────────────────────────────────
        public static void Postfix_Sim33ms(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.Sim33ms, __state);
        }

        public static void Postfix_Sim200msBucket(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.Sim200msBucket, __state);
        }

        public static void Postfix_Sim1000ms(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.Sim1000ms, __state);
        }

        public static void Postfix_Sim4000ms(long __state)
        {
            FrameTimings.Instance.StopTiming(TimingKey.Sim4000ms, __state);
        }

        // ─── Unused keys get no-op postfixes (GasConduit/LiquidConduit
        //     handled by Postfix_Conduit above) ──────────────────────
        public static void Postfix_GasConduit(long __state) { /* handled by Postfix_Conduit */ }
        public static void Postfix_LiquidConduit(long __state) { /* handled by Postfix_Conduit */ }
    }
}
