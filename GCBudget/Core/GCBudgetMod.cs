using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace GCBudget.Core
{
    /// <summary>
    /// Mod entry point. Initializes GCBudgetManager immediately on load to set GC to Manual mode.
    /// </summary>
    public sealed class GCBudgetMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(Config.GCBudgetOptions));
            GCBudgetManager.Init();
        }
    }
}
