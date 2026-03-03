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
    /// </summary>
    public static class TimingPatchManager
    {
        private static Harmony harmony;
        private static bool isPatched;

        // Tracks all patched methods for clean removal
        private static readonly List<MethodInfo> patchedMethods = new List<MethodInfo>();

        public static void ApplyPatches()
        {
            if (isPatched) return;

            harmony = harmony ?? new Harmony("OniProfiler.Timing");
            patchedMethods.Clear();

            // Frame level
            TryPatch(typeof(Game), "Update", TimingKey.GameUpdate);
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
            TryPatchByName("PathProber", "UpdateProbe", TimingKey.PathProbe);
            TryPatch(typeof(StateMachineUpdater), "AdvanceOneSimSubTick", TimingKey.BrainAdvance);
            TryPatch(typeof(ChoreConsumer), "FindNextChore", TimingKey.FindNextChore);
            TryPatch(typeof(FetchManager), "UpdatePickups", TimingKey.FetchUpdatePickups);
            TryPatch(typeof(Sensor), "Update", TimingKey.SensorUpdate);

            // World
            TryPatch(typeof(RoomProber), "Sim1000ms", TimingKey.RoomProber);
            TryPatchByName("DecorProvider", "Sim1000ms", TimingKey.DecorRecalc);
            TryPatch(typeof(GameScheduler), "Sim200ms", TimingKey.GameScheduler);
            TryPatchByName("WorldContainer", "Sim200ms", TimingKey.WorldContainer);

            // Rendering
            TryPatch(typeof(StateMachineUpdater), "Render", TimingKey.SMRender);
            TryPatch(typeof(StateMachineUpdater), "RenderEveryTick", TimingKey.SMRenderEveryTick);
            TryPatchByName("OverlayScreen", "LateUpdate", TimingKey.OverlayRefresh);

            // Scheduler bucket dispatchers — each ISim interface has its own Trigger method
            // on SimAndRenderScheduler. We patch the individual bucket dispatch methods.
            TryPatchByName("UpdateBucketWithUpdater`1", "Update", TimingKey.Sim33ms);
            // Note: Sim200ms/1000ms/4000ms are dispatched through the same UpdateBucket
            // mechanism — they share the Update method. We time individual ISim callbacks
            // through the system-level patches above (ConduitFlow.Sim200ms, etc.) rather
            // than trying to time the bucket dispatcher itself.

            isPatched = true;
        }

        public static void RemovePatches()
        {
            if (!isPatched || harmony == null) return;

            harmony.UnpatchAll("OniProfiler.Timing");
            patchedMethods.Clear();
            isPatched = false;
        }

        public static bool IsPatched => isPatched;

        /// <summary>
        /// Returns all methods we attempted to patch, for mod detection scanning.
        /// </summary>
        public static IReadOnlyList<MethodInfo> GetPatchedMethods() => patchedMethods;

        private static void TryPatch(Type type, string methodName, TimingKey key)
        {
            if (type == null) return;
            var method = AccessTools.Method(type, methodName);
            if (method == null) return;

            var prefix = SystemPatches.GetPrefix(key);
            var postfix = SystemPatches.GetPostfix(key);
            if (prefix == null || postfix == null) return;

            try
            {
                harmony.Patch(method,
                    prefix: new HarmonyMethod(prefix),
                    postfix: new HarmonyMethod(postfix));
                patchedMethods.Add(method);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OniProfiler] Failed to patch {type.Name}.{methodName}: {e.Message}");
            }
        }

        private static void TryPatchByName(string typeName, string methodName, TimingKey key)
        {
            var type = AccessTools.TypeByName(typeName);
            if (type != null)
                TryPatch(type, methodName, key);
        }

        /// <summary>
        /// ConduitFlow has gas and liquid instances sharing the same class.
        /// We patch the Sim200ms method once — the postfix distinguishes by instance.
        /// However, since we can only patch a method once, we use the combined approach:
        /// patch ConduitFlow.Sim200ms and split in the postfix based on which instance called.
        /// </summary>
        private static void TryPatchConduit(TimingKey key, bool isGas)
        {
            // Both gas and liquid use ConduitFlow.Sim200ms — single patch,
            // the postfix uses __instance to determine which conduit type.
            // We only need to patch once; handled in SystemPatches with instance check.
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
                        prefix: new HarmonyMethod(prefix),
                        postfix: new HarmonyMethod(postfix));
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
