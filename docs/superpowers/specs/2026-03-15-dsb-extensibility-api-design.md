# DuplicantStatusBar Extensibility API — Design Spec

**Status:** Experimental
**Date:** 2026-03-15
**Scope:** Public API for custom alerts, tooltip hooks, lifecycle events, and documentation

## Overview

DuplicantStatusBar exposes a static registration API that lets external mods add custom alerts, modify tooltips, and react to lifecycle events without patching DSB internals. The API lives in `DuplicantStatusBar.API.Experimental` — the namespace signals that the contract may change between versions.

The design targets two audiences: external mod authors who reference the DLL, and community contributors who fork and PR.

## API Surface

All registration goes through one static class. Every `Register*` / `On*` method returns `IDisposable` for clean unregistration.

```csharp
namespace DuplicantStatusBar.API.Experimental
{
    public static class DSBApi
    {
        // ── Custom Alerts ──
        public static IDisposable RegisterAlert(AlertRegistration alert);

        // ── Tooltip Hooks ──
        public static IDisposable RegisterTooltipHook(Action<TooltipContext> callback);

        // ── Event Listeners ──
        public static IDisposable OnAlertChanged(Action<AlertChangedEvent> callback);
        public static IDisposable OnWidgetCreated(Action<WidgetEvent> callback);
        public static IDisposable OnWidgetDestroyed(Action<WidgetEvent> callback);
        public static IDisposable OnSnapshotUpdated(Action<SnapshotEvent> callback);
        public static IDisposable OnBarVisibilityChanged(Action<bool> callback);

        // ── Query ──
        public static bool IsLoaded { get; }
        public static Version ApiVersion { get; }
        public static int RegisteredAlertCount { get; }
    }
}
```

## Data Types

### AlertRegistration

What a mod passes to register a custom alert.

```csharp
public sealed class AlertRegistration
{
    public string Id { get; set; }                          // unique key, e.g. "MyMod.LowSuitO2"
    public string DisplayName { get; set; }                 // tooltip label (supports LocString)
    public Color BaseColor { get; set; }                    // badge + tooltip text color
    public AlertPattern Pattern { get; set; }               // Pulse, Heartbeat, or Flicker
    public float CycleDuration { get; set; }                // animation cycle length in seconds
    public float GlowIntensity { get; set; }                // glow power (0-1)
    public int Priority { get; set; }                       // lower = more urgent (shown first)
    public Func<MinionIdentity, bool> Detector { get; set; } // called every 0.25s per dupe
}
```

`AlertRegistration` uses setters for object-initializer convenience. The registry snapshots all fields at registration time — mutations after `RegisterAlert()` have no effect.
```

`RegisterAlert` validates inputs and throws `ArgumentException` when:
- `Id` is null, empty, or already registered
- `Detector` is null
- `DisplayName` is null or empty
- `CycleDuration` is <= 0 (default: 2.0)
- `GlowIntensity` is outside 0-1 range (default: 0.5)

### AlertPattern

```csharp
public enum AlertPattern
{
    Pulse,      // smooth sine wave
    Heartbeat,  // double-peak pulse
    Flicker     // randomized noise
}
```

Priority values for built-in alerts range from 0 (Incapacitated) to 12 (Idle). External alerts should use values 100+ to appear after built-in alerts by default, or lower values to override them.

### TooltipContext

What tooltip hooks receive. Mutable — hooks can append, insert, or rewrite.

```csharp
public sealed class TooltipContext
{
    public DupeSnapshot Snapshot { get; }      // current dupe state (read-only)
    public StringBuilder Text { get; }         // full tooltip text, mutable
    public int AlertSlotIndex { get; }         // active alert slot count
}
```

### Event Types

Lightweight, read-only structs.

```csharp
public readonly struct AlertChangedEvent
{
    public readonly MinionIdentity Identity;
    public readonly string AlertId;    // built-in alerts use "Builtin.Suffocating" etc.
    public readonly bool Active;       // true = triggered, false = cleared
}

public readonly struct WidgetEvent
{
    public readonly DupePortraitWidget Widget;
    public readonly DupeSnapshot Snapshot;
}

public readonly struct SnapshotEvent
{
    public readonly DupeSnapshot Snapshot;
}
```

Built-in alerts emit `AlertChangedEvent` with IDs prefixed `"Builtin."` (e.g., `"Builtin.Suffocating"`). Custom alerts use whatever ID was passed in `AlertRegistration.Id`.

`DupeSnapshot` is a public struct (already public in the codebase). External mods can read all its fields: `Name`, `StressPercent`, `HealthPercent`, `BreathPercent`, `BodyTemperature`, `BladderPercent`, `ChoreDescription`, `AlertMask`, `StressTier`, `Identity`, `Selectable`.

## Internal Architecture

### Integration Points

The existing pipeline is:

```
DupeStatusTracker.Update()
  → builds DupeSnapshot list
    → StatusBarScreen.RefreshWidgets()
      → DupePortraitWidget.SetSnapshot()
      → DupeTooltip.Show()
