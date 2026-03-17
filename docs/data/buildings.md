# Buildings  - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

## Section Index
Use `Read` tool with `offset` and `limit` to load specific sections only.

| # | Topic | Lines |
|-|-|-|
| 1 | Power - Generators | 31-45 |
| 2 | Power - Infrastructure | 46-59 |
| 3 | Oxygen | 60-70 |
| 4 | Plumbing | 71-87 |
| 5 | HVAC (Gas) | 88-100 |
| 6 | Conveyor (Solid) | 101-109 |
| 7 | Storage | 110-118 |
| 8 | Food & Farming | 119-131 |
| 9 | Research | 132-146 |
| 10 | Automation | 147-159 |
| 11 | Medical | 160-171 |
| 12 | Rocketry - Engines | 172-183 |
| 13 | Rocketry - Modules | 184-200 |
| 14 | Refinement & Industry | 201-213 |
| 15 | Radiation / HEP | 214-222 |
| 16 | Stations & Furniture | 223-235 |
| 17 | Decor | 236-253 |
| 18 | Tiles & Structure | 254-264 |
| 19 | Critter Management | 265-276 |

Source: `Assembly-CSharp.dll` IBuildingConfig implementors. Building ID = config class name minus "Config" suffix.

## Power  - Generators

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| ManualGeneratorConfig | ManualGenerator | Manual Generator | (starter) | Power |
| GeneratorConfig | Generator | Coal Generator | PowerGeneration | Power |
| WoodGasGeneratorConfig | WoodGasGenerator | Wood Burner | PowerGeneration | Power |
| HydrogenGeneratorConfig | HydrogenGenerator | Hydrogen Generator | RenewableEnergy | Power |
| MethaneGeneratorConfig | MethaneGenerator | Natural Gas Generator | FossilFuels | Power |
| PetroleumGeneratorConfig | PetroleumGenerator | Petroleum Generator | ImprovedCombustion | Power |
| PeatGeneratorConfig | PeatGenerator | Peat Generator | Agriculture | Power |
| SteamTurbineConfig2 | SteamTurbineConfig2 | Steam Turbine | RenewableEnergy | Power |
| SolarPanelConfig | SolarPanel | Solar Panel | RenewableEnergy | Power |
| NuclearReactorConfig | NuclearReactor | Research Reactor | NuclearRefinement | Power |

## Power  - Infrastructure

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| BatteryConfig | Battery | Battery | PowerGeneration | Power |
| BatteryMediumConfig | BatteryMedium | Jumbo Battery | PowerRegulation | Power |
| BatterySmartConfig | BatterySmart | Smart Battery | GenericSensors | Power |
| WireConfig | Wire | Wire | (starter) | Power |
| WireRefinedConfig | WireRefined | Conductive Wire | ImprovedPowerRegulation | Power |
| WireBridgeConfig | WireBridge | Wire Bridge | (starter) | Power |
| WireRefinedBridgeConfig | WireRefinedBridge | Conductive Wire Bridge | ImprovedPowerRegulation | Power |
| PowerTransformerConfig | PowerTransformer | Power Transformer | PowerRegulation | Power |
| SwitchConfig | Switch | Power Shutoff | AdvancedPowerRegulation | Power |

## Oxygen

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| AlgaeHabitatConfig | AlgaeHabitat | Algae Terrarium | (starter) | Oxygen |
| ElectrolyzerConfig | Electrolyzer | Electrolyzer | ImprovedOxygen | Oxygen |
| AirFilterConfig | AirFilter | Deodorizer | (starter) | Oxygen |
| CO2ScrubberConfig | CO2Scrubber | Carbon Skimmer | ImprovedOxygen | Oxygen |
| AlgaeDistilleryConfig | AlgaeDistillery | Algae Distiller | Distillation | Oxygen |
| OxyliteRefineryConfig | OxyliteRefinery | Oxylite Refinery | OxyliteRefinement | Oxygen |

