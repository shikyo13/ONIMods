# Tier 2 — Late-Game Lag Spike Investigation

## Colony Profile

| Stat | Value |
|-|-|
| Cycle | 198 |
| Duplicants | 19 |
| Buildings | 6709 |
| Heap | ~2450 MB |
| Active mods | 33 (incl. FastTrack, OniProfiler, GCBudget) |
| GC mode | Manual (GCBudget) or Enabled (baseline) |

## Recording Catalogue

18 recording sessions across 2026-03-05 / 2026-03-06.

| # | Timestamp | GC Mode | Spikes | Notes |
|-|-|-|-|-|
| 1 | 20260305_094532 | Enabled | 8 | Baseline, no GCBudget |
| 2 | 20260305_095135 | Enabled | 5 | Baseline continued |
| 3 | 20260305_100239 | Enabled | 12 | Extended recording |
| 4 | 20260305_101742 | Enabled | 7 | Pre-fix baseline |
| 5 | 20260305_110318 | Enabled | 3 | Short session |
| 6 | 20260305_120045 | Manual | 9 | First GCBudget test |
| 7 | 20260305_131215 | Manual | 6 | GCBudget, raised threshold |
| 8 | 20260305_140830 | Manual | 11 | GCBudget, longer session |
| 9 | 20260305_145722 | Manual | 4 | GCBudget, pre-fix |
| 10 | 20260305_151650 | Manual | 8 | First PlayerLoop phase data |
| 11 | 20260305_155430 | Manual | 5 | Phase + bulk timings |
| 12 | 20260305_162015 | Manual | 7 | Full instrumentation |
| 13 | 20260305_170345 | Manual | 3 | Short verification |
| 14 | 20260306_083012 | Manual | 6 | Frame-end probe active |
| 15 | 20260306_085540 | Manual | 4 | Frame-end + coroutine census |
| 16 | 20260306_091918 | Manual | 7 | Key recording — offset bug confirmed |
| 17 | 20260306_094530 | Manual | 5 | Verification session |
| 18 | 20260306_101215 | Manual | 3 | Final pre-fix capture |

## Hypotheses Tested

| Hypothesis | Verdict | Evidence |
|-|-|-|
| Gen2 GC pauses cause all spikes | **Partial** | GCBudget eliminated GC spikes; non-GC spikes remained |
| Incremental GC would help | **No** | Unity 2020.3 doesn't support incremental in ONI's config |
| Mystery spike = unmeasured system | **No** | All 193 bulk Update() methods instrumented, still unaccounted |
| Coroutine storm | **No** | CoroutineTimings shows <5 starts/frame on spike frames |
| Inter-frame render/vsync | **No** | InterFrameGapMs < 2ms on spike frames |
| Profiler reading stale data (race) | **Yes (fixed)** | Frame-end probe eliminated MonoBehaviour race |
| One-frame offset in spike detection | **YES** | Main CSV vs spike CSV comparison proves it (see below) |

## The One-Frame Offset Bug

### Root Cause

`Time.unscaledDeltaTime` is set during Unity's `TimeUpdate` phase at the **start** of each frame. It reflects the **previous** frame's wall-clock cycle duration. Our `OnFrameEnd` callback runs at the **end** of PostLateUpdate — so it captures current-frame diagnostics (system timings, GC flags) but uses previous-frame timing for spike detection.

On spike frame N:
- `Time.unscaledDeltaTime` = 16ms (frame N-1's cycle, which was fast)
- System timings = frame N's actual data (PathProbe_Async = 321ms)
- Spike detection: 16ms < threshold → **no spike detected**

On frame N+1:
- `Time.unscaledDeltaTime` = 334ms (frame N's cycle, the spike)
- System timings = frame N+1's actual data (PathProbe_Async = 2ms, fast frame)
- Spike detection: 334ms > threshold → **spike detected, but data is from the wrong frame**

### Proof — Recording 20260306_091918, timestamp 09:19:37

| Source | GameUpdate | PathProbe_Async |
|-|-|-|
| Main CSV (Stopwatch, correct frame) | **323.7ms** | **321.9ms** |
| Spike CSV (unscaledDelta, wrong frame) | 6.0ms | 2.4ms |

All 7 spikes in this recording show the same pattern: `PathProbe_Async_max ≈ 321-339ms` in the main CSV's per-second max columns, but `PathProbe_Async_ms ≈ 0.2-2.4ms` in the spike CSV.

### The Fix

Replace `Time.unscaledDeltaTime` with a Stopwatch measuring end-of-PostLateUpdate(N) to end-of-PostLateUpdate(N+1). This aligns the wall-clock delta with the same frame's system data. The old value is preserved as `WallClockCycleMs` for reference.

## The Actual Culprit: FastTrack PathProbe_Async

Once the offset bug is understood, the main CSV's per-second `PathProbe_Async_max` values reveal the truth:

| Recording | Spike count | PathProbe_Async_max range |
|-|-|-|
| 20260306_091918 | 7 | 321-339ms |
| 20260306_085540 | 4 | 298-325ms |
| 20260306_083012 | 6 | 305-341ms |

`PathProbe_Async` is FastTrack's async pathfinding system. It was always the dominant spike contributor — the offset bug made it invisible in spike CSVs by capturing the fast frame after each spike instead.

### What "mystery spikes" actually were

The 25% "mystery spikes" from cross-recording analysis (recording 20260305_151650 etc.) showed:
- Phase_UpdateScriptRun = 267-280ms (dominates frame)
- All individually-instrumented systems total ~13ms
- Unaccounted = 264-279ms

These were offset-corrupted PathProbe_Async spikes. The system was reading the fast recovery frame's data.

## What GCBudget Actually Achieved

GCBudget's contribution is confirmed and valid — `GC.CollectionCount` is frame-independent:

| Metric | GC Enabled | GC Manual (GCBudget) |
|-|-|-|
| Gen2 collections per minute | ~3-5 | 0 (controlled) |
| GC-caused spikes (>100ms) | ~43% of all spikes | 0% |
| Non-GC spikes | Present | Present (PathProbe_Async) |

GCBudget eliminated an entire category of lag spikes. The remaining spikes are all FastTrack PathProbe_Async.

## Next Steps

1. **Re-record with fixed profiler** — first trustworthy spike dataset with correct frame correlation
2. **Verify fix** — spike CSV `GameUpdate_ms` should match main CSV `GameUpdate_max` on spike seconds
3. **Investigate FastTrack PathProbe_Async** — determine if this is configurable or needs a patch
4. **Consider**: disable FastTrack's async pathfinding vs. tune its batch size
