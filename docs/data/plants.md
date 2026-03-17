# Plants & Crops - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

## Growth Constants

| Constant | Value | Notes |
|-|-|-|
| GROWTH_RATE | 0.00167/s | Domesticated base rate |
| WILD_GROWTH_RATE | 0.000417/s | 25% of domesticated |
| WILD_GROWTH_RATE_MODIFIER | 0.25 | Multiplier for wild plants |
| SELF_HARVEST_TIME | 2400s (4 cycles) | Time to auto-harvest when mature |
| BASE_BONUS_SEED_PROBABILITY | 0.1 | 10% chance for extra seed |
| FERTILIZATION_GAIN_RATE | 1.667 | Rate fertilization amount fills |
| FERTILIZATION_LOSS_RATE | -0.167 | Rate fertilization drains when unfed |

## Food Crops

| ID | Display Name | Growth (cycles) | Yield | Temp Low/High (C) | Lethal Low/High (C) | Irrigation (kg/cycle) | Fertilizer (kg/cycle) | Atmosphere | Notes |
|-|-|-|-|-|-|-|-|-|-|
| BasicSingleHarvestPlant | Mealwood | 3 | 1x BasicPlantFood | 10/30 | -55/125 | - | Dirt 10 kg/cyc | O2, PO2, CO2 | No tinkering; 12 fiber/cyc |
| PrickleFlower | Bristle Blossom | 6 | 1x PrickleFruit | 5/30 | -55/125 | Water 20 kg/cyc | - | O2, PO2, CO2 | Requires 200 lux; pollen emitter |
| MushroomPlant | Dusk Cap | 7.5 | 1x Mushroom | 5/35 | -45/125 | - | Slime 4 kg/cyc | CO2 | Prefers darkness |
| ColdWheat | Sleet Wheat | 18 | 18x ColdWheatSeed | -55/5 | -155/-85 | Water 20 kg/cyc | Dirt 5 kg/cyc | O2, PO2, CO2 | Seed is the food item |
| SpiceVine | Pincha Pepperplant | 8 | 4x SpiceNut | 35/85 | -15/175 | Polluted Water 35 kg/cyc | Phosphorite 1 kg/cyc | any | Hanging; 16 fiber/cyc |
| BeanPlant | Nosh Sprout | 21 | 12x BeanPlantSeed | -25/0 | -75/50 | Ethanol 20 kg/cyc | Dirt 5 kg/cyc | CO2 | Seed is the food item |
| SeaLettuce | Waterweed | 12 | 12x Lettuce | 22/65 | -25/125 | Salt Water 5 kg/cyc | Bleach Stone 0.5 kg/cyc | Water, Salt Water, Brine | Underwater plant |
| SwampHarvestPlant | Bog Bucket | 6.6 | 1x SwampFruit | 10/30 | -55/125 | Polluted Water 40 kg/cyc | - | O2, PO2, CO2 | DLC1; prefers darkness |
| WormPlant | Grubfruit Plant | 4 | 1x WormBasicFruit | 15/50 | 0/100 | - | Sulfur 10 kg/cyc | O2, PO2, CO2 | DLC1; transforms to SuperWormPlant when Sweetle-tended; 8 fiber/cyc |
| SuperWormPlant | Grubfruit Plant (Variant) | 8 | 8x WormSuperFruit | 15/50 | 0/100 | - | Sulfur 10 kg/cyc | O2, PO2, CO2 | DLC1; reverts to WormPlant on harvest; 16 fiber/cyc |
| CritterTrapPlant | Saturn Critter Trap | 30 | 10x PlantMeat | -30/0 | -100/-90 | Polluted Water 10 kg/cyc | - | any | DLC1; traps walkers/hoverers; emits H2 |
| HardSkinBerryPlant | Frozen Mealberry | 3 | 1x HardSkinBerry | -55/-14 | -155/-4 | - | Phosphorite 5 kg/cyc | O2, PO2, CO2 | DLC2; 12 fiber/cyc |
| CarrotPlant | Plume Squash | 9 | 1x Carrot | -55/-14 | -155/-4 | Ethanol 15 kg/cyc | - | O2, PO2, CO2 | DLC2 |
| GardenFoodPlant | Spikefruit | 3 | 1x GardenFoodPlantFood | -5/40 | -10/50 | - | Peat 10 kg/cyc | O2, PO2, CO2 | DLC4; requires pollination |
| ButterflyPlant | Flutter Bloom | 5 | 1x Butterfly | 10/45 | -40/80 | - | Dirt 10 kg/cyc | O2, PO2, CO2, Cl2 | DLC4; seed is food item |
| KelpPlant | Kelp | 5 | 50x Kelp | -10/85 | -20/100 | - | Toxic Sand 10 kg/cyc | Water, PO2 Water, Salt Water, Brine, Phyto Oil, Nat. Resin | DLC4; hanging; underwater |

## Industrial/Resource Crops

