using GCBudget.Core;
using HarmonyLib;

namespace GCBudget.Patches
{
    [HarmonyPatch(typeof(Game), "Update")]
    internal static class GameUpdatePatch
    {
        static void Postfix()
        {
            GCBudgetManager.OnFrame();
        }
    }
}
