# Foods & Recipes - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

Source: `TUNING.FOOD.FOOD_TYPES`, `EdiblesManager.FoodInfo`, cooking station configs.

FoodInfo constructor: `(id, caloriesPerUnit, quality, preserveTemp, rotTemp, spoilTime, canRot, dlcIds)`
Quality tiers: -1 Awful, 0 Terrible, 1 Mediocre, 2 Good, 3 Great, 4 Amazing, 5 Wonderful, 6 More Wonderful
Spoil times: DEFAULT=4800s (8cy), QUICK=2400s (4cy), SLOW=9600s (16cy), VERYSLOW=19200s (32cy)

## Foraged / Uncraftable Foods

| ID | Calories (kcal) | Quality | Spoil (cycles) | Can Rot | DLC | Notes |
|-|-|-|-|-|-|-|
| FieldRation | 800 | -1 | 32 | no | base | Found in care packages/lockers |
| BasicForagePlant | 800 | -1 | 8 | no | base | Muckroot, buried object |
| ForestForagePlant | 6400 | -1 | 8 | no | base | Hexalent fruit |
| SwampForagePlant | 2400 | -1 | 8 | no | DLC1 | Swamp Chard Heart |
| IceCavesForagePlant | 800 | -1 | 8 | no | DLC2 | Frostbite Fruitage |
| GardenForagePlant | 800 | -1 | 8 | no | DLC4 | Garden forage plant |

## Raw Plant Foods

| ID | Calories (kcal) | Quality | Spoil (cycles) | Can Rot | DLC | Notes |
|-|-|-|-|-|-|-|
| BasicPlantFood | 600 | -1 | 8 | yes | base | Meal Lice |
| PrickleFruit | 1600 | 0 | 8 | yes | base | Bristle Berry |
| Mushroom | 2400 | 0 | 8 | yes | base | Dusk Cap |
| Lettuce | 400 | 0 | 4 | yes | base | Waterweed Lettuce; +SeafoodRadResist (DLC1) |
| VineFruit | 325 | 0 | 8 | yes | DLC4 | Vine Fruit |
| SwampFruit | 1840 | 0 | 4 | yes | DLC1 | Bog Jelly |
| WormBasicFruit | 800 | 0 | 8 | yes | DLC1 | Spindly Grubfruit |
| WormSuperFruit | 250 | 1 | 4 | yes | DLC1 | Grubfruit |
| HardSkinBerry | 800 | -1 | 16 | yes | DLC2 | Pikeapple |
| Carrot | 4000 | 0 | 16 | yes | DLC2 | Plume Squash |
| GardenFoodPlantFood | 800 | -1 | 16 | yes | DLC4 | Garden food plant |
| ButterflyFood | 1500 | 1 | 8 | yes | DLC4 | Butterfly food (cooked from seed) |

## Raw Animal Foods

| ID | Calories (kcal) | Quality | Spoil (cycles) | Can Rot | DLC | Notes |
|-|-|-|-|-|-|-|
| Meat | 1600 | -1 | 8 | yes | base | Raw meat |
| RawEgg | 1600 | -1 | 8 | yes | base | Raw egg |
| FishMeat | 1000 | 2 | 4 | yes | base | Pacu fillet; +SeafoodRadResist (DLC1) |
| ShellfishMeat | 1000 | 2 | 4 | yes | base | Shellfish meat; +SeafoodRadResist (DLC1) |
| PlantMeat | 1200 | 1 | 4 | yes | DLC1 | Plant meat (Saturn Critter Trap) |
| PrehistoricPacuFillet | 1000 | 3 | 4 | yes | DLC4 | Jawbo Fillet |
| DinosaurMeat | 0 | -1 | 4 | yes | DLC4 | Dinosaur meat (ingredient only) |

## Ingredient-Only Foods (0 kcal)

