# Diseases & Sicknesses — ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

## Germs / Diseases (Environmental)

These are `Klei.AI.Disease` subclasses registered in `Database.Diseases`. Germs exist on elements/items and cause sicknesses on exposure.

| Field Name | Class | ID (const) | Display Name (approx) | Causes Sickness |
|-|-|-|-|-|
| FoodGerms | `Klei.AI.FoodGerms` | `FoodPoisoning` | Food Poisoning | FoodSickness |
| SlimeGerms | `Klei.AI.SlimeGerms` | `SlimeLung` | Slimelung | SlimeSickness |
| PollenGerms | `Klei.AI.PollenGerms` | `PollenGerms` | Floral Scents | Allergies |
| ZombieSpores | `Klei.AI.ZombieSpores` | `ZombieSpores` | Zombie Spores | ZombieSickness |
| RadiationPoisoning | `Klei.AI.RadiationPoisoning` | `RadiationSickness` | Radioactive Contaminant | RadiationSickness |

## Sicknesses (Dupe Afflictions)

These are `Klei.AI.Sickness` subclasses registered in `Database.Sicknesses`. SicknessType enum: `Pathogen=0`, `Ailment=1`, `Injury=2`.

| ID (const) | Class | Severity | Duration (s) | Type | Key Effects | Infection Vector | Cure |
|-|-|-|-|-|-|-|-|
| `FoodSickness` | `Klei.AI.FoodSickness` | Minor | 1020 | Pathogen (0) | Stamina +0.33, Bladder -0.20, Calories -0.05; Vomit every 200s | Contact (1) | BasicCure (Curative Tablet) |
| `SlimeSickness` | `Klei.AI.SlimeSickness` | Major | 2220 | Pathogen (0) | Athletics -3, Cough every 20s (0.1kg PollutedO2 + 1000 germs) | Contact (2) | IntermediateCure (Medical Pack) |
| `ZombieSickness` | `Klei.AI.ZombieSickness` | Major | 10800 | Pathogen (0) | All attributes -10 (11 attrs); Custom sick FX | Contact (2), Inhalation (0) | AdvancedCure (Serum Vial) |
| `Allergies` | `Klei.AI.Allergies` | Minor | 60 | Pathogen (0) | Stress +0.025/s (~15/cycle), Sneezing +10 | Contact (2) | Antihistamine |
| `RadiationSickness` | `Klei.AI.RadiationSickness` | Major | 10800 | Pathogen (0) | All attributes -10 (11 attrs); Custom sick FX | Contact (2), Inhalation (0) | AdvancedCure (Serum Vial) |
| `SunburnSickness` | `Klei.AI.Sunburn` | Minor | 1020 | Ailment (1) | Stress +0.033/s; uses Sunburn animation | Exposure (3) | Aloe Vera (3 cure items) |

### Recovery Effects

Each sickness with a RECOVERY_ID grants a post-recovery effect (immunity period):

| Sickness | Recovery Effect ID |
|-|-|
| FoodSickness | `FoodSicknessRecovery` |
| SlimeSickness | `SlimeSicknessRecovery` |
| ZombieSickness | `ZombieSicknessRecovery` |
| RadiationSickness | `RadiationSicknessRecovery` |

## Medicine / Cure Items

Defined in `TUNING.MEDICINE`. MedicineType enum: `Booster=0`, `CureAny=1`, `CureSpecific=2`.

| TUNING Field | Item Config | Type | Cures Sicknesses | Cures Effects | Doctor Station |
|-|-|-|-|-|-|
| BASICBOOSTER | BasicBoosterConfig | Booster (0) | (none) | (none) | (none) |
| INTERMEDIATEBOOSTER | IntermediateBoosterConfig | Booster (0) | (none) | (none) | (none) |
| BASICCURE | BasicCureConfig | CureSpecific (2) | `FoodSickness` | (none) | (none) |
| ANTIHISTAMINE | AntihistamineConfig | CureSpecific (2) | `Allergies` | `HistamineSuppression` | (none) |
| INTERMEDIATECURE | IntermediateCureConfig | CureSpecific (2) | `SlimeSickness` | (none) | DoctorStation |
| ADVANCEDCURE | AdvancedCureConfig | CureSpecific (2) | `ZombieSickness` | (none) | AdvancedDoctorStation |
| BASICRADPILL | BasicRadPillConfig | Booster (0) | (none) | (none) | (none) |
| INTERMEDIATERADPILL | IntermediateRadPillConfig | Booster (0) | (none) | (none) | AdvancedDoctorStation |

### Doctoring Constants

| Constant | Value |
|-|-|
| `RECUPERATION_DISEASE_MULTIPLIER` | 1.1x |
| `RECUPERATION_DOCTORED_DISEASE_MULTIPLIER` | 1.2x |
| `WORK_TIME` | 10s |

## Sickness Components

Each sickness is composed of reusable `SicknessComponent` subclasses:

| Component Class | Purpose |
|-|-|
| `CommonSickEffectSickness` | Standard sick visual overlay |
| `CustomSickEffectSickness` | Custom kanim sick effect (Zombie/Radiation) |
| `AnimatedSickness` | Override dupe animation set |
| `AttributeModifierSickness` | Apply attribute modifiers while sick |
| `PeriodicEmoteSickness` | Periodic emote (vomit, cough, sneeze) |

## Key Types for Modding

| Type | Namespace | Purpose |
|-|-|-|
| `Sickness` | `Klei.AI` | Abstract base for all sicknesses |
| `SicknessInstance` | `Klei.AI` | Runtime instance on a dupe |
| `Sicknesses` | `Klei.AI` | Component managing active sicknesses on a GameObject |
| `Disease` | `Klei.AI` | Abstract base for germ types |
| `Database.Sicknesses` | `Database` | Registry of all sickness definitions |
| `Database.Diseases` | `Database` | Registry of all germ definitions |
| `MedicineInfo` | (global) | Cure/booster definition (id, effect, type, curedSicknesses, doctorStationId) |
| `SicknessType` | (nested in Sickness) | Enum: Pathogen, Ailment, Injury |
| `SicknessExposureInfo` | (global) | Struct: sicknessID + sourceInfo |
| `SicknessMonitor` | (global) | State machine: healthy/sick/post states |
| `SicknessTrigger` | (global) | Component that triggers sickness on buildings/items |
