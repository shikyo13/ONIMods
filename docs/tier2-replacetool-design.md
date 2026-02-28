# Tier 2 — ReplaceTool Design Reference

Full design doc: `docs/oni-replace-stuff-design_1.md` (341 lines, read on demand)

## Replacement Categories by Release Tier

**Tier 1 (launch):** Tile↔tile, furniture upgrades (Cot→Bed, Outhouse→Lavatory), door swaps, lighting. Same footprint only.

**Tier 2:** Pipes (Regular→Insulated→Radiant), wires, gas pipes, conveyors, storage, farm tiles. Off by default due to fluid spill risk.

**Tier 3 (stretch):** Cross-size replacements (different footprints), batch mode (drag-select region replace).

## Key Game Systems to Patch

| System | Class | Why |
|-|-|-|
| Build placement | `BuildTool` | Intercept clicks on occupied cells |
| Building defs | `BuildingDef` | Size, placement rules, materials |
| Deconstruct | `Deconstructable` | Queue + detect completion |
| Construct | `Constructable` | Build errand lifecycle |
| Priority | `Prioritizable` | Inherit player-set priority |
| World grid | `Grid` | Cell-based queries |
| Validity | `BuildingDef.IsValidBuildLocation` | Override "cell occupied" rejection |

## Critical Architecture Decisions

**Ghost management:** Mod-owned, not game ghost system. Build errand created only after deconstruct completes. Ghost visual managed separately by `ReplaceGhostManager` for immediate player feedback.
→ `UI/ReplaceGhostManager.cs`

**Cancellation atomicity:** Cancel either errand → cancel both. The deconstruct/build pair is always atomic.
→ `Patches/CancelPatches.cs`

**State machine:** `Pending → Deconstructing → ReadyToBuild → Building → Complete | Cancelled`
→ `Core/ReplacementEntry.cs` (enum), `Systems/ReplacementTracker.cs` (transitions)

**Serialization:** `ReplacementTracker` uses KSerialization on SaveGame. On load: re-validate all pending entries (building defs may have changed).
→ `Systems/ReplacementTracker.cs`

## Edge Cases (don't try to "fix" these)

- **Temperature:** Disperses on deconstruct, new tile builds at material temp. This is vanilla behavior — don't preserve temp.
  → No mod code needed — vanilla handles this.
- **Material drops:** 100% refund on deconstruct, full cost on build. Not "free upgrade."
  → No mod code needed — vanilla handles this.
- **Structural deps:** Warn via tooltip, don't block. Player may know what they're doing.
  → `UI/ReplaceToolTip.cs` (warning text), `Core/ReplacementValidator.cs` (detection)
- **Liquid/gas pressure:** Warn, don't block. Brief cell exposure same as manual deconstruct.
  → `UI/ReplaceToolTip.cs` (warning text)
- **Pipe contents:** Spill on deconstruct. Tooltip notes "contents will be released."
  → `UI/ReplaceToolTip.cs` (warning text)
- **Multi-cell buildings:** Use primary cell (anchor) for tracking, validate full footprint.
  → `Systems/ReplacementTracker.cs` (keyed by primary cell), `Core/ReplacementValidator.cs` (footprint check)

## Technical Risks

1. `IsValidBuildLocation` has many internal checks — postfix must only override the "occupied" rejection, not other validity.
   → `Patches/BuildToolPatches.cs` (the IsValidBuildLocation postfix)
2. ONI chore system may not support "blocked errands" — fallback: delay build errand creation to deconstruct callback.
   → `Patches/DeconstructPatches.cs` (creates build errand on completion)
3. Serialized data may break across game updates — validate + discard stale entries on load.
   → `Systems/ReplacementTracker.cs` (OnDeserialized validation)
4. Deconstruct→build must happen same game tick to prevent physics gaps (fluid rushing into empty cell).
   → `Patches/DeconstructPatches.cs` (synchronous errand creation in callback)
