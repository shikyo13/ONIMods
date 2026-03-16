# Effects — ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

## Room Bonuses

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| RoomRelaxationEffect | Room Relaxation | 240s | +3 Morale | Rec Room / Park / Nature Reserve |
| RecTimeEffect | Rec Time | 120s | +2 Morale | Using rec building |
| BedroomStamina | Well Rested | 600s | +2 Morale | Sleeping in Bedroom |
| DecorBonus | Decor Bonus | permanent | +1 Morale per room tier | High-decor room |
| HighDecor | High Decor | 240s | +1 Morale | Exposure to high decor |

## Food / Morale

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| GoodFoodQuality | Good Meal | 600s | varies by food quality | Eating high-quality food |
| PoorFoodQuality | Poor Meal | 600s | varies by food quality | Eating low-quality food |
| WellFed | Well Fed | varies | +2 Morale | Eating food above expectation |
| SpicedFood | Spiced Food | 600s | varies by spice | Eating spiced food |
| UnspicedFood | Bland Food | 600s | -1 Morale | Eating unspiced food (if spice expected) |
| RehydratedFoodConsumed | Rehydrated Food | 600s | -1 Morale | Eating rehydrated food |

## Water / Hygiene

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| Showered | Refreshed | 600s | +3 Morale | Using Shower |
| SoakingWet | Soaking Wet | permanent | -3 Athletics | Standing in liquid |
| WetFeet | Wet Feet | permanent | -2 Athletics | Feet in liquid |
| FullBladder | Full Bladder | permanent | -1 Athletics | Needs bathroom |
| DecontaminationShower | Decontaminated | 600s | removes germs | Decontamination Shower |

## Temperature / Environmental

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| ColdAir | Chilly Surroundings | permanent | -1 Athletics | Ambient temp below comfort range |
| WarmAir | Warm Surroundings | permanent | -1 Athletics | Ambient temp above comfort range |
| FeelingCold | Feeling Cold | permanent | -2 Athletics | Below cold threshold |
| FeelingWarm | Feeling Warm | permanent | -2 Athletics | Above warm threshold |
| WarmingUp | Warming Up | 120s | recovery marker | Recovering from cold |
| ExitingCold | Leaving Cold | varies | recovery marker | Transitioning from cold zone |
| ExitingHot | Leaving Heat | varies | recovery marker | Transitioning from hot zone |
| RecoveringWarmnth | Recovering Warmth | 120s | temperature recovery | Warming station / campfire |
| Scalding | Scalded | permanent | -5 Athletics, is_bad | Extreme heat |
| Hypothermia | Hypothermia | permanent | -5 Athletics, is_bad | Extreme cold |
| ColdRegulating | Cold Regulating | permanent | temperature control | Thermoregulator suit |
| WarmRegulating | Warm Regulating | permanent | temperature control | Thermoregulator suit |
| Warm_Vest | Warm Sweater | permanent | +insulation | Wearing warm sweater |
| IsWarming | Is Warming | varies | warming marker | Near warming station |
| Cramped | Confined | permanent | -5 Athletics | In cramped space |
| BonusLitWorkspace | Lit Workspace | permanent | +15% speed | Working in lit area |
| PoorDecor | Ugly Surroundings | permanent | +Stress | Low decor exposure |

## Sleep

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| Sleeping | Sleeping | permanent | stamina recovery | Normal sleep |
| SleepingExhausted | Sleeping (Exhausted) | permanent | stamina recovery | Exhaustion collapse |
| SleepingInterruptedByNoise | Interrupted Sleep (Noise) | permanent | reduced stamina | Noise during sleep |
| SleepingInterruptedByLight | Interrupted Sleep (Light) | permanent | reduced stamina | Light during sleep |
| SleepingInterruptedByFearOfDark | Interrupted Sleep (Dark) | permanent | reduced stamina | Afraid of dark |
| SleepingInterruptedByMovement | Interrupted Sleep (Shaking) | permanent | reduced stamina | Movement nearby |
| SleepingInterruptedByCold | Interrupted Sleep (Cold) | permanent | reduced stamina | Cold during sleep |
| PassedOutSleep | Passed Out | permanent | stamina recovery | Exhaustion collapse |
| FloorSleep | Floor Sleep | 600s | -1 Morale | Slept on floor |
| NarcolepticSleep | Narcoleptic Sleep | varies | narcolepsy trigger | Narcolepsy trait |
| TerribleSleep | Terrible Sleep | 600s | -2 Morale | Poor sleep conditions |
| BadSleep | Restless Sleep | 600s | -1 Morale | Disturbed sleep |
| BadSleepMovement | Restless (Movement) | 600s | -1 Morale | Shaking during sleep |
| BadSleepAfraidOfDark | Restless (Dark) | 600s | -1 Morale | Afraid of dark trait |
| BadSleepCold | Restless (Cold) | 600s | -1 Morale | Cold during sleep |
| SleepClinic | Sleep Clinic | 600s | +stamina bonus | Sleep clinic bed |
| SleepClinicPajamas | Sleep Clinic Pajamas | 600s | +stamina bonus | Wearing pajamas in sleep clinic |
| BeachChairLit | Beach Chair (Lit) | 240s | +2 Morale | Beach chair in lit area |
| BeachChairUnlit | Beach Chair (Unlit) | 240s | +1 Morale | Beach chair in dark area |

