# Taming Boehm GC for ONI performance mods

**Unity's Mono Boehm garbage collector is the single biggest architectural obstacle to smooth late-game ONI performance, but a combination of manual GC control via `GarbageCollector.GCMode`, aggressive allocation reduction through Harmony transpilers, and strategic collection timing can dramatically reduce lag spikes.** No existing ONI mod directly tackles GC tuning — the community has focused on reducing workload (pathfinding, simulation) rather than collector behavior itself. This creates a clear opportunity. ONI runs on Unity 2020.3.x LTS with Mono backend, meaning the `GarbageCollector.GCMode` API (available since Unity 2018.3) is confirmed functional and documented for both Mono and IL2CPP builds.

---

## How Boehm GC actually works inside Unity's Mono runtime

Unity's embedded Mono runtime uses the Boehm-Demers-Weiser conservative collector, which differs fundamentally from .NET's generational GC. **Boehm is non-generational, non-compacting, and fully stop-the-world in non-incremental mode.** Every collection pass scans the *entire* managed heap — there is no nursery or old-generation separation. Calling `GC.Collect(int generation)` always triggers a full heap sweep regardless of the generation parameter; `GC.MaxGeneration` returns 0 under Mono because only one "generation" exists.

The collector operates as a mark-sweep algorithm in four phases: preparation (clear mark bits), marking (trace all reachable objects from roots), sweeping (return unmarked objects to free lists), and finalization. Boehm is *conservative*, meaning it scans stack, registers, and static data for anything resembling a valid heap pointer — including integers that coincidentally match heap addresses. This causes occasional false retention of dead objects, a known but generally minor issue.

The critical performance characteristic is that **GC pause duration scales linearly with total heap size, not garbage volume**. A 500MB heap takes roughly 5× longer to collect than a 100MB heap even if both contain the same amount of garbage. Developer reports consistently show pauses ranging from under 1ms to several hundred milliseconds. One developer with 16 million voxels (large arrays in memory) reported the GC spending **50ms per frame** just scanning live objects. Unity's own documentation warns pauses can last "hundreds of milliseconds." For ONI late-game with hundreds of duplicants, critters, pipe networks, and simulation state, heap sizes easily reach hundreds of megabytes.

Boehm's memory layout uses size-segregated free lists within ~4-8KB blocks. Small objects get allocated from per-size free lists, large objects from a separate best-fit allocator. Collection triggers when allocation would fail and total allocation since last GC exceeds `heap_size / GC_free_space_divisor` — otherwise the heap simply expands. Crucially, **Unity never releases managed heap memory back to the OS**. Once the heap grows to 1GB, it stays at 1GB even after collection. This ratchet effect compounds the problem in long-running ONI sessions.

Mono historically moved to SGen (a generational, compacting collector) as its default in Mono 4.x/5.x, but Unity never adopted SGen due to licensing complications and engineering risk. Even after Microsoft's MIT relicense of Mono, Unity continues shipping Boehm.

---

## GarbageCollector.GCMode works on Mono — here's what to know

The `UnityEngine.Scripting.GarbageCollector.GCMode` API is the most powerful tool available for a GC-focused mod. **It is explicitly documented as working on both Mono and IL2CPP scripting backends**, with the only unsupported contexts being the Unity Editor and WebGL builds. The 2018.3 changelog that introduced it states support for "Mono and IL2CPP scripting backends" — this is not IL2CPP-only.

Three modes are available. `Mode.Enabled` is the default auto-collection behavior. `Mode.Disabled` completely prevents GC, including suppressing `GC.Collect()` calls — memory only grows. **`Mode.Manual` is the recommended choice for modding**: it disables automatic collection while allowing explicit `GC.Collect()` calls, giving full control over collection timing.

Under the hood, setting GCMode calls into Unity's native C++ layer via `[InternalCall]`, which sets a flag preventing Boehm from triggering. Multiple developers have confirmed this works in production Mono builds by using patterns like:

```csharp
#if !UNITY_EDITOR
GarbageCollector.GCMode = GarbageCollector.Mode.Manual;
#endif
```

The `#if !UNITY_EDITOR` guard is needed because the API throws an error in-editor — not because of any Mono limitation. One developer documented running for 25-30 minutes with GC disabled, then manually collecting, confirming the approach works in shipped Mono players.

For ONI modding specifically, since the game ships as a built player on Unity 2020.3.x, the API should be accessible from injected mod code at runtime. The `GarbageCollector.GCModeChanged` event provides notification when the mode changes, useful for coordinating with other mods.

**`GC.TryStartNoGCRegion` is NOT available** — it throws `NotSupportedException` on all Mono versions, including Unity's embedded Mono. The dotnet/runtime team explicitly stated they are "not planning on implementing these APIs in the near future." `GarbageCollector.GCMode` is the only viable mechanism for controlling collection timing.

