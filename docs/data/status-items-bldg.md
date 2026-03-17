# Building Status Items  - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

324 fields from `Database.BuildingStatusItems`. All are `StatusItem` unless noted.
String pattern: `STRINGS.BUILDING.STATUSITEMS.<UPPER_ID>.NAME` / `.TOOLTIP`

## Power (24)
NeedPower, NotEnoughPower, PowerLoopDetected, PowerButtonOff, NoWireConnected, WireConnected, WireNominal, WireDisconnected, Overloaded, NoPowerConsumers, StoredCharge, BatteryJoulesAvailable, ElectrobankJoulesAvailable, Wattage, Wattson, SolarPanelWattage, ModuleSolarPanelWattage, SteamTurbineWattage, ManualGeneratorChargingUp, ManualGeneratorReleasingEnergy, GeneratorOffline, PowerBankChargerInProgress, ModuleGeneratorPowered, ModuleGeneratorNotPowered

## Plumbing (26)
NeedLiquidIn, NeedLiquidOut, LiquidVentObstructed, LiquidVentOverPressure, LiquidPipeEmpty, LiquidPipeObstructed, NoLiquidElementToPump, PumpingLiquidOrGas, PumpingStation, EmptyPumpingStation, PipeFull, PipeMayMelt, Pipe, ConduitBlocked, ConduitBlockedMultiples, OutputPipeFull, OutputTileBlocked, FlushToilet, FlushToiletInUse, Toilet, ToiletNeedsEmptying, DesalinatorNeedsEmptying, MilkSeparatorNeedsEmptying, ValveRequest, LimitValveLimitReached, LimitValveLimitNotReached

## HVAC / Gas (16)
NeedGasIn, NeedGasOut, GasVentObstructed, GasVentOverPressure, GasPipeEmpty, GasPipeObstructed, NoGasElementToPump, PressureOk, UnderPressure, EmittingElement, EmittingOxygenAvg, EmittingGasAvg, EmittingBlockedHighPressure, EmittingBlockedLowTemperature, ElementConsumer, ElementEmitterOutput

## Solid Conveyor (6)
NeedSolidIn, NeedSolidOut, SolidPipeObstructed, SolidConduitBlockedMultiples, Conveyor, DirectionControl

## Temperature (15)
Overheated, TooCold, Cooling, CoolingStalledHotEnv, CoolingStalledColdGas, CoolingStalledHotLiquid, CoolingStalledColdLiquid, CannotCoolFurther, CoolingWater, NoCoolant, FridgeCooling, FridgeSteady, HotTubWaterTooCold, HotTubTooHot, HotTubFilling

## Automation / Logic (11)
NoLogicWireConnected, LogicOverloaded, SwitchStatusActive, SwitchStatusInactive, LogicSwitchStatusActive, LogicSwitchStatusInactive, LogicSensorStatusActive, LogicSensorStatusInactive, PendingSwitchToggle, ChangeDoorControlState, CurrentDoorControlState

## Construction / Repair (26)
UnderConstruction, UnderConstructionNoWorker, MissingRequirements, MaterialsUnavailable `[MaterialsStatusItem]`, MaterialsUnavailableForRefill `[MaterialsStatusItem]`, WaitingForMaterials `[MaterialsStatusItem]`, WaitingForRepairMaterials, WaitingForHighEnergyParticles, PartiallyDamaged, Broken, PendingRepair, PendingUpgrade, PendingDeconstruction, PendingDemolition, MissingFoundation, MissingFoundationBackwall, InvalidBuildingLocation, InvalidPortOverlap, ConstructableDigUnreachable, ConstructionUnreachable, PendingWork, RequiresSkillPerk, DigRequiresSkillPerk, ColonyLacksRequiredSkillPerk, ClusterColonyLacksRequiredSkillPerk, WorkRequiresMinion

## Production / Fabrication (28)
Working, GettingReady, FabricatorIdle, FabricatorEmpty, FabricatorLacksHEP, FabricatorAcceptsMutantSeeds, ComplexFabricatorCooking, ComplexFabricatorProducing, ComplexFabricatorTraining, ComplexFabricatorResearching, NoResearchSelected, NoApplicableResearchSelected, NoApplicableAnalysisSelected, NoResearchOrDestinationSelected, Researching, NeedResourceMass, NoFilterElementSelected, NoSpiceSelected, NoLureElementSelected, EmittingLight, NeedSeed, AwaitingSeedDelivery, NoAvailableSeed, NeedEgg, AwaitingEggDelivery, NoAvailableEgg, NeedPlant, AwaitingBaitDelivery

## Storage (7)
NoStorageFilterSet, RationBoxContents, ChangeStorageTileTarget, StorageUnreachable, AwaitingWaste, AwaitingCompostFlip, AwaitingEmptyBuilding

## Damage / Hazard (7)
AngerDamage, Flooded, NotSubmerged, Entombed, MeltingDown, Expired, NeutroniumUnminable

## Rooms / Assignment (8)
AssignedTo, Unassigned, AssignedPublic, AssignedToRoom, NotInAnyRoom, NotInRequiredRoom, NotInRecommendedRoom, NeedsValidRegion

