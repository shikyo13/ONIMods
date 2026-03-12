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

## Key Finding: Multiple Spike Sources

### FastTrack PathProbe_Async

`PathProbe_Async` wraps `AsyncPathProber+WorkOrder.Execute()` — FastTrack's async pathfinding system. It has **batch limiting** via `PriorityBrainScheduler` (3 scheduling modes), NOT all-or-nothing. However, `EndJob()` blocks the main thread until all probes queued for that frame complete.

- **"Background Pathing" toggle** (Settings > Game): Controls sync-vs-async threading only, NOT pathfinding workload. Same `Execute()` runs synchronously when OFF. Confirmed by experiment: Test B (OFF + restart) still showed PathProbe_Async 309–331ms.
- **Impact**: 280–342ms per spike (present in all 4 post-fix recordings)
- **Cannot be eliminated** without disabling FastTrack entirely (counterproductive)

### The True Root Cause: Gen2 Garbage Collection Pauses

The dominant spike source is stop-the-world Gen2 GC pauses. In Oxygen Not Included (Unity 2020.3), **Incremental Garbage Collection is disabled/unsupported**. Whenever a Gen2 collection triggers, it performs a full, synchronous, blocking pause.

**Whichever method triggers the allocation that crosses the GC threshold absorbs the full pause in its Stopwatch:**
* If FastTrack's `PathProbe_Async` allocates a List buffer that tips the scale → 300ms PathProbe spike
* If vanilla physics (`WorldLateUpdate`) tips the scale → 300ms WorldLateUpdate spike
* If Unity internals tip the scale outside instrumented methods → 300ms `Unaccounted`

**Gen2 causes ~90% of >200ms spikes** (77 of 79+ spikes across 8 recordings have `GC_Gen2=1`). The remaining ~10% have independent causes (see below).

**Spike magnitude scales with heap size**: ~300ms at cycle 198 (2.5GB heap) → ~500ms at cycle 281 (larger heap). Larger colonies will see progressively worse pauses.

### Non-Gen2 Spike Sources

Two confirmed non-Gen2 >200ms spikes in recording 20260308_084928 (cycle 281):
- **KScreenManager.Update()**: 175ms in a single BulkUpdate call (251ms total frame). UI system, not GC-related.
- **WorldLateUpdate + InterFrame gap**: 87ms + 94ms gap = 218ms total frame. Possibly scheduling/contention.

### "Massive Unaccounted" Was an Offset Bug Artifact

Previous recordings showed 270-280ms Unaccounted per Gen2 spike. After the offset bug fix, Gen2 spikes show only **1-16ms Unaccounted**. The GC pause is cleanly captured by whichever system's Stopwatch was running. The old "Unaccounted" evidence was actually offset-corrupted data from the fast recovery frame (all systems ~2ms, stale `unscaledDeltaTime` ~286ms → 270ms gap).

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

### Root Cause: Gen2 Garbage Collection

~90% of >200ms spikes are stop-the-world Gen2 GC pauses. Unity 2020.3's Mono runtime does not support Incremental GC. Whichever method triggers the allocation that tips the GC threshold absorbs the full pause in its Stopwatch.

**Evidence**: 77 of 79+ spikes across 8 recordings have `GC_Gen2=1`. Two confirmed non-Gen2 spikes (KScreenManager BulkUpdate, WorldLateUpdate + InterFrame gap) in recording 20260308_084928.

**Spike magnitude scales with heap**: ~300ms at cycle 198 → ~500ms at cycle 281. Late-game colonies face progressively worse pauses.

### GCBudget Impact

GCBudget demonstrably reduces spike frequency at cycle 198:
- Tests A/B (GCBudget ON): 5-6 spikes per recording (~1.5/min)
- Tests C/D (GCBudget OFF): 9-13 spikes per recording

At cycle 281, effectiveness decreases — 19 Gen2 spikes in 4.5 min (~4.2/min) despite GCBudget ON. Higher allocation rate at larger colony sizes overwhelms the budget's ability to space out collections.

### PathProbe_Async as Primary Trigger

PathProbe_Async is not independently slow, but it IS the heaviest per-frame allocator. In Test A (GCBudget ON), all 6 spikes blamed PathProbe_Async (319-342ms). It triggers GC more often than other systems because it allocates the most memory per frame. Reducing its allocations would space out GC pauses further.

### Next Steps

1. **Allocation profiling**: Identify top allocating methods per frame to guide optimization
2. **GCBudget improvements**: Tune alloc threshold and cooldown for better spike spacing
3. **PathProbe allocation reduction**: Investigate buffer reuse in FastTrack's async pathfinding

## Known Caveats

- **CoroutineTimings counts starts, not active**: StartCoroutine calls are tracked, not running MoveNext count.
- **BulkUpdate excludes profiler types**: Game, GameScheduler, KComponentSpawn, Global, OnDemandUpdater, GridVisibleArea, ProfilerOverlay are individually timed.
- **~35ms spikes are normal**: Frame variance near the 33ms threshold. Can raise threshold or filter during analysis.