---

## What ONI modders have already tried (and haven't)

The ONI modding community, led primarily by **Peter Han (peterhaneve)**, has produced the most significant performance work through the **Fast Track** mod. Fast Track claims 40%+ frame rate improvement in late game — one player reported jumping from 20-32 FPS to 60+ FPS on a 1545-cycle colony. However, **no existing ONI mod directly addresses GC behavior**. The community's approach has been to reduce the workload that causes allocations rather than tuning the collector itself.

Fast Track's optimizations target several systems. Background threading moves room/cavity probing off the main thread. Pathfinding optimization reduces recalculation scope — pathfinding consumes approximately **80% of frame time** in large colonies. Critter idle movement reduction decreases pathfinding load (with the side effect that morbs produce less polluted oxygen, since emissions are tied to movement). Batched/deferred updates reduce per-frame computation for sensors, overcrowding checks, and conduit networks.

A key Klei forums thread from February 2022 documented ONI consuming **23GB of RAM** after repeated save/load cycles, with developer GuyPerfect diagnosing it as orphaned collections persisting because "managed memory will often not invoke the garbage collector at logical times." The proposed solutions — manual `GC.Collect()` after save/load and reusing collections via `Clear()` rather than re-allocating — remain at the suggestion level and haven't been implemented in any mod.

Klei's own November 2025 QoL update validated the community's findings, officially implementing async pathfinding, reduced per-frame allocations, and core system optimizations. The patch notes explicitly state: "Significantly reduced the number of memory allocations per frame, which resulted in a slightly smoother framerate. This is the groundwork for a future optimization." This confirms allocation reduction is a known priority, and that per-frame allocations were a real, measurable problem in the base game.

Other relevant mods include **Fast Save** (optimizing save/load memory), **Stock Bug Fix** (fixing wasted computation), and **Black Hole Garbage Disposal** (reducing entity count to lighten simulation load). The community consensus is that debris/clutter, gas mixing in open spaces, jet suit pathfinding expansion, and daily report accumulation are the primary late-game bottlenecks.

---

## Practical allocation-reduction techniques for Mono Boehm

The golden rule for Boehm GC is **zero managed allocations per frame in gameplay loops**. Unlike .NET's generational GC, which handles short-lived objects efficiently through a nursery, Boehm treats every allocation equally — each one contributes to heap growth and eventual full-heap collection.

**Object pooling delivers the highest impact.** Pool not just GameObjects but pure C# objects — Lists, Dictionaries, arrays, any reference type created repeatedly. Unity 2021.1+ includes `UnityEngine.Pool.ObjectPool<T>`, `ListPool<T>`, `DictionaryPool<TKey,TValue>`, and `HashSetPool<T>`. For ONI's Unity 2020.3.x, implement a simple generic pool or use `System.Buffers.ArrayPool<T>.Shared` for temporary arrays. Pre-warm pools during loading to avoid allocation spikes during gameplay.

**Collection reuse via `Clear()` instead of re-creation** is critical. `List<T>.Clear()` preserves the underlying array capacity — no allocation occurs. Pre-size collections with expected capacity (`new List<int>(256)`) to avoid resize-and-copy operations, where each resize creates a garbage array.

**Avoiding boxing** matters more under Boehm than under generational GC. Any struct used as a Dictionary key must implement `IEquatable<T>` and override `GetHashCode()` — without these, every lookup boxes the key. Enum dictionary keys are a common offender; provide a custom `IEqualityComparer<TEnum>`. The `params` keyword always allocates a heap array; provide fixed-parameter overloads for common cases. LINQ allocates iterator objects and closures on every call — replace with explicit `for` loops in hot paths.

**Closures and lambdas that capture variables** allocate a compiler-generated class on each invocation. Non-capturing lambdas are cached after first use and are safe. Pre-allocate `Action`/`Func` delegates as static fields for hot paths.

One critical Mono-specific warning: **NativeArray element access is approximately 9× slower under Mono than regular C# arrays**. Unity acknowledged this performance gap (confirmed through at least Unity 2019.3). Use NativeArray only for Job System/Burst interop or bulk storage that doesn't need frequent per-element C# access — not as a drop-in replacement for `T[]` in hot Mono code paths.

For measuring GC impact from within a mod, the `UnityEngine.Profiling.Profiler` API works in both Mono and IL2CPP:

- `Profiler.GetMonoHeapSizeLong()` — total reserved managed heap
- `Profiler.GetMonoUsedSizeLong()` — currently used heap (increases until GC runs)
- Unity 2020.2+ adds `ProfilerRecorder` for zero-overhead measurement: `ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame")` tracks per-frame allocation without attaching the Profiler window

---

## Writing GC-efficient Harmony patches for hot paths

