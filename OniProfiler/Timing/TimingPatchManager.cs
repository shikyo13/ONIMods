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
    /// Prefix priority: Priority.First (0) — runs before all other prefixes (outermost).
    /// Postfix priority: Priority.Last (800) — runs after all other postfixes (outermost).
    /// Game.Update: uses a **finalizer** instead of postfix — guaranteed to run after ALL
    /// postfixes (including GCBudget's GC.Collect), solving the timing misattribution bug.
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

            // LateUpdate subsystems (PreLateUpdate phase) — the 300ms mystery zone
            TryPatchByName("Global", "LateUpdate", TimingKey.GlobalLateUpdate);
            TryPatchByName("KBatchedAnimUpdater", "LateUpdate", TimingKey.AnimBatchUpdate);
            TryPatch(typeof(World), "LateUpdate", TimingKey.WorldLateUpdate);
            TryPatchByName("PropertyTextures", "LateUpdate", TimingKey.PropertyTexUpdate);

            // Update subsystems (Update phase) — the 300ms Update-phase mystery
            TryPatchByName("KComponentSpawn", "Update", TimingKey.KCompSpawnUpdate);
            TryPatchByName("Global", "Update", TimingKey.GlobalUpdate);
            TryPatchByName("OnDemandUpdater", "Update", TimingKey.OnDemandUpdate);
            TryPatchByName("GridVisibleArea", "Update", TimingKey.GridVisAreaUpdate);

            // Async patches — only if FastTrack is present
            ApplyAsyncPatches();

            // Bulk-discover and patch ALL remaining MonoBehaviour.Update() methods
            BulkUpdateTimings.DiscoverAndPatch(harmony);

            // Coroutine census — tracks StartCoroutine calls per frame
            CoroutineTimings.Patch(harmony);

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
                Debug.Log($"[OniProfiler] Patched {method.DeclaringType?.Name}.{method.Name} → {key}");
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
            if (method == null)
            {
                Debug.LogWarning($"[OniProfiler] Method not found: {type.Name}.{methodName}");
                return;
            }
            PatchMethod(method, key);
        }

        private static void TryPatchByName(string typeName, string methodName, TimingKey key)
        {
            var type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                Debug.LogWarning($"[OniProfiler] Type not found: {typeName}");
                return;
            }
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
            var finalizer = AccessTools.Method(typeof(SystemPatches), nameof(SystemPatches.Finalizer_GameUpdate));
            if (prefix == null || finalizer == null) return;

            try
            {
                harmony.Patch(method,
                    prefix: new HarmonyMethod(prefix) { priority = Priority.First },
                    finalizer: new HarmonyMethod(finalizer));
                patchedMethods.Add(method);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OniProfiler] Failed to patch Game.Update: {e.Message}");
            }

            // Diagnostic: log all patches on Game.Update so we can verify ordering
            try
            {
                var patches = Harmony.GetPatchInfo(method);
                if (patches != null)
                {
                    Debug.Log("[OniProfiler] Game.Update patches:");
                    foreach (var p in patches.Prefixes)
                        Debug.Log($"  Prefix: {p.owner} priority={p.priority} method={p.PatchMethod.Name}");
                    foreach (var p in patches.Postfixes)
                        Debug.Log($"  Postfix: {p.owner} priority={p.priority} method={p.PatchMethod.Name}");
                    foreach (var p in patches.Finalizers)
                        Debug.Log($"  Finalizer: {p.owner} priority={p.priority} method={p.PatchMethod.Name}");
                }
            }
            catch { /* diagnostic only — don't break patching */ }
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
