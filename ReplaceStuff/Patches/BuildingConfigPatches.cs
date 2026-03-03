using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ReplaceStuff.Patches
{
    /// <summary>
    /// Postfixes on building config methods to inject replacement fields
    /// into vanilla's BuildingDef and KPrefabID systems.
    ///
    /// CreateBuildingDef patches: set ReplacementLayer, ReplacementCandidateLayers,
    /// and ReplacementTags on the BuildingDef so vanilla's TryBuild gate passes.
    ///
    /// DoPostConfigureComplete patches: add group tags to existing buildings'
    /// KPrefabID so CanReplace() finds them via HasAnyTags(ReplacementTags).
    /// </summary>
    public static class BuildingConfigPatches
    {
        // --- Replacement group tags ---
        private static readonly Tag BedTag = TagManager.Create("ReplaceStuff_Bed");
        internal static readonly Tag DoorTag = TagManager.Create("ReplaceStuff_Door");
        private static readonly Tag Generator2x2Tag = TagManager.Create("ReplaceStuff_Generator2x2");
        private static readonly Tag Generator4x3Tag = TagManager.Create("ReplaceStuff_Generator4x3");
        private static readonly Tag StorageTag = TagManager.Create("ReplaceStuff_Storage");
        private static readonly Tag ToiletTag = TagManager.Create("ReplaceStuff_Toilet");
        private static readonly Tag WashStationTag = TagManager.Create("ReplaceStuff_WashStation");

        private static readonly List<ObjectLayer> BuildingCandidateLayers =
            new List<ObjectLayer> { ObjectLayer.Building };

        internal static void InjectReplacementConfig(BuildingDef def, Tag groupTag)
        {
            def.Replaceable = true;
            def.ReplacementLayer = ObjectLayer.ReplacementTravelTube;
            def.ReplacementCandidateLayers = BuildingCandidateLayers;
            def.ReplacementTags = new List<Tag> { groupTag, GameTags.FloorTiles };
        }

        internal static void InjectReplacementTag(GameObject go, Tag groupTag)
        {
            go.GetComponent<KPrefabID>().AddTag(groupTag);
        }

        // ===== Beds (2x2) =====

        [HarmonyPatch(typeof(BedConfig), "CreateBuildingDef")]
        internal static class BedConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, BedTag);
        }

        [HarmonyPatch(typeof(BedConfig), "DoPostConfigureComplete")]
        internal static class BedConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, BedTag);
        }

        [HarmonyPatch(typeof(LadderBedConfig), "CreateBuildingDef")]
        internal static class LadderBedConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, BedTag);
        }

        [HarmonyPatch(typeof(LadderBedConfig), "DoPostConfigureComplete")]
        internal static class LadderBedConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, BedTag);
        }

        // ===== Doors (1x2) =====

        [HarmonyPatch(typeof(DoorConfig), "CreateBuildingDef")]
        internal static class DoorConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, DoorTag);
        }

        [HarmonyPatch(typeof(DoorConfig), "DoPostConfigureComplete")]
        internal static class DoorConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, DoorTag);
        }

        [HarmonyPatch(typeof(ManualPressureDoorConfig), "CreateBuildingDef")]
        internal static class ManualPressureDoorConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, DoorTag);
        }

        [HarmonyPatch(typeof(ManualPressureDoorConfig), "DoPostConfigureComplete")]
        internal static class ManualPressureDoorConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, DoorTag);
        }

        [HarmonyPatch(typeof(PressureDoorConfig), "CreateBuildingDef")]
        internal static class PressureDoorConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, DoorTag);
        }

        [HarmonyPatch(typeof(PressureDoorConfig), "DoPostConfigureComplete")]
        internal static class PressureDoorConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, DoorTag);
        }

        // ===== Generators 2x2 =====

        [HarmonyPatch(typeof(ManualGeneratorConfig), "CreateBuildingDef")]
        internal static class ManualGeneratorConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, Generator2x2Tag);
        }

        [HarmonyPatch(typeof(ManualGeneratorConfig), "DoPostConfigureComplete")]
        internal static class ManualGeneratorConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, Generator2x2Tag);
        }

        [HarmonyPatch(typeof(WoodGasGeneratorConfig), "CreateBuildingDef")]
        internal static class WoodGasGeneratorConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, Generator2x2Tag);
        }

        [HarmonyPatch(typeof(WoodGasGeneratorConfig), "DoPostConfigureComplete")]
        internal static class WoodGasGeneratorConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, Generator2x2Tag);
        }

        // ===== Generators 4x3 =====

        [HarmonyPatch(typeof(HydrogenGeneratorConfig), "CreateBuildingDef")]
        internal static class HydrogenGeneratorConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, Generator4x3Tag);
        }

        [HarmonyPatch(typeof(HydrogenGeneratorConfig), "DoPostConfigureComplete")]
        internal static class HydrogenGeneratorConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, Generator4x3Tag);
        }

        [HarmonyPatch(typeof(MethaneGeneratorConfig), "CreateBuildingDef")]
        internal static class MethaneGeneratorConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, Generator4x3Tag);
        }

        [HarmonyPatch(typeof(MethaneGeneratorConfig), "DoPostConfigureComplete")]
        internal static class MethaneGeneratorConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, Generator4x3Tag);
        }

        // ===== Storage (1x2) =====

        [HarmonyPatch(typeof(StorageLockerConfig), "CreateBuildingDef")]
        internal static class StorageLockerConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, StorageTag);
        }

        [HarmonyPatch(typeof(StorageLockerConfig), "DoPostConfigureComplete")]
        internal static class StorageLockerConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, StorageTag);
        }

        [HarmonyPatch(typeof(StorageLockerSmartConfig), "CreateBuildingDef")]
        internal static class StorageLockerSmartConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, StorageTag);
        }

        [HarmonyPatch(typeof(StorageLockerSmartConfig), "DoPostConfigureComplete")]
        internal static class StorageLockerSmartConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, StorageTag);
        }

        // ===== Toilets (2x3) =====

        [HarmonyPatch(typeof(OuthouseConfig), "CreateBuildingDef")]
        internal static class OuthouseConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, ToiletTag);
        }

        [HarmonyPatch(typeof(OuthouseConfig), "DoPostConfigureComplete")]
        internal static class OuthouseConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, ToiletTag);
        }

        [HarmonyPatch(typeof(FlushToiletConfig), "CreateBuildingDef")]
        internal static class FlushToiletConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, ToiletTag);
        }

        [HarmonyPatch(typeof(FlushToiletConfig), "DoPostConfigureComplete")]
        internal static class FlushToiletConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, ToiletTag);
        }

        // ===== Wash Stations (2x3) =====

        [HarmonyPatch(typeof(WashBasinConfig), "CreateBuildingDef")]
        internal static class WashBasinConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, WashStationTag);
        }

        [HarmonyPatch(typeof(WashBasinConfig), "DoPostConfigureComplete")]
        internal static class WashBasinConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, WashStationTag);
        }

        [HarmonyPatch(typeof(WashSinkConfig), "CreateBuildingDef")]
        internal static class WashSinkConfig_Patch
        {
            static void Postfix(BuildingDef __result) => InjectReplacementConfig(__result, WashStationTag);
        }

        [HarmonyPatch(typeof(WashSinkConfig), "DoPostConfigureComplete")]
        internal static class WashSinkConfig_Tag_Patch
        {
            static void Postfix(GameObject go) => InjectReplacementTag(go, WashStationTag);
        }
    }
}