## Nuclear / Radiation (8)
ReactorMeltdown, ReactorRefuelDisabled, DeadReactorCoolingOff, CollectingHEP, LosingRadbolts, WellPressurizing, WellOverpressure, ReleasingPressure

## Rocket / Space (39)
InOrbit, InFlight, WaitingToLand, DestinationOutOfRange, RocketStranded, RocketName, RocketChecklistIncomplete, RocketCargoEmptying, RocketCargoFilling, RocketCargoFull, FlightAllCargoFull, FlightCargoRemaining, LandedRocketLacksPassengerModule, PilotNeeded, AutoPilotActive, InFlightPiloted, InFlightUnpiloted, InFlightAutoPiloted, InFlightSuperPilot, RocketPlatformCloseToCeiling, InOrbitRequired, PathNotClear, HasGantry, MissingGantry, DisembarkingDuplicant, PassengerModuleUnreachable, RocketRestrictionActive, RocketRestrictionInactive, NoRocketRestriction, SpacePOIHarvesting, CollectingHexCellInventoryItems, RailgunpayloadNeedsEmptying, RailGunCooldown, SpecialCargoBayClusterCritterStored, MissionControlAssistingRocket, NoRocketsToMissionControlBoost, NoRocketsToMissionControlClusterBoost, MissionControlBoosted, NoSurfaceSight

## Suits / Transit (9)
NoSuitMarker, SuitMarkerWrongSide, SuitMarkerTraversalAnytime, SuitMarkerTraversalOnlyWhenRoomAvailable, InvalidMaskStationConsumptionState, NoTubeConnected, NoTubeExits, TransitTubeEntranceWaxReady, WindTunnelIntake

## Critter / Ranching (13)
PendingFish, NoFishableWaterBelow, Baited, TrapNeedsArming, TrapArmed, TrapHasCritter, IncubatorProgress, HabitatNeedsEmptying, CreatureManipulatorWaiting, CreatureManipulatorProgress, CreatureManipulatorMorphModeLocked, CreatureManipulatorMorphMode, CreatureManipulatorWorking

## Telescope / Detection (10)
DetectorScanning, IncomingMeteors, TelescopeWorking, ClusterTelescopeAllWorkComplete, ClusterTelescopeMeteorWorking, ArtifactAnalysisAnalyzing, SkyVisNone, SkyVisLimited, BroadcasterOutOfRange, DataMinerEfficiency

## Geotuner / Geysers (6)
GeoTunerNoGeyserSelected, GeoTunerResearchNeeded, GeoTunerResearchInProgress, GeoTunerBroadcasting, GeoTunerGeyserStatus, GeyserGeotuned

## Kettle / Smelting (4)
KettleInsuficientSolids, KettleInsuficientFuel, KettleInsuficientLiquidSpace, KettleMelting

## Geo Vents Quest (11)
GeoVentQuestBlockage, GeoVentsDisconnected, GeoVentsOverpressure, GeoControllerCantVent, GeoVentsReady, GeoVentsVenting, GeoQuestPendingReconnectPipes, GeoQuestPendingUncover, GeoControllerOffline, GeoControllerStorageStatus, GeoControllerTemperatureStatus

## MegaBrain Tank (5)
MegaBrainNotEnoughOxygen, MegaBrainTankActivationProgress, MegaBrainTankDreamAnalysis, MegaBrainTankAllDupesAreDead, MegaBrainTankComplete

## Fossil (6)
FossilMineIdle, FossilMineEmpty, FossilEntombed, FossilMinePendingWork, FossilHuntExcavationOrdered, FossilHuntExcavationInProgress

## MorbRover Maker (7)
MorbRoverMakerDusty, MorbRoverMakerBuildingRevealed, MorbRoverMakerGermCollectionProgress, MorbRoverMakerNoGermsConsumedAlert, MorbRoverMakerCraftingBody, MorbRoverMakerReadyForDoctor, MorbRoverMakerDoctorWorking

## Hijack Headquarters (3)
HijackHeadquartersIdle, HijackHeadquartersReadyToPrint, HijackHeadquartersPrinting

## Remote Work (2)
RemoteWorkDockMakingWorker, RemoteWorkTerminalNoDock

## Mercury Light (4)
MercuryLight_Charging, MercuryLight_Charged, MercuryLight_Depleating, MercuryLight_Depleated

## Misc (23)
Normal, BuildingDisabled, Unusable, UnusableGunked, DigUnreachable, MopUnreachable, DispenseRequested, NewDuplicantsAvailable, ClinicOutsideHospital, EmergencyPriority, SkillPointsAvailable, GeneShuffleCompleted, GeneticAnalysisCompleted, DuplicantActivationRequired, Grave, GraveEmpty, PedestalNoItemDisplayed, OrnamentDisabled, TanningLightSufficient, TanningLightInsufficient, WarpPortalCharging, WarpConduitPartnerDisabled, GunkEmptierFull

---
*ID = field name on `Database.BuildingStatusItems`. To resolve display string at runtime: `Strings.Get("STRINGS.BUILDING.STATUSITEMS." + id.ToUpperInvariant() + ".NAME")`*
