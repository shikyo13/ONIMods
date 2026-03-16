# Chore Types -- ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

Priority Value = scheduling weight (higher = done first). Interrupt Priority = preemption rank (higher = harder to interrupt).
Chore Groups determine which errand tab a chore belongs to. Chores with no group are automatic/involuntary.
3 fields exist but are uninitialized (unused): Warmup, Cooldown, CompostWorkable.

## Life Support

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| Die | -- | 10000 | 100000 |
| Entombed | -- | 9950 | 99900 |
| HealCritical | -- | 9650 | 99800 |
| BeIncapacitated | -- | 9600 | 99700 |
| GeneShuffle | -- | 9450 | 99700 |
| Migrate | -- | 9400 | 99700 |
| BeOffline | -- | 9500 | 99600 |

## Emergency

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| DebugGoTo | -- | 9350 | 99500 |
| StressVomit | -- | 8450 | 99400 |
| MoveTo | -- | 9300 | 99300 |
| RocketEnterExit | -- | 9250 | 99300 |
| FindOxygenSourceItem_Critical | -- | 9150 | 99200 |
| BionicAbsorbOxygen_Critical | -- | 9100 | 99200 |
| RecoverBreath | -- | 8950 | 99200 |
| ReturnSuitUrgent | -- | 7400 | 99100 |
| UglyCry | -- | 8400 | 99000 |
| BansheeWail | -- | 8350 | 98900 |
| StressShock | -- | 8300 | 98900 |
| BingeEat | -- | 8250 | 98900 |
| WaterDamageZap | -- | 9550 | 98800 |
| ExpellGunk | -- | 9050 | 98700 |

## Compulsory

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| Pee | -- | 9000 | 98600 |
| EmoteHighPriority | -- | 8600 | 98600 |
| StressActingOut | -- | 8200 | 98600 |
| Vomit | -- | 8150 | 98600 |
| Cough | -- | 8100 | 98600 |
| RadiationPain | -- | 8050 | 98600 |
| SwitchHat | -- | 8000 | 98600 |
| StressIdle | -- | 7950 | 98600 |
| RescueIncapacitated | -- | 7900 | 98600 |
| OilChange | -- | 7650 | 98600 |
| SolidOilChange | -- | 7600 | 98600 |
| MoveToQuarantine | -- | 8750 | 98500 |
| TopPriority | -- | 6150 | 98400 |
| RocketControl | Rocketry | 6550 | 98300 |
| Attack | Combat | 5000 | 98200 |

## Survival

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| Flee | -- | 8800 | 98100 |
| BreakPee | -- | 7850 | 98000 |
| Eat | -- | 7800 | 98000 |
| ReloadElectrobank | -- | 7750 | 98000 |
| LearnSkill | -- | 6750 | 98000 |
| UnlearnSkill | -- | 6700 | 98000 |
| BionicAbsorbOxygen | -- | 7550 | 97900 |
| FindOxygenCanister | -- | 7500 | 97900 |
| TakeMedicine | -- | 7200 | 97800 |
| SleepDueToDisease | -- | 7350 | 97700 |
| BionicRestDueToDisease | -- | 7300 | 97700 |
| RestDueToDisease | -- | 7100 | 97700 |
| Heal | -- | 6850 | 97700 |

## Personal Needs

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| Narcolepsy | -- | 7450 | 97600 |
| Sleep | -- | 7250 | 97600 |
| BionicBedtimeMode | -- | 7050 | 97600 |
| GetDoctored | -- | 7150 | 97500 |
| DoctorChore | MedicalAid | 5000 | 97500 |

## Leisure

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| Emote | -- | 8650 | 97400 |
| Hug | -- | 8500 | 97400 |
| Fart | -- | 6500 | 97400 |
| Mourn | -- | 6200 | 97300 |
| StressHeal | -- | 6450 | 97200 |
| JoyReaction | -- | 6600 | 97100 |
| Party | -- | 6400 | 97000 |
| Relax | Recreation | 6350 | 96900 |

