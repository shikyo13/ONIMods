# Tech Tree - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

## Section Index
Use `Read` tool with `offset` and `limit` to load specific sections only.

| # | Topic | Lines |
|-|-|-|
| 1 | Tier Cost Summary | 26-41 |
| 2 | Tier 1 | 42-73 |
| 3 | Tier 2 | 74-99 |
| 4 | Tier 3 | 100-126 |
| 5 | Tier 4 | 127-151 |
| 6 | Tier 5 | 152-171 |
| 7 | Tier 6 | 172-181 |
| 8 | Tier 7 | 182-189 |
| 9 | Tier 8 | 190-198 |
| 10 | Tier 9 (Base Game Only) | 199-205 |
| 11 | Research Type Reference | 206-215 |
| 12 | Source Types | 216-226 |

Research types: **basic** (Novice, Research Station), **advanced** (Advanced, Super Computer), **space** (Interstellar, Virtual Planetarium - base game), **orbital** (Data Analysis, Orbital/DLC1 Cosmic Research - SO/FP), **nuclear** (Applied Sciences, Nuclear Research Center - SO/FP)

Note: Tier costs differ between base game and Spaced Out. Techs marked [SO] are Spaced Out/DLC1 only; [BG] are base game only; [DLC3] require The Bionic Booster DLC. Unlocks list uses building/item config IDs.

## Tier Cost Summary

| Tier | Base Game | Spaced Out |
|-|-|-|
| 1 | basic:15 | basic:15 |
| 2 | basic:20 | basic:20 |
| 3 | basic:30, advanced:20 | basic:30, advanced:20 |
| 4 | basic:35, advanced:30 | basic:35, advanced:30 |
| 5 | basic:40, advanced:50 | basic:40, advanced:50, nuclear:20 |
| 6 | basic:50, advanced:70 | basic:50, advanced:70, orbital:30, nuclear:40 |
| 7 | basic:70, advanced:100 | basic:70, advanced:100, orbital:250, nuclear:370 |
| 8 | basic:70, advanced:100, space:200 | basic:100, advanced:130, orbital:400, nuclear:435 |
| 9 | basic:70, advanced:100, space:400 | basic:100, advanced:130, orbital:600 |
| 10 | basic:70, advanced:100, space:800 | basic:100, advanced:130, orbital:800 |
| 11 | basic:70, advanced:100, space:1600 | basic:100, advanced:130, orbital:1600 |

## Tier 1

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| FarmingTech | Basic Farming | - | AlgaeHabitat, PlanterBox, RationBox, Compost |
| FineDining | Meal Preparation | - | CookingStation, EggCracker, DiningTable, FarmTile |
| GasPiping | Ventilation | - | GasConduit, GasConduitBridge, GasPump, GasVent |
| LiquidPiping | Plumbing | - | LiquidConduit, LiquidConduitBridge, LiquidPump, LiquidVent |
| MedicineI | Pharmacology | - | Apothecary, LubricationStick |
| Jobs | Employment | - | WaterCooler, CraftingTable, DisposableElectrobank_RawMetal, Campfire |
| PowerRegulation | Power Regulation | - | BatteryMedium, Switch, WireBridge, SmallElectrobankDischarger |
| InteriorDecor | Interior Decor | - | FlowerVase, FloorLamp, CeilingLight |
| LogicControl | Smart Home | - | AutomationOverlay, LogicSwitch, LogicWire, LogicWireBridge, LogicDuplicantSensor |
| PressureManagement | Pressure Management | - | LiquidValve, GasValve, GasPermeableMembrane, ManualPressureDoor |
| Combustion | Internal Combustion | - | Generator, WoodGasGenerator, PeatGenerator |
| ImprovedCombustion | Fossil Fuels | - | MethaneGenerator, OilRefinery, PetroleumGenerator |
| AdvancedPowerRegulation | Advanced Power Regulation | - | HighWattageWire, WireBridgeHighWattage, HydrogenGenerator, LogicPowerRelay, PowerTransformerSmall, LogicWattageSensor |
| Artistry | Artistic Expression | - | WoodenDoor, FlowerVaseWall, FlowerVaseHanging, CornerMoulding, CrownMoulding, ItemPedestal, SmallSculpture, IceSculpture |
| Acoustics | Sound Amplifiers | - | BatterySmart, Phonobox, PowerControlStation, ElectrobankCharger, Electrobank |
| PortableGasses | Portable Gases | - | GasBottler, BottleEmptierGas, OxygenMask, OxygenMaskLocker, OxygenMaskMarker, Oxysconce |
| ImprovedLiquidPiping | Improved Plumbing | - | InsulatedLiquidConduit, LogicPressureSensorLiquid, LiquidLogicValve, LiquidConduitPreferentialFlow, LiquidConduitOverflow, LiquidReservoir |
| Suits | Hazard Protection | - | SuitsOverlay, AtmoSuit, SuitFabricator, SuitMarker, SuitLocker |
| TravelTubes | Transit Tubes | - | TravelTubeEntrance, TravelTube, TravelTubeWallBridge, VerticalWindTunnel |
| AdvancedFiltration | Filtration | - | GasFilter, LiquidFilter, SludgePress, OilChanger |
| Distillation | Distillation | - | AlgaeDistillery, EthanolDistillery, WaterPurifier |
| LiquidFiltering | Liquid-Based Refinement | - | OreScrubber, Desalinator |
| MedicineIV | Micro-Targeted Medicine | - | AdvancedDoctorStation, AdvancedApothecary, HotTub, LogicRadiationSensor |
| SpaceCombustion | Advanced Combustion [SO] | - | SugarEngine, SmallOxidizerTank |
| Smelting | Smelting [SO tier 1, BG tier 4] | - [SO]; RefinedObjects [BG] | MetalRefinery, MetalTile |
| HighTempForging | Superheated Forging [SO tier 1, BG tier 5] | - [SO]; Smelting [BG] | GlassForge, BunkerTile, BunkerDoor, GeoTuner (+Gantry in SO) |
| SmartStorage | Smart Storage [BG tier 1, SO tier 3] | - [BG]; BasicRefinement [SO] | ConveyorOverlay, SolidTransferArm, StorageLockerSmart, ObjectDispenser |

