# DuplicantStatusBar — Extensibility API Guide

**Namespace:** `DuplicantStatusBar.API.Experimental`
**Status:** Experimental — API may change between versions.

## Getting Started

Reference `DuplicantStatusBar.dll` in your mod's `.csproj`. All API access goes through the static `DSBApi` class.

```csharp
using DuplicantStatusBar.API.Experimental;
```

Check `DSBApi.IsLoaded` before interacting — it's false until the status bar UI initializes.

## Custom Alerts

Register a custom alert to have DSB evaluate it every 0.25s per dupe. Active alerts get badge icons and tooltip entries.

```csharp
public class MyMod : KMod.UserMod2
{
    private IDisposable alertHandle;

    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);

        alertHandle = DSBApi.RegisterAlert(new AlertRegistration
        {
            Id = "MyMod.LowSuitO2",
            DisplayName = "Low Suit O2",
            BaseColor = new Color(0.4f, 0.7f, 1f),
            Pattern = AlertPattern.Pulse,
            CycleDuration = 2f,
            GlowIntensity = 0.5f,
            Priority = 110,
            BadgeSymbol = "!",
            Detector = identity =>
            {
                // Your detection logic here
                // Called every 0.25s per dupe — keep it fast
                return false;
            }
        });
    }
}
```

### AlertRegistration Fields

| Field | Type | Default | Description |
|-|-|-|-|
| Id | string | required | Unique key (e.g. "MyMod.LowSuitO2") |
| DisplayName | string | required | Tooltip label |
| BaseColor | Color | white | Badge and tooltip text color |
| Pattern | AlertPattern | Pulse | Animation pattern (Pulse, Heartbeat, Flicker) |
| CycleDuration | float | 2.0 | Animation cycle in seconds |
| GlowIntensity | float | 0.5 | Glow power (0–1) |
| Priority | int | 100 | Lower = more urgent. Built-ins use 0–12 |
| Detector | Func\<MinionIdentity, bool\> | required | Detection function |
| BadgeSymbol | string | "!" | Badge character |

All fields are snapshotted at registration — mutations after `RegisterAlert()` have no effect.

### Priority Guidance

Built-in alert priorities (0 = most urgent):

| Priority | Alert |
|-|-|
| 0 | Incapacitated |
| 1 | Suffocating |
| 2 | Low HP |
| 3–7 | Scalding, Hypothermia, Stuck, Irradiated, Starving |
| 8–12 | Overstressed, Bladder, Diseased, Overjoyed, Idle |

Use 100+ for custom alerts that should appear after built-ins. Use lower values to override them.

## Tooltip Hooks

Append custom lines to the hover tooltip.

```csharp
var handle = DSBApi.RegisterTooltipHook(ctx =>
{
    ctx.Text.AppendLine("Suit O2: 45%");
});
```

The `TooltipContext` provides:
- `Snapshot` — current `DupeSnapshot` (read-only)
- `Text` — mutable `StringBuilder` with the full tooltip
- `AlertSlotIndex` — number of active alert slots

## Event Listeners

React to state changes without polling.

```csharp
// Alert state changed (built-in or custom)
var h1 = DSBApi.OnAlertChanged(e =>
{
    // e.Identity, e.AlertId ("Builtin.Suffocating" or "MyMod.LowSuitO2"), e.Active
});

// Widget lifecycle
var h2 = DSBApi.OnWidgetCreated(e => { /* e.Widget, e.Snapshot */ });
var h3 = DSBApi.OnWidgetDestroyed(e => { /* e.Widget, e.Snapshot */ });

// Per-dupe snapshot updated
var h4 = DSBApi.OnSnapshotUpdated(e => { /* e.Snapshot */ });

// Bar collapsed/expanded
var h5 = DSBApi.OnBarVisibilityChanged(visible => { });
```

All callbacks are wrapped in try/catch — a broken callback never crashes the status bar.

## Cleanup

Every `Register*` / `On*` method returns `IDisposable`. Dispose to unregister:

```csharp
alertHandle.Dispose();  // stops detection + removes badge/tooltip
```

Since `UserMod2` has no `OnUnload`, dispose handles via a `Game.OnDestroy` Harmony postfix, or accept the minor leak (callbacks tolerate stale state).

## API Reference

| Method | Returns | Description |
|-|-|-|
| `DSBApi.RegisterAlert(alert)` | IDisposable | Register custom alert |
| `DSBApi.RegisterTooltipHook(callback)` | IDisposable | Add tooltip hook |
| `DSBApi.OnAlertChanged(callback)` | IDisposable | Listen for alert state changes |
| `DSBApi.OnWidgetCreated(callback)` | IDisposable | Listen for widget creation |
| `DSBApi.OnWidgetDestroyed(callback)` | IDisposable | Listen for widget destruction |
| `DSBApi.OnSnapshotUpdated(callback)` | IDisposable | Listen for snapshot updates |
| `DSBApi.OnBarVisibilityChanged(callback)` | IDisposable | Listen for collapse/expand |
| `DSBApi.IsLoaded` | bool | True once UI is initialized |
| `DSBApi.ApiVersion` | Version | Current API version (1.0.0) |
| `DSBApi.RegisteredAlertCount` | int | Number of registered custom alerts |

## Performance

Detector functions run every 0.25s per dupe. Keep them fast:
- No LINQ queries
- No allocations (no `new`, no string concatenation in hot path)
- Cache component lookups where possible
- Early-return on null checks
