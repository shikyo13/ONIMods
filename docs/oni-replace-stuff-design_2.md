# ReplaceTool v2 — Vanilla-Integrated Replacement Design

## Architecture: Work WITH vanilla, not around it

v1 built a parallel state machine (tracker, entries, ghost manager, deconstruct→build chaining).
v2 injects replacement support into vanilla's existing system via BuildingDef config patches.

### How vanilla replacement works (verified from decompilation)

1. `BuildTool.TryBuild`: if `def.TryPlace()` returns null AND `def.ReplacementLayer != NumLayers`:
   - Calls `def.GetReplacementCandidate(cell)` — iterates `ReplacementCandidateLayers`, finds `BuildingComplete` on those layers
   - Checks `!def.IsReplacementLayerOccupied(cell)` — ensures no pending replacement
   - Checks `candidate.Def.Replaceable && def.CanReplace(candidate)` — tag matching via `ReplacementTags`
   - Calls `def.TryReplaceTile()` → instantiates `BuildingUnderConstruction` with `IsReplacementTile = true`
   - Stores ghost on `Grid.Objects[cell, ReplacementLayer]`

2. `Constructable.OnCompleteWork` (when `IsReplacementTile == true`):
   - Calls `building.Def.GetReplacementCandidate(cell)` to find old building
   - For non-tile buildings (no SimCellOccupier): calls `SpawnItemsFromConstruction` (material refund), triggers event, `DeleteObject()`
   - Calls `FinishConstruction()` → `def.Build()` to place the completed building

**Key insight:** Step 2 already handles arbitrary buildings, not just tiles. We just need to configure the fields correctly.

### The gate problem

`TryBuild` gate: `if (gameObject == null && def.ReplacementLayer != ObjectLayer.NumLayers)`

No `ReplacementBuilding` layer exists in `ObjectLayer`. We can't add enum values at runtime.

### Solution: Prefix patch on TryBuild

A prefix on `BuildTool.TryBuild` that detects furniture replacement scenarios and handles them directly:

1. Check if `def.ReplacementLayer == NumLayers` but `def.ReplacementCandidateLayers` is configured
2. Call `def.GetReplacementCandidate(cell)` to find existing building
3. Validate via `Replaceable` and `CanReplace`
4. Temporarily set `def.ReplacementLayer = def.ObjectLayer`, call `def.TryReplaceTile()`, restore
5. Store ghost reference and return false (skip original TryBuild)

Why temporary layer swap: `TryReplaceTile` internally calls `IsValidPlaceLocation` which may check `ReplacementLayer`. By temporarily using `def.ObjectLayer`, the ghost registers on the building layer. Since the old building gets deleted on completion, no persistent conflict.

Why not permanent layer: No safe layer exists that's guaranteed empty at all furniture cells across all game states.

## Replacement Groups

| Group | Tag | Buildings | Footprint |
|-|-|-|-|
| Bed | `ReplaceTool_Bed` | Bed, LadderBed | 2×2 |
| Door | `ReplaceTool_Door` | Door, ManualPressureDoor, PressureDoor | 1×2 |
| Generator 2×2 | `ReplaceTool_Generator2x2` | ManualGenerator, WoodGasGenerator | 2×2 |
| Generator 4×3 | `ReplaceTool_Generator4x3` | HydrogenGenerator, MethaneGenerator | 4×3 |
| Storage | `ReplaceTool_Storage` | StorageLocker, StorageLockerSmart | 1×2 |
| Toilet | `ReplaceTool_Toilet` | Outhouse, FlushToilet | 2×3 |
| WashStation | `ReplaceTool_WashStation` | WashBasin, WashSink | 2×3 |

### Excluded from plan (footprint mismatch)
- LuxuryBed (4×2) — cannot replace Bed (2×2)
- CeilingLight (1×1, ceiling) vs SunLamp (2×4, floor) — incompatible
- HandSanitizer (1×3) vs WashBasin/WashSink (2×3) — different width
- Coal Generator (3×3) vs ManualGenerator (2×2) — different size

### Matching rules

All buildings in a group must share:
- Same footprint (WidthInCells × HeightInCells)
- Same ObjectLayer (Building)
- Same or compatible BuildLocationRule

`CanReplace` uses `ReplacementTags` — the new def's tags must include the group tag, and the candidate must also have that tag. Both buildings in a pair need the tag.

### Config fields injected per building

```
def.Replaceable = true;                                          // already true by default
def.ReplacementCandidateLayers = new List<ObjectLayer> { ObjectLayer.Building };
def.ReplacementTags = new List<Tag> { new Tag("ReplaceTool_<Group>") };
```

`ReplacementLayer` is NOT set permanently — handled by the TryBuild prefix.

## File Plan

| File | Action | Reason |
|-|-|-|
| `Core/ReplaceToolMod.cs` | **Rewrite** | Entry point stays, remove SaveGame patch (no Tracker/GhostManager) |
| `Core/ReplacementEntry.cs` | **Delete** | Vanilla tracks state internally |
| `Core/ReplacementValidator.cs` | **Delete** | Vanilla's CanReplace/Replaceable handles validation |
| `Systems/ReplacementTracker.cs` | **Delete** | Vanilla tracks via IsReplacementTile + grid layers |
| `Patches/BuildToolPatches.cs` | **Rewrite** | Remove old prefix; add gate-bypass prefix |
| `Patches/CancelPatches.cs` | **Delete** | Vanilla handles cancel (ghost removed → old building stays) |
| `Patches/ConstructPatches.cs` | **Delete** | Vanilla's OnCompleteWork handles completion |
| `Patches/DeconstructPatches.cs` | **Delete** | No deconstruct→build flow; vanilla swaps atomically |
| `UI/ReplaceGhostManager.cs` | **Delete** | Vanilla creates ghosts natively |
| `UI/ReplaceToolTip.cs` | **Keep/modify** | "Replacing X → Y" tooltip using vanilla's IsReplacementTile |
| `Config/ReplaceToolOptions.cs` | **Keep/modify** | User configuration (simplified) |
| **NEW** `Patches/BuildingConfigPatches.cs` | **Create** | Postfixes to inject replacement fields on BuildingDefs |

## Vanilla systems we rely on (no mod code needed)

- **Ghost visuals:** `BuildingUnderConstruction` with `IsReplacementTile = true` — vanilla renders it
- **Material refund:** `Deconstructable.SpawnItemsFromConstruction()` called during `OnCompleteWork`
- **Old building removal:** `replacementCandidate.DeleteObject()` in `OnCompleteWork`
- **Cancellation:** Cancel the ghost → `Constructable.OnCancel` → `UnmarkArea` → ghost destroyed, old building untouched
- **Save/load:** `Constructable` is KSerialized, `IsReplacementTile` persists, grid references restore

## Edge cases

- **Same building, different material:** Vanilla already handles: checks `component.Def != def || selectedElements[0] != tag` before allowing replacement
- **Instant build (sandbox):** Vanilla's `InstantBuildReplace` handles non-tile buildings: calls `Object.Destroy(tile)` then `def.Build()`
- **Multi-cell buildings:** `GetReplacementCandidate` checks single cell; `MarkArea` handles full footprint via `PlacementOffsets`
- **Generator footprint mismatch:** Only same-footprint generators share the group tag; different-size generators excluded