## Tier 2

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| Agriculture | Agriculture | FineDining | FarmStation, FertilizerMaker, Refrigerator, HydroponicFarm, ParkSign, RadiationLight |
| Ranching | Ranching | FineDining | RanchStation, CreatureDeliveryPoint, ShearingStation, CreatureFeeder, FishDeliveryPoint, FishFeeder, CritterPickUp, CritterDropOff |
| ImprovedOxygen | Air Systems | LiquidPiping | Electrolyzer, RustDeoxidizer |
| SanitationSciences | Sanitation | LiquidPiping | FlushToilet, WashSink, Shower, MeshTile, GunkEmptier |
| MedicineII | Medical Equipment | MedicineI | DoctorStation, HandSanitizer |
| AdvancedResearch | Advanced Research | Jobs | BetaResearchPoint, AdvancedResearchCenter, ResetSkillsStation, ClusterTelescope, ExobaseHeadquarters, AdvancedCraftingTable |
| BasicRefinement | Brute-Force Refinement | Jobs | RockCrusher, Kiln |
| GenericSensors | Generic Sensors | LogicControl | FloorSwitch, LogicElementSensorGas, LogicElementSensorLiquid, LogicGateNOT, LogicTimeOfDaySensor, LogicTimerSensor, LogicLightSensor, LogicClusterLocationSensor |
| ImprovedGasPiping | Improved Ventilation | PressureManagement | InsulatedGasConduit, LogicPressureSensorGas, GasLogicValve, GasVentHighPressure |
| TemperatureModulation | Temperature Modulation | PressureManagement | InsulatedDoor, LiquidCooledFan, IceCooledFan, IceMachine, IceKettle, InsulationTile, SpaceHeater |
| DirectedAirStreams | Decontamination | PressureManagement | AirFilter, CO2Scrubber, PressureDoor |
| SpaceGas | Advanced Gas Flow | PressureManagement | CO2Engine, ModularLaunchpadPortGas, ModularLaunchpadPortGasUnloader, GasCargoBaySmall |
| PrettyGoodConductors | Low-Resistance Conductors | AdvancedPowerRegulation | WireRefined, WireRefinedBridge, WireRefinedHighWattage, WireRefinedBridgeHighWattage, PowerTransformer, LargeElectrobankDischarger |
| Clothing | Textile Production | Artistry | WoodenDoor, ClothingFabricator, CarpetTile, ExteriorWall |
| FineArt | Fine Art | Artistry | Canvas, Sculpture, Shelf |
| LiquidTemperature | Liquid Tuning | ImprovedLiquidPiping | LiquidConduitRadiant, LiquidConditioner, LiquidConduitTemperatureSensor, LiquidConduitElementSensor, LiquidHeater, LiquidLimitValve, ContactConductivePipeBridge |
| Plastics | Plastic Manufacturing | ImprovedCombustion [+SpaceCombustion in SO] | Polymerizer, OilWellCap |
| AdvancedDistillation | Emulsification | Distillation | ChemicalRefinery |
| SolidTransport | Solid Transport [BG] | AdvancedPowerRegulation | SolidConduitInbox, SolidConduit, SolidConduitBridge, SolidVent, SolidCargoBaySmall, ModularLaunchpadPortSolid, ModularLaunchpadPortSolidUnloader, ModularLaunchpadPortBridge |
| RefinedObjects | Refined Renovations [SO] | BasicRefinement | FabricatedWoodMaker, FirePole, ThermalBlock, LadderBed |
| HighPressureForging | Pressurized Forging [SO] | HighTempForging | DiamondPress |

