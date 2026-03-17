# Overlays - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

## Overlay Modes

Extracted from `OverlayModes` (nested types), `OverlayMenu` (hotkeys), and `OverlayScreen` (registration).

| ID (HashedString) | Class | Hotkey Action | Description |
|-|-|-|-|
| `HashedString.Invalid` | None | (none) | Default view, no overlay active |
| `Oxygen` | Oxygen | Action.Overlay1 | Ambient oxygen density |
| `Power` | Power | Action.Overlay2 | Power grid components, circuits, wattage |
| `Temperature` | Temperature | Action.Overlay3 | Ambient temperature (cell coloring by temp) |
| `TileMode` | TileMode | Action.Overlay4 | Material information per tile |
| `Light` | Light | Action.Overlay5 | Visibility radius of light sources |
| `LiquidConduit` | LiquidConduits | Action.Overlay6 | Liquid pipe system components |
| `GasConduit` | GasConduits | Action.Overlay7 | Gas pipe system components |
| `Decor` | Decor | Action.Overlay8 | Morale-boosting decor values |
| `Disease` | Disease | Action.Overlay9 | Areas of disease/germ risk |
| `Crop` | Crop | Action.Overlay10 | Plant growth progress |
| `Rooms` | Rooms | Action.Overlay11 | Special purpose rooms and bonuses |
| `Suit` | Suit | Action.Overlay12 | Exosuits and related buildings; requires tech `SuitsOverlay` |
| `Logic` | Logic | Action.Overlay13 | Automation grid components; requires tech `AutomationOverlay` |
| `SolidConveyor` | SolidConveyor | Action.Overlay14 | Conveyor transport components; requires tech `ConveyorOverlay` |
| `Radiation` | Radiation | Action.Overlay15 | Radiation levels; only registered if `Sim.IsRadiationEnabled()` (DLC) |

## Non-Menu Overlays

These are registered in `OverlayScreen.RegisterModes()` but have no button in `OverlayMenu`. They are toggled programmatically.

| ID (HashedString) | Class | Triggered By | Description |
|-|-|-|-|
| `ThermalConductivity` | ThermalConductivity | Temperature sub-mode toggle | Thermal conductivity per cell |
| `HeatFlow` | HeatFlow | Temperature sub-mode toggle | Comfortable temperature ranges for Duplicants |
| `HarvestWhenReady` | Harvest | Plant interaction context | Harvest-ready plant highlighting |
| `Priorities` | Priorities | Priority tool | Work priority values per building |
| `PathProber` | PathProber | Debug/internal | Duplicant pathfinding reachability |
| `Sound` | Sound | Not in menu (unused?) | Ambient noise levels (NoisePolluter partition) |

## Key Implementation Details

- **OverlayScreen** is a singleton (`OverlayScreen.Instance`) that manages mode switching via `ToggleOverlay(HashedString)`
- All modes inherit from `OverlayModes.Mode` (abstract base)
- `ConduitMode` is a shared base for `GasConduits`, `LiquidConduits` with network highlighting
- `BasePlantMode` is a shared base for `Crop` and `Harvest`
- Toggling any overlay to non-None activates `TechFilterOnMigrated` audio snapshot and dynamic music overlay
- `SimDebugView.SetMode()` applies per-cell coloring; mapping in `SimDebugView.colourByID` dictionary
- Each mode provides `GetCustomLegendData()` for the `OverlayLegend` panel
- Tech-gated overlays (`Suit`, `Logic`, `SolidConveyor`) check `IsUnlocked()` before showing the button

## Tag Collections (OverlayScreen static fields)

Used by overlay modes to filter visible buildings:

| Field | Used By |
|-|-|
| `WireIDs` | Power |
| `GasVentIDs` | GasConduits |
| `LiquidVentIDs` | LiquidConduits |
| `HarvestableIDs` | Crop, Harvest |
| `DiseaseIDs` | Disease |
| `SuitIDs` | Suit |
| `SolidConveyorIDs` | SolidConveyor |
| `RadiationIDs` | Radiation |

## String Paths

Button text: `STRINGS.UI.OVERLAYS.<CATEGORY>.BUTTON`
Tooltip text: `STRINGS.UI.TOOLTIPS.<ID>OVERLAYSTRING`

Categories: `OXYGEN`, `ELECTRICAL`, `TEMPERATURE`, `TILEMODE`, `LIGHTING`, `LIQUIDPLUMBING`, `GASPLUMBING`, `DECOR`, `DISEASE`, `CROPS`, `ROOMS`, `SUIT`, `LOGIC`, `CONVEYOR`, `RADIATION`