```

Custom registrations plug in at four points:

#### 1. Alert Detection (DupeStatusTracker)

After computing built-in alerts for each dupe, iterate all registered custom alerts and call `Detector(identity)`. Results stored in a `Dictionary<string, bool>` per snapshot — separate from the built-in `AlertMask` ushort bitmask.

This separation is critical: the built-in `AlertType` enum and `AlertMask` bitmask are unchanged. Custom alerts use string IDs, not enum values. No enum extension, no save-compat risk.

Previous alert state is diffed against current state to fire `AlertChangedEvent`.

#### 2. Tooltip Construction (DupeTooltip)

After building stats text and populating built-in alert slots, invoke all registered tooltip hooks with a `TooltipContext`. Hooks execute in registration order. Each hook receives the mutable StringBuilder and can append, insert, or rewrite.

Custom alerts that are active get their own animated alert text slots using the same pooled TMP system. The pool is extended dynamically if needed (current max is 5 slots).

#### 3. Event Dispatch

Thin dispatch layer — no queueing, no async. Events fire synchronously at these points:

| Event | Fires from | Timing |
|-|-|-|
| `OnAlertChanged` | `DupeStatusTracker.Update()` | After alert diff, before widget refresh |
| `OnWidgetCreated` | `StatusBarScreen.RefreshWidgets()` | After widget added to list |
| `OnWidgetDestroyed` | `StatusBarScreen.RefreshWidgets()` | Before widget Destroy() |
| `OnSnapshotUpdated` | `DupeStatusTracker.Update()` | End of update cycle, per dupe |
| `OnBarVisibilityChanged` | `StatusBarScreen.ToggleCollapse()` | After collapse state changes |

All callbacks are wrapped in try/catch — a broken external callback never crashes the status bar.

#### 4. Badge Rendering (DupePortraitWidget)

Custom alerts join the same priority queue as built-in alerts for badge display. Up to 3 badges shown simultaneously. Priority ordering uses `AlertRegistration.Priority` — sorted alongside built-in alert priorities.

Badge color comes from `AlertRegistration.BaseColor`. Animation pattern and glow from the registration's `Pattern`, `CycleDuration`, and `GlowIntensity` fields — full `AlertEffect` parity.

### Internal Registry

A new `AlertRegistry` internal static class stores:

- `List<AlertRegistration>` — registered custom alerts (insertion order)
- `List<Action<TooltipContext>>` — tooltip hooks (insertion order)
- `Dictionary<string, List<Action<T>>>` — event listeners keyed by event type
- `Dictionary<int, Dictionary<string, bool>>` — previous custom alert state per dupe (for diffing)

`IDisposable` handles returned from `Register*` / `On*` methods remove the corresponding entry from these lists on `Dispose()`.

### Thread Safety

All registration and disposal must happen on the Unity main thread. The registry uses copy-on-iterate for lists: iteration snapshots the list into a temporary array, so `Dispose()` during a callback does not corrupt iteration. No locks — single-threaded by contract.

## File Structure

```
DuplicantStatusBar/
  API/
    Experimental/
      DSBApi.cs              — public static entry point
      AlertRegistration.cs   — alert registration type
      TooltipContext.cs      — tooltip hook context
      Events.cs              — event struct definitions
    Internal/
      AlertRegistry.cs       — registration storage + dispatch
      EventDispatcher.cs     — try/catch wrapped event firing
```

## Documentation Deliverables

### 1. `docs/api-guide.md` — External mod authors

Target: readable in 10 minutes. Sections:

- **Getting Started** — add DLL reference, one-liner alert registration
- **Custom Alerts** — complete example with AlertRegistration, detection function, priority guidance
- **Tooltip Hooks** — example appending a custom stat line
- **Event Listeners** — example reacting to alert changes
- **API Reference** — table of every method, params, return type
- **Experimental Notice** — stability expectations, how to report issues

### 2. XML docs — On all public types and methods

IDE autocomplete shows description, params, and return type. Every public member in `API.Experimental` namespace gets XML docs.

### 3. `DuplicantStatusBar/HANDOVER.md` update — Contributors

New section covering:

- Registry location and how registrations flow into the pipeline
- How to add a new event hook (pattern to follow)
- How custom alerts differ from built-in (string ID vs enum, separate storage)
- Event dispatch timing and error handling

## Consumer Example

A complete example of what an external mod author writes:

```csharp
using DuplicantStatusBar.API.Experimental;
using UnityEngine;

public class SuitAlertMod : KMod.UserMod2
{
    private IDisposable alertHandle;
    private IDisposable tooltipHandle;
    private IDisposable eventHandle;

    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);

        // Register a custom alert
        alertHandle = DSBApi.RegisterAlert(new AlertRegistration
        {
            Id = "SuitAlert.LowO2",
            DisplayName = "Low Suit O2",
            BaseColor = new Color(0.4f, 0.7f, 1f),
            Pattern = AlertPattern.Pulse,
            CycleDuration = 2f,
            GlowIntensity = 0.5f,
            Priority = 110,
            Detector = identity =>
            {
                var suit = identity.GetComponent<SuitEquipper>()
                    ?.IsWearingAirtightSuit();
                // check suit O2 level...
                return false;
            }
        });

        // Add a tooltip line
        tooltipHandle = DSBApi.RegisterTooltipHook(ctx =>
        {
            ctx.Text.AppendLine("Suit O2: 45%");
        });

        // React to alerts (store handle for cleanup)
        eventHandle = DSBApi.OnAlertChanged(e =>
        {
            if (e.AlertId == "SuitAlert.LowO2" && e.Active)
                Debug.Log($"{e.Identity.name} suit is low!");
        });
    }
}
```

The example stores `alertHandle`, `tooltipHandle`, and `eventHandle` as class fields. All `IDisposable` handles should be disposed when the mod unloads. Since `UserMod2` has no `OnUnload`, mods should dispose handles via a `Game.OnDestroy` Harmony postfix or accept the minor leak (callbacks are try/caught and tolerate stale state).

## Constraints

- **No enum changes** — AlertType enum is frozen. Custom alerts use string IDs.
- **No DupeSnapshot struct changes** — custom alert state stored separately via the registry's dictionary, not added to the struct.
- **Error isolation** — every external callback is try/caught. A broken mod never crashes the status bar.
- **Experimental namespace** — API may change between versions. No formal deprecation cycle yet.
- **Performance budget** — detector functions run every 0.25s per dupe. Document that detectors should be fast (no LINQ, no allocs).
