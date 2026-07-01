using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FastInsulatedSelfSealingAirLock
{
    [HarmonyPatch(typeof(Door), "OnPrefabInit")]
    internal static class DoorAnimPatch
    {
        private static void Postfix(Door __instance)
        {
            __instance.overrideAnims = new[] { Assets.GetAnim("anim_use_remote_kanim") };
        }
    }

    [HarmonyPatch(typeof(Door), "SetSimState")]
    public static class DoorSetSimStatePatch
    {
        public static void Prefix(Door __instance, bool is_door_open, IList<int> cells)
        {
            if (__instance.GetComponent<KPrefabID>().PrefabTag != AirlockConstants.BuildingId)
            {
                return;
            }

            var controlState = Traverse.Create(__instance).Field("controlState").GetValue<Door.ControlState>();
            foreach (int cell in cells)
            {
                SimMessages.SetInsulation(cell, 0f);
                if (controlState != Door.ControlState.Opened)
                {
                    SimMessages.SetCellProperties(cell, AirlockConstants.DoorCellProperties, AirlockConstants.NoCallback);
                }
                else
                {
                    SimMessages.ClearCellProperties(cell, AirlockConstants.DoorCellProperties, AirlockConstants.NoCallback);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Door), "OnCleanUp")]
    public static class DoorCleanUpPatch
    {
        public static void Prefix(Door __instance)
        {
            foreach (int cell in __instance.building.PlacementCells)
            {
                SimMessages.ClearCellProperties(cell, AirlockConstants.DoorCellProperties, AirlockConstants.NoCallback);
            }
        }
    }
}
