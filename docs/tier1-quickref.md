# Tier 1 — Quick Reference (read once per session)

Hard cap: 150 lines. Universal gotchas + per-mod module maps.

## Workspace Mod Index

| Mod | Entry Point | Status |
|-|-|-|
| ReplaceStuff | `Core/ReplaceStuffMod.cs` | v2.1, active |
| BuildThrough | `Core/BuildThroughMod.cs` | Initial commit |
| OniProfiler | `Core/OniProfilerMod.cs` | Active — see `OniProfiler/HANDOVER.md` for architecture |
| GCBudget | `Core/GCBudgetMod.cs` | POC, alloc-gated gen0 GC collections |
| DuplicantStatusBar | `Core/DuplicantStatusBarMod.cs` | v2.3.0 — persistent dupe status HUD, expression-driven portraits + blink |

## ReplaceStuff Module Map

| File | Purpose | Key Types |
|-|-|-|
| `Core/ReplaceStuffMod.cs` | Entry point, PLib init, soft modded-door patches | `ReplaceStuffMod : UserMod2` |
| `Patches/BuildingConfigPatches.cs` | 28 postfixes injecting replacement config into 14 vanilla buildings | `InjectReplacementConfig`, `InjectReplacementTag`, 7 group tags |
| `Patches/BuildToolPatches.cs` | `IsValidReplaceLocation` postfix — guards preview when `CanReplace` fails | Rejects wrong-group replacements |
| `Patches/ConstructionPatches.cs` | `OnCompleteWork` postfix — multi-cell tile cleanup with refund | Destroys non-anchor tiles |
| `UI/ReplaceToolTip.cs` | "Replacing X → Y" tooltip for furniture replacements | `HoverTextDrawer` usage |
| `Config/ReplaceStuffOptions.cs` | `EnableBuildings` toggle | `SingletonOptions<ReplaceStuffOptions>` |

## ReplaceStuff Dependency Graph

| File | Requires |
|-|-|
| `ReplaceStuffMod.cs` | `ReplaceStuffOptions`, `BuildingConfigPatches` (DoorTag, helpers) |
| `BuildingConfigPatches.cs` | Nothing (self-contained config injection) |
| `BuildToolPatches.cs` | Nothing (reads vanilla `BuildingDef` methods) |
| `ConstructionPatches.cs` | Nothing (reads vanilla `Building.PlacementCells`) |
| `ReplaceToolTip.cs` | Nothing (reads vanilla replacement state) |

**Hub file:** `BuildingConfigPatches.cs` — defines all group tags and injection helpers. Adding a new replacement group starts here.

## ReplaceStuff Replacement Flow

1. Game loads → `BuildingConfigPatches` postfixes inject `ReplacementLayer`, `ReplacementTags`, group tags into building defs
2. Player clicks build on occupied cell → vanilla's `TryBuild` sees `Replaceable = true` + matching tags
3. `BuildToolPatches.IsValidReplaceLocation` postfix rejects if `CanReplace` fails (wrong group)
4. Vanilla handles: deconstruct old → build new (same-frame, no gap)
5. `ConstructionPatches.OnCompleteWork` postfix cleans up non-anchor tiles for multi-cell buildings
6. `ReplaceToolTip` shows "Replacing X → Y" on hover

## BuildThrough Module Map

| File | Purpose | Key Types |
|-|-|-|
| `Core/BuildThroughMod.cs` | Entry point, PLib init | `BuildThroughMod : UserMod2` |
| `Config/BuildThroughOptions.cs` | `Enabled` toggle | `SingletonOptions<BuildThroughOptions>` |
| `Patches/OffsetTablePatch.cs` | Transpiler: bypass `Grid.Solid` for build/deconstruct errands | `[ThreadStatic] skipSolidCheck`, `IsCellBlocking` helper |

## GCBudget Module Map

| File | Purpose | Key Types |
|-|-|-|
| `Core/GCBudgetMod.cs` | Entry point, PLib init | `GCBudgetMod : UserMod2` |
| `Core/GCBudgetManager.cs` | GC mode control, alloc-gated collection | `Init()`, `OnFrame()`, `DoCollect()`, `Restore()` |
| `Config/GCBudgetOptions.cs` | PLib options schema | `GCBudgetOptions : SingletonOptions` |
| `Patches/GameUpdatePatch.cs` | `Game.Update` postfix → `OnFrame()` | `GameUpdatePatch` |
| `Patches/SavePausePatch.cs` | Save/pause/quit triggers + cleanup | 4 patch classes |

## ONI Modding Gotchas

- `using HarmonyLib;` — never `using Harmony;` (v1 is dead)
- All patch methods must be `static`
- `__result` needs `ref` keyword in postfix to modify return value
- Prefix returning `false` skips original + all other prefixes — use sparingly
- `base.OnLoad(harmony)` calls `PatchAll()` — don't double-patch
- Game DLLs change every update — always decompile latest `Assembly-CSharp.dll`
- `<Private>false</Private>` on ALL game refs or your mod ships 50MB of duplicates
- KSerialization: forgetting `[Serialize]` = field silently lost on save/load
- `OnPrefabInit` runs during prefab setup (no world yet); `OnSpawn` runs when placed — don't access `Grid` in `OnPrefabInit`
- `Db.Initialize()` postfix is the correct hook for adding buildings to tech tree — too early and `Techs` doesn't exist
- `___privateField` (3 underscores) to access private fields — easy to miscount
- Patching overloaded methods requires `new Type[] { ... }` in `[HarmonyPatch]` attribute
- `AddOrGet<T>()` is idempotent; raw `AddComponent<T>()` can create duplicates silently
- Protected methods (e.g., `OnCompleteWork`) — use string `"OnCompleteWork"` in HarmonyPatch, not `nameof()`

## Breaking Changes

(None tracked yet — add game patch notes here when ONI updates break mod behavior.)