## Medical / Status

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| MassageTableComplete | De-Stressed | 600s | -30 Stress | Massage table |
| RecentlyHotTub | Hot Tubbed | 600s | -10 Stress | Hot tub use |
| HotTubRelaxing | Hot Tub Relaxation | permanent | stress reduction | In hot tub |
| DoctoredOffCotEffect | Medical Attention | 600s | +immunity | Doctored medical cot |
| StressReduction | Stress Reduction | varies | -Stress | Various sources |
| LowImmunity | Low Immunity | permanent | +sickness risk | Immune system compromised |
| Overjoyed | Overjoyed | permanent | special bonus | Morale greatly exceeds expectation |
| Stressed | Stressed | permanent | stress response | Morale below expectation |
| Narcolepsy | Narcolepsy | permanent | may fall asleep | Narcoleptic trait |
| Uncomfortable | Uncomfortable | permanent | +Stress | Wearing uncomfortable suit |

## Sickness

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| FoodSickness | Food Poisoning | 300s | -vomiting, -Athletics | Ingesting food poisoning germs |
| SlimeSickness | Slimelung | 300s | -breathing, -Athletics | Breathing slimelung germs |
| ZombieSickness | Zombie Spores | 300s | -all stats | Zombie spore infection |
| SunburnSickness | Sunburn | 300s | -Athletics | Radiation exposure (DLC) |
| FoodSicknessRecovery | Recovering (Food) | 600s | immunity | Post-food poisoning recovery |
| SlimeSicknessRecovery | Recovering (Slime) | 600s | immunity | Post-slimelung recovery |
| ZombieSicknessRecovery | Recovering (Zombie) | 600s | immunity | Post-zombie spore recovery |

## Radiation (DLC)

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| RadiationExposureMinor | Radiation Exposure (Minor) | permanent | -minor debuffs | Low radiation |
| RadiationExposureMajor | Radiation Exposure (Major) | permanent | -major debuffs | Medium radiation |
| RadiationExposureExtreme | Radiation Exposure (Extreme) | permanent | -severe debuffs | High radiation |
| SeafoodRadiationResistance | Radiation Resistance | 600s | +radiation resist | Eating irradiated seafood |

## Bionic (DLC)

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| GunkSick | Gunk Sick | permanent | -debuffs, is_bad | Gunk buildup |
| ExpellingGunk | Expelling Gunk | 20s | -Athletics | Expelling gunk |
| GunkHungover | Gunk Hungover | 50s | -debuffs | Post-gunk expulsion |
| NoLubricationMinor | Low Lubrication | 20s | -Athletics | Needs lubrication |
| NoLubricationMajor | No Lubrication | permanent | -4 Athletics | Severe lubrication deficit |
| BionicOffline | Bionic Offline | permanent | -8 all stats | Battery depleted |
| BionicOfflineIncapacitated | Offline (Incapacitated) | permanent | incapacitated | Total battery failure |
| BionicBedTimeEffect | Bionic Bedtime | permanent | sleep mode | Scheduled rest |
| BionicWaterStress | Water Stress | 20s | -debuffs | Water on circuits |
| BionicCriticalBattery | Critical Battery | permanent | warning marker | Very low battery |
| BionicExplorerBooster | Explorer Booster | permanent | +Athletics | Explorer booster installed |
| BionicRadiationExposureMinor | Radiation (Minor) | permanent | -minor | Bionic radiation (minor) |
| BionicRadiationExposureMajor | Radiation (Major) | permanent | -major | Bionic radiation (major) |
| BionicRadiationExposureExtreme | Radiation (Extreme) | permanent | -severe | Bionic radiation (extreme) |
| WarmTouch | Warm Touch | 40s | +healing, temp stabilize | MedBay touch skill |
| WarmTouchFood | Warm Touch (Food) | 600s | +food bonus | Warm touch on food |
| RefreshingTouch | Refreshing Touch | 120s | +stress relief | Refreshing touch skill |

