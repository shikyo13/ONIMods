using HarmonyLib;

namespace FastInsulatedSelfSealingAirLock
{
    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class DatabasePatches
    {
        public static void Prefix()
        {
            string prefix = "STRINGS.BUILDINGS.PREFABS." + AirlockConstants.BuildingId.ToUpperInvariant();
            Strings.Add(prefix + ".NAME", ModStrings.Building.NAME);
            Strings.Add(prefix + ".EFFECT", ModStrings.Building.EFFECT);
            Strings.Add(prefix + ".DESC", ModStrings.Building.DESC);
        }

        public static void Postfix()
        {
            Db.Get().Techs.TryGet(AirlockConstants.Tech)?.unlockedItemIDs.Add(AirlockConstants.BuildingId);
            ModUtil.AddBuildingToPlanScreen(
                AirlockConstants.Category,
                AirlockConstants.BuildingId,
                "doors",
                AirlockConstants.AfterBuildingId,
                ModUtil.BuildingOrdering.After);
        }
    }
}
