using System.Reflection;
using HarmonyLib;

namespace OniProfiler.Census
{
    /// <summary>
    /// Snapshot of entity counts read from game statics. All reads are O(1) —
    /// we're reading .Count from existing collections, not iterating anything.
    /// </summary>
    public struct EntityCensusData
    {
        public int DupeCount;
        public int CritterCount;
        public int DebrisCount;
        public int BuildingCount;
        public int GasPipeSegments;
        public int LiquidPipeSegments;
        public int ConveyorSegments;
        public int JetSuitCount;
        public int WalkerCount;
        public int Sim200msSubscribers;
        public int Sim1000msSubscribers;
    }

    /// <summary>
    /// Reads entity counts from ONI's Components statics and conduit flow instances.
    /// Updated once per frame when the profiler is visible.
    /// </summary>
    public static class EntityCensus
    {
        public static EntityCensusData Current { get; private set; }

        public static void Update()
        {
            var data = new EntityCensusData();
            var game = Game.Instance;
            if (game == null)
            {
                Current = data;
                return;
            }

            // Dupe count from LiveMinionIdentities
            var liveMinions = Components.LiveMinionIdentities.Items;
            data.DupeCount = liveMinions?.Count ?? 0;

            // Critter count = total brains minus dupes
            int totalBrains = Components.Brains.Items?.Count ?? 0;
            data.CritterCount = totalBrains > data.DupeCount
                ? totalBrains - data.DupeCount
                : 0;

            // Debris / pickupables
            data.DebrisCount = Components.Pickupables.Items?.Count ?? 0;

            // Active buildings
            data.BuildingCount = Components.BuildingCompletes.Items?.Count ?? 0;

            // Pipe segments — soaInfo is public on ConduitFlow, private on SolidConduitFlow
            data.GasPipeSegments = game.gasConduitFlow?.soaInfo?.NumEntries ?? 0;
            data.LiquidPipeSegments = game.liquidConduitFlow?.soaInfo?.NumEntries ?? 0;
            data.ConveyorSegments = GetSolidConduitCount(game.solidConduitFlow);

            // Nav breakdown (small iteration — only ~30 dupes typically)
            data.JetSuitCount = NavTypeBreakdown.CountFlyers(liveMinions);
            data.WalkerCount = data.DupeCount - data.JetSuitCount;

            Current = data;
        }

        // SolidConduitFlow.soaInfo is private — use cached FieldInfo
        private static readonly FieldInfo solidSoaField =
            AccessTools.Field(typeof(SolidConduitFlow), "soaInfo");

        private static int GetSolidConduitCount(SolidConduitFlow flow)
        {
            if (flow == null || solidSoaField == null) return 0;
            var soaInfo = solidSoaField.GetValue(flow) as SolidConduitFlow.SOAInfo;
            return soaInfo?.NumEntries ?? 0;
        }
    }
}
