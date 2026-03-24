# Tier 2 - Late-Game Lag Spike Investigation (Summary)

> Full recording catalogue, experiment protocol, and per-recording analysis: `tier3-lag-investigation-data.md`

## Colony Profile

| Stat | Value |
|-|-|
| Cycle | 198-281 |
| Duplicants | 19 |
| Buildings | 6709 |
| Heap | ~2450 MB (cycle 198), larger at cycle 281 |
| Active mods | 33 (incl. FastTrack, OniProfiler, GCBudget) |

## Key Findings

### 1. One-Frame Offset Bug (FIXED)

`Time.unscaledDeltaTime` reflects the **previous** frame's duration. Our `OnFrameEnd` callback captured current-frame diagnostics but previous-frame timing, causing spike detection to attribute spike data to the wrong frame.

**Fix**: Replaced with Stopwatch measuring end-of-PostLateUpdate(N) to end-of-PostLateUpdate(N+1).

### 2. Multiple Spike Sources (Primary Finding)

The problem is systemic - NOT a single culprit.

| Spike Source | System | Magnitude |
|-|-|-|
| PathProbe_Async | `AsyncPathProber+WorkOrder.Execute()` | 280-340ms |
| WorldLateUpdate | `World.LateUpdate()` | 282-300ms |
| GlobalUpdate | `Global.Update()` | 336ms |
| FindNextChore | `ChoreConsumer.FindNextChore()` | 297ms |
| KScreenManager | `KScreenManager.Update()` BulkUpdate | 175ms |
| Unaccounted | Unknown in Update/PostLateUpdate | 275-300ms |

Consistent ~300ms magnitude across diverse systems suggests a shared root cause - memory pressure, cache thrashing, or blocking lock.

### 3. GC is ~90% of Spikes, Not 100%

Post-bugfix data: 19/21 spikes (90.5%) have Gen2=1. Two confirmed non-Gen2 spikes: KScreenManager.Update() and WorldLateUpdate+InterFrame gap.

### 4. GCBudget Effectiveness

| Metric | GC Enabled | GC Manual (GCBudget) |
|-|-|-|
| Gen2 collections per minute | ~3-5 | 0 (controlled) |
| GC-caused spikes (>100ms) | ~43% of all spikes | 0% |

GCBudget eliminates GC-caused spikes but becomes less effective at higher colony sizes (1.5 spikes/min at cycle 198 vs 4.2/min at cycle 281).

### 5. Spike Magnitude Scales with Heap

| Colony | Gen2 spike range | Heap |
|-|-|-|
| Cycle 198 | ~280-340ms | ~2.5 GB |
| Cycle 281 | ~500-562ms | larger |

## Open Items

1. **Unaccounted time** - FastTrack transpiler patches may bypass Harmony hooks
2. **New spike sources to instrument** - World.LateUpdate(), Global.Update(), ChoreConsumer.FindNextChore()
3. **Shared root cause investigation** - memory pressure / cache thrashing hypothesis
