using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace DuplicantStatusBar.Core
{
    /// <summary>
    /// Mod entry point. Registers PLib options for the duplicant status bar HUD.
    /// </summary>
    public sealed class DuplicantStatusBarMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(Config.StatusBarOptions));
        }
    }
}
