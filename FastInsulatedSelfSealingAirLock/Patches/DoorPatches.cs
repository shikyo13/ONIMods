using System.Collections.Generic;
using System;
using HarmonyLib;
using UnityEngine;

namespace FastInsulatedSelfSealingAirLock
{
    internal static class DoorPatchHelpers
    {
        private static readonly HashSet<string> DoorRemoteOverrideFallbackPrefabIds = new HashSet<string>(StringComparer.Ordinal)
        {
            AirlockConstants.BuildingId,
            "Door",
            "ManualPressureDoor",
            "PressureDoor",
            "BunkerDoor",
            "WoodenDoor",
            "InsulatedDoor",
            "GravitasDoor",
            "POIFacilityDoor",
            "POIDlc2ShowroomDoor",
            "POIDoorInternal"
        };

        public static bool IsFastAirlockDoor(Door door)
        {
            var prefabId = door?.GetComponent<KPrefabID>();
            return prefabId != null && prefabId.PrefabTag == AirlockConstants.BuildingId;
        }

        public static bool ShouldUseDoorRemoteOverrideFallback(Door door)
        {
            var prefabId = door?.GetComponent<KPrefabID>();
            return prefabId != null && DoorRemoteOverrideFallbackPrefabIds.Contains(prefabId.PrefabTag.ToString());
        }

        public static bool IsFastAirlockPrimaryElement(PrimaryElement primaryElement)
        {
            var prefabId = primaryElement?.GetComponent<KPrefabID>();
            return prefabId != null && prefabId.PrefabTag == AirlockConstants.BuildingId;
        }

        public static void EnsureValidDoorTemperature(Door door, IList<int> cells, string phase)
        {
            if (!IsFastAirlockDoor(door))
            {
                return;
            }

            var primaryElement = door.GetComponent<PrimaryElement>();
            if (primaryElement == null || primaryElement.Temperature > 0f)
            {
                return;
            }

            float repairedTemperature = GetBestDoorTemperature(primaryElement, cells);
            if (repairedTemperature <= 0f)
            {
                return;
            }

            float previousTemperature = primaryElement.Temperature;
            primaryElement.Temperature = repairedTemperature;
            DoorDiagnostics.LogTemperatureRepair(phase, door, previousTemperature, repairedTemperature);
        }

        public static void RepairInvalidTemperatureRead(PrimaryElement primaryElement, ref float __result, string phase)
        {
            if (__result > 0f || !IsFastAirlockPrimaryElement(primaryElement))
            {
                return;
            }

            var building = primaryElement.GetComponent<Building>();
            float repairedTemperature = GetBestDoorTemperature(primaryElement, building?.PlacementCells);
            if (repairedTemperature <= 0f)
            {
                return;
            }

            float previousTemperature = __result;
            __result = repairedTemperature;
            bool updatedInternalTemperature = primaryElement.InternalTemperature <= 0f;
            if (updatedInternalTemperature)
            {
                primaryElement.InternalTemperature = repairedTemperature;
            }

            DoorDiagnostics.LogTemperatureReadRepair(phase, primaryElement, previousTemperature, repairedTemperature, updatedInternalTemperature);
        }

        public static void ApplySelfSealingState(Door door, IList<int> cells, string phase)
        {
            if (!IsFastAirlockDoor(door) || cells == null)
            {
                return;
            }

            bool allowTransmission = door.CurrentState == Door.ControlState.Opened;
            DoorDiagnostics.LogSelfSealingApplication(phase + ".before", door, cells, allowTransmission);

            try
            {
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
            catch (Exception ex)
            {
                DoorDiagnostics.LogPatchException("ApplySelfSealingState." + phase, door, ex);
            }

            DoorDiagnostics.LogSelfSealingApplication(phase + ".after", door, cells, allowTransmission);
        }

        private static float GetBestDoorTemperature(PrimaryElement primaryElement, IList<int> cells)
        {
            if (primaryElement.InternalTemperature > 0f)
            {
                return primaryElement.InternalTemperature;
            }

            float cellTemperatureTotal = 0f;
            int cellTemperatureCount = 0;

            if (cells != null)
            {
                foreach (int cell in cells)
                {
                    if (!Grid.IsValidCell(cell) || Grid.Temperature[cell] <= 0f)
                    {
                        continue;
                    }

                    cellTemperatureTotal += Grid.Temperature[cell];
                    cellTemperatureCount++;
                }
            }

            if (cellTemperatureCount > 0)
            {
                return cellTemperatureTotal / cellTemperatureCount;
            }

            Element element = ElementLoader.FindElementByHash(primaryElement.ElementID);
            if (element != null && element.defaultValues.temperature > 0f)
            {
                return element.defaultValues.temperature;
            }

            return 300f;
        }
    }

