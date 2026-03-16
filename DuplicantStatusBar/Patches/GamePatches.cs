using HarmonyLib;
using DuplicantStatusBar.Core;

namespace DuplicantStatusBar.Patches
{
    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    internal static class Game_OnPrefabInit_Patch
    {
        static void Postfix(Game __instance)
        {
            DSBLog.Log("Patch", "Game.OnPrefabInit fired — adding StatusBarScreen");
            __instance.gameObject.AddOrGet<UI.StatusBarScreen>();
        }
    }
}
