using HarmonyLib;
using UnityEngine;

namespace ReplaceStuff.Patches
{
    /// <summary>
    /// Guards vanilla's replacement preview for furniture buildings.
    ///
    /// With ReplacementLayer set permanently to ReplacementTravelTube, vanilla's
    /// TryBuild gate passes natively. However, vanilla's IsValidReplaceLocation
    /// doesn't check CanReplace — so without this postfix, the build preview
    /// would show green for ANY furniture on ANY other furniture (e.g. Bed on Door).
    ///
    /// This postfix rejects replacements where CanReplace fails (wrong group tag).
    /// </summary>
    public static class BuildToolPatches
    {
        [HarmonyPatch(typeof(BuildingDef), "IsValidReplaceLocation",
            new[] { typeof(Vector3), typeof(Orientation), typeof(ObjectLayer), typeof(ObjectLayer) })]
        internal static class BuildingDef_IsValidReplaceLocation_Patch
        {
            static void Postfix(BuildingDef __instance, ref bool __result, Vector3 pos)
            {
                // Only guard when vanilla already approved
                if (!__result)
                    return;

                // Only for our furniture defs (they have ReplacementCandidateLayers set)
                if (__instance.ReplacementCandidateLayers == null || __instance.ReplacementCandidateLayers.Count == 0)
                    return;

                // Reject if CanReplace fails (different replacement group)
                int cell = Grid.PosToCell(pos);
                var candidate = __instance.GetReplacementCandidate(cell);
                if (candidate == null || !__instance.CanReplace(candidate))
                    __result = false;
            }
        }
    }
}
