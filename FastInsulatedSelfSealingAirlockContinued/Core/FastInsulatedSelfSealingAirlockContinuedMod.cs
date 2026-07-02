using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Database;
using PeterHan.PLib.Options;

namespace FastInsulatedSelfSealingAirlockContinued
{
    public sealed class FastInsulatedSelfSealingAirlockContinuedMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PLocalization().Register();
            LocString.CreateLocStringKeys(typeof(ModStrings), string.Empty);
            new POptions().RegisterOptions(this, typeof(Options));
        }
    }
}