## Critter / Ranching

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| Ranched | Ranched | 600s | +happiness | Grooming station |
| Groomed | Groomed | 600s | +happiness | Being groomed |
| EggSong | Lullabied | 600s | +incubation speed | Egg lullaby |
| HadMilk | Had Milk | 600s | +happiness | Milked by rancher |
| HuggingFrenzy | Hugging Frenzy | 600s | +reproduction | Hug interaction |
| EggHug | Egg Hug | 600s | +incubation speed | Hug from critter |
| DivergentCropTended | Crop Tended | 600s | +growth speed | Sweetle tending |
| DivergentCropTendedWorm | Crop Tended (Worm) | 600s | +growth speed | Grub tending |
| ButterflyPollinated | Pollinated | 600s | +growth speed | Butterfly pollination |
| MooWellFed | Well Fed (Moo) | 600s | produces natural gas | Gassy moo fed |
| HuskyMooWellFed | Well Fed (Husky Moo) | 600s | increased production | DLC husky moo fed |
| GlassDeerWellFed | Well Fed (Gazelle) | 600s | increased production | Gazelle fed |
| GoldBellyWellFed | Well Fed (Gold Belly) | 600s | increased production | Gold belly fed |
| IceBellyWellFed | Well Fed (Ice Belly) | 600s | increased production | Ice belly fed |
| RaptorWellFed | Well Fed (Raptor) | 600s | increased production | Raptor fed |
| WoodDeerWellFed | Well Fed (Wood Deer) | 600s | increased production | Wood deer fed |
| MosquitoFed | Mosquito Fed | 600s | +reproduction | Mosquito fed |
| DupeMosquitoBite | Mosquito Bite | 600s | -debuffs, is_bad | Bitten by mosquito |
| PredatorFailedHunt | Failed Hunt | 600s | hunt cooldown | Predator missed prey |
| PreyEvadedHunt | Evaded Hunt | 600s | safety marker | Prey escaped predator |

## Misc / Decor Tiers

| Effect ID | Display Name | Duration | Modifiers | Source |
|-|-|-|-|-|
| DecorMinus1 | Very Ugly | permanent | -1 Morale | Decor < -25 |
| Decor0 | Ugly | permanent | 0 Morale | Decor -25 to 0 |
| Decor1 | Plain | permanent | +1 Morale | Decor 0 to 35 |
| Decor2 | Presentable | permanent | +2 Morale | Decor 35 to 50 |
| Decor3 | Attractive | permanent | +3 Morale | Decor 50 to 65 |
| Decor4 | Charming | permanent | +4 Morale | Decor 65 to 80 |
| Decor5 | Gorgeous | permanent | +5 Morale | Decor 80+ |
| RecentlySlippedTracker | Recently Slipped | 200s | slip tracking | Slipped on liquid |
| HotStuff | Hot Stuff | 600s | +heat tolerance | Ate hot food |
| PuftAlphaNearbyOxylite | Nearby Oxylite | permanent | +production | Puft near oxylite |
| OilFloaterDecor | Decor Bonus | permanent | +decor output | Decor oil floater |
| GlassDeerDecor | Decor Aura | permanent | +decor output | Glass gazelle |

## Usage Notes

- Apply via `Effects` component: `go.GetComponent<Effects>().Add("EffectId", should_save)`
- Remove via: `go.GetComponent<Effects>().Remove("EffectId")`
- Effect lookup: `Db.Get().effects.Get("EffectId")`
- Display names resolve from `STRINGS.DUPLICANTS.MODIFIERS.<ID>.NAME` at runtime
- Duration 0 = permanent (lasts until removed). Negative values = special handling
- Effects defined in `ModifierSet.LoadEffects()`, `ModifierSet.CreateCritteEffects()`, `ModifierSet.CreateMosquitoEffects()`
