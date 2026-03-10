# DuplicantStatusBar — Handover

RimWorld-style colonist bar showing dupe portraits with stress-colored borders and alert badges. Always visible at top-center of screen.

## Architecture

| File | Purpose |
|-|-|
| `Core/DuplicantStatusBarMod.cs` | UserMod2 entry, PLib init |
| `Config/StatusBarOptions.cs` | PLib options (sort, size, opacity, thresholds, alert toggles) |
| `Data/DupeStatusTracker.cs` | Polls `LiveMinionIdentities` every 0.25s, creates `DupeSnapshot` structs |
| `UI/StatusBarScreen.cs` | MonoBehaviour on Game object; builds uGUI Canvas + manages widgets |
| `UI/DupePortraitWidget.cs` | Individual portrait: colored border + initial letter + alert badge |
| `UI/DupeTooltip.cs` | Hover tooltip: name, task, stress/health/breath, temperature, alert |
| `Patches/GamePatches.cs` | `Game.OnPrefabInit` postfix — injects `StatusBarScreen` |

## Data Flow

1. `GamePatches` adds `StatusBarScreen` to `Game.gameObject` on prefab init
2. Every 0.25s, `DupeStatusTracker.Update()` polls all dupes on the active world
3. For each dupe: reads Stress, HitPoints, Breath amounts + PrimaryElement temp + ChoreDriver + Sicknesses + JoyBehaviourMonitor
4. Computes `StressTier` (5 tiers) and `AlertType` (7 types, priority-ordered)
5. `StatusBarScreen.RefreshWidgets()` syncs widget count and updates each
6. Auto-shrink: portraits scale from configured size down to 28px when >80% screen width

## Key Design Decisions

- **uGUI, not IMGUI**: portraits need proper layout, sprites, pointer events
- **MonoBehaviour + own Canvas**, not KScreen: independent of game's screen stack, won't conflict
- **Colored initials, not rendered portraits**: avoids heavyweight CrewPortrait render-to-texture; readable at small sizes
- **Stress border**: 5-tier color gradient (green→lime→yellow→orange→red) with pulse on critical
- **Alert badges**: priority system (suffocating > lowHP > scalding > hypothermia > overstressed > diseased > overjoyed)
- **Drag via header**: position saved to PlayerPrefs, survives restarts
- **Collapse button**: minimizes to just the header bar
- **Auto-shrink**: when dupe count × portrait size > 80% screen width, shrinks to minimum 28px

## Stress Tiers

| Tier | Default Range | Color |
|-|-|-|
| Calm | 0–20% | #4ade80 (green) |
| Mild | 20–40% | #a3e635 (lime) |
| Stressed | 40–60% | #fbbf24 (yellow) |
| High | 60–80% | #f97316 (orange) |
| Critical | 80%+ | #ef4444 (red) + pulse |

## Alert Detection

| Alert | Detection Method |
|-|-|
| Suffocating | `Amounts.Breath` < 30% of max |
| Low HP | `Amounts.HitPoints` < 30% of max |
| Scalding | `PrimaryElement.Temperature` > 348.15K (75°C) |
| Hypothermia | `PrimaryElement.Temperature` < 263.15K (-10°C) |
| Overstressed | Stress >= 100% |
| Diseased | `Sicknesses.IsInfected()` |
| Overjoyed | `JoyBehaviourMonitor.Instance` in `overjoyed` state |

## Not Yet Implemented

- 2-row wrapping (currently single row with auto-shrink)
- Scroll arrows for overflow beyond minimum size
- Actual dupe portrait sprites (would need CrewPortrait render-to-texture)
- Overjoyed gold glow animation (badge shown, but no glow effect)
