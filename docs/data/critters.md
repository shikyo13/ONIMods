# Critters - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

Temperatures in Kelvin from source; converted to Celsius below. Lifespan = Age.maxAttribute (cycles).
Diet output shows primary poop element; some variants have additional scale/shear drops noted.

## Hatches

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Hatch | Hatch | Sand, Sandstone, Clay, CrushedRock, Dirt, SedimentaryRock, Shale + all food | Carbon | 10 - 40 (lethal: -45 / 100) | 100 | HatchEgg |
| Stone Hatch | HatchHard | SedimentaryRock, IgneousRock, Obsidian, Granite + metal ores | Carbon | 10 - 40 (lethal: -45 / 100) | 100 | HatchHardEgg |
| Sage Hatch | HatchVeggie | Dirt, SlimeMold, Algae, Fertilizer, ToxicSand + all food | Carbon | 10 - 40 (lethal: -45 / 100) | 100 | HatchVeggieEgg |
| Smooth Hatch | HatchMetal | Metal ores (ore -> refined metal) | Refined metals | 10 - 40 (lethal: -45 / 100) | 100 | HatchMetalEgg |

Egg chances (base Hatch): HatchEgg 0.98, HatchHardEgg 0.02, HatchVeggieEgg 0.02
Egg chances (Stone): HatchEgg 0.32, HatchHardEgg 0.65, HatchMetalEgg 0.02
Egg chances (Sage): HatchEgg 0.33, HatchVeggieEgg 0.67
Egg chances (Smooth): HatchEgg 0.11, HatchHardEgg 0.22, HatchMetalEgg 0.67

Tuning: calories/cycle = 700,000 | starve cycles = 10 | eaten/cycle = 140 kg (100 kg for Smooth)

## Pacus

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Pacu | Pacu | Algae, seeds (+Kelp DLC4) | ToxicSand | 0 - 60 (lethal: -20 / 100) | 25 | PacuEgg |
| Tropical Pacu | PacuTropical | Algae, seeds (+Kelp DLC4) | ToxicSand | 30 - 80 (lethal: 10 / 100) | 25 | PacuTropicalEgg |
| Gulp Fish | PacuCleaner | Algae, seeds (+Kelp DLC4) | ToxicSand | -30 - 5 (lethal: -50 / 25) | 25 | PacuCleanerEgg |

Gulp Fish also passively converts DirtyWater -> Water (0.2 kg/s).

Egg chances (base Pacu): PacuEgg 0.98, PacuTropicalEgg 0.02, PacuCleanerEgg 0.02
Egg chances (Tropical): PacuEgg 0.32, PacuTropicalEgg 0.65, PacuCleanerEgg 0.02
Egg chances (Gulp Fish): PacuEgg 0.32, PacuCleanerEgg 0.65, PacuTropicalEgg 0.02

Tuning: calories/cycle = 100,000 | starve cycles = 5 | eaten/cycle = 7.5 kg algae

## Dreckos

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Drecko | Drecko | SpiceVine, SwampLily, Mealwood (plant growth) | Phosphorite | 10 - 60 (lethal: -30 / 100) | 150 | DreckoEgg |
| Glossy Drecko | DreckoPlastic | Mealwood, BristleBlossom (plant growth) | Phosphorite | 20 - 50 (lethal: -30 / 100) | 150 | DreckoPlasticEgg |

Drecko shears Reed Fiber (0.25 kg/cycle, 8-cycle growth, needs H2 atmo).
Glossy Drecko shears Plastic (50 kg/cycle, 3-cycle growth, needs H2 atmo).

Egg chances (base Drecko): DreckoEgg 0.98, DreckoPlasticEgg 0.02
Egg chances (Glossy): DreckoEgg 0.35, DreckoPlasticEgg 0.65

Tuning: calories/cycle = 2,000,000 | starve cycles = 5

