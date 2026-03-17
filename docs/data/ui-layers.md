# UI Layers & Screens - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

## GameScreenManager Render Targets

Enum `GameScreenManager.UIRenderTarget` (Assembly-CSharp-firstpass). Each value maps to a dedicated canvas GameObject.

| Enum Value | Name | Canvas Field | Notes |
|-|-|-|-|
| 0 | WorldSpace | `worldSpaceCanvas` | World-space canvas; used by OverlayScreen power labels, disease/suit overlays |
| 1 | ScreenSpaceCamera | `ssCameraCanvas` | Screen-space Camera; default fallback in `SetCamera` |
| 2 | ScreenSpaceOverlay | `ssOverlayCanvas` | Screen-space Overlay; default target for `StartScreen`/`InstantiateScreen` |
| 3 | HoverTextScreen | `ssHoverTextCanvas` | Hover text canvas; has its own camera |
| 4 | ScreenshotModeCamera | `screenshotModeCanvas` | Screenshot mode canvas |

Default render target for `ActivateScreen`, `InstantiateScreen`, `StartScreen` is `ScreenSpaceOverlay`.

## KScreen Sort Key Constants

Screens are sorted in `KScreenManager.screenStack` by `GetSortKey()`. Higher key = processed later (on top for input).

| Constant | Value | Usage |
|-|-|-|
| (default) | 0 | Base KScreen; most screens |
| FULLSCREEN_SCREEN_SORT_KEY | 20 | Fullscreen management screens |
| PAUSE_MENU_SORT_KEY | 30 | Pause menu |
| LOCKER_SORT_KEY | 40 | Locker/wardrobe screens |
| EDITING_SCREEN_SORT_KEY | 50 | Any screen with `isEditing = true` (consumes all input) |
| MODAL_SCREEN_SORT_KEY | 100 | Modal screens (block ScreenUpdate to screens below) |

`ManagementMenu.GetSortKey()` returns 21 (just above fullscreen threshold).

## Fullscreen Management Screens

`ManagementMenu.fullscreenUIs` array, checked by `IsFullscreenUIActive()`. When active, overlays are disabled.

| Screen | Toggle Info | Hotkey | Notes |
|-|-|-|-|
| ResearchScreen | `researchInfo` | ManageResearch | Requires ResearchCenter; disabled without one |
| SkillsScreen | `skillsInfo` | ManageSkills | Requires RoleStation |
| StarmapScreen | `starmapInfo` | ManageStarmap | Base game only; requires Telescope |
| ClusterMapScreen | `clusterMapInfo` | ManageStarmap | Spaced Out DLC only; always available |

## Non-Fullscreen Management Screens

| Screen | Toggle Info | Hotkey | Tab Index |
|-|-|-|-|
| VitalsTableScreen | `vitalsInfo` | ManageVitals | 2 |
| ConsumablesTableScreen | `consumablesInfo` | ManageConsumables | 3 |
| JobsTableScreen | `jobsInfo` | ManagePriorities | 1 |
| ScheduleScreen | `scheduleInfo` | ManageSchedule | 7 |
| ReportScreen | `reportsInfo` | ManageReport | 4 |
| CodexScreen | `codexInfo` | ManageDatabase | 6 |

Mutually exclusive with `AllResourcesScreen` and `AllDiagnosticsScreen` (hidden when any management screen opens).

## KScreen Base Class

| Field/Property | Type | Default | Notes |
|-|-|-|-|
| `activateOnSpawn` | bool | false | Auto-activate in OnSpawn |
| `isEditing` | bool | false | When true, sort key = 50 and all input consumed |
| `ConsumeMouseScroll` | bool | false | Eats ZoomIn/ZoomOut when mouse is over screen |
| `fadeIn` | bool | false | Enables WidgetTransition on show |
| `mouseOver` | bool | false | Tracked via IPointerEnter/ExitHandler |
| `isActive` | bool | false | Set by Activate/Deactivate |
| `canvas` | Canvas | (resolved) | Nearest parent Canvas, resolved in OnSpawn |

Key methods: `Activate()` pushes to `KScreenManager.screenStack`. `Deactivate()` pops and destroys GameObject. `IsModal()` returns false by default; override to block update propagation.

## KCanvasScaler

Wraps Unity `CanvasScaler` with resolution-adaptive scaling. Persists user scale via `KPlayerPrefs` key `"UIScalePref"`.

| Resolution (height) | Auto Scale Factor |
|-|-|
| 720 or below (or aspect < 16:10) | 0.86 |
| 1080 | 1.0 |
| 2160+ | 1.33 |
| Between steps | Lerp between nearest steps |

Final scale = `userScale * ScreenRelativeScale()`. User scale range: 0.75 to 2.0.

## OverlayScreen

Singleton managing overlay mode switching. Uses `WorldSpaceCanvas` for label canvases (power labels, disease, suit, crop overlays).

| Overlay Mode | ID |
|-|-|
| None | `OverlayModes.None.ID` |
| Oxygen | `OverlayModes.Oxygen` |
| Power | `OverlayModes.Power` |
| Temperature | `OverlayModes.Temperature` |
| ThermalConductivity | `OverlayModes.ThermalConductivity` |
| Light | `OverlayModes.Light` |
| LiquidConduits | `OverlayModes.LiquidConduits` |
| GasConduits | `OverlayModes.GasConduits` |
| Decor | `OverlayModes.Decor` |
| Disease | `OverlayModes.Disease` |
| Crop | `OverlayModes.Crop` |
| Harvest | `OverlayModes.Harvest` |
| Priorities | `OverlayModes.Priorities` |
| HeatFlow | `OverlayModes.HeatFlow` |
| Rooms | `OverlayModes.Rooms` |
| Suit | `OverlayModes.Suit` |
| Logic | `OverlayModes.Logic` |
| SolidConveyor | `OverlayModes.SolidConveyor` |
| TileMode | `OverlayModes.TileMode` |
| Radiation | `OverlayModes.Radiation` |

`ToggleOverlay()` calls `ManagementMenu.Instance.CloseAll()` when entering or leaving an overlay, enforcing mutual exclusivity with management screens.
