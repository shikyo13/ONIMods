using HarmonyLib;
using KMod;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using OniProfiler.Timing;
using OniProfiler.UI;

namespace OniProfiler.Core
{
    public sealed class OniProfilerMod : UserMod2
    {
        public static OniProfilerMod Instance { get; private set; }
        public static PAction ToggleAction { get; private set; }

        public override void OnLoad(Harmony harmony)
        {
            Instance = this;
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(Config.OniProfilerOptions));
            ToggleAction = new PActionManager().CreateAction(
                "OniProfiler.TogglePanel",
                "Toggle OniProfiler",
                new PKeyBinding(KKeyCode.F8, Modifier.None));
        }
    }

    /// <summary>
    /// Attaches the profiler overlay MonoBehaviour once the game is running.
    /// </summary>
    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    internal static class Game_OnPrefabInit_Patch
    {
        static void Postfix(Game __instance)
        {
            __instance.gameObject.AddOrGet<ProfilerOverlay>();
        }
    }
}
