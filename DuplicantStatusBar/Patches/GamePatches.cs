using HarmonyLib;

namespace DuplicantStatusBar.Patches
{
    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    internal static class Game_OnPrefabInit_Patch
    {
        static void Postfix(Game __instance)
        {
            __instance.gameObject.AddOrGet<UI.StatusBarScreen>();
        }
    }
}
