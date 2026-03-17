# BuildThrough — Handover

Allows duplicants to build and deliver materials through solid tiles. Normally, `OffsetTableTracker.IsValidRow` marks cells unreachable if `Grid.Solid[cell]` is true, preventing construction/deconstruction errands from being worked through walls. This mod bypasses that check selectively.

**Version**: v1.0, initial release
**Branch**: master

## Architecture

| File | Purpose |
|-|-|
| `Core/BuildThroughMod.cs` | UserMod2 entry, PLib init + options registration (~17 lines) |
| `Config/BuildThroughOptions.cs` | Single `Enabled` bool toggle, `SingletonOptions` pattern (~16 lines) |
| `Patches/OffsetTablePatch.cs` | All patching logic: two patches + one helper (~117 lines) |

Single-patch mod. No state management, no serialization, no UI.

## Patch Strategy

1. **Prefix on `UpdateOffsets`**: Sets `[ThreadStatic] skipSolidCheck` flag when the tracker belongs to a `Constructable` or `Deconstructable` component
2. **Transpiler on `IsValidRow`**: Replaces `Grid.Solid[cell]` indexer call with `IsCellBlocking(cell)` helper that returns `false` when flag is set
3. **Postfix on `UpdateOffsets`**: Clears the flag

IL pattern targeted:
```
ldsflda  Grid::Solid              → nop (keep labels)
ldloc.1                            → (kept)
call     BuildFlagsSolidIndexer::get_Item(int32)  → call IsCellBlocking(int32)
```

## Key Design Decisions

- **Transpiler over prefix/postfix**: `IsValidRow` is a tight loop; prefix can't selectively bypass one check, postfix can't undo rejection
- **ThreadStatic flag**: defensive against cross-thread leakage (ONI is single-threaded but defensive)
- **Transpiler fallback**: if IL pattern changes in game update, logs warning and falls back to vanilla (no crash)
- **Restart required**: `[RestartRequired]` on options — transpilers can't be toggled at runtime
- **Scope**: only bypasses solid checks for `Constructable`/`Deconstructable` components; normal dupe pathing unaffected

## Configuration

Single option `Enabled` (bool toggle, restart required).

## Known Issues / Next

None. Very simple, focused mod.
