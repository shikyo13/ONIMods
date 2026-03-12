using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace BuildThrough.Core
{
    /// <summary>
    /// Mod entry point. Registers PLib options; all patching handled by attribute-driven Harmony patches.
    /// </summary>
    public sealed class BuildThroughMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(Config.BuildThroughOptions));
        }
    }
}