    [HarmonyPatch(typeof(StructureTemperatureComponents), "OnGetTemperature")]
    public static class StructureTemperatureGetPatch
    {
        public static void Postfix(PrimaryElement primary_element, ref float __result)
        {
            try
            {
                DoorPatchHelpers.RepairInvalidTemperatureRead(primary_element, ref __result, "StructureTemperature.OnGetTemperature");
            }
            catch (Exception ex)
            {
                FISSACLog.Error("StructureTemperatureGetPatch.Postfix", ex);
            }
        }
    }

    [HarmonyPatch(typeof(Door), "OnPrefabInit")]
    internal static class DoorAnimPatch
    {
        private const string DoorRemoteWorkAnim = "anim_use_remote_kanim";
        private static readonly KAnimFile[] NoOverrideAnims = new KAnimFile[0];
        private static KAnimFile[] resolvedDoorOverrideAnims;

        private static void Postfix(Door __instance)
        {
            try
            {
                bool isFastAirlockDoor = DoorPatchHelpers.IsFastAirlockDoor(__instance);
                RepairDoorOverrideAnims(__instance, "OnPrefabInit.Postfix");

                if (isFastAirlockDoor)
                {
                    DoorDiagnostics.LogPrefabInit(__instance);
                }
            }
            catch (Exception ex)
            {
                DoorDiagnostics.LogPatchException("DoorAnimPatch.Postfix", __instance, ex);
            }
        }

        private static void RepairDoorOverrideAnims(Door door, string phase)
        {
            if (door == null)
            {
                return;
            }

            KAnimFile[] previousOverrideAnims = door.overrideAnims;
            KAnimFile[] safeOverrideAnims = GetSafeDoorOverrideAnims(previousOverrideAnims);
            if ((safeOverrideAnims == null || safeOverrideAnims.Length == 0)
                && DoorPatchHelpers.ShouldUseDoorRemoteOverrideFallback(door)
                && TryGetDoorOverrideAnims(out var doorOverrideAnims))
            {
                safeOverrideAnims = doorOverrideAnims;
            }

            door.overrideAnims = safeOverrideAnims;
            DoorDiagnostics.LogOverrideAnimRepair(phase, door, previousOverrideAnims, safeOverrideAnims);
        }

        internal static KAnimFile[] GetSafeDoorOverrideAnims(KAnimFile[] overrideAnims)
        {
            if (overrideAnims == null || overrideAnims.Length == 0)
            {
                return overrideAnims;
            }

            int safeCount = 0;
            for (int index = 0; index < overrideAnims.Length; index++)
            {
                if (overrideAnims[index] != null)
                {
                    safeCount++;
                }
            }

            if (safeCount == overrideAnims.Length)
            {
                return overrideAnims;
            }

            if (safeCount == 0)
            {
                return NoOverrideAnims;
            }

            var safeOverrideAnims = new KAnimFile[safeCount];
            int safeIndex = 0;
            for (int index = 0; index < overrideAnims.Length; index++)
            {
                if (overrideAnims[index] != null)
                {
                    safeOverrideAnims[safeIndex++] = overrideAnims[index];
                }
            }

            return safeOverrideAnims;
        }