## Tier 3

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| AnimalControl | Animal Control | Agriculture, Ranching | CreatureAirTrap, CreatureGroundTrap, WaterTrap, EggIncubator, LogicCritterCountSensor |
| FoodRepurposing | Food Repurposing | Agriculture | Juicer, SpiceGrinder, MilkPress, Smoker |
| MedicineIII | Pathogen Diagnostics | AdvancedResearch, MedicineII | GasConduitDiseaseSensor, LiquidConduitDiseaseSensor, LogicDiseaseSensor |
| ArtificialFriends | Artificial Friends | AdvancedResearch, BasicRefinement | SweepBotStation, ScoutModule, FetchDrone |
| NotificationSystems | Notification Systems | AdvancedResearch | LogicHammer, LogicAlarm, Telephone |
| SpaceProgram | Space Program [SO] | AdvancedResearch | LaunchPad, HabitatModuleSmall, OrbitalCargoModule, RocketControlStation |
| NuclearResearch | Materials Science Research [SO] | AdvancedResearch | DeltaResearchPoint, NuclearResearchCenter, ManualHighEnergyParticleSpawner, DisposableElectrobank_UraniumOre |
| FlowRedirection | Flow Redirection | SanitationSciences | MechanicalSurfboard, LiquidBottler, ModularLaunchpadPortLiquid, ModularLaunchpadPortLiquidUnloader, LiquidCargoBaySmall |
| HVAC | HVAC | TemperatureModulation | AirConditioner, LogicTemperatureSensor, GasConduitTemperatureSensor, GasConduitElementSensor, GasConduitRadiant, GasReservoir, GasLimitValve |
| GasDistribution | Gas Distribution | ImprovedGasPiping [+SpaceGas in SO] | BottleEmptierConduitGas, RocketInteriorGasInput, RocketInteriorGasOutput, OxidizerTankCluster |
| PrecisionPlumbing | Advanced Caffeination | LiquidTemperature | EspressoMachine, LiquidFuelTankCluster, MercuryCeilingLight |
| LogicCircuits | Advanced Automation | GenericSensors | LogicGateAND, LogicGateOR, LogicGateBUFFER, LogicGateFILTER |
| ParallelAutomation | Parallel Automation | GenericSensors | LogicRibbon, LogicRibbonBridge, LogicRibbonWriter, LogicRibbonReader |
| RenewableEnergy | Renewable Energy | PrettyGoodConductors | SteamTurbine2, SolarPanel, Sauna, SteamEngineCluster |
| SpacePower | Space Power | Acoustics, PrettyGoodConductors | BatteryModule, SolarPanelModule, RocketInteriorPowerPlug |
| Luxury | Home Luxuries | Clothing | LuxuryBed, LadderFast, PlasticTile, ClothingAlterationStation, WoodTile, MultiMinionDiningTable |
| RefractiveDecor | High Culture | FineArt | CanvasWide, MetalSculpture, WoodSculpture |
| ValveMiniaturization | Valve Miniaturization | Plastics | LiquidMiniPump, GasMiniPump |
| SmartStorage | Smart Storage [SO] | BasicRefinement | ConveyorOverlay, SolidTransferArm, StorageLockerSmart, ObjectDispenser |
| RefinedObjects | Refined Renovations [BG] | BasicRefinement | FabricatedWoodMaker, FirePole, ThermalBlock, LadderBed |
| SolidTransport | Solid Transport [SO] | AdvancedPowerRegulation, SmartStorage | SolidConduitInbox, SolidConduit, SolidConduitBridge, SolidVent, SolidCargoBaySmall, ModularLaunchpadPortSolid, ModularLaunchpadPortSolidUnloader, ModularLaunchpadPortBridge |
| SolidSpace | Solid Control [BG] | SolidTransport | SolidLogicValve, SolidConduitOutbox, SolidLimitValve, RocketInteriorSolidInput, RocketInteriorSolidOutput |