| ID | Calories (kcal) | Quality | Spoil (cycles) | Can Rot | DLC | Notes |
|-|-|-|-|-|-|-|
| BeanPlantSeed | 0 | 3 | 8 | yes | base | Nosh Bean |
| SpiceNut | 0 | 0 | 4 | yes | base | Pincha Peppernut |
| ColdWheatSeed | 0 | 0 | 16 | yes | base | Sleet Wheat Grain |
| FernFood | 0 | 2 | 16 | yes | DLC4 | Fern frond (alt. for ColdWheatSeed in recipes) |
| ButterflyPlantSeed | 0 | 2 | 8 | yes | DLC4 | Butterfly plant seed |

## Microbe Musher Recipes

Station: `MicrobeMusher` | No skill required

| Output ID | Calories (kcal) | Quality | Spoil (cy) | Ingredients | Cook Time |
|-|-|-|-|-|-|
| MushBar | 800 | -1 | 8 | 75 kg Dirt + 75 kg Water | 40s |
| BasicPlantBar | 1700 | 0 | 8 | 2x BasicPlantFood + 50 kg Water | 50s |
| Tofu | 3600 | 2 | 4 | 6x BeanPlantSeed + 50 kg Water | 50s |
| FruitCake | 4000 | 3 | 32 (no rot) | 5x ColdWheatSeed/FernFood + 1x PrickleFruit (or 2x HardSkinBerry) | 50s |
| Pemmican | 2600 | 2 | 32 (no rot) | 1x Meat + 1x Tallow | 50s | DLC2 |

## Electric Grill Recipes

Station: `CookingStation` | Skill: CanElectricGrill | 60W

| Output ID | Calories (kcal) | Quality | Spoil (cy) | Ingredients | Cook Time |
|-|-|-|-|-|-|
| PickledMeal | 1800 | -1 | 32 | 3x BasicPlantFood | 30s |
| FriedMushBar | 1050 | 0 | 8 | 1x MushBar | 50s |
| FriedMushroom | 2800 | 1 | 8 | 1x Mushroom | 50s |
| GrilledPrickleFruit | 2000 | 1 | 8 | 1x PrickleFruit | 50s |
| SwampDelights | 2240 | 1 | 8 | 1x SwampFruit | 50s | DLC1 |
| CookedPikeapple | 1200 | 1 | 8 | 1x HardSkinBerry | 50s | DLC2 |
| ButterflyFood | 1500 | 1 | 8 | 1x ButterflyPlantSeed | 50s | DLC4 |
| WormBasicFood | 1200 | 1 | 8 | 1x WormBasicFruit | 50s | DLC1 |
| ColdWheatBread | 1200 | 2 | 8 | 3x ColdWheatSeed/FernFood | 50s |
| CookedEgg | 2800 | 2 | 4 | 1x RawEgg | 50s |
| Pancakes | 3600 | 3 | 8 | 1x RawEgg + 2x ColdWheatSeed/FernFood | 50s |
| CookedFish | 1600 | 3 | 4 | 1x FishMeat/ShellfishMeat | 50s | +SeafoodRadResist (DLC1) |
| CookedMeat | 4000 | 3 | 4 | 2x Meat | 50s |
| WormSuperFood | 2400 | 3 | 32 | 8x WormSuperFruit + 4 kg Sucrose | 50s | DLC1 |

## Gas Range Recipes (Gourmet Cooking Station)

Station: `GourmetCookingStation` | Skill: CanGasRange | 240W | Requires Methane gas input

