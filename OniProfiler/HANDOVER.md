# OniProfiler — Handover

## Current State

Real-time in-game performance profiler for ONI, built as a Harmony mod. Toggles with backtick (`). When closed, zero patches are active (zero overhead). When open, instruments ~29 game systems via Harmony + 10 Unity PlayerLoop phases via injected probes + 193 bulk Update() methods + coroutine census.

**Branch**: master
**Build**: clean, 0 warnings, deployed to local mods folder.

## Architecture Overview

```
OniProfilerMod.OnLoad(Harmony)
  └─ PLib init, stores Harmony instance

Game.OnPrefabInit [patch]
  └─ Attaches ProfilerOverlay MonoBehaviour to Game object

ProfilerOverlay (MonoBehaviour)
  ├─ Update()
  │   ├─ Backtick toggle → Show/Hide
  │   └─ spikePanel.Update() (UI-only display refresh)
  ├─ OnFrameEnd() [static, called from PostLateUpdate probe]
  │   ├─ FrameTimings.RecordFrameEnd()
  │   ├─ BulkUpdateTimings.CommitFrame()
  │   ├─ CoroutineTimings.CommitFrame()
  │   ├─ PlayerLoopTimings.CommitFrame()
  │   ├─ GCMonitor.Update()
  │   ├─ SpikeTracker.CheckFrame()
  │   ├─ EntityCensus.Update()
  │   └─ DataRecorder.RecordFrame() / .Tick()
  └─ OnGUI()
      ├─ TimingBarRenderer.Draw()  — horizontal bars per system
      ├─ SpikePanel.Draw()         — last spike details + phase breakdown
      └─ AlertPanel.Draw()         — warnings (FastTrack, GC mode)

TimingPatchManager.ApplyPatches()
  ├─ 29 individual system patches (Harmony prefix/postfix)
  ├─ BulkUpdateTimings.DiscoverAndPatch() — 193 MonoBehaviour.Update() methods
  ├─ CoroutineTimings.Patch() — StartCoroutine census
  └─ PlayerLoopTimings.Inject() — 10 phase probes + OniFrameEnd callback

PlayerLoopTimings.Inject()
  ├─ 7 top-level phases: begin/end Stopwatch probes
  ├─ 3 Update sub-phases: ScriptRunBehaviourUpdate, DirectorUpdate, Coroutines
  └─ OniFrameEnd: appended to end of PostLateUpdate, fires frameEndCallback
```

## Key Design: PostLateUpdate Frame-End Probe

All frame-end data collection runs from a PlayerLoop system appended to the **end of PostLateUpdate** — the last top-level phase before rendering. This guarantees:

- All MonoBehaviour.Update() callbacks have completed (including Game.Update + GCBudget postfix)
- All LateUpdate callbacks have completed
- All timing accumulators reflect current-frame data (no race with Game.Update)
- GCMonitor reads collection counts AFTER any GC triggered during game logic
- SystemPatches finalizer heap-drop detection has already set `gcDuringGameLogic` flag

**Why not MonoBehaviour.Update()?** Both ProfilerOverlay.Update() and Game.Update() run in the same ScriptRunBehaviourUpdate phase — execution order is non-deterministic. When ProfilerOverlay ran first, it read stale previous-frame timing data.

## Cross-Recording Analysis (13 recordings, 103 spikes)

| Category | Count | % | Signature |
|-|-|-|-|
| Gen2 GC pauses | 44 | 43% | GC_Gen2=1, unaccounted ~300ms |
| PathAsync storms | 23 | 22% | GC_Gen2=0, PathAsync=300-356ms |
| Mystery spikes | 26 | 25% | GC_Gen2=0, PathAsync=0ms, unaccounted ~270ms |
| Minor (<100ms) | 10 | 10% | Mixed causes, lower severity |

### Mystery Spikes

From recording `20260305_151650` (4 mystery spikes with Phase data):
- Phase_UpdateScriptRun = 267-280ms (dominates frame)
- All individually-instrumented systems total ~13ms
- PathProbe_Async = 0.000ms, GC_Gen2 = 0
- Unaccounted = 264-279ms

## What Was Just Implemented

### PostLateUpdate Frame-End Probe (spike misattribution fix)

Previous recordings showed `GameUpdate_ms = 7ms` on 340ms spike frames — ProfilerOverlay.Update() was racing with Game.Update() and reading stale data.

**Fix**: Moved all data collection from `ProfilerOverlay.Update()` to `OnFrameEnd()`, called via a PlayerLoop system at end of PostLateUpdate. This structurally eliminates the race condition.

**Also fixed**: `GCMonitor.GetCurrentGCMode()` — reads GC mode dynamically via reflection instead of stale load-time value (before GCBudget changes it to Manual).

### Prior improvements (this working session)
- BulkUpdateTimings: 193 MonoBehaviour.Update() methods individually timed
- CoroutineTimings: StartCoroutine census (count + top 5 types per frame)
- 4 LateUpdate subsystem keys: GlobalLateUpdate, AnimBatchUpdate, WorldLateUpdate, PropertyTexUpdate
- SystemPatches: finalizer with heap-drop GC detection + `gcDuringGameLogic` flag
- SpikeTracker: captures inter-frame gap, alloc data, bulk/coroutine top-5

## What To Do Next

### Step 1: Capture Recording (user action)
Run 3-5 minutes with the current build. Check spike data:
- `GameUpdate_ms` should be ~340ms on spike frames (not 7ms)
- `GC_Gen2` should be 1 on GC spike frames
- `GC_InGameLogic` should be 1 on GC spike frames
- `GC_Mode` in recording .txt should say "Manual"

### Step 2: Analyze
```powershell
.\OniProfiler\tools\analyze-spikes.ps1
```

### Step 3: If mystery spikes persist
- Check BulkUpdateTop5 for ~270ms culprit MonoBehaviour
- If Phase_UpdateCoroutines shows ~270ms → coroutines are the culprit

## Known Caveats

- **CoroutineTimings counts starts, not active**: StartCoroutine calls are tracked, not running MoveNext count.
- **BulkUpdate excludes profiler types**: Game, GameScheduler, KComponentSpawn, Global, OnDemandUpdater, GridVisibleArea, ProfilerOverlay are individually timed.
- **~35ms spikes are normal**: Frame variance near the 33ms threshold. Can raise threshold or filter during analysis.
