# Tier 2 — ReplaceStuff Design Reference

Read when editing ReplaceStuff mod. Source on `v2-vanilla-replacement` branch.

## What It Does

Enables one-click replacement of same-footprint furniture buildings (e.g., Cot → Comfy Bed, Outhouse → Lavatory). Piggybacks on vanilla's built-in tile replacement system (`ReplacementLayer`, `CanReplace`, `TryReplaceTile`) by injecting config fields and group tags into existing building defs at load time.

## Architecture

| File | Purpose | Lines |
|-|-|-|
| `Core/ReplaceStuffMod.cs` | Entry point, PLib init, soft modded-door patches | 60 |
| `Patches/BuildingConfigPatches.cs` | 28 attribute postfixes injecting replacement config into 14 vanilla buildings | 267 |
| `Patches/BuildToolPatches.cs` | `IsValidReplaceLocation` postfix — guards preview when `CanReplace` fails | 31 |
| `Patches/ConstructionPatches.cs` | `OnCompleteWork` postfix — multi-cell tile cleanup with refund | 63 |
| `UI/ReplaceToolTip.cs` | "Replacing X → Y" tooltip for furniture replacements | 57 |
| `Config/ReplaceStuffOptions.cs` | `EnableBuildings` toggle, `SingletonOptions` pattern | 18 |

## Replacement Groups (footprint-strict)

| Group | Tag | Size | Buildings |
|-|-|-|-|
| Bed | `ReplaceStuff_Bed` | 2×2 | BedConfig, LadderBedConfig |
| Door | `ReplaceStuff_Door` | 1×2 | DoorConfig, ManualPressureDoorConfig, PressureDoorConfig, +modded |
| Generator 2×2 | `ReplaceStuff_Generator2x2` | 2×2 | ManualGeneratorConfig, WoodGasGeneratorConfig |
| Generator 4×3 | `ReplaceStuff_Generator4x3` | 4×3 | HydrogenGeneratorConfig, MethaneGeneratorConfig |
| Storage | `ReplaceStuff_Storage` | 1×2 | StorageLockerConfig, StorageLockerSmartConfig |
| Toilet | `ReplaceStuff_Toilet` | 2×3 | OuthouseConfig, FlushToiletConfig |
| Wash Station | `ReplaceStuff_WashStation` | 2×3 | WashBasinConfig, WashSinkConfig |

## Key Technical Decisions

- **ReplacementLayer = ReplacementTravelTube**: Permanent, set at config time. Using `ObjectLayer.Building` would block via `IsReplacementLayerOccupied`
- **Config injection pattern**: `InjectReplacementConfig(def, tag)` sets `Replaceable`, `ReplacementLayer`, `ReplacementCandidateLayers`, `ReplacementTags` on BuildingDef. `InjectReplacementTag(go, tag)` adds group tag to KPrefabID via `DoPostConfigureComplete`
- **Footprint-strict groups**: Only same `WidthInCells × HeightInCells` buildings share tags — prevents mismatched replacements
- **`GameTags.FloorTiles` in ReplacementTags**: Tiles pass `CanReplace()`, vanilla's `TryReplaceTile(replace_tile:true)` exempts them from `IsAreaClear`

## Modded Door Compatibility

Soft-patches third-party mods at runtime:
- `AccessTools.TypeByName("Namespace.ClassName")` — returns null if assembly not loaded
- Manual `harmony.Patch(method, postfix: ...)` for runtime patching
- Supported: **Fast Insulated Self Sealing AirLock** (Workshop 3231839363, class `FastInsulatedSelfSealingAirLock.Door`, 1×2)

## Multi-Cell Tile Cleanup
→ `Patches/ConstructionPatches.cs`

Vanilla's `OnCompleteWork` only destroys the anchor cell's tile. For multi-cell buildings over tiles, non-anchor tiles become orphaned. The postfix:
1. Iterates `Building.PlacementCells`, skips anchor cell
2. Finds `FloorTiles`-tagged objects on `ObjectLayer.Building`
3. Refunds via `Deconstructable.SpawnItemsFromConstruction(worker)`
4. Sets `KAnimGraphTileVisualizer.skipCleanup = true`
5. Calls `SimCellOccupier.DestroySelf(callback)` → `DeleteObject()` in callback

## Anti-Patterns (don't do these)

- **Don't preserve temperature across replacement** — dispersal on deconstruct is vanilla behavior; fighting it creates gameplay changes
- **Don't block on structural dependencies** — warn via tooltip, don't prevent. Player may know what they're doing
- **Don't override all `IsValidBuildLocation` checks** — only the replacement preview guard (`IsValidReplaceLocation`); other validity checks must still run
- **Don't delay build errand to next tick** — deconstruct→build must happen same frame or fluids rush into the empty cell

## Previous Bugs (resolved)

- 6 configs don't override `ConfigureBuildingTemplate` — switched all tag patches to `DoPostConfigureComplete`
- `ObjectLayer.Building` as ReplacementLayer caused `IsReplacementLayerOccupied` to block
- `Constructable.OnCompleteWork` is `protected` — use string `"OnCompleteWork"` in HarmonyPatch, not `nameof()`
