using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OniProfiler.Timing
{
    /// <summary>
    /// Dynamically applies and removes all timing Harmony patches on panel toggle.
    /// When the profiler is closed, zero patches are active — zero overhead.
    ///
    /// All patches use Priority.First for prefix (captures start time before other mods'
    /// prefixes do replacement work) and Priority.Last for postfix (captures total elapsed
    /// time after all other postfixes). This ensures accurate timing even when performance
    /// mods like FastTrack replace vanilla methods via prefix-returning-false.
    /// </summary>
    public static class TimingPatchManager
    {
        private static Harmony harmony;
        private static bool isPatched;

        // Tracks all patched methods for clean removal
        private static readonly List<MethodInfo> patchedMethods = new List<MethodInfo>();

        /// <summary>
        /// Whether FastTrack was detected at patch time.
        /// </summary>
        public static bool FastTrackDetected { get; private set; }

        public static void ApplyPatches()
        {
            if (isPatched) return;

            harmony = harmony ?? new Harmony("OniProfiler.Timing");
            patchedMethods.Clear();

            // Frame level — Game.Update uses dedicated prefix with inter-frame + GC tracking
            TryPatchGameUpdate();
            TryPatch(typeof(Game), "LateUpdate", TimingKey.GameLateUpdate);

            // Simulation
            TryPatch(typeof(Game), "UnsafeSim200ms", TimingKey.Sim200ms);
            TryPatchConduit(TimingKey.GasConduit, isGas: true);
            TryPatchConduit(TimingKey.LiquidConduit, isGas: false);
            TryPatch(typeof(SolidConduitFlow), "Sim200ms", TimingKey.SolidConduit);
            TryPatch(typeof(CircuitManager), "Sim200msFirst", TimingKey.CircuitFirst);
            TryPatch(typeof(CircuitManager), "Sim200msLast", TimingKey.CircuitLast);
            TryPatch(typeof(EnergySim), "EnergySim200ms", TimingKey.EnergySim);

            // AI & Pathfinding
            // PathProber: FastTrack patches Run(Navigator, List<int>) with prefix→false.
            // Patching Run instead of UpdateProbe ensures we wrap FastTrack's replacement.
            // Fall back to UpdateProbe if Run overload not found (vanilla-only / older builds).
            TryPatchPathProber();
            TryPatch(typeof(StateMachineUpdater), "AdvanceOneSimSubTick", TimingKey.BrainAdvance);
            TryPatch(typeof(ChoreConsumer), "FindNextChore", TimingKey.FindNextChore);
            TryPatchByName("FetchManager+FetchablesByPrefabId", "UpdatePickups", TimingKey.FetchUpdatePickups);
            TryPatch(typeof(Sensors), "UpdateSensors", TimingKey.SensorUpdate);

            // World
            TryPatch(typeof(RoomProber), "Sim1000ms", TimingKey.RoomProber);
            TryPatchByName("DecorProvider", "Sim1000ms", TimingKey.DecorRecalc);
            TryPatch(typeof(GameScheduler), "Update", TimingKey.GameScheduler);


            // Rendering
            TryPatch(typeof(StateMachineUpdater), "Render", TimingKey.SMRender);
            TryPatch(typeof(StateMachineUpdater), "RenderEveryTick", TimingKey.SMRenderEveryTick);
            TryPatchByName("OverlayScreen", "LateUpdate", TimingKey.OverlayRefresh);

            // Async patches — only if FastTrack is present
            ApplyAsyncPatches();

            // PlayerLoop phase timing — brackets each top-level phase with Stopwatch probes
            PlayerLoopTimings.Inject();

            isPatched = true;
        }

        public static void RemovePatches()
        {
            if (!isPatched || harmony == null) return;

            PlayerLoopTimings.Remove();
            harmony.UnpatchAll("OniProfiler.Timing");
            patchedMethods.Clear();
            isPatched = false;
        }

        public static bool IsPatched => isPatched;

        /// <summary>
        /// Returns all methods we attempted to patch, for mod detection scanning.
        /// </summary>
        public static IReadOnlyList<MethodInfo> GetPatchedMethods() => patchedMethods;

        private static void PatchMethod(MethodInfo method, TimingKey key)
        {
            var prefix = SystemPatches.GetPrefix(key);
            var postfix = SystemPatches.GetPostfix(key);
            if (prefix == null || postfix == null) return;

            try
            {
                harmony.Patch(method,
                    prefix: new HarmonyMethod(prefix) { priority = Priority.First },
                    postfix: new HarmonyMethod(postfix) { priority = Priority.Last });
                patchedMethods.Add(method);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OniProfiler] Failed to patch {method.DeclaringType?.Name}.{method.Name}: {e.Message}");
            }
        }

        private static void TryPatch(Type type, string methodName, TimingKey key)
        {
            if (type == null) return;
            var method = AccessTools.Method(type, methodName);
            if (method == null) return;
            PatchMethod(method, key);
        }

        private static void TryPatchByName(string typeName, string methodName, TimingKey key)
        {
            var type = AccessTools.TypeByName(typeName);
            if (type != null)
                TryPatch(type, methodName, key);
        }

        private static void TryPatchByName(string typeName, string methodName, Type[] argTypes, TimingKey key)
        {
            var type = AccessTools.TypeByName(typeName);
            if (type == null) return;
            var method = AccessTools.Method(type, methodName, argTypes);
            if (method == null) return;
            PatchMethod(method, key);
        }

        /// <summary>
        /// Game.Update gets a dedicated prefix that also records inter-frame gap and GC check start.
        /// </summary>
        private static void TryPatchGameUpdate()
        {
            var method = AccessTools.Method(typeof(Game), "Update");
            if (method == null) return;

            var prefix = AccessTools.Method(typeof(SystemPatches), nameof(SystemPatches.TimingPrefix_GameUpdate));
            var postfix = SystemPatches.GetPostfix(TimingKey.GameUpdate);
            if (prefix == null || postfix == null) return;

            try
            {
                harmony.Patch(method,
                    prefix: new HarmonyMethod(prefix) { priority = Priority.First },
                    postfix: new HarmonyMethod(postfix) { priority = Priority.Last });
                patchedMethods.Add(method);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OniProfiler] Failed to patch Game.Update: {e.Message}");
            }
        }

        /// <summary>
        /// Patches PathProber.Run(Navigator, List&lt;int&gt;) to wrap FastTrack's prefix.
        /// Falls back to UpdateProbe if the Run overload isn't found.
        /// </summary>
        private static void TryPatchPathProber()
        {
            var type = AccessTools.TypeByName("PathProber");
            if (type == null) return;

            // Try the sync Run overload first — this is what FastTrack patches
            var method = AccessTools.Method(type, "Run",
                new[] { typeof(Navigator), typeof(System.Collections.Generic.List<int>) });

            if (method == null)
            {
                // Fallback: older game builds or vanilla without the overload
                method = AccessTools.Method(type, "UpdateProbe");
            }

            if (method == null) return;
            PatchMethod(method, TimingKey.PathProbe);
        }

        /// <summary>
        /// Detects FastTrack at runtime and patches background worker entry points
        /// to capture async computational cost.
        /// </summary>
        private static void ApplyAsyncPatches()
        {
            FastTrackDetected = AccessTools.TypeByName("PeterHan.FastTrack.FastTrackMod") != null;
            if (!FastTrackDetected) return;

            Debug.Log("[OniProfiler] FastTrack detected — applying async timing patches");

            // AsyncPathProber.WorkOrder.Execute — background thread path probing
            TryPatchByName("AsyncPathProber+WorkOrder", "Execute", TimingKey.PathProbe_Async);
        }

        /// <summary>
        /// ConduitFlow has gas and liquid instances sharing the same class.
        /// We patch the Sim200ms method once — the postfix distinguishes by instance.
        /// </summary>
        private static void TryPatchConduit(TimingKey key, bool isGas)
        {
            var method = AccessTools.Method(typeof(ConduitFlow), "Sim200ms");
            if (method == null) return;

            // Only patch if not already patched (first call patches it)
            if (key == TimingKey.GasConduit)
            {
                var prefix = SystemPatches.GetPrefix(TimingKey.GasConduit);
                var postfix = SystemPatches.GetConduitPostfix();
                if (prefix == null || postfix == null) return;

                try
                {
                    harmony.Patch(method,
                        prefix: new HarmonyMethod(prefix) { priority = Priority.First },
                        postfix: new HarmonyMethod(postfix) { priority = Priority.Last });
                    patchedMethods.Add(method);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[OniProfiler] Failed to patch ConduitFlow.Sim200ms: {e.Message}");
                }
            }
            // LiquidConduit key doesn't need its own patch — handled in the shared postfix
        }
    }
}
