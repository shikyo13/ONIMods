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

## One-Frame Offset Bug (discovered & fixed)

All pre-fix recordings (18 sessions) had a critical spike attribution bug:

`Time.unscaledDeltaTime` is set at frame START (TimeUpdate) = previous frame's cycle. Our `OnFrameEnd` reads it at frame END but captures current-frame diagnostics. Result: spike detection fired one frame late — system data came from the fast recovery frame, not the spike frame.

**Proof** — recording 20260306_091918:

| Source | GameUpdate | PathProbe_Async |
|-|-|-|
| Main CSV (Stopwatch max, correct) | 323.7ms | 321.9ms |
| Spike CSV (unscaledDelta, offset) | 6.0ms | 2.4ms |

**Fix**: Stopwatch-based frame delta in `OnFrameEnd` — measures end-of-PostLateUpdate(N) to end-of-PostLateUpdate(N+1). Old `Time.unscaledDeltaTime` preserved as `WallClockCycleMs` for reference.

**Actual spike culprit**: FastTrack PathProbe_Async (321-339ms per spike), visible in main CSV the entire time.

See `docs/tier2-lag-investigation.md` for full investigation log.

## What Was Implemented

### Stopwatch frame delta (offset bug fix)
- `ProfilerOverlay.OnFrameEnd()` now uses `Stopwatch.GetTimestamp()` instead of `Time.unscaledDeltaTime`
- `SpikeEvent.WallClockCycleMs` field preserves the old value for comparison
- `DataRecorder` spike CSV includes `WallClockCycle_ms` column
- `SpikePanel` displays both values

### PlayerLoop tree dump
- `PlayerLoopTimings.DumpPlayerLoopTree()` logs full Unity PlayerLoop hierarchy on injection
- Writes to `playerloop_dump.txt` alongside recordings

### PostLateUpdate frame-end probe (prior fix)
- All data collection runs from `OniFrameEnd` system at end of PostLateUpdate
- Eliminated race between ProfilerOverlay.Update() and Game.Update()

### Instrumentation (prior)
- 29 game systems via Harmony prefix/postfix
- 10 PlayerLoop phases via injected probes
- 193 bulk Update() methods individually timed
- Coroutine census (StartCoroutine count + top 5 types)
- 4 LateUpdate subsystem keys
- Finalizer heap-drop GC detection + `gcDuringGameLogic` flag
- Inter-frame gap, per-system alloc, bulk/coroutine top-5 in spike events

## What To Do Next

### Step 1: Re-record with fixed profiler
Run 3-5 minutes. Verify:
- Spike CSV `GameUpdate_ms` ≈ main CSV `GameUpdate_max` (same-second)
- `PathProbe_Async_ms` ≈ 321-339ms on spike frames (not 2ms)
- `Unaccounted_ms` < 10ms (not 320ms)
- `WallClockCycle_ms` ≈ 334ms (one frame delayed, for reference)
- `playerloop_dump.txt` appears alongside recording files

### Step 2: Investigate FastTrack PathProbe_Async
- Is batch size configurable?
- Can async pathfinding be disabled without losing other FastTrack benefits?
- Consider profiling PathProbe_Async's internal breakdown

## Known Caveats

- **CoroutineTimings counts starts, not active**: StartCoroutine calls are tracked, not running MoveNext count.
- **BulkUpdate excludes profiler types**: Game, GameScheduler, KComponentSpawn, Global, OnDemandUpdater, GridVisibleArea, ProfilerOverlay are individually timed.
- **~35ms spikes are normal**: Frame variance near the 33ms threshold. Can raise threshold or filter during analysis.
