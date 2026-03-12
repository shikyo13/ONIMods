using HarmonyLib;
using KMod;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using OniProfiler.Memory;
using OniProfiler.Timing;
using OniProfiler.UI;

namespace OniProfiler.Core
{
    /// <summary>
    /// Mod entry point. Stores singleton instance, registers PLib toggle keybind (backtick), and logs GC capabilities.
    /// </summary>
    public sealed class OniProfilerMod : UserMod2
    {
        /// <summary>Singleton instance, set during OnLoad.</summary>
        public static OniProfilerMod Instance { get; private set; }
        /// <summary>PLib keybinding action for toggling the profiler panel.</summary>
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
                new PKeyBinding(KKeyCode.BackQuote, Modifier.None));

            GCMonitor.LogCapabilities();
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
