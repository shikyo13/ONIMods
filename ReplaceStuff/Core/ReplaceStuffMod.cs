using System;
using System.Reflection;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using ReplaceStuff.Patches;
using UnityEngine;

namespace ReplaceStuff.Core
{
    /// <summary>
    /// Mod entry point. Registers PLib options and soft-patches modded doors for replacement group compatibility.
    /// </summary>
    public sealed class ReplaceStuffMod : UserMod2
    {
        /// <summary>
        /// Initializes PLib, registers options, and applies runtime patches for any detected modded door configs.
        /// </summary>
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(Config.ReplaceStuffOptions));
            PatchModdedDoors(harmony);
        }

        private static void PatchModdedDoors(Harmony harmony)
        {
            // Fast Insulated Self Sealing AirLock (Workshop ID 3231839363)
            // 1x2 door — fits the Door replacement group
            TryPatchDoor(harmony, "FastInsulatedSelfSealingAirLock.Door");
        }

        private static void TryPatchDoor(Harmony harmony, string typeName)
        {
            var configType = AccessTools.TypeByName(typeName);
            if (configType == null)
                return;

            var createDef = AccessTools.Method(configType, "CreateBuildingDef");
            var postConfig = AccessTools.Method(configType, "DoPostConfigureComplete");

            if (createDef != null)
                harmony.Patch(createDef,
                    postfix: new HarmonyMethod(typeof(ReplaceStuffMod), nameof(DoorDef_Postfix)));
            if (postConfig != null)
                harmony.Patch(postConfig,
                    postfix: new HarmonyMethod(typeof(ReplaceStuffMod), nameof(DoorTag_Postfix)));
        }

        static void DoorDef_Postfix(BuildingDef __result)
        {
            BuildingConfigPatches.InjectReplacementConfig(__result, BuildingConfigPatches.DoorTag);
        }

        static void DoorTag_Postfix(GameObject go)
        {
            BuildingConfigPatches.InjectReplacementTag(go, BuildingConfigPatches.DoorTag);
        }
    }
}
