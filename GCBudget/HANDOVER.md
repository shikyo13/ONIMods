# GCBudget — Handover

## Current State

Alloc-gated gen0 GC collection mod (proof of concept). Sets Unity's GarbageCollector to Manual mode, then triggers collections only when heap growth exceeds a configurable threshold. Eliminates GC-caused lag spikes during gameplay.

**Branch**: master
**Build**: clean, 0 warnings, deployed to local mods folder.

## Architecture

```
GCBudgetMod.OnLoad(Harmony)
  ├─ PLib init + register options
  └─ GCBudgetManager.Init()
      ├─ Reflect GarbageCollector.GCMode property
      ├─ Save original mode, set to Manual (2)
      └─ Init thresholds from options

Game.Update [postfix patch]
  └─ GCBudgetManager.OnFrame()
      └─ if heap >= threshold OR heap >= ceiling → DoCollect()

SaveLoader.Save [prefix patch]
  └─ GCBudgetManager.OnSave() → DoCollect("Pre-save")

SpeedControlScreen.TogglePause [postfix patch]
  └─ GCBudgetManager.OnPause() → DoCollect("Pause") with 2s cooldown

Game.OnDestroy / OnApplicationQuit [prefix patches]
  └─ GCBudgetManager.Restore() → resets GCMode to original
```

## Module Map

| File | Purpose | Key Types |
|-|-|-|
| `Core/GCBudgetMod.cs` | Entry point, PLib init | `GCBudgetMod : UserMod2` |
| `Core/GCBudgetManager.cs` | GC mode control, alloc-gated collection | `Init()`, `OnFrame()`, `DoCollect()`, `Restore()` |
| `Config/GCBudgetOptions.cs` | PLib options schema | `GCBudgetOptions : SingletonOptions` |
| `Patches/GameUpdatePatch.cs` | `Game.Update` postfix → `OnFrame()` | `GameUpdatePatch` |
| `Patches/SavePausePatch.cs` | Save/pause/quit GC triggers + cleanup | `SaveLoaderPatch`, `PausePatch`, `GameDestroyPatch`, `GameQuitPatch` |

## Options Schema

| Option | Default | Range | Description |
|-|-|-|-|
| GrowthAllowanceMB | 256 | 64-512 | Collect when heap grows this much past last collection |
| HeapCeilingMB | 3072 | 1024-4096 | Emergency force-collect above this |
| CollectOnPause | true | — | Collect when game is paused |
| CollectOnSave | true | — | Collect before auto-save |

## Key Design Decisions

- **Reflection-based GCMode access**: `GarbageCollector.GCMode` is in `UnityEngine.CoreModule` — accessed via reflection to avoid hard dependency on specific Unity version.
- **Triple gate**: collection requires (1) heap > threshold OR heap > ceiling, (2) no cooldown active, (3) manual mode verified at init.
- **Restore on exit**: `OnDestroy` and `OnApplicationQuit` patches ensure GCMode is restored even on crash-to-menu.
- **OniProfiler interop**: OniProfiler's `GCMonitor.GetCurrentGCMode()` reads GCMode dynamically via the same reflection — correctly shows "Manual" in recordings.

## Key Findings

GCBudget **eliminated GC-caused lag spikes** (~43% of all spikes in GC Enabled mode). Verified across 18 recording sessions:
- Gen2 collections in Manual mode: 0 (controlled by GCBudget, triggered only at thresholds)
- GC-attributed spikes (>100ms): 0% in Manual mode vs ~43% in Enabled mode
- `GC.CollectionCount` is frame-independent, so these findings are unaffected by the profiler's one-frame offset bug

Remaining spikes after GCBudget are FastTrack PathProbe_Async (see `docs/tier2-lag-investigation.md`).

## Caveats

- **POC quality**: no automated tests, manual verification only
- **Heap growth**: with Manual mode, heap grows until threshold — watch for memory pressure on 16GB systems
- **Mod interaction**: any mod calling `GC.Collect()` directly bypasses GCBudget's scheduling