| ID | Display Name | Growth (cycles) | Yield | Temp Low/High (C) | Lethal Low/High (C) | Irrigation (kg/cycle) | Fertilizer (kg/cycle) | Atmosphere | Notes |
|-|-|-|-|-|-|-|-|-|-|
| BasicFabricPlant | Thimble Reed | 2 | 1x Reed Fiber | 22/37 | -25/125 | Polluted Water 160 kg/cyc | - | O2, PO2, CO2, PO2 Water, Water | Underwater-safe |
| SwampLily | Balm Lily | 12 | 2x SwampLilyFlower | 35/85 | -15/175 | - | - | Chlorine | 24 fiber/cyc |
| SaltPlant | Dasha Saltvine | 6 | 65x Salt | -25/50 | -75/120 | - | Sand 7 kg/cyc | Chlorine | Hanging; consumes Cl2 gas 3.6 kg/cyc |
| ForestTree | Arbor Tree | 4.5 (branch) | 300 kg WoodLog/branch | 15/40 | -15/175 | Polluted Water 70 kg/cyc | Dirt 10 kg/cyc | any | 7 branch slots, max 5 active; trunk never harvested |
| SpaceTree | Bonbon Tree | trunk 4.5 | 20 kg SugarWater/harvest | -75/-15 | -100/20 | - | - | O2, PO2, CO2, Snow, Vacuum | DLC2; needs Snow (100 kg/cyc); light-dependent branching |
| FilterPlant | Hydrocactus | 10 | 350 kg Water | 20/110 | -20/170 | Polluted Water 65 kg/cyc | Sand 5 kg/cyc | Oxygen | DLC1; deprecated; consumes O2 gas |

## Utility Plants (Gas/Resource Production)

| ID | Display Name | Temp Low/High (C) | Lethal Low/High (C) | Irrigation (kg/cycle) | Fertilizer (kg/cycle) | Atmosphere | Function |
|-|-|-|-|-|-|-|-|
| Oxyfern | Oxyfern | 0/40 | -20/100 | Water 19 kg/cyc | Dirt 4 kg/cyc | CO2 | Converts CO2 to O2 (50x ratio) |

## Decorative Plants

| ID | Display Name | Temp Low/High (C) | Lethal Low/High (C) | Decor | Atmosphere | Notes |
|-|-|-|-|-|-|-|
| PrickleGrass | Bluff Briar | 10/30 | -55/125 | +3 / -3 (wilt) | O2, PO2, CO2 | Base game |
| LeafyPlant | Mirth Leaf | 20/50 | 15/100 | +3 / -3 (wilt) | O2, PO2, CO2, Cl2, H2 | Base game |
| BulbPlant | Buddy Bud | 15/20 | -25/60 | +1 / -3 (wilt) | O2, PO2, CO2 | Base game; pollen emitter |
| EvilFlower | Sporechid | -15/240 | -105/290 | +7 / -5 (wilt) | CO2 | Emits Zombie Spores (1000/s) |
| Cylindrica | Cylindrica | 20/50 | 15/100 | +3 / -3 (wilt) | O2, PO2, CO2 | DLC1 |
| ToePlant | Tranquil Toes | -30/0 | -100/-90 | +3 / -3 (wilt) | O2, PO2, CO2 | DLC1 |

## Crop Yield Reference (from TUNING.CROPS)

Authoritative yield and growth time from `CROPS.CROP_TYPES` list.

| Crop ID | Growth (s) | Growth (cycles) | Yield Count | Notes |
|-|-|-|-|-|
| BasicPlantFood | 1800 | 3.0 | 1 | Mealwood |
| PrickleFruit | 3600 | 6.0 | 1 | Bristle Blossom |
| SwampFruit | 3960 | 6.6 | 1 | Bog Bucket |
| Mushroom | 4500 | 7.5 | 1 | Dusk Cap |
| ColdWheatSeed | 10800 | 18.0 | 18 | Sleet Wheat |
| SpiceNut | 4800 | 8.0 | 4 | Pincha Pepperplant |
| BasicFabric | 1200 | 2.0 | 1 | Thimble Reed |
| SwampLilyFlower | 7200 | 12.0 | 2 | Balm Lily |
| PlantFiber | 2400 | 4.0 | 400 | Fiber plants |
| WoodLog | 2700 | 4.5 | 300 | Arbor Tree branch |
| SugarWater | 150 | 0.25 | 20 | Bonbon Tree |
| HardSkinBerry | 1800 | 3.0 | 1 | Frozen Mealberry |
| Carrot | 5400 | 9.0 | 1 | Plume Squash |
| VineFruit | 1800 | 3.0 | 1 | Vine plant |
| OxyRock | 1200 | 2.0 | ~36 | Oxylite crop |
| Lettuce | 7200 | 12.0 | 12 | Waterweed |
| Kelp | 3000 | 5.0 | 50 | Kelp |
| BeanPlantSeed | 12600 | 21.0 | 12 | Nosh Sprout |
| PlantMeat | 18000 | 30.0 | 10 | Saturn Critter Trap |
| WormBasicFruit | 2400 | 4.0 | 1 | Grubfruit (basic) |
| WormSuperFruit | 4800 | 8.0 | 8 | Grubfruit (super) |
| DewDrip | 1200 | 2.0 | 1 | Dew Drop |
| FernFood | 5400 | 9.0 | 36 | Fern food |
| Salt | 3600 | 6.0 | 65 | Dasha Saltvine |
| Water | 6000 | 10.0 | 350 | Hydrocactus |
| Amber | 7200 | 12.0 | 264 | Amber plant |
| GardenFoodPlantFood | 1800 | 3.0 | 1 | Spikefruit |
| Butterfly | 3000 | 5.0 | 1 | Flutter Bloom |