| Output ID | Calories (kcal) | Quality | Spoil (cy) | Ingredients | Cook Time |
|-|-|-|-|-|-|
| Salsa | 4400 | 4 | 4 | 2x GrilledPrickleFruit + 2x SpiceNut | 50s |
| MushroomWrap | 4800 | 4 | 4 | 1x FriedMushroom + 4x Lettuce | 50s | +SeafoodRadResist (DLC1) |
| SurfAndTurf | 6000 | 4 | 4 | 1x CookedMeat + 1x CookedFish | 50s | +SeafoodRadResist (DLC1) |
| Curry | 5000 | 4 | 16 | 4x Ginger + 4x BeanPlantSeed | 50s | +HotStuff, +WarmTouchFood |
| SpiceBread | 4000 | 5 | 8 | 10x ColdWheatSeed/FernFood + 1x SpiceNut | 50s |
| SpicyTofu | 4000 | 5 | 4 | 1x Tofu + 1x SpiceNut | 50s | +WarmTouchFood |
| Quiche | 6400 | 5 | 4 | 1x CookedEgg + 1x Lettuce + 1x FriedMushroom | 50s | +SeafoodRadResist (DLC1) |
| BerryPie | 4200 | 5 | 4 | 3x ColdWheatSeed/FernFood + 4x WormSuperFruit + 1x GrilledPrickleFruit (or 1.67x CookedPikeapple or 6.15x VineFruit) | 50s | DLC1 |
| Burger | 6000 | 6 | 4 | 1x ColdWheatBread + 1x Lettuce + 1x CookedMeat | 50s | +GoodEats, +SeafoodRadResist (DLC1) |

## Deep Fryer Recipes (DLC2)

Station: `Deepfryer` | Skill: CanDeepFry | 480W | Requires Kitchen room

| Output ID | Calories (kcal) | Quality | Spoil (cy) | Ingredients | Cook Time |
|-|-|-|-|-|-|
| FriesCarrot | 5400 | 3 | 4 | 1x Carrot + 1x Tallow | 50s |
| DeepFriedNosh | 5000 | 3 | 8 | 6x BeanPlantSeed + 1x Tallow | 50s |
| DeepFriedFish | 4200 | 4 | 4 | 1x FishMeat + 2.4x Tallow + 2x ColdWheatSeed/FernFood | 50s | +SeafoodRadResist (DLC1) |
| DeepFriedShellfish | 4200 | 4 | 4 | 1x ShellfishMeat + 2.4x Tallow + 2x ColdWheatSeed/FernFood | 50s | +SeafoodRadResist (DLC1) |
| DeepFriedMeat | 4000 | 3 | 4 | (no recipe found in code) | -- | Food type exists but recipe not registered |

## Smoker Recipes (DLC4)

Station: `Smoker` | Skill: CanGasRange | No power | Burns Peat, outputs CO2

| Output ID | Calories (kcal) | Quality | Spoil (cy) | Ingredients | Cook Time |
|-|-|-|-|-|-|
| SmokedDinosaurMeat | 5000 | 3 | 8 | 6x DinosaurMeat + 100 kg Wood/Peat | 600s |
| SmokedFish | 2800 | 3 | 32 | 6x FishMeat/PrehistoricPacuFillet + 100 kg Wood/Peat | 600s | +SeafoodRadResist (DLC1) |
| SmokedVegetables | 2863 | 2 | 16 | 7x GardenFoodPlantFood/HardSkinBerry/WormBasicFruit + 100 kg Wood/Peat | 600s |

## Uncraftable Cooked Foods

These have food info definitions but no recipe found in station configs.

| ID | Calories (kcal) | Quality | Spoil (cy) | DLC | Notes |
|-|-|-|-|-|-|
| GammaMush | 1050 | 1 | 4 | base | Food type exists; recipe not found in current build |

## Quality Tier Summary

| Quality | Tier Name | Morale Bonus | Example Foods |
|-|-|-|-|
| -1 | Awful | -1 | MushBar, FieldRation, BasicPlantFood, Meat, RawEgg |
| 0 | Terrible | 0 | BasicPlantBar, FriedMushBar, Mushroom, Lettuce |
| 1 | Mediocre | +1 | GrilledPrickleFruit, FriedMushroom, PlantMeat, GammaMush |
| 2 | Good | +2 | ColdWheatBread, CookedEgg, FishMeat, Tofu, Pemmican |
| 3 | Great | +4 | CookedMeat, CookedFish, Pancakes, FruitCake, FriesCarrot |
| 4 | Amazing | +8 | Salsa, SurfAndTurf, MushroomWrap, Curry, DeepFriedFish |
| 5 | Wonderful | +12 | SpiceBread, SpicyTofu, Quiche, BerryPie |
| 6 | More Wonderful | +16 | Burger |