## Tier 4

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| AnimalComfort | Creature Comforts | AnimalControl | CritterCondo, UnderwaterCritterCondo, AirBorneCritterCondo |
| FinerDining | Gourmet Meal Preparation | AnimalControl | GourmetCookingStation, FoodDehydrator, FoodRehydrator, Deepfryer |
| Bioengineering | Bioengineering [SO] | FoodRepurposing | GeneticAnalysisStation |
| AdvancedNuclearResearch | More Materials Science Research [SO] | NuclearResearch | HighEnergyParticleSpawner, HighEnergyParticleRedirector, HEPBridgeTile |
| CrashPlan | Crash Plan [SO] | SpaceProgram | OrbitalResearchPoint, PioneerModule, OrbitalResearchCenter, DLC1CosmicResearchCenter |
| SkyDetectors | Celestial Detection | NotificationSystems, SpaceProgram [SO]; DupeTrafficControl [BG] | CometDetector, Telescope, ResearchClusterModule, ClusterTelescopeEnclosed, AstronautTrainingCenter |
| RoboticTools | Robotic Tools | ArtificialFriends | AutoMiner, RailGunPayloadOpener, RoboPilotModule |
| LiquidDistribution | Liquid Distribution | FlowRedirection | BottleEmptierConduitLiquid, RocketInteriorLiquidInput, RocketInteriorLiquidOutput, WallToilet |
| Catalytics | Catalytics | HVAC | OxyliteRefinery, Chlorinator, SupermaterialRefinery, SUPER_LIQUIDS, SodaFountain, GasCargoBayCluster |
| DupeTrafficControl | Computing | LogicCircuits | LogicCounter, LogicMemory, LogicGateXOR, ArcadeMachine, Checkpoint, CosmicResearchCenter |
| Jetpacks | Personal Flight | PrecisionPlumbing, TravelTubes | JetSuit, JetSuitMarker, JetSuitLocker, LiquidCargoBayCluster |
| RenaissanceArt | Renaissance Art | RefractiveDecor | CanvasTall, MarbleSculpture, FossilSculpture, CeilingFossilSculpture |
| GlassFurnishings | Glass Blowing | Luxury | GlassTile, FlowerVaseHangingFancy, SunLamp |
| HydrocarbonPropulsion | Hydrocarbon Propulsion [SO] | ValveMiniaturization | KeroseneEngineClusterSmall, MissionControlCluster |
| DairyOperation | Brackene Flow [BG] | ValveMiniaturization | MilkFeeder, MilkFatSeparator, MilkingStation |
| SolidSpace | Solid Control [SO] | SolidTransport | SolidLogicValve, SolidConduitOutbox, SolidLimitValve, RocketInteriorSolidInput, RocketInteriorSolidOutput |
| HighTempForging | Superheated Forging [BG] | Smelting | GlassForge, BunkerTile, BunkerDoor, GeoTuner |
| Smelting | Smelting [BG] | RefinedObjects | MetalRefinery, MetalTile |
| Missiles | Missiles [BG] | PrecisionPlumbing | MissileFabricator, MissileLauncher |
| SolidManagement | Solid Management [BG] | SolidSpace | SolidFilter, SolidConduitTemperatureSensor, SolidConduitElementSensor, SolidConduitDiseaseSensor, StorageTile, CargoBayCluster |

