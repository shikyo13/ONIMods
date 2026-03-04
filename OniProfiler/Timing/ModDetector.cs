using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace OniProfiler.Timing
{
    /// <summary>
    /// Scans Harmony patch info for all timed methods to detect other mods' patches.
    /// Builds a label map: TimingKey → list of mod assembly names with patches on that method.
    /// </summary>
    public static class ModDetector
    {
        private static readonly Dictionary<TimingKey, List<string>> modLabels
            = new Dictionary<TimingKey, List<string>>();

        /// <summary>
        /// Scans all currently patched methods for other mods' Harmony patches.
        /// Called when the profiler panel opens.
        /// </summary>
        public static void Scan()
        {
            modLabels.Clear();

            var ourHarmonyId = "OniProfiler.Timing";
            var patchedMethods = TimingPatchManager.GetPatchedMethods();

            for (int i = 0; i < patchedMethods.Count; i++)
            {
                var method = patchedMethods[i];
                var patches = Harmony.GetPatchInfo(method);
                if (patches == null) continue;

                var otherMods = new HashSet<string>();
                CollectOtherMods(patches.Prefixes, ourHarmonyId, otherMods);
                CollectOtherMods(patches.Postfixes, ourHarmonyId, otherMods);
                CollectOtherMods(patches.Transpilers, ourHarmonyId, otherMods);

                if (otherMods.Count > 0)
                {
                    // Map method back to its TimingKey by matching against patch list
                    var key = FindKeyForMethod(method);
                    if (key.HasValue)
                        modLabels[key.Value] = otherMods.ToList();
                }
            }
        }

        /// <summary>
        /// Returns mod labels for a timing key, or null if no other mods patch that method.
        /// LiquidConduit shares the same patch as GasConduit, so labels are mirrored.
        /// </summary>
        public static string GetModLabel(TimingKey key)
        {
            // LiquidConduit shares ConduitFlow.Sim200ms with GasConduit
            var lookupKey = key == TimingKey.LiquidConduit ? TimingKey.GasConduit : key;
            if (modLabels.TryGetValue(lookupKey, out var mods) && mods.Count > 0)
                return string.Join(", ", mods);
            return null;
        }

        public static bool HasModPatches(TimingKey key) => modLabels.ContainsKey(key);

        private static void CollectOtherMods(
            IEnumerable<Patch> patches, string ourId, HashSet<string> result)
        {
            if (patches == null) return;
            foreach (var patch in patches)
            {
                if (patch.owner == ourId) continue;

                // Try to get a friendly name from the patch's declaring type assembly
                string modName = GetFriendlyName(patch);
                result.Add(modName);
            }
        }

        private static string GetFriendlyName(Patch patch)
        {
            // Use Harmony ID as the mod name (most mods use their mod name as ID)
            var owner = patch.owner;
            if (string.IsNullOrEmpty(owner))
            {
                // Fallback: use the declaring type's assembly name
                var asm = patch.PatchMethod?.DeclaringType?.Assembly;
                return asm?.GetName()?.Name ?? "Unknown";
            }

            // Clean up common Harmony ID patterns
            // e.g., "PeterHan.FastTrack" → "FastTrack"
            int lastDot = owner.LastIndexOf('.');
            return lastDot >= 0 ? owner.Substring(lastDot + 1) : owner;
        }

        private static TimingKey? FindKeyForMethod(MethodInfo method)
        {
            // Match the method against known patch targets
            var declType = method.DeclaringType;
            var name = method.Name;

            if (declType == typeof(Game) && name == "Update") return TimingKey.GameUpdate;
            if (declType == typeof(Game) && name == "LateUpdate") return TimingKey.GameLateUpdate;
            if (declType == typeof(Game) && name == "UnsafeSim200ms") return TimingKey.Sim200ms;
            // ConduitFlow.Sim200ms is shared by gas and liquid — report as GasConduit
            // (both are covered by the same patch, mod labels apply to both)
            if (declType == typeof(ConduitFlow) && name == "Sim200ms") return TimingKey.GasConduit;
            if (declType == typeof(SolidConduitFlow) && name == "Sim200ms") return TimingKey.SolidConduit;
            if (declType == typeof(CircuitManager) && name == "Sim200msFirst") return TimingKey.CircuitFirst;
            if (declType == typeof(CircuitManager) && name == "Sim200msLast") return TimingKey.CircuitLast;
            if (declType == typeof(EnergySim) && name == "EnergySim200ms") return TimingKey.EnergySim;
            if (declType == typeof(StateMachineUpdater) && name == "AdvanceOneSimSubTick") return TimingKey.BrainAdvance;
            if (declType == typeof(ChoreConsumer) && name == "FindNextChore") return TimingKey.FindNextChore;
            if (declType?.Name == "FetchablesByPrefabId" && name == "UpdatePickups") return TimingKey.FetchUpdatePickups;
            if (declType == typeof(Sensors) && name == "UpdateSensors") return TimingKey.SensorUpdate;
            if (declType == typeof(RoomProber) && name == "Sim1000ms") return TimingKey.RoomProber;
            if (declType == typeof(GameScheduler) && name == "Update") return TimingKey.GameScheduler;
            if (declType == typeof(StateMachineUpdater) && name == "Render") return TimingKey.SMRender;
            if (declType == typeof(StateMachineUpdater) && name == "RenderEveryTick") return TimingKey.SMRenderEveryTick;

            // Types resolved by name (may not be available at compile time)
            if (declType?.Name == "PathProber" && name == "UpdateProbe") return TimingKey.PathProbe;
            if (declType?.Name == "PathProber" && name == "Run") return TimingKey.PathProbe;
            if (declType?.Name == "WorkOrder" && name == "Execute") return TimingKey.PathProbe_Async;
            if (declType?.Name == "DecorProvider" && name == "Sim1000ms") return TimingKey.DecorRecalc;
            if (declType?.Name == "OverlayScreen" && name == "LateUpdate") return TimingKey.OverlayRefresh;

            return null;
        }
    }
}
