# Equipment & Clothing - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

## Equipment Slots

| Slot ID | Used By |
|-|-|
| Suit | Atmo Suit, Jet Suit, Lead Suit, Oxygen Mask |
| Outfit | Funky Vest, Warm Vest, Custom Clothing, Sleep Clinic Pajamas |
| Toy | Equippable Balloon |

## Suits

| ID | Display Name | Slot | Mass | Durability/Cycle | Output Element | Modifiers | Effect Immunities | DLC | Notes |
|-|-|-|-|-|-|-|-|-|-|
| Atmo_Suit | STR:EQUIPMENT.PREFABS.ATMO_SUIT | Suit | 200 kg | -0.1 | Dirt | Athletics -6, Insulation +50, ThermalBarrier +0.2, Digging +10, Scalding +1000, Scolding -1000 | SoakingWet, WetFeet, ColdAir, WarmAir, PoppedEarDrums, RecentlySlippedTracker | Base | O2 tank: capacity = O2_USED/s * 600 * 1.25; tags: Suit, Clothes, PedestalDisplayable, AirtightSuit; worn ID: Worn_Atmo_Suit |
| Jet_Suit | STR:EQUIPMENT.PREFABS.JET_SUIT | Suit | 200 kg | -0.1 | Steel | Athletics -6, Insulation +50, ThermalBarrier +0.2, Digging +10, Scalding +1000, Scolding -1000 | SoakingWet, WetFeet, ColdAir, WarmAir, PoppedEarDrums, RecentlySlippedTracker | Base | O2 tank same as Atmo; adds JetSuitTank + hover anim; worn ID: Worn_Jet_Suit; recipe tech: JetSuit |
| Lead_Suit | STR:EQUIPMENT.PREFABS.LEAD_SUIT | Suit | 200 kg | -0.1 | Dirt | Athletics -8, Insulation +50, ThermalBarrier +0.3, Scalding +1000, Scolding -1000, RadiationResistance +0.66, Strength +10 | SoakingWet, WetFeet, ColdAir, WarmAir, PoppedEarDrums, RecentlySlippedTracker | DLC1 | O2 tank: capacity = O2_USED/s * 400; LeadSuitTank batteryDuration=200; worn ID: Worn_Lead_Suit |
| Oxygen_Mask | STR:EQUIPMENT.PREFABS.OXYGEN_MASK | Suit | 15 kg | -0.2 | Dirt | Athletics -2 | (none) | Base | O2 tank capacity=20 kg; IsBody=false (head only); worn ID: Worn_Oxygen_Mask |

## Suit Expert Perk

All suits check for `SkillPerks.ExosuitExpertise` (Suits1 skill). If equipped dupe has this perk, the Athletics penalty is negated via an additional +Athletics modifier equal to the suit's Athletics penalty.

Durability skill bonus: `SUIT_DURABILITY_SKILL_BONUS = 0.25` (25% slower decay).

## Clothing

Clothing uses `ClothingWearer.ClothingInfo` for stat application rather than `AttributeModifier` lists.

| ID | Display Name | Slot | Mass | Decor | Conductivity Mod | Homeostasis Mult | Notes |
|-|-|-|-|-|-|-|-|
| (default) | COOL_VEST.GENERICNAME | (worn by default) | - | -5 | +0.0025 | -1.25 | BASIC_CLOTHING; applied when no clothing equipped |
| Warm_Vest | STR:EQUIPMENT.PREFABS.WARM_VEST | Outfit | 4 kg | 0 | +0.008 | -1.25 | WARM_CLOTHING; best insulation vest |
| Funky_Vest | STR:EQUIPMENT.PREFABS.FUNKY_VEST | Outfit | 4 kg | +30 | +0.0025 | -1.25 | FANCY_CLOTHING; highest base-game decor |
| CustomClothing | STR:EQUIPMENT.PREFABS.CUSTOMCLOTHING | Outfit | 7 kg | +40 | +0.0025 | -1.25 | CUSTOM_CLOTHING; supports EquippableFacadeResource skins |
| SleepClinicPajamas | STR:EQUIPMENT.PREFABS.SLEEPCLINICPAJAMAS | Outfit | 4 kg | +30 | +0.0025 | -1.25 | Uses FANCY_CLOTHING stats; adds "SleepClinic" effect; destroyed on unequip; has ClinicDreamable (workTime=300s) |

Note: `COOL_CLOTHING` info exists in code (decor -10, conductivity +0.0005, homeostasis 0) but `CoolVestConfig` type is not present in Assembly-CSharp.dll - likely removed or never shipped.

## Toys

| ID | Display Name | Slot | Mass | Effects | Notes |
|-|-|-|-|-|-|
| EquippableBalloon | STR:EQUIPMENT.PREFABS.EQUIPPABLEBALLOON | Toy | 1 kg | Adds "HasBalloon" effect; spawns BalloonFX | Joy reaction reward; unequippable=false; destroyed on unequip; duration from `TRAITS.JOY_REACTIONS.JOY_REACTION_DURATION`; supports BalloonOverrideSymbol for visual variants |

## Fabrication

| Equipment | Fabricator | Fab Time | Ingredients |
|-|-|-|-|
| Atmo_Suit | SuitFabricator | 40s | (defined in recipe, not config) |
| Jet_Suit | SuitFabricator | 40s | (defined in recipe, not config) |
| Lead_Suit | SuitFabricator | 40s | (defined in recipe, not config) |
| Oxygen_Mask | SuitFabricator | 20s | (defined in recipe, not config) |
| Warm_Vest | ClothingFabricator | 180s | (defined in recipe, not config) |
| Funky_Vest | ClothingFabricator | 180s | (defined in recipe, not config) |
| CustomClothing | ClothingFabricator | 180s | (defined in recipe, not config) |

## Key Types

| Type | Purpose |
|-|-|
| `EquipmentDef` | ScriptableObject definition - ID, slot, mass, modifiers, callbacks |
| `EquipmentTemplates` | Factory: `CreateEquipmentDef()` builds EquipmentDef instances |
| `EquipmentConfigManager` | Registers all `IEquipmentConfig` implementations at startup |
| `EquipmentSlot` | Subclass of `AssignableSlot(id, name, showInUI)` |
| `Equippable` | MonoBehaviour on equipment GO; holds def + slot assignment |
| `ClothingWearer` | MonoBehaviour on dupes; applies decor + conductivity from ClothingInfo |
| `Durability` | MonoBehaviour on suits; tracks wear via `durabilityLossPerCycle`; links to worn prefab |
| `SuitTank` | MonoBehaviour on suits; stores O2 element + capacity |
| `RepairableEquipment` | MonoBehaviour on worn equipment prefabs |

## String Paths

- Equipment names: `STRINGS.EQUIPMENT.PREFABS.<UPPER_ID>.NAME`
- Equipment desc: `STRINGS.EQUIPMENT.PREFABS.<UPPER_ID>.DESC`
- Equipment effect: `STRINGS.EQUIPMENT.PREFABS.<UPPER_ID>.EFFECT`
- Generic name: `STRINGS.EQUIPMENT.PREFABS.<UPPER_ID>.GENERICNAME`
- Worn name/desc: `STRINGS.EQUIPMENT.PREFABS.<UPPER_ID>.WORN_NAME` / `.WORN_DESC`
