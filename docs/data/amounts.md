# Amounts — ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

## Duplicant

| ID | Display Name | Min | Max | Notes |
|-|-|-|-|-|
| Stamina | Stamina | 0 | 100 | STR:DUPLICANTS.STATS.STAMINA |
| Calories | Calories | 0 | dynamic | Max set per-dupe from stomach size; STR:DUPLICANTS.STATS.CALORIES |
| ImmuneLevel | Immune Level | 0 | TUNING | Max from `DUPLICANTSTATS.IMMUNE_LEVEL_MAX`; STR:DUPLICANTS.STATS.IMMUNELEVEL |
| Breath | Breath | 0 | 100 | STR:DUPLICANTS.STATS.BREATH |
| Stress | Stress | 0 | 100 | STR:DUPLICANTS.STATS.STRESS |
| Toxicity | Toxicity | 0 | 100 | showMax=true; STR:DUPLICANTS.STATS.TOXICITY |
| Bladder | Bladder | 0 | 100 | STR:DUPLICANTS.STATS.BLADDER |
| Decor | Decor | -1000 | 1000 | STR:DUPLICANTS.STATS.DECOR |
| RadiationBalance | Radiation Balance | 0 | 10000 | STR:DUPLICANTS.STATS.RADIATIONBALANCE |
| HitPoints | Hit Points | 0 | dynamic | Max set per-dupe; showMax=true; STR:DUPLICANTS.STATS.HITPOINTS |
| Temperature | Temperature | 0 | 10000 | Units=Kelvin; STR:DUPLICANTS.STATS.TEMPERATURE |

## Bionic

| ID | Display Name | Min | Max | Notes |
|-|-|-|-|-|
| BionicOxygenTank | Bionic Oxygen Tank | 0 | TUNING | Max from static tuning; STR:DUPLICANTS.STATS.BIONICOXYGENTANK |
| BionicOil | Bionic Oil | 0 | 200 | STR:DUPLICANTS.STATS.BIONICOIL |
| BionicGunk | Bionic Gunk | 0 | TUNING | Max from `BionicUpgradesConfig.MAX_GUNK`; STR:DUPLICANTS.STATS.BIONICGUNK |
| BionicInternalBattery | Bionic Internal Battery | 0 | 480000 | Joules; STR:DUPLICANTS.STATS.BIONICINTERNALBATTERY |

## Critter

| ID | Display Name | Min | Max | Notes |
|-|-|-|-|-|
| CritterTemperature | Critter Temperature | 0 | 10000 | Units=Kelvin; STR:CREATURES.STATS.CRITTERTEMPERATURE |
| AirPressure | Air Pressure | 0 | 1000000000 | STR:CREATURES.STATS.AIRPRESSURE |
| Fertility | Fertility | 0 | 100 | STR:CREATURES.STATS.FERTILITY |
| Wildness | Wildness | 0 | 100 | STR:CREATURES.STATS.WILDNESS |
| Incubation | Incubation | 0 | 100 | STR:CREATURES.STATS.INCUBATION |
| ScaleGrowth | Scale Growth | 0 | 100 | STR:CREATURES.STATS.SCALEGROWTH |
| ElementGrowth | Element Growth | 0 | 100 | STR:CREATURES.STATS.ELEMENTGROWTH |
| Beckoning | Beckoning | 0 | 100 | delta=100.5; STR:CREATURES.STATS.BECKONING |
| MilkProduction | Milk Production | 0 | 100 | STR:CREATURES.STATS.MILKPRODUCTION |
| Age | Age | 0 | dynamic | Max set per-critter; STR:CREATURES.STATS.AGE |
| OldAge | Old Age | 0 | dynamic | Not shown in UI; STR:CREATURES.STATS.OLDAGE |
| Viability | Viability | 0 | 100 | STR:CREATURES.STATS.VIABILITY |
| PowerCharge | Power Charge | 0 | 100 | STR:CREATURES.STATS.POWERCHARGE |

## Plant

| ID | Display Name | Min | Max | Notes |
|-|-|-|-|-|
| Maturity | Maturity | 0 | dynamic | Max set per-plant; STR:CREATURES.STATS.MATURITY |
| Maturity2 | Maturity (Trunk) | 0 | dynamic | Arbor tree trunk variant; STR:CREATURES.STATS.MATURITY2 |
| Fertilization | Fertilization | 0 | 100 | STR:CREATURES.STATS.FERTILIZATION |
| Irrigation | Irrigation | 0 | 1 | Fractional (0..1); STR:CREATURES.STATS.IRRIGATION |
| Illumination | Illumination | 0 | 1 | Boolean-like (0 or 1); STR:CREATURES.STATS.ILLUMINATION |
| Rot | Rot | 0 | dynamic | Max set per-item; not shown in UI; STR:CREATURES.STATS.ROT |

## Robot / Battery

| ID | Display Name | Min | Max | Notes |
|-|-|-|-|-|
| InternalBattery | Internal Battery | 0 | dynamic | Sweepy; max set at runtime; STR:ROBOTS.STATS.INTERNALBATTERY |
| InternalChemicalBattery | Internal Chemical Battery | 0 | dynamic | STR:ROBOTS.STATS.INTERNALCHEMICALBATTERY |
| InternalBioBattery | Internal Bio Battery | 0 | dynamic | STR:ROBOTS.STATS.INTERNALBIOBATTERY |
| InternalElectroBank | Internal Electro Bank | 0 | dynamic | STR:ROBOTS.STATS.INTERNALELECTROBANK |
