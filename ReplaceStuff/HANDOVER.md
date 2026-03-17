# ReplaceStuff — Handover

One-click replacement of same-footprint furniture buildings (e.g., Cot to Comfy Bed, Outhouse to Lavatory). Piggybacks on vanilla's built-in tile replacement system by injecting config fields and group tags into existing building defs at load time.

**Version**: v2.1, active
**Branch**: master

## Architecture

| File | Purpose |
|-|-|
| `Core/ReplaceStuffMod.cs` | UserMod2 entry, PLib init, soft modded-door patches (~60 lines) |
| `Patches/BuildingConfigPatches.cs` | 28 attribute postfixes injecting replacement config into 14 vanilla buildings (~267 lines) |
| `Patches/BuildToolPatches.cs` | `IsValidReplaceLocation` postfix — guards preview when `CanReplace` fails (~31 lines) |
| `Patches/ConstructionPatches.cs` | `OnCompleteWork` postfix — multi-cell tile cleanup with refund (~63 lines) |
| `UI/ReplaceToolTip.cs` | "Replacing X → Y" tooltip for furniture replacements (~57 lines) |
| `Config/ReplaceStuffOptions.cs` | `EnableBuildings` toggle, `SingletonOptions` pattern (~18 lines) |

## Data Flow

1. Game loads → `BuildingConfigPatches` postfixes inject `ReplacementLayer`, `ReplacementTags`, group tags into building defs
2. Player clicks build on occupied cell → vanilla's `TryBuild` sees `Replaceable = true` + matching tags
3. `BuildToolPatches.IsValidReplaceLocation` postfix rejects if `CanReplace` fails (wrong group)
4. Vanilla handles: deconstruct old → build new (same-frame, no gap)
5. `ConstructionPatches.OnCompleteWork` postfix cleans up non-anchor tiles for multi-cell buildings
6. `ReplaceToolTip` shows "Replacing X → Y" on hover

## Key Design Decisions

- **ReplacementLayer = ReplacementTravelTube** (permanent, set at config time). Using `ObjectLayer.Building` would block via `IsReplacementLayerOccupied`
- **Config injection pattern**: `InjectReplacementConfig(def, tag)` + `InjectReplacementTag(go, tag)` via DoPostConfigureComplete
- **Footprint-strict groups**: only same `WidthInCells` x `HeightInCells` buildings share tags
- **Soft modded-door compat**: `AccessTools.TypeByName` + manual `harmony.Patch` in OnLoad — silently skips if mod absent
- **Supported modded door**: Fast Insulated Self Sealing AirLock (Workshop 3231839363, class `FastInsulatedSelfSealingAirLock.Door`, 1x2)
- **Tile replacement**: `GameTags.FloorTiles` added to `ReplacementTags` — tiles pass `CanReplace()`, vanilla's `TryReplaceTile(replace_tile:true)` exempts them from `IsAreaClear`
- **Multi-cell cleanup**: vanilla's `OnCompleteWork` only destroys anchor cell's tile; `ConstructionPatches` postfix destroys non-anchor tiles (refund via `Deconstructable.SpawnItemsFromConstruction`, sim cleanup via `SimCellOccupier.DestroySelf(callback)`, then `DeleteObject` in callback)

## Replacement Groups

| Group | Tag | Size | Buildings |
|-|-|-|-|
| Bed | `ReplaceStuff_Bed` | 2x2 | BedConfig, LadderBedConfig |
| Door | `ReplaceStuff_Door` | 1x2 | DoorConfig, ManualPressureDoorConfig, PressureDoorConfig, +modded |
| Generator 2x2 | `ReplaceStuff_Generator2x2` | 2x2 | ManualGeneratorConfig, WoodGasGeneratorConfig |
| Generator 4x3 | `ReplaceStuff_Generator4x3` | 4x3 | HydrogenGeneratorConfig, MethaneGeneratorConfig |
| Storage | `ReplaceStuff_Storage` | 1x2 | StorageLockerConfig, StorageLockerSmartConfig |
| Toilet | `ReplaceStuff_Toilet` | 2x3 | OuthouseConfig, FlushToiletConfig |
| Wash Station | `ReplaceStuff_WashStation` | 2x3 | WashBasinConfig, WashSinkConfig |

## Configuration

Single option `EnableBuildings` (bool toggle) via PLib mod options.

## Previous Bugs (resolved)

- 6 configs don't override `ConfigureBuildingTemplate` — switched all tag patches to `DoPostConfigureComplete`
- `ObjectLayer.Building` as ReplacementLayer caused `IsReplacementLayerOccupied` to block
- `Constructable.OnCompleteWork` is `protected` — use string `"OnCompleteWork"` in HarmonyPatch, not `nameof()`

## Not Yet Implemented

None currently tracked.