## Tier 5

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| RadiationProtection | Radiation Protection [SO] | AdvancedNuclearResearch | LeadSuit, LeadSuitMarker, LeadSuitLocker, LogicHEPSensor |
| NuclearStorage | Radbolt Containment [SO] | AdvancedNuclearResearch | HEPBattery |
| DurableLifeSupport | Durable Life Support [SO] | CrashPlan | NoseconeBasic, HabitatModuleMedium, ArtifactAnalysisStation, ArtifactCargoBay, SpecialCargoBayCluster |
| AdvancedScanners | Sensitive Microimaging [SO] | DupeTrafficControl | ScannerModule, LogicInterasteroidSender, LogicInterasteroidReceiver |
| Missiles | Missiles [SO] | SkyDetectors | MissileFabricator, MissileLauncher |
| BetterHydroCarbonPropulsion | Improved Hydrocarbon Propulsion [SO] | HydrocarbonPropulsion, RenewableEnergy | KeroseneEngineCluster, BiodieselEngineCluster |
| AdvancedSanitation | Advanced Sanitation [SO] | LiquidDistribution | DecontaminationShower |
| Multiplexing | Multiplexing | DupeTrafficControl | LogicGateMultiplexer, LogicGateDemultiplexer |
| Screens | New Media | GlassFurnishings, RenaissanceArt | PixelPack |
| EnvironmentalAppreciation | Environmental Appreciation | GlassFurnishings | BeachChair |
| SolidManagement | Solid Management [SO] | SolidSpace | SolidFilter, SolidConduitTemperatureSensor, SolidConduitElementSensor, SolidConduitDiseaseSensor, StorageTile, CargoBayCluster |
| HighVelocityTransport | High Velocity Transport [SO] | SolidSpace | RailGun, LandingBeacon |
| SkyDetectors | Celestial Detection [BG] | DupeTrafficControl | CometDetector, Telescope, ResearchClusterModule, ClusterTelescopeEnclosed, AstronautTrainingCenter |
| HighTempForging | Superheated Forging [BG] | Smelting | GlassForge, BunkerTile, BunkerDoor, GeoTuner |
| DairyOperation | Brackene Flow [SO] | ValveMiniaturization | MilkFeeder, MilkFatSeparator, MilkingStation |

## Tier 6

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| NuclearRefinement | Radiation Refinement [SO] | HighTempForging, NuclearStorage, RadiationProtection | NuclearReactor, UraniumCentrifuge, SelfChargingElectrobank |
| DataScience | Data Science [SO, DLC3] | AdvancedScanners | DataMiner, RemoteWorkerDock, RemoteWorkTerminal |
| HighVelocityDestruction | High Velocity Destruction [SO] | HighVelocityTransport | NoseconeHarvest |
| Monuments | Monuments | EnvironmentalAppreciation, Screens | MonumentBottom, MonumentMiddle, MonumentTop |
| BasicRocketry | Introductory Rocketry [BG] | SkyDetectors | CommandModule, SteamEngine, ResearchModule, Gantry |

## Tier 7

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| NuclearPropulsion | Radbolt Propulsion [SO] | Jetpacks, NuclearRefinement | HEPEngine |
| CargoI | Solid Cargo [BG] | BasicRocketry | CargoBay |
| EnginesI | Solid Fuel Combustion [BG] | BasicRocketry, Jetpacks | SolidBooster, MissionControl |

## Tier 8

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| CryoFuelPropulsion | Cryofuel Propulsion [SO] | BetterHydroCarbonPropulsion, NuclearPropulsion | HydrogenEngineCluster, OxidizerTankLiquidCluster |
| CargoII | Liquid and Gas Cargo [BG] | CargoI | LiquidCargoBay, GasCargoBay |
| EnginesII | Hydrocarbon Combustion [BG] | EnginesI | KeroseneEngine, BiodieselEngine, LiquidFuelTank, OxidizerTank |
| DataScienceBaseGame | Data Science [BG, DLC3] | EnginesI | DataMiner, RemoteWorkerDock, RemoteWorkTerminal, RoboPilotCommandModule |

## Tier 9 (Base Game Only)

| Tech ID | Display Name | Prerequisites | Unlocks |
|-|-|-|-|
| CargoIII | Unique Cargo [BG] | CargoII | TouristModule, SpecialCargoBay |
| EnginesIII | Cryofuel Combustion [BG] | EnginesII | OxidizerTankLiquid, OxidizerTankCluster, HydrogenEngine |

## Research Type Reference

| ID | Display Name | Research Station | Base Game Tier Range |
|-|-|-|-|
| basic | Novice Research | ResearchCenter | 1-11 |
| advanced | Advanced Research | AdvancedResearchCenter | 3-11 |
| space | Interstellar Research | CosmicResearchCenter | 8-11 (base game only) |
| orbital | Data Analysis Research | OrbitalResearchCenter, DLC1CosmicResearchCenter | 5-11 (SO only) |
| nuclear | Applied Sciences Research | NuclearResearchCenter | 5-8 (SO only) |

## Source Types

- `Tech` class: `Tech` (global namespace)
- Tech registry: `Database.Techs`
- Tech items: `Database.TechItems`
- Research types: `ResearchTypes`
- Display names: `STRINGS.RESEARCH.TECHS.<UPPER_ID>.NAME`
- Tree data: GraphML embedded in `resources.assets` (researchTreeFileVanilla, researchTreeFileExpansion1)
- Tier calculation: `Techs.GetTier()` - recursive max prereq depth + 1
- Costs: `Techs.TECH_TIERS[tier]` - list of (researchTypeID, cost) tuples
