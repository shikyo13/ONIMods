using HarmonyLib;
using DuplicantStatusBar.Core;

namespace DuplicantStatusBar.Patches
{
    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    internal static class Game_OnPrefabInit_Patch
    {
        static void Postfix(Game __instance)
        {
            DSBLog.Log("Patch", "Game.OnPrefabInit fired - adding StatusBarScreen");
            __instance.gameObject.AddOrGet<UI.StatusBarScreen>();
        }
    }

    /// <summary>
    /// Tracks whether a management screen is currently open.
    /// ManagementMenu.IsFullscreenUIActive() is broken (fullscreenUIs array
    /// is populated before its members are assigned, so it contains nulls).
    /// This patch watches ToggleScreen to track state reliably.
    /// </summary>
    [HarmonyPatch(typeof(ManagementMenu), nameof(ManagementMenu.ToggleScreen))]
    internal static class ManagementMenu_ToggleScreen_Patch
    {
        internal static bool IsScreenOpen;

        static void Postfix(ManagementMenu __instance, ManagementMenu.ScreenData ___activeScreen)
        {
            IsScreenOpen = ___activeScreen != null;
        }
    }
}
