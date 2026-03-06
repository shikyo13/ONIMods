using GCBudget.Core;
using HarmonyLib;

namespace GCBudget.Patches
{
    [HarmonyPatch(typeof(SaveLoader), nameof(SaveLoader.Save),
        new[] { typeof(string), typeof(bool), typeof(bool) })]
    internal static class SaveLoaderPatch
    {
        static void Prefix() => GCBudgetManager.OnSave();
    }

    [HarmonyPatch(typeof(SpeedControlScreen), "TogglePause")]
    internal static class PausePatch
    {
        static void Postfix()
        {
            if (SpeedControlScreen.Instance != null && SpeedControlScreen.Instance.IsPaused)
                GCBudgetManager.OnPause();
        }
    }

    [HarmonyPatch(typeof(Game), "OnDestroy")]
    internal static class GameDestroyPatch
    {
        static void Prefix() => GCBudgetManager.Restore();
    }

    [HarmonyPatch(typeof(Game), "OnApplicationQuit")]
    internal static class GameQuitPatch
    {
        static void Prefix() => GCBudgetManager.Restore();
    }
}
