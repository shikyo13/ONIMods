using System;
using System.Collections.Generic;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Database;
using PeterHan.PLib.Options;

namespace FastInsulatedSelfSealingAirLock
{
    public sealed class FastInsulatedSelfSealingAirLockMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PatchClass(harmony, typeof(DatabasePatches));
            PatchClass(harmony, typeof(TranslationPatch));
            PUtil.InitLibrary();
            new PLocalization().Register();
            LocString.CreateLocStringKeys(typeof(ModStrings), string.Empty);
            new POptions().RegisterOptions(this, typeof(Options));
            FISSACLog.Log("Lifecycle", "OnLoad complete; DatabasePatches installed early.");
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
        {
            FISSACLog.Log("Lifecycle", "OnAllModsLoaded patch installation starting.");
            PatchClass(harmony, typeof(StructureTemperatureGetPatch));
            PatchClass(harmony, typeof(DoorAnimPatch));
            PatchClass(harmony, typeof(DoorWorkableGetAnimPatch));
            PatchClass(harmony, typeof(DoorSetSimStatePatch));
            PatchClass(harmony, typeof(DoorOnSpawnPatch));
            PatchClass(harmony, typeof(DoorCleanUpPatch));
            PatchClass(harmony, typeof(GameLeakSamplerPatch));
            FISSACLog.Log("Lifecycle", "OnAllModsLoaded patch installation complete.");
        }

        private static void PatchClass(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.CreateClassProcessor(patchType).Patch();
                FISSACLog.Log("Lifecycle", "Patched " + patchType.FullName);
            }
            catch (Exception ex)
            {
                FISSACLog.Error("PatchClass " + patchType.FullName, ex);
                throw;
            }
        }
    }
}