## Plumbing

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| LiquidConduitConfig | LiquidConduit | Liquid Pipe | ImprovedPlumbing | Plumbing |
| InsulatedLiquidConduitConfig | InsulatedLiquidConduit | Insulated Liquid Pipe | HVAC | Plumbing |
| LiquidConduitBridgeConfig | LiquidConduitBridge | Liquid Bridge | ImprovedPlumbing | Plumbing |
| LiquidPumpConfig | LiquidPump | Liquid Pump | ImprovedPlumbing | Plumbing |
| LiquidMiniPumpConfig | LiquidMiniPump | Mini Liquid Pump | ValveMiniaturization | Plumbing |
| LiquidVentConfig | LiquidVent | Liquid Vent | ImprovedPlumbing | Plumbing |
| WaterPurifierConfig | WaterPurifier | Water Sieve | Distillation | Plumbing |
| DesalinatorConfig | Desalinator | Desalinator | Distillation | Plumbing |
| OuthouseConfig | Outhouse | Outhouse | (starter) | Plumbing |
| FlushToiletConfig | FlushToilet | Lavatory | SanitationSciences | Plumbing |
| ShowerConfig | Shower | Shower | SanitationSciences | Plumbing |
| WashSinkConfig | WashSink | Wash Basin | (starter) | Plumbing |

## HVAC (Gas)

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| GasConduitConfig | GasConduit | Gas Pipe | Ventilation | HVAC |
| InsulatedGasConduitConfig | InsulatedGasConduit | Insulated Gas Pipe | HVAC | HVAC |
| GasConduitBridgeConfig | GasConduitBridge | Gas Bridge | Ventilation | HVAC |
| GasPumpConfig | GasPump | Gas Pump | Ventilation | HVAC |
| GasMiniPumpConfig | GasMiniPump | Mini Gas Pump | ValveMiniaturization | HVAC |
| GasVentConfig | GasVent | Gas Vent | Ventilation | HVAC |
| AirConditionerConfig | AirConditioner | Thermo Aquatuner | HVAC | HVAC |
| SpaceHeaterConfig | SpaceHeater | Space Heater | TemperatureModulation | HVAC |

## Conveyor (Solid)

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| SolidConduitConfig | SolidConduit | Conveyor Rail | SolidTransport | Conveyor |
| SolidConduitBridgeConfig | SolidConduitBridge | Conveyor Bridge | SolidTransport | Conveyor |
| SolidVentConfig | SolidVent | Conveyor Chute | SolidTransport | Conveyor |
| AutoMinerConfig | AutoMiner | Auto-Sweeper | SolidTransport | Conveyor |

## Storage

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| StorageLockerConfig | StorageLocker | Storage Bin | (starter) | Storage |
| StorageTileConfig | StorageTile | Storage Tile | SolidTransport | Storage |
| RationBoxConfig | RationBox | Ration Box | (starter) | Storage |
| RefrigeratorConfig | Refrigerator | Refrigerator | FoodRepurposing | Storage |

## Food & Farming

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| FarmTileConfig | FarmTile | Farm Tile | Agriculture | Food |
| AtmoicGardenConfig | AtmoicGarden | Atmospheric Garden | Agriculture | Food |
| FarmStationConfig | FarmStation | Farm Station | Agriculture | Food |
| CookingStationConfig | CookingStation | Electric Grill | FinerDining | Food |
| DeepfryerConfig | Deepfryer | Deep Fryer | FinerDining | Food |
| DiningTableConfig | DiningTable | Mess Table | (starter) | Food |
| EggCrackerConfig | EggCracker | Egg Cracker | AnimalHusbandry | Food |
| EggIncubatorConfig | EggIncubator | Incubator | AnimalHusbandry | Food |

## Research

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| ResearchCenterConfig | ResearchCenter | Research Station | (starter) | Research |
| AdvancedResearchCenterConfig | AdvancedResearchCenter | Super Computer | AdvancedResearch | Research |
| CosmicResearchCenterConfig | CosmicResearchCenter | Telescope | Astronomy | Research |
| DLC1CosmicResearchCenterConfig | DLC1CosmicResearchCenter | Virtual Planetarium | SpaceProgram | Research |
| NuclearResearchCenterConfig | NuclearResearchCenter | Nuclear Research Station | NuclearResearch | Research |
| OrbitalResearchCenterConfig | OrbitalResearchCenter | Orbital Data Collection Lab | SpaceProgram | Research |
| ClusterTelescopeConfig | ClusterTelescope | Telescope | Astronomy | Research |
| ClusterTelescopeEnclosedConfig | ClusterTelescopeEnclosed | Enclosed Telescope | SpaceProgram | Research |
| DataMinerConfig | DataMiner | Data Miner | DataScience | Research |
| ArtifactAnalysisStationConfig | ArtifactAnalysisStation | Artifact Analysis Station | SpaceProgram | Research |

