using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace FastInsulatedSelfSealingAirLock
{
    internal static class DoorPatchHelpers
    {
        public static bool IsFastAirlockDoor(Door door)
        {
            var prefabId = door?.GetComponent<KPrefabID>();
            return prefabId != null && prefabId.PrefabTag == AirlockConstants.BuildingId;
        }

        public static void ApplySelfSealingState(Door door, IList<int> cells)
        {
            if (!IsFastAirlockDoor(door) || cells == null)
            {
                return;
            }

            bool allowTransmission = door.CurrentState == Door.ControlState.Opened;
            foreach (int cell in cells)
            {
                if (allowTransmission)
                {
                    SimMessages.SetInsulation(cell, 1f);
                    SimMessages.ClearCellProperties(cell, AirlockConstants.DoorCellProperties, AirlockConstants.NoCallback);
                }
                else
                {
                    SimMessages.SetInsulation(cell, 0f);
                    SimMessages.SetCellProperties(cell, AirlockConstants.DoorCellProperties, AirlockConstants.NoCallback);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Door), "OnPrefabInit")]
    internal static class DoorAnimPatch
    {
        private static void Postfix(Door __instance)
        {
            if (!DoorPatchHelpers.IsFastAirlockDoor(__instance))
            {
                return;
            }

            var remoteAnim = Assets.GetAnim("anim_use_remote_kanim");
            if (remoteAnim != null)
            {
                __instance.overrideAnims = new[] { remoteAnim };
            }
        }
    }

    [HarmonyPatch(typeof(Door), "SetSimState")]
    public static class DoorSetSimStatePatch
    {
        public static void Postfix(Door __instance, IList<int> cells)
        {
            DoorPatchHelpers.ApplySelfSealingState(__instance, cells);
        }
    }

    [HarmonyPatch(typeof(Door), "OnSpawn")]
    public static class DoorOnSpawnPatch
    {
        public static void Postfix(Door __instance)
        {
            DoorPatchHelpers.ApplySelfSealingState(__instance, __instance.building?.PlacementCells);
        }
    }

    [HarmonyPatch(typeof(Door), "OnCleanUp")]
    public static class DoorCleanUpPatch
    {
        public static void Prefix(Door __instance)
        {
            if (!DoorPatchHelpers.IsFastAirlockDoor(__instance) || __instance.building?.PlacementCells == null)
            {
                return;
            }

            foreach (int cell in __instance.building.PlacementCells)
            {
                SimMessages.ClearCellProperties(cell, AirlockConstants.DoorCellProperties, AirlockConstants.NoCallback);
            }
        }
    }
}