## Shine Bugs (Light Bugs)

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Shine Bug | LightBug | PrickleFruit, GrilledPrickleFruit, Phosphorite | (none) | 10 - 40 (lethal: -100 / 100) | 25 | LightBugEgg |
| Sun Bug | LightBugOrange | Mushroom, FriedMushroom, GrilledPrickleFruit, Phosphorite | (none) | 10 - 40 (lethal: -100 / 100) | 25 | LightBugOrangeEgg |
| Royal Bug | LightBugPurple | FriedMushroom, GrilledPrickleFruit, SpiceNut, SpiceBread, Phosphorite | (none) | 10 - 40 (lethal: -100 / 100) | 25 | LightBugPurpleEgg |
| Coral Bug | LightBugPink | FriedMushroom, SpiceBread, PrickleFruit, GrilledPrickleFruit, Salsa, Phosphorite | (none) | 10 - 40 (lethal: -100 / 100) | 25 | LightBugPinkEgg |
| Azure Bug | LightBugBlue | SpiceBread, Salsa, Phosphorite, Phosphorus | (none) | 10 - 40 (lethal: -100 / 100) | 25 | LightBugBlueEgg |
| Abyss Bug | LightBugBlack | Salsa, Meat, CookedMeat, Abyssalite, Phosphorus | (none) | 10 - 40 (lethal: -100 / 100) | 75 | LightBugBlackEgg |
| Radiant Bug | LightBugCrystal | CookedMeat, Diamond | (none) | 10 - 40 (lethal: -100 / 100) | 75 | LightBugCrystalEgg |

Egg chain: Base -> Orange -> Purple -> Pink -> Blue -> Black -> Crystal (each 0.02 chance of next morph).

Tuning: calories/cycle = 40,000 | starve cycles = 8

## Pokeshells (Crabs)

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Pokeshell | Crab | ToxicSand, RotPile | Sand | 0 - 40 (lethal: -50 / 100) | 100 | CrabEgg |
| Oakshell | CrabWood | ToxicSand, RotPile, SlimeMold | Sand | 0 - 40 (lethal: -50 / 100) | 100 | CrabWoodEgg |
| Sanishell | CrabFreshWater | ToxicSand, RotPile, SlimeMold | Sand | 0 - 40 (lethal: -50 / 100) | 100 | CrabFreshWaterEgg |

Oakshell drops CrabWoodShell (500 kg on death); molts 100 kg/cycle when happy.
Sanishell drops ShellfishMeat on death; cleans germs from adjacent liquid tiles.

Egg chances (base Pokeshell): CrabEgg 0.97, CrabWoodEgg 0.02, CrabFreshWaterEgg 0.01
Egg chances (Oakshell): CrabEgg 0.32, CrabWoodEgg 0.65, CrabFreshWaterEgg 0.02
Egg chances (Sanishell): CrabEgg 0.32, CrabWoodEgg 0.02, CrabFreshWaterEgg 0.65

Tuning: calories/cycle = 100,000 | starve cycles = 10 | eaten/cycle = 70 kg

## Pufts

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Puft | Puft | ContaminatedOxygen (gas) | SlimeMold | 15 - 55 (lethal: -50 / 100) | 75 | PuftEgg |
| Puft Prince | PuftAlpha | PollutedO2, Chlorine, Oxygen (gas) | SlimeMold/BleachStone/OxyRock | 20 - 40 (lethal: -50 / 100) | 75 | PuftAlphaEgg |
| Dense Puft | PuftOxylite | Oxygen (gas) | OxyRock | 0 - 60 (lethal: -50 / 100) | 75 | PuftOxyliteEgg |
| Squeaky Puft | PuftBleachstone | ChlorineGas (gas) | BleachStone | 0 - 60 (lethal: -50 / 100) | 75 | PuftBleachstoneEgg |

Egg chances (base Puft): PuftEgg 0.98, PuftAlphaEgg 0.02, PuftOxyliteEgg 0.02, PuftBleachstoneEgg 0.02
Egg chances (Prince): PuftEgg 0.98, PuftAlphaEgg 0.02
Egg chances (Dense): PuftEgg 0.31, PuftAlphaEgg 0.02, PuftOxyliteEgg 0.67
Egg chances (Squeaky): PuftEgg 0.31, PuftAlphaEgg 0.02, PuftBleachstoneEgg 0.67