        internal static bool TryGetDoorOverrideAnims(out KAnimFile[] doorOverrideAnims)
        {
            if (resolvedDoorOverrideAnims != null)
            {
                doorOverrideAnims = resolvedDoorOverrideAnims;
                return true;
            }

            if (Assets.TryGetAnim(DoorRemoteWorkAnim, out var remoteAnim) && remoteAnim != null)
            {
                resolvedDoorOverrideAnims = new[] { remoteAnim };
                doorOverrideAnims = resolvedDoorOverrideAnims;
                return true;
            }

            doorOverrideAnims = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(Workable), nameof(Workable.GetAnim))]
    internal static class DoorWorkableGetAnimPatch
    {
        private static void Postfix(Workable __instance, ref Workable.AnimInfo __result)
        {
            try
            {
                if (!(__instance is Door door))
                {
                    return;
                }

                __result.overrideAnims = DoorAnimPatch.GetSafeDoorOverrideAnims(__result.overrideAnims);
                if (!DoorPatchHelpers.ShouldUseDoorRemoteOverrideFallback(door))
                {
                    DoorDiagnostics.LogWorkableAnimInfo("Workable.GetAnim.Postfix", door, __result.overrideAnims);
                    return;
                }

                if ((__result.overrideAnims == null || __result.overrideAnims.Length == 0)
                    && DoorAnimPatch.TryGetDoorOverrideAnims(out var doorOverrideAnims))
                {
                    __result.overrideAnims = doorOverrideAnims;
                }

                DoorDiagnostics.LogWorkableAnimInfo("Workable.GetAnim.Postfix", door, __result.overrideAnims);
            }
            catch (Exception ex)
            {
                DoorDiagnostics.LogPatchException("DoorWorkableGetAnimPatch.Postfix", __instance as Door, ex);
            }
        }
    }

    [HarmonyPatch(typeof(Door), "SetSimState")]
    public static class DoorSetSimStatePatch
    {
        public static void Prefix(Door __instance, IList<int> cells)
        {
            try
            {
                DoorPatchHelpers.EnsureValidDoorTemperature(__instance, cells, "SetSimState.Prefix");
            }
            catch (Exception ex)
            {
                DoorDiagnostics.LogPatchException("DoorSetSimStatePatch.Prefix", __instance, ex);
            }
        }

        public static void Postfix(Door __instance, IList<int> cells)
        {
            try
            {
                DoorPatchHelpers.ApplySelfSealingState(__instance, cells, "SetSimState.Postfix");
            }
            catch (Exception ex)
            {
                DoorDiagnostics.LogPatchException("DoorSetSimStatePatch.Postfix", __instance, ex);
            }
        }
    }

    [HarmonyPatch(typeof(Door), "OnSpawn")]
    public static class DoorOnSpawnPatch
    {
        public static void Postfix(Door __instance)
        {
            try
            {
                DoorDiagnostics.RegisterDoor(__instance);
                DoorPatchHelpers.ApplySelfSealingState(__instance, __instance.building?.PlacementCells, "OnSpawn.Postfix");
            }
            catch (Exception ex)
            {
                DoorDiagnostics.LogPatchException("DoorOnSpawnPatch.Postfix", __instance, ex);
            }
        }
    }

    [HarmonyPatch(typeof(Door), "OnCleanUp")]
    public static class DoorCleanUpPatch
    {
        public static void Prefix(Door __instance)
        {
            try
            {
                if (!DoorPatchHelpers.IsFastAirlockDoor(__instance) || __instance.building?.PlacementCells == null)
                {
                    return;
                }

                DoorDiagnostics.LogCleanUp(__instance, __instance.building.PlacementCells);
                DoorDiagnostics.UnregisterDoor(__instance);

                foreach (int cell in __instance.building.PlacementCells)
                {
                    SimMessages.ClearCellProperties(cell, AirlockConstants.DoorCellProperties, AirlockConstants.NoCallback);
                }
            }
            catch (Exception ex)
            {
                DoorDiagnostics.LogPatchException("DoorCleanUpPatch.Prefix", __instance, ex);
            }
        }
    }

    [HarmonyPatch(typeof(Game), "UnsafeSim200ms")]
    public static class GameLeakSamplerPatch
    {
        public static void Postfix()
        {
            try
            {
                DoorDiagnostics.SampleRegisteredDoors();
            }
            catch (Exception ex)
            {
                FISSACLog.Error("GameLeakSamplerPatch.Postfix", ex);
            }
        }
    }
}
