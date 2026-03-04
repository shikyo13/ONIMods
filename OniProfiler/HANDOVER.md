# OniProfiler — Handover

## Current State

Real-time in-game performance profiler for ONI, built as a Harmony mod. Toggles with F8. When closed, zero patches are active (zero overhead). When open, instruments ~21 game systems via Harmony + 7 Unity PlayerLoop phases via injected probes.

**Branch**: `feature/oni-profiler` (worktree at `.worktrees/oni-profiler`)
**Build**: clean, 0 warnings, deployed to local mods folder.

## What Was Just Implemented (and Why)

### Problem
~300ms lag spikes observed in late-game colonies. The profiler's Harmony patches on game systems (Sim, pathfinding, rendering callbacks, etc.) only account for ~40ms during spike frames. The remaining ~260ms is "unaccounted" — it hides somewhere in the frame that we weren't measuring.

### Solution: PlayerLoop Phase Instrumentation
Added `PlayerLoopTimings.cs` which injects Stopwatch-based begin/end probes into each of Unity's 7 top-level PlayerLoop phases:

1. **Initialization** — engine startup tasks
2. **EarlyUpdate** — input, XR, script execution order setup
3. **FixedUpdate** — physics step(s)
4. **PreUpdate** — animations, AI NavMesh
5. **Update** — MonoBehaviour.Update (where Game.Update lives)
6. **PreLateUpdate** — director, particle systems
7. **PostLateUpdate** — rendering, UI layout, PlayerSendFrameComplete

This reveals WHICH phase the spike time hides in (rendering? physics? animation?), narrowing the search from "somewhere in the frame" to a specific ~50-system phase.

### Supporting Changes
- **SpikeTracker**: Now captures per-phase ms snapshot alongside per-system ms on every spike. Pre-allocated pool arrays (zero GC).
- **SpikePanel**: Displays phase breakdown with dominant phase highlighted (arrow marker).
- **DataRecorder**: Spike CSV rows now include `Phase_*_ms` columns for offline analysis.
- **FrameTimings/SystemPatches**: Expanded with inter-frame gap tracking, GC-during-game-logic detection, per-system allocation tracking.
- **GCMonitor**: Added incremental GC detection, GC mode reporting, alloc rate.

## What To Do Next

1. **Test in-game**: Load a late-game colony, wait for spikes, check SpikePanel's phase breakdown.
2. **Identify culprit phase**: The phase with the largest ms during spike frames is where the time hides. Expected suspects: `PostLateUpdate` (rendering/UI), `FixedUpdate` (physics), or `EarlyUpdate`.
3. **Drill deeper**: Once the culprit phase is identified, add Harmony patches to specific systems within that phase (e.g., if PostLateUpdate → patch `Camera.Render`, UGUI layout, etc.).
4. **Consider**: If spikes correlate with `Gen2GC` and show up as inter-frame time, the cause is GC stop-the-world pauses (outside any PlayerLoop phase). The GC location indicator helps distinguish this.

## Architecture Overview

```
OniProfilerMod.OnLoad(Harmony)
  └─ PLib init, stores Harmony instance

Game.OnPrefabInit [patch]
  └─ Attaches ProfilerOverlay MonoBehaviour to Game object

ProfilerOverlay (MonoBehaviour)
  ├─ Update()
  │   ├─ F8 toggle → TimingPatchManager.Apply/Remove + PlayerLoopTimings.Inject/Remove
  │   ├─ PlayerLoopTimings.CommitFrame()
  │   ├─ FrameTimings.RecordFrameEnd()
  │   ├─ SpikeTracker.CheckFrame()
  │   ├─ GCMonitor.Update()
  │   ├─ EntityCensus.Update()
  │   └─ DataRecorder.RecordFrame() / .Tick()
  └─ OnGUI()
      ├─ TimingBarRenderer.Draw()  — horizontal bars per system
      ├─ SpikePanel.Draw()         — last spike details + phase breakdown
      └─ AlertPanel.Draw()         — warnings (FastTrack, GC mode)
```

## Known Caveats

- **Snapshot staleness**: `CommitFrame()` runs during Update. Phases that execute AFTER Update in the same frame (Update, PreLateUpdate, PostLateUpdate) reflect **previous-frame** values. This is a one-frame lag, acceptable for spike attribution since spikes persist across frames.
- **PlayerLoop injection order**: Other mods could theoretically also inject into PlayerLoop. Our `Remove()` only strips systems whose type is in our marker set, so it won't disturb other mods.
- **IMGUI rendering**: Uses OnGUI for simplicity. Has minor perf cost when visible (~0.5ms). Not an issue since the profiler is a debug tool.