## Automation

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| LogicWireConfig | LogicWire | Automation Wire | LogicCircuits | Automation |
| LogicRibbonConfig | LogicRibbon | Automation Ribbon | ParallelAutomation | Automation |
| LogicWireBridgeConfig | LogicWireBridge | Automation Wire Bridge | LogicCircuits | Automation |
| LogicRibbonBridgeConfig | LogicRibbonBridge | Automation Ribbon Bridge | ParallelAutomation | Automation |
| LogicSwitchConfig | LogicSwitch | Signal Switch | LogicCircuits | Automation |
| FloorSwitchConfig | FloorSwitch | Weight Plate | LogicCircuits | Automation |
| TemperatureControlledSwitchConfig | TemperatureControlledSwitch | Thermo Sensor | LogicCircuits | Automation |
| CometDetectorConfig | CometDetector | Meteor Scanner | SkyDetectors | Automation |

## Medical

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| MedicalCotConfig | MedicalCot | Triage Cot | MedicineI | Medical |
| DoctorStationConfig | DoctorStation | Sick Bay | MedicineII | Medical |
| AdvancedDoctorStationConfig | AdvancedDoctorStation | Disease Clinic | MedicineIII | Medical |
| ApothecaryConfig | Apothecary | Apothecary | MedicineII | Medical |
| AdvancedApothecaryConfig | AdvancedApothecary | Advanced Apothecary | MedicineIV | Medical |
| DecontaminationShowerConfig | DecontaminationShower | Decontamination Shower | MedicineIV | Medical |
| CheckpointConfig | Checkpoint | Duplicant Checkpoint | SanitationSciences | Medical |

## Rocketry  - Engines

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| CO2EngineConfig | CO2Engine | CO2 Engine | EnginesI | Rocketry |
| SteamEngineConfig | SteamEngine | Steam Engine | EnginesI | Rocketry |
| KeroseneEngineConfig | KeroseneEngine | Petroleum Engine | EnginesII | Rocketry |
| HydrogenEngineConfig | HydrogenEngine | Hydrogen Engine | EnginesIII | Rocketry |
| BiodieselEngineConfig | BiodieselEngine | Biodiesel Engine | EnginesII | Rocketry |
| SugarEngineConfig | SugarEngine | Sugar Engine | EnginesI | Rocketry |
| HEPEngineConfig | HEPEngine | Radbolt Engine | NuclearPropulsion | Rocketry |

## Rocketry  - Modules

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| CommandModuleConfig | CommandModule | Command Capsule | CommandModule | Rocketry |
| BatteryModuleConfig | BatteryModule | Battery Module | EnginesI | Rocketry |
| SolidCargoBayConfig | SolidCargoBay | Cargo Bay | EnginesI | Rocketry |
| LiquidCargoBayConfig | LiquidCargoBay | Liquid Cargo Tank | EnginesI | Rocketry |
| GasCargoBayConfig | GasCargoBay | Gas Cargo Canister | EnginesI | Rocketry |
| ArtifactCargoBayConfig | ArtifactCargoBay | Artifact Transport Module | SpaceProgram | Rocketry |
| ResearchModuleConfig | ResearchModule | Research Module | SpaceProgram | Rocketry |
| ScannerModuleConfig | ScannerModule | Cartographic Module | SpaceProgram | Rocketry |
| ScoutModuleConfig | ScoutModule | Drillcone Module | SpaceProgram | Rocketry |
| SolarPanelModuleConfig | SolarPanelModule | Solar Panel Module | RenewableEnergy | Rocketry |
| LaunchPadConfig | LaunchPad | Rocket Platform | SpaceProgram | Rocketry |
| TouristModuleConfig | TouristModule | Spacefarer Module | SpaceProgram | Rocketry |

## Refinement & Industry

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| MetalRefineryConfig | MetalRefinery | Metal Refinery | SmartStorage | Industry |
| OilRefineryConfig | OilRefinery | Oil Refinery | ImprovedCombustion | Industry |
| ChemicalRefineryConfig | ChemicalRefinery | Chemical Plant | Plastics | Industry |
| SupermaterialRefineryConfig | SupermaterialRefinery | Molecular Forge | Supermaterial | Industry |
| EthanolDistilleryConfig | EthanolDistillery | Ethanol Distillery | Distillation | Industry |
| DiamondPressConfig | DiamondPress | Diamond Press | PressurizedForging | Industry |
| OilWellConfig | OilWell | Oil Well | ImprovedCombustion | Industry |
| ChlorinatorConfig | Chlorinator | Chlorinator | Catalytics | Industry |