Harmony's core mechanism — generating a replacement DynamicMethod wrapper at patch time — is well-optimized for the steady state. **Basic prefix/postfix patches with typed parameter injection do NOT allocate per-call.** The generated IL directly calls your static patch methods via direct method calls, not through delegates or reflection.

However, specific patterns cause per-call allocations that are devastating in hot paths. **`object[] __args` injection allocates a new array and boxes all value-type arguments every call** — Harmony's documentation explicitly warns about this overhead. Declaring `__result` as `object` instead of the matching value type causes boxing. Using `__state` with a class type forces a `new` allocation each call; use value-type `__state` (e.g., `out long __state` with `Stopwatch.GetTimestamp()`).

**Transpiler patches have zero runtime overhead** — they modify the original method's IL once at patch time, and the JIT compiles the modified method identically to native code. This makes transpilers the ideal tool for removing allocations from the host game's code. Practical transpiler techniques include replacing `newobj` instructions with loads from static cached fields, replacing LINQ chains with indexed loops, replacing `new List<T>()` with `Clear()` on a static list, and caching results of idempotent method calls. Real-world examples include the LethalPerformance mod (Lethal Company) removing material allocations and caching normals arrays via transpilers.

The tradeoff is fragility — transpilers depend on exact IL patterns that break with game updates. For code that changes frequently, prefix patches that return `false` (skipping the original) and provide a complete replacement implementation offer more resilience, at the cost of ~5-20ns overhead per call. For a method called 10,000 times per frame at 60fps, basic prefix overhead totals approximately 3-12ms/sec — generally acceptable. At 100,000 calls per frame, transpilers become necessary.

**`AccessTools.FieldRefAccess<TClass, TField>()`** provides allocation-free private field access by returning a `ref TField` — cache the accessor in a static field and use it instead of `___privateField` injection when accessing fields in tight loops. Consider also using **Assembly Publicizer** (BepInEx tool) to make private members accessible at compile time, eliminating the need for many Harmony patches entirely. The BSMG Wiki explicitly recommends this approach as "easier to read, faster, and creates less garbage."

Known Harmony-Mono issues to watch for: patching methods containing calls to Unity native methods (e.g., `GameObject.Find`, `Random.Range`) can cause `MissingMethodException` due to lazy native linking — ensure patching happens after Unity's startup phase. Methods returning structs larger than 8 bytes on 64-bit have ABI complexities with the struct return buffer that can cause `InvalidProgramException`.

---

## A practical manual-GC strategy for high-allocation games

The recommended architecture uses `GarbageCollector.Mode.Manual` combined with threshold-based collection timing:

```csharp
GarbageCollector.GCMode = GarbageCollector.Mode.Manual;

// In update loop:
long currentHeap = Profiler.GetMonoUsedSizeLong();
if (currentHeap > HIGH_WATER_MARK) {
    System.GC.Collect();
}
```

**Trigger collections during natural pauses** — loading screens, pause menus, between simulation ticks if idle time exists, and after save/load operations (which the Klei forums thread identified as a major accumulation point). Unity's own example code uses thresholds of 8MB incremental / 128MB full collection.

At **10 MB/s allocation with GC disabled, the heap grows ~600MB per minute**. For 64-bit applications, the theoretical address space is enormous, but practical limits hit earlier. Developers report managed heaps above **500MB–1GB** causing unacceptable collection pauses (hundreds of milliseconds to seconds). Since Unity never releases heap memory back to the OS, a heap that grows to 2GB stays at 2GB even after collection. The strategy must balance collection frequency against pause duration — collecting more often on a smaller heap produces shorter pauses than collecting rarely on a bloated heap.

**Heap fragmentation under Boehm** follows specific patterns. Without collection running, fragmentation doesn't occur — allocations simply expand the heap linearly. When GC runs on a large heap, sweeping creates many free-list entries of varying sizes. Boehm's size-segregated free lists mitigate this for same-sized allocations (objects sharing a size class fill gaps efficiently), but varied allocation sizes — common in game code — cause genuine fragmentation. Since Boehm is non-compacting, the only defragmentation path is when entire blocks become completely empty and can be coalesced.

---

## Conclusion: the untapped opportunity

The ONI modding community has achieved remarkable results — 40%+ FPS improvement — through algorithmic optimization alone, without touching GC behavior. **Direct GC control represents an unexplored frontier.** A performance mod combining `GarbageCollector.Mode.Manual` with strategic collection timing, Harmony transpilers removing allocations from hot paths (especially pathfinding, conduit updates, and sensor ticks), and `Profiler` API-based heap monitoring could address the lag spike problem at its root rather than working around it. The key risks — OOM from unchecked heap growth and long pauses when collection finally runs — are manageable through threshold-based collection during natural game pauses. The most impactful approach combines allocation reduction (eliminating the pressure) with manual collection timing (controlling when the remaining pressure is relieved), rather than relying on either strategy alone.