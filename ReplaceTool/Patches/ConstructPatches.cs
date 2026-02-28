using HarmonyLib;
using ReplaceTool.Systems;

namespace ReplaceTool.Patches
{
    /// <summary>
    /// When construction finishes on a tracked cell, clean up the replacement entry.
    /// </summary>
    public static class ConstructPatches
    {
        [HarmonyPatch(typeof(Constructable), "OnCompleteWork")]
        internal static class Constructable_OnCompleteWork_Patch
        {
            internal static void Postfix(Constructable __instance)
            {
                if (ReplacementTracker.Instance == null)
                    return;

                int cell = Grid.PosToCell(__instance.transform.position);
                if (ReplacementTracker.Instance.HasReplacement(cell))
                {
                    ReplacementTracker.Instance.OnBuildComplete(cell);
                }
            }
        }
    }
}
