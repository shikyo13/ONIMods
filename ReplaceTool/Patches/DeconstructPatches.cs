using HarmonyLib;
using ReplaceTool.Systems;

namespace ReplaceTool.Patches
{
    /// <summary>
    /// When deconstruction finishes on a tracked cell, trigger the build phase.
    /// </summary>
    public static class DeconstructPatches
    {
        [HarmonyPatch(typeof(Deconstructable), "OnCompleteWork")]
        internal static class Deconstructable_OnCompleteWork_Patch
        {
            internal static void Postfix(Deconstructable __instance)
            {
                if (ReplacementTracker.Instance == null)
                    return;

                int cell = Grid.PosToCell(__instance.transform.position);
                if (ReplacementTracker.Instance.HasReplacement(cell))
                {
                    ReplacementTracker.Instance.OnDeconstructComplete(cell);
                }
            }
        }
    }
}
