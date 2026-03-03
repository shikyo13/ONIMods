# Tier 2 — BuildThrough Design Reference

Read when editing BuildThrough mod.

## What It Does

Allows duplicants to build and deliver materials through solid tiles. Normally, `OffsetTableTracker.IsValidRow` marks cells unreachable if `Grid.Solid[cell]` is true, preventing construction/deconstruction errands from being worked through walls. This mod bypasses that check selectively.

## Architecture

Single-patch mod. No state management, no serialization, no UI.

| File | Purpose | Lines |
|-|-|-|
| `Core/BuildThroughMod.cs` | Entry point. PLib init + options registration | 17 |
| `Config/BuildThroughOptions.cs` | Single `Enabled` bool toggle. `SingletonOptions` pattern | 16 |
| `Patches/OffsetTablePatch.cs` | All patching logic. Two patches + one helper | 117 |

## Patch Strategy

### The Problem
`OffsetTableTracker.IsValidRow` calls `Grid.Solid[cell]` to determine if a cell blocks pathfinding for errand reachability. This prevents dupes from reaching build/deconstruct sites behind walls.

### The Solution (Transpiler + Flag)
1. **Prefix on `UpdateOffsets`**: Sets `[ThreadStatic] skipSolidCheck` flag when the tracker belongs to a `Constructable` or `Deconstructable` component
2. **Transpiler on `IsValidRow`**: Replaces `Grid.Solid[cell]` indexer call with `IsCellBlocking(cell)` helper that returns `false` when the flag is set
3. **Postfix on `UpdateOffsets`**: Clears the flag so non-construction pathfinding is unaffected

### IL Pattern Targeted
```
ldsflda  Grid::Solid              → nop (keep labels)
ldloc.1                            → (kept)
call     BuildFlagsSolidIndexer::get_Item(int32)  → call IsCellBlocking(int32)
```

### Why Transpiler (not Prefix/Postfix)
`IsValidRow` is a tight loop over cell offsets. A prefix cannot selectively bypass one internal check. A postfix cannot undo the side effects of the solid-cell rejection. The transpiler surgically replaces exactly the `Grid.Solid` read.

## Edge Cases
- **ThreadStatic**: Ensures no flag leakage across threads (ONI is single-threaded, but defensive)
- **Transpiler fallback**: If IL pattern changes in a game update, logs `[BuildThrough] Transpiler failed...` warning and falls back to vanilla behavior (no crash)
- **Restart required**: `[RestartRequired]` on options — transpilers cannot be toggled at runtime
- **Scope**: Only bypasses solid checks for Constructable/Deconstructable components. Normal dupe pathing unaffected

## Dependencies
- PLib 4.19.0+ (NuGet, ILMerged)
- No inter-mod dependencies
