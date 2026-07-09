using PeterHan.PLib.Options;
using TUNING;
using UnityEngine;

namespace FastInsulatedSelfSealingAirLock
{
    internal sealed class FastInsulatedDoorConfig : IBuildingConfig
    {
        private readonly int multiplier = SingletonOptions<Options>.Instance.Multiplier;

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                AirlockConstants.BuildingId,
                1,
                2,
                "Insulated_door_manual_kanim",
                30,
                60f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
                MATERIALS.ALL_METALS,
                1600f,
                BuildLocationRule.Tile,
                BUILDINGS.DECOR.PENALTY.TIER2,
                NOISE_POLLUTION.NONE,
                1f);

            def.ThermalConductivity = 0f;
            def.BaseMeltingPoint = 9999f;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.IsFoundation = true;
            def.TileLayer = ObjectLayer.FoundationTile;
            def.AudioCategory = "Metal";
            def.PermittedRotations = PermittedRotations.R90;
            def.SceneLayer = Grid.SceneLayer.TileMain;
            def.ForegroundLayer = Grid.SceneLayer.InteriorWall;

            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_gear_LP", NOISE_POLLUTION.NOISY.TIER1);
            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_open", NOISE_POLLUTION.NOISY.TIER2);
            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_close", NOISE_POLLUTION.NOISY.TIER2);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            var door = go.AddOrGet<Door>();
            door.hasComplexUserControls = true;
            door.poweredAnimSpeed = multiplier;
            door.unpoweredAnimSpeed = multiplier;
            door.doorType = Door.DoorType.ManualPressure;

            go.AddOrGet<Insulator>();
            go.AddOrGet<ZoneTile>();
            go.AddOrGet<AccessControl>();
            go.AddOrGet<KBoxCollider2D>();
            Prioritizable.AddRef(go);
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Door;
            go.AddOrGet<Workable>().workTime = 5f;
            Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<AccessControl>().controlEnabled = true;
            go.GetComponent<KBatchedAnimController>().initialAnim = "closed";
        }
    }
}
