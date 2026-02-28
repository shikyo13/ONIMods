using HarmonyLib;
using ReplaceTool.Systems;

namespace ReplaceTool.Patches
{
    /// <summary>
    /// Atomic cancellation: cancel one errand (decon or build) → cancel the paired errand.
    /// </summary>
    public static class CancelPatches
    {
        [HarmonyPatch(typeof(Cancellable), "OnCancel")]
        internal static class Cancellable_OnCancel_Patch
        {
            /// <summary>
            /// Track that we're inside a cancellation triggered by the tracker,
            /// so we don't recurse infinitely.
            /// </summary>
            private static bool _suppressReentrant;

            internal static void Postfix(Cancellable __instance)
            {
                if (_suppressReentrant)
                    return;

                if (ReplacementTracker.Instance == null)
                    return;

                int cell = Grid.PosToCell(__instance.transform.position);
                if (!ReplacementTracker.Instance.HasReplacement(cell))
                    return;

                try
                {
                    _suppressReentrant = true;
                    ReplacementTracker.Instance.OnCancelled(cell);
                }
                finally
                {
                    _suppressReentrant = false;
                }
            }
        }
    }
}