## Work

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| SeekAndInstallUpgrade | -- | 7700 | 96800 |
| Equip | -- | 6650 | 96800 |
| Unequip | -- | 6250 | 96800 |
| SuitMarker | -- | 9900 | 96700 |
| Slip | -- | 9850 | 96700 |
| Checkpoint | -- | 9800 | 96700 |
| TravelTubeEntrance | -- | 9750 | 96700 |
| WashHands | -- | 9700 | 96700 |
| DropUnusedInventory | -- | 9200 | 96700 |
| StressEmote | -- | 8550 | 96700 |
| ScrubOre | -- | 7000 | 96700 |
| DeliverFood | -- | 6950 | 96700 |
| Sigh | -- | 6900 | 96700 |
| Shower | -- | 6800 | 96700 |
| Recharge | -- | 6300 | 96700 |
| Toggle | Toggle | 5000 | 96700 |
| Capture | Ranching | 5000 | 96700 |
| CreatureFetch | Ranching | 5000 | 96700 |
| RanchingFetch | Ranching, Hauling | 5000 | 96700 |
| EggSing | Ranching | 5000 | 96700 |
| Astronaut | MachineOperating | 5000 | 96700 |
| FetchCritical | Hauling, LifeSupport | 5000 | 96700 |
| Art | Art | 5000 | 96700 |
| EmptyStorage | Basekeeping, Hauling | 5000 | 96700 |
| Mop | Basekeeping | 5000 | 96700 |
| Relocate | -- | 5000 | 96700 |
| Disinfect | Basekeeping | 5000 | 96700 |
| Repair | Basekeeping | 5000 | 96700 |
| RepairFetch | Basekeeping, Hauling | 5000 | 96700 |
| Deconstruct | Build | 5000 | 96700 |
| Demolish | Build | 5000 | 96700 |
| Research | Research | 5000 | 96700 |
| AnalyzeArtifact | Research, Art | 5000 | 96700 |
| AnalyzeSeed | Research, Farming | 5000 | 96700 |
| ExcavateFossil | Research, Art, Dig | 5000 | 96700 |
| ResearchFetch | Research, Hauling | 5000 | 96700 |
| GeneratePower | MachineOperating | 5000 | 96700 |
| CropTend | Farming | 5000 | 96700 |
| PowerTinker | MachineOperating | 5000 | 96700 |
| RemoteOperate | MachineOperating | 5000 | 96700 |
| MachineTinker | MachineOperating | 5000 | 96700 |
| MachineFetch | MachineOperating, Hauling | 5000 | 96700 |
| Harvest | Farming | 5000 | 96700 |
| FarmFetch | Farming, Hauling | 5000 | 96700 |
| Uproot | Farming | 5000 | 96700 |
| CleanToilet | Basekeeping | 5000 | 96700 |
| EmptyDesalinator | Basekeeping | 5000 | 96700 |
| LiquidCooledFan | MachineOperating | 5000 | 96700 |
| IceCooledFan | MachineOperating | 5000 | 96700 |
| Train | MachineOperating | 5000 | 96700 |
| ProcessCritter | Ranching | 5000 | 96700 |
| Cook | Cook | 5000 | 96700 |
| CookFetch | Cook, Hauling | 5000 | 96700 |
| DoctorFetch | MedicalAid, Hauling | 5000 | 96700 |
| Ranch | Ranching | 5000 | 96700 |
| PowerFetch | MachineOperating, Hauling | 5000 | 96700 |
| FlipCompost | Farming | 5000 | 96700 |
| Depressurize | MachineOperating | 5000 | 96700 |
| FarmingFabricate | Farming | 5000 | 96700 |
| PowerFabricate | MachineOperating | 5000 | 96700 |
| Compound | MedicalAid | 5000 | 96700 |
| Fabricate | MachineOperating | 5000 | 96700 |
| FabricateFetch | MachineOperating, Hauling | 5000 | 96700 |
| FoodFetch | Hauling | 5000 | 96700 |
| Transport | Hauling, Basekeeping | 5000 | 96700 |
| Build | Build | 5000 | 96700 |
| BuildDig | Build, Dig | 5000 | 96700 |
| BuildUproot | Build, Farming | 5000 | 96700 |
| BuildFetch | Build, Hauling | 5000 | 96700 |
| Dig | Dig | 5000 | 96700 |
| Fetch | Storage | 5000 | 96700 |
| StorageFetch | Storage | 5000 | 96700 |
| EquipmentFetch | Hauling | 5000 | 96700 |
| ArmTrap | Ranching | 3600 | 96700 |
| MoveToSafety | -- | 3550 | 96700 |

## Recovery / Transition

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| RecoverWarmth | -- | 8900 | 96600 |
| RecoverFromHeat | -- | 8850 | 96600 |
| EmoteIdle | -- | 8700 | 96500 |
| ReturnSuitIdle | -- | 3500 | 96500 |

## Idle

| ID | Chore Groups | Priority Value | Interrupt Priority |
|-|-|-|-|
| IdleChore | -- | 3450 | 96400 |

**Total: 142 chore types (3 unused fields omitted: Warmup, Cooldown, CompostWorkable)**