## Radiation / HEP

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| HighEnergyParticleSpawnerConfig | HighEnergyParticleSpawner | Radbolt Generator | NuclearRefinement | Radiation |
| ManualHighEnergyParticleSpawnerConfig | ManualHighEnergyParticleSpawner | Manual Radbolt Generator | NuclearResearch | Radiation |
| HighEnergyParticleRedirectorConfig | HighEnergyParticleRedirector | Radbolt Reflector | NuclearRefinement | Radiation |
| HEPBridgeTileConfig | HEPBridgeTile | Radbolt Joint Plate | NuclearRefinement | Radiation |

## Stations & Furniture

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| BedConfig | Bed | Cot | (starter) | Furniture |
| LuxuryBedConfig | LuxuryBed | Comfy Bed | InteriorDecor | Furniture |
| DoorConfig | Door | Pneumatic Door | (starter) | Furniture |
| BunkerDoorConfig | BunkerDoor | Bunker Door | Smelting | Furniture |
| LadderConfig | Ladder | Ladder | (starter) | Furniture |
| TravelTubeConfig | TravelTube | Transit Tube | TravelTubes | Furniture |
| AstronautTrainingCenterConfig | AstronautTrainingCenter | Astronaut Training Center | SpaceProgram | Stations |
| ExobaseHeadquartersConfig | ExobaseHeadquarters | Mini-Pod | SpaceProgram | Stations |

## Decor

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| FloorLampConfig | FloorLamp | Lamp | (starter) | Decor |
| CeilingLightConfig | CeilingLight | Ceiling Light | InteriorDecor | Decor |
| SculptureConfig | Sculpture | Sculpture Block | Artistry | Decor |
| SmallSculptureConfig | SmallSculpture | Small Sculpture | Artistry | Decor |
| MarbleSculptureConfig | MarbleSculpture | Marble Sculpture | FineArt | Decor |
| MetalSculptureConfig | MetalSculpture | Metal Sculpture | FineArt | Decor |
| CanvasConfig | Canvas | Painting Canvas | Artistry | Decor |
| CanvasTallConfig | CanvasTall | Portrait Canvas | FineArt | Decor |
| CanvasWideConfig | CanvasWide | Landscape Canvas | FineArt | Decor |
| ArcadeMachineConfig | ArcadeMachine | Arcade Cabinet | Recreation | Decor |
| EspressoMachineConfig | EspressoMachine | Espresso Machine | Recreation | Decor |
| BeachChairConfig | BeachChair | Beach Chair | Recreation | Decor |
| CarpetTileConfig | CarpetTile | Carpet Tile | InteriorDecor | Decor |

## Tiles & Structure

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| TileConfig | Tile | Tile | (starter) | Base |
| MeshTileConfig | MeshTile | Mesh Tile | (starter) | Base |
| GlassTileConfig | GlassTile | Window Tile | Luxury | Base |
| MetalTileConfig | MetalTile | Metal Tile | Smelting | Base |
| InsulationTileConfig | InsulationTile | Insulated Tile | TemperatureModulation | Base |
| BunkerTileConfig | BunkerTile | Bunker Tile | Smelting | Base |

## Critter Management

| Config Class | Building ID | Display Name | Tech | Category |
|-|-|-|-|-|
| CreatureDeliveryPointConfig | CreatureDeliveryPoint | Critter Drop-Off | AnimalHusbandry | Ranching |
| CreatureFeederConfig | CreatureFeeder | Critter Feeder | AnimalHusbandry | Ranching |
| CreatureTrapConfig | CreatureTrap | Critter Trap | AnimalControl | Ranching |
| CritterCondoConfig | CritterCondo | Critter Condo | AnimalComfort | Ranching |
| AirBorneCritterCondoConfig | AirBorneCritterCondo | Airborne Critter Condo | AnimalComfort | Ranching |

<!-- Conduit sensors: Gas/LiquidConduit{Temperature,Element,Disease}SensorConfig  - 6 types, all in HVAC/LiquidFiltering tech -->
