using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace ReplaceTool.Core
{
    public sealed class ReplaceToolMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(Config.ReplaceToolOptions));
        }
    }

    [HarmonyPatch(typeof(SaveGame), "OnPrefabInit")]
    internal static class SaveGame_OnPrefabInit_Patch
    {
        internal static void Postfix(SaveGame __instance)
        {
            __instance.gameObject.AddOrGet<Systems.ReplacementTracker>();
            __instance.gameObject.AddOrGet<UI.ReplaceGhostManager>();
        }
    }
}
