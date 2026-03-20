using System;
using HarmonyLib;
using UnityEngine;
using DuplicantStatusBar.Core;

namespace DuplicantStatusBar.Patches
{
    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    internal static class Game_OnPrefabInit_Patch
    {
        static void Postfix(Game __instance)
        {
            try
            {
                DSBLog.Log("Patch", "Game.OnPrefabInit fired - adding StatusBarScreen");
                __instance.gameObject.AddOrGet<UI.StatusBarScreen>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DSB] Failed to add StatusBarScreen: {ex}");
            }
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
            try
            {
                IsScreenOpen = ___activeScreen != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DSB] ManagementMenu.ToggleScreen error: {ex}");
            }
        }
    }
}