Tuning: calories/cycle = 200,000 | starve cycles = 6 | eaten/cycle = 50 kg (30 kg for Prince/Squeaky)

## Slicksters (Oil Floaters)

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Slickster | Oilfloater | CarbonDioxide (gas) | CrudeOil | 50 - 140 (lethal: 0 / 200) | 100 | OilfloaterEgg |
| Molten Slickster | OilfloaterHighTemp | CarbonDioxide (gas) | Petroleum | 100 - 200 (lethal: 50 / 300) | 100 | OilfloaterHighTempEgg |
| Longhair Slickster | OilfloaterDecor | Oxygen (gas) | (none) | 0 - 50 (lethal: -50 / 100) | 150 | OilfloaterDecorEgg |

Egg chances (base Slickster): OilfloaterEgg 0.98, OilfloaterHighTempEgg 0.02, OilfloaterDecorEgg 0.02
Egg chances (Molten): OilfloaterEgg 0.33, OilfloaterHighTempEgg 0.66, OilfloaterDecorEgg 0.02
Egg chances (Longhair): OilfloaterEgg 0.33, OilfloaterHighTempEgg 0.02, OilfloaterDecorEgg 0.66

Tuning: calories/cycle = 120,000 | starve cycles = 5 | eaten/cycle = 20 kg (30 kg for Longhair)

## Pips (Squirrels)

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Pip | Squirrel | Arbor Tree, Thimble Reed (+SpaceTree DLC2) (plant growth) | Dirt | 10 - 40 (lethal: -45 / 100) | 100 | SquirrelEgg |
| Cuddle Pip | SquirrelHug | Arbor Tree, Thimble Reed (+SpaceTree DLC2) (plant growth) | Dirt | 10 - 40 (lethal: -45 / 100) | 100 | SquirrelHugEgg |

Pips can plant seeds in natural tiles. Cuddle Pips can hug duplicants for morale.

Egg chances (base Pip): SquirrelEgg 0.98, SquirrelHugEgg 0.02
Egg chances (Cuddle): SquirrelEgg 0.35, SquirrelHugEgg 0.65

Tuning: calories/cycle = 100,000 | starve cycles = 10

## Shove Voles (Moles)

| Variant | ID | Diet (Input) | Diet (Output) | Temp Range (C) | Lifespan (cycles) | Egg ID |
|-|-|-|-|-|-|-|
| Shove Vole | Mole | Regolith, Dirt, IronOre | (consumed, no poop tag) | -100 - 400 (lethal: -200 / 500) | 100 | MoleEgg |
| Delicacy Vole | MoleDelicacy | Regolith, Dirt, IronOre | (consumed, no poop tag) | -100 - 100 (lethal: -200 / 500) | 100 | MoleDelicacyEgg |

Delicacy Vole grows Ginger when kept at 70-80 C (1 kg/cycle, 8-cycle growth).
Shove Voles burrow through solid tiles. Overcrowding space = 0 (unlimited).

Egg chances (base Mole): MoleEgg 0.98, MoleDelicacyEgg 0.02
Egg chances (Delicacy): MoleEgg 0.32, MoleDelicacyEgg 0.65

Tuning: calories/cycle = 4,800,000 | starve cycles = 10

## Notes

- Temp ranges shown as "comfortable low - comfortable high (lethal: low / high)" in Celsius
- Kelvin to Celsius: subtract 273.15
- Egg weights listed in tuning classes (typically 0.5-4 kg)
- `CONVERSION_EFFICIENCY.NORMAL` = standard output ratio; `GOOD_1/2/3` = higher; `BAD_1/2` = lower
- Smooth Hatch outputs refined metal matching the ore type (e.g., IronOre -> Iron, GoldAmalgam -> Gold)
- Pacus also eat all plant seeds; diet is shared via BasePacuConfig
- Shine Bugs emit light (except Abyss Bug) and radiation (if DLC enabled); they eat but produce no solid waste
- DLC2 (Frosty Planet) and DLC4 (Biome Bundle) add extra diet items to some species
