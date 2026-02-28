# Tier 1 — Quick Reference (read once per session)

## ReplaceTool Module Map

| File | Purpose | Key Types |
|-|-|-|
| `Core/ReplaceToolMod.cs` | Entry point, Harmony init, PLib setup | `ReplaceToolMod : UserMod2` |
| `Core/ReplacementEntry.cs` | Data class for a replacement pair | cell, oldDef, newDef, materials, state |
| `Core/ReplacementValidator.cs` | Rules: can X replace Y? | Footprint, tech, layer checks |
| `Config/ReplaceToolOptions.cs` | PLib settings class | tile/building on-off, structural/pipe warning toggles |
| `Systems/ReplacementTracker.cs` | Central state — Dict<int, ReplacementEntry> | KSerialization, save/load |
| `Patches/BuildToolPatches.cs` | Intercept build placement on occupied cells | `BuildTool.TryBuild` prefix |
| `Patches/CancelPatches.cs` | Atomic cancel: cancel one errand → cancel both | `Cancelable.OnCancel` postfix |
| `Patches/ConstructPatches.cs` | On build complete → clean up tracker | `Constructable.OnCompleteWork` postfix |
| `Patches/DeconstructPatches.cs` | On deconstruct complete → trigger build | `Deconstructable.OnCompleteWork` postfix |
| `UI/ReplaceGhostManager.cs` | Visual ghost overlay for pending replacements | Custom rendering, not game ghost system |
| `UI/ReplaceToolTip.cs` | "Replacing X → Y" tooltip + warnings | Postfix on `SelectToolHoverTextCard.UpdateHoverElements` |

## Dependency Graph

Which files load/consume which — follow arrows when a change ripples.

| File | Requires |
|-|-|
| `ReplaceToolMod.cs` | `ReplaceToolOptions.cs`, all `Patches/*` (via PatchAll) |
| `BuildToolPatches.cs` | `ReplacementValidator`, `ReplacementTracker`, `ReplacementEntry` |
| `CancelPatches.cs` | `ReplacementTracker` |
| `ConstructPatches.cs` | `ReplacementTracker` |
| `DeconstructPatches.cs` | `ReplacementTracker`, `ReplacementEntry` |
| `ReplacementTracker.cs` | `ReplacementEntry` |
| `ReplacementValidator.cs` | `ReplacementEntry` (reads BuildingDef from it) |
| `ReplaceGhostManager.cs` | `ReplacementTracker`, `ReplacementEntry` |
| `ReplaceToolTip.cs` | `ReplacementTracker`, `ReplacementEntry` |

**Hub file:** `ReplacementEntry` — touched by nearly everything. Change its fields carefully.

## Replacement Flow

1. Player clicks build on occupied cell → `BuildToolPatches` intercepts
2. `ReplacementValidator` checks legality (footprint, tech, layer)
3. `ReplacementEntry` created, stored in `ReplacementTracker`
4. Deconstruct errand placed on old building
5. Ghost visual shown via `ReplaceGhostManager` (mod-managed, not game ghost)
6. Deconstruct completes → `DeconstructPatches` triggers build errand
7. Build completes → `ConstructPatches` cleans up tracker
8. Cancel either → `CancelPatches` cancels both atomically

## ONI Modding Gotchas

- `using HarmonyLib;` — never `using Harmony;` (v1 is dead)
- All patch methods must be `static`
- `ElementLoader` unavailable during static init — use `SimHashes.X.CreateTag()` instead
- `__result` needs `ref` keyword in postfix to modify return value
- Prefix returning `false` skips original + all other prefixes — use sparingly
- `base.OnLoad(harmony)` calls `PatchAll()` — don't double-patch
- Game DLLs change every update — always decompile latest `Assembly-CSharp.dll`
- Build output: `<Private>false</Private>` on ALL game refs or your mod ships 50MB of duplicates
- KSerialization: forgetting `[Serialize]` = field silently lost on save/load
- `SaveGame.OnPrefabInit` postfix is the standard hook for attaching persistent components
- `OnPrefabInit` runs during prefab setup (no world yet); `OnSpawn` runs when placed in world — don't access `Grid` in `OnPrefabInit`
- `Db.Initialize()` postfix is the correct hook for adding buildings to tech tree — too early and `Techs` doesn't exist
- `___privateField` (3 underscores) to access private fields in patches — easy to miscount
- Patching overloaded methods requires `new Type[] { ... }` in `[HarmonyPatch]` attribute
- `AddOrGet<T>()` is idempotent; raw `AddComponent<T>()` can create duplicates silently

## Anti-Patterns

- **Don't preserve temperature across replacement** — dispersal on deconstruct is vanilla behavior; fighting it creates gameplay changes, not QoL
- **Don't block on structural dependencies** — warn via tooltip, don't prevent. Player may know what they're doing
- **Don't override all `IsValidBuildLocation` checks** — only suppress the "cell occupied" rejection; other validity checks (foundation, conduit layer) must still run
- **Don't delay build errand to next tick** — deconstruct→build must happen same frame or fluids rush into the empty cell

## Breaking Changes

(None tracked yet — add game patch notes here when ONI updates break mod behavior.)

## Project Config

- PLib via NuGet: `PLib 4.19.0` (ILMerged into output)
- Game managed DLLs: `D:\SteamLibrary\...\Managed\` (set in `<GameManaged>` property)
- Mod metadata: `mod_info.yaml` (supportedContent: ALL, APIVersion: 2) + `mod.yaml` (title, staticID)
