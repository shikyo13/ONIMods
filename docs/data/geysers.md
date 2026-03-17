# Geysers & Vents - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

Source: `GeyserGenericConfig.GenerateConfigs()` + `GeyserConfigurator.GeyserType`

Default cycle params (unless overridden): iteration 60-1140s, 10-90% active, year 15000-135000s, 40-80% active, body temp 372.15K

## Gas Vents

| ID | Output Element | Output Temp (C) | Rate (g/s) min-max | Max Pressure (kg) | Disease | DLC | Notes |
|-|-|-|-|-|-|-|-|
| steam | Steam | 110 | 1000-2000 | 5 | - | Base | Steam Vent |
| hot_steam | Steam | 500 | 500-1000 | 5 | - | Base | Hot Steam Vent |
| hot_co2 | Carbon Dioxide | 500 | 70-140 | 5 | - | Base | Carbon Dioxide Vent |
| hot_hydrogen | Hydrogen | 500 | 70-140 | 5 | - | Base | Hydrogen Vent |
| hot_po2 | Polluted Oxygen | 500 | 70-140 | 5 | - | Base | Hot Polluted O2 Vent |
| slimy_po2 | Polluted Oxygen | 60 | 70-140 | 5 | Slime Lung (5000) | Base | Infectious Polluted O2 Vent |
| chlorine_gas | Chlorine | 60 | 70-140 | 5 | - | Base | Chlorine Gas Vent |
| chlorine_gas_cool | Chlorine | 5 | 70-140 | 5 | - | Base | NOT generic (never random-spawns) |
| methane | Natural Gas | 150 | 70-140 | 5 | - | Base | Natural Gas Vent |

## Liquid Vents

| ID | Output Element | Output Temp (C) | Rate (g/s) min-max | Max Pressure (kg) | Disease | DLC | Notes |
|-|-|-|-|-|-|-|-|
| hot_water | Water | 95 | 2000-4000 | 500 | - | Base | Cool Steam Vent (below boiling) |
| slush_water | Polluted Water | -10 | 1000-2000 | 500 | - | Base | Polluted Water Vent; body temp -10C; custom cycle: iter 60-1140s 10-90%, year 15000-135000s 40-80% |
| filthy_water | Polluted Water | 30 | 2000-4000 | 500 | Food Poisoning (20000) | Base | Infectious Polluted Water Vent |
| slush_salt_water | Brine | -10 | 1000-2000 | 500 | - | Base | Salt Water slush; body temp -10C; same custom cycle as slush_water |
| salt_water | Salt Water | 95 | 2000-4000 | 500 | - | Base | Salt Water Geyser |
| liquid_co2 | Liquid CO2 | -55.15 | 100-200 | 50 | - | Base | Liquid CO2 Vent; body temp -55.15C; custom cycle same as slush |
| oil_drip | Crude Oil | 326.85 | 1-250 | 50 | - | Base | Oil Reservoir; custom cycle: iter 600s fixed, 100% active, year 100-500s |
| liquid_sulfur | Liquid Sulfur | 165.2 | 1000-2000 | 500 | - | SO DLC | Liquid Sulfur Geyser |

## Molten Vents (Volcanoes)

| ID | Output Element | Output Temp (C) | Rate (g/s) min-max | Max Pressure (kg) | DLC | Cycle Type | Notes |
|-|-|-|-|-|-|-|-|
| small_volcano | Magma | 1726.85 | 400-800 | 150 | Base | Infrequent: iter 6000-12000s, 0.5-1% active | Minor Volcano |
| big_volcano | Magma | 1726.85 | 800-1600 | 150 | Base | Infrequent: iter 6000-12000s, 0.5-1% active | Volcano |
| molten_copper | Molten Copper | 2226.85 | 200-400 | 150 | Base | Frequent: iter 480-1080s, 1.7-10% active | Copper Volcano |
| molten_iron | Molten Iron | 2526.85 | 200-400 | 150 | Base | Frequent: iter 480-1080s, 1.7-10% active | Iron Volcano |
| molten_gold | Molten Gold | 2626.85 | 200-400 | 150 | Base | Frequent: iter 480-1080s, 1.7-10% active | Gold Volcano |
| molten_aluminum | Molten Aluminum | 1726.85 | 200-400 | 150 | SO DLC | Frequent: iter 480-1080s, 1.7-10% active | Aluminum Volcano |
| molten_tungsten | Molten Tungsten | 3726.85 | 200-400 | 150 | SO DLC | Frequent: iter 480-1080s, 1.7-10% active | NOT generic |
| molten_niobium | Molten Niobium | 3226.85 | 800-1600 | 150 | SO DLC | Infrequent: iter 6000-12000s, 0.5-1% active | NOT generic |
| molten_cobalt | Molten Cobalt | 2226.85 | 200-400 | 150 | SO DLC | Frequent: iter 480-1080s, 1.7-10% active | Cobalt Volcano |

## Rate Constants (from GeyserGenericConfig.RATES)

| Category | Min (g/s) | Max (g/s) |
|-|-|-|
| GAS_SMALL | 40 | 80 |
| GAS_NORMAL | 70 | 140 |
| GAS_BIG | 100 | 200 |
| LIQUID_SMALL | 500 | 1000 |
| LIQUID_NORMAL | 1000 | 2000 |
| LIQUID_BIG | 2000 | 4000 |
| MOLTEN_NORMAL | 200 | 400 |
| MOLTEN_BIG | 400 | 800 |
| MOLTEN_HUGE | 800 | 1600 |

## Temperature Constants (from GeyserGenericConfig.TEMPERATURES)

| Name | Value (K) | Value (C) |
|-|-|-|
| BELOW_FREEZING | 263.15 | -10 |
| DUPE_NORMAL | 303.15 | 30 |
| DUPE_HOT | 333.15 | 60 |
| BELOW_BOILING | 368.15 | 95 |
| ABOVE_BOILING | 383.15 | 110 |
| HOT1 | 423.15 | 150 |
| HOT2 | 773.15 | 500 |
| MOLTEN_MAGMA | 2000 | 1726.85 |

## Max Pressure Constants

| Name | Value (kg) |
|-|-|
| GAS | 5 |
| GAS_HIGH | 15 |
| MOLTEN | 150 |
| LIQUID_SMALL | 50 |
| LIQUID | 500 |

## Cycle Iteration Constants

| Category | Iter Pct Min | Iter Pct Max | Iter Len Min (s) | Iter Len Max (s) |
|-|-|-|-|-|
| INFREQUENT_MOLTEN | 0.5% | 1% | 6000 | 12000 |
| FREQUENT_MOLTEN | 1.67% | 10% | 480 | 1080 |

## Notes

- `isGenericGeyser: false` means the type exists but never spawns from the random "GeyserGeneric" spawner
- Rate values are per-cycle (600s), randomized via sigmoid resample between min-max
- Actual emit rate = massPerCycle / (600/iterLen) / onDuration
- Body temp (geyserTemperature) defaults to 372.15K (99C); some geysers override this
- `DlcManager.EXPANSION1` = Spaced Out DLC
