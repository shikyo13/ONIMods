# Components — ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

## Duplicant — Identity & Stats

| Component | Purpose | Key Methods | Common Access Pattern |
|-|-|-|-|
| `MinionIdentity` | Core dupe identity and name | `GetProperName()`, `SetName()`, `GetEquipment()`, `SetGender()` | `go.GetComponent<MinionIdentity>()` |
| `Health` | Hit points and damage tracking | `Damage()`, `IsIncapacitated()`, `IsDefeated()`, `percent()`, `hitPoints` | `go.GetComponent<Health>()` |
| `MinionResume` | Skills, XP, and mastery | `HasMasteredSkill()`, `MasterSkill()`, `HasPerk()`, `AddExperience()`, `AvailableSkillpoints` | `go.GetComponent<MinionResume>()` |
| `Klei.AI.Traits` | Trait collection on a dupe | `HasTrait()`, `Add()`, `Remove()`, `GetTraitIds()`, `IsChoreGroupDisabled()` | `go.GetComponent<Traits>()` |
| `Klei.AI.Effects` | Timed status effects | `Add()`, `Remove()`, `HasEffect()`, `Get()`, `AddImmunity()` | `go.GetComponent<Effects>()` |
| `Klei.AI.Modifiers` | Attribute modifier aggregator | `GetPreModifiedAttributeValue()`, `GetPreModifiers()` | `go.GetComponent<Modifiers>()` |
| `Klei.AI.AttributeLevels` | Attribute XP and leveling | `GetLevel()`, `GetAttributeLevel()`, `AddExperience()`, `SetLevel()`, `GetPercentComplete()` | `go.GetComponent<AttributeLevels>()` |
| `ChoreConsumer` | Chore selection and priority | `FindNextChore()`, `SetPermittedByUser()`, `GetPersonalPriority()`, `SetPersonalPriority()`, `CanReach()` | `go.GetComponent<ChoreConsumer>()` |
| `ChoreDriver` | Active chore execution | (StateMachine — use `.sm` fields) | `go.GetComponent<ChoreDriver>()` |
| `Schedulable` | Schedule block assignment | `GetSchedule()`, `IsAllowed()`, `OnScheduleChanged()` | `go.GetComponent<Schedulable>()` |
| `Navigator` | Pathfinding and movement | `GoTo()`, `CanReach()`, `GetNavigationCost()`, `IsMoving()`, `Stop()` | `go.GetComponent<Navigator>()` |

## Duplicant — Monitors (StateMachine instances via `.smi`)

| Component | Purpose | Key Access Pattern |
|-|-|-|
| `StaminaMonitor` | Sleep need tracking | `go.GetSMI<StaminaMonitor.Instance>()` |
| `StressMonitor` | Stress level tracking | `go.GetSMI<StressMonitor.Instance>()` |
| `CalorieMonitor` | Hunger/calorie tracking | `go.GetSMI<CalorieMonitor.Instance>()` |
| `BreathMonitor` | Oxygen breath tracking | `go.GetSMI<BreathMonitor.Instance>()` |
| `BladderMonitor` | Bathroom need tracking | `go.GetSMI<BladderMonitor.Instance>()` |
| `TemperatureMonitor` | Body temperature tracking | `go.GetSMI<TemperatureMonitor.Instance>()` |
| `RadiationMonitor` | Radiation exposure tracking | `go.GetSMI<RadiationMonitor.Instance>()` |
| `DecorMonitor` | Decor perception tracking | `go.GetSMI<DecorMonitor.Instance>()` |
| `IdleMonitor` | Idle state detection | `go.GetSMI<IdleMonitor.Instance>()` |
| `SicknessMonitor` | Disease state tracking | `go.GetSMI<SicknessMonitor.Instance>()` |
| `DrowningMonitor` | Drowning detection | `go.GetSMI<DrowningMonitor.Instance>()` |
| `ScaldingMonitor` | Scalding detection | `go.GetSMI<ScaldingMonitor.Instance>()` |
| `SuffocationMonitor` | Suffocation detection | `go.GetSMI<SuffocationMonitor.Instance>()` |
| `IncapacitationMonitor` | Incapacitated state tracking | `go.GetSMI<IncapacitationMonitor.Instance>()` |
| `DeathMonitor` | Death state tracking | `go.GetSMI<DeathMonitor.Instance>()` |
| `SleepChoreMonitor` | Sleep chore management | `go.GetSMI<SleepChoreMonitor.Instance>()` |
| `ToiletMonitor` | Toilet usage tracking | `go.GetSMI<ToiletMonitor.Instance>()` |
| `RationMonitor` | Food consumption tracking | `go.GetSMI<RationMonitor.Instance>()` |
| `ThreatMonitor` | Combat threat detection | `go.GetSMI<ThreatMonitor.Instance>()` |
| `StressBehaviourMonitor` | Stress reaction triggers | `go.GetSMI<StressBehaviourMonitor.Instance>()` |
| `GermExposureMonitor` | Germ exposure tracking | `go.GetSMI<GermExposureMonitor.Instance>()` |
| `HygieneMonitor` | Hygiene need tracking | `go.GetSMI<HygieneMonitor.Instance>()` |
| `GunkMonitor` | Bionic gunk accumulation | `go.GetSMI<GunkMonitor.Instance>()` |

## Building

| Component | Purpose | Key Methods | Common Access Pattern |
|-|-|-|-|
| `BuildingComplete` | Completed building marker | `SetCreationTime()`, `UpdatePosition()` | `go.GetComponent<BuildingComplete>()` |
| `Constructable` | Under-construction building | `SelectedElementsTags`, `Recipe` | `go.GetComponent<Constructable>()` |
| `Deconstructable` | Deconstruction control | `QueueDeconstruction()`, `CancelDeconstruction()`, `IsMarkedForDeconstruction()`, `SetAllowDeconstruction()` | `go.GetComponent<Deconstructable>()` |
| `Operational` | Building operational state | `IsOperational`, `IsFunctional`, `IsActive`, `SetFlag()`, `GetFlag()`, `SetActive()` | `go.GetComponent<Operational>()` |
| `KSelectable` | Selection and status items | `AddStatusItem()`, `RemoveStatusItem()`, `SetStatusItem()`, `HasStatusItem()`, `GetName()` | `go.GetComponent<KSelectable>()` |
| `Building` | Building def and cell data | `Def`, `GetCell()`, `Orientation` | `go.GetComponent<Building>()` |
| `BuildingDef` | Static building definition | `Build()`, `TryPlace()`, `PrefabID` | accessed via `Building.Def` |

## Resource & Element

| Component | Purpose | Key Methods | Common Access Pattern |
|-|-|-|-|
| `PrimaryElement` | Element, mass, temperature | `Element`, `Mass`, `Temperature`, `SetElement()`, `SetMassTemperature()`, `DiseaseIdx` | `go.GetComponent<PrimaryElement>()` |
| `Storage` | Item container | `Store()`, `Drop()`, `DropAll()`, `Find()`, `FindFirst()`, `Has()`, `MassStored()`, `IsFull()`, `GetMassAvailable()`, `GetItems()` | `go.GetComponent<Storage>()` |
| `FilteredStorage` | Storage with tag filtering | `FilterChanged()`, `SetHasMeter()` | accessed via `StorageLocker` |

## AI & Scheduling

| Component | Purpose | Key Methods | Common Access Pattern |
|-|-|-|-|
| `GameplayEventMonitor` | Colony event tracking | `go.GetSMI<GameplayEventMonitor.Instance>()` | StateMachine instance |
| `RoomMonitor` | Room assignment tracking | `go.GetSMI<RoomMonitor.Instance>()` | StateMachine instance |
| `SafeCellMonitor` | Safe cell finding | `go.GetSMI<SafeCellMonitor.Instance>()` | StateMachine instance |
| `FallMonitor` | Falling state detection | `go.GetSMI<FallMonitor.Instance>()` | StateMachine instance |
| `ReactionMonitor` | Emote/reaction triggers | `go.GetSMI<ReactionMonitor.Instance>()` | StateMachine instance |
| `MingleMonitor` | Social mingling triggers | `go.GetSMI<MingleMonitor.Instance>()` | StateMachine instance |

## Critter — Monitors

| Component | Purpose | Key Access Pattern |
|-|-|-|
| `CreatureCalorieMonitor` | Critter hunger tracking | `go.GetSMI<CreatureCalorieMonitor.Instance>()` |
| `CreatureSleepMonitor` | Critter sleep cycles | `go.GetSMI<CreatureSleepMonitor.Instance>()` |
| `FertilityMonitor` | Egg laying tracking | `go.GetSMI<FertilityMonitor.Instance>()` |
| `HappinessMonitor` | Critter happiness/taming | `go.GetSMI<HappinessMonitor.Instance>()` |
| `WildnessMonitor` | Wild/tame state tracking | `go.GetSMI<WildnessMonitor.Instance>()` |
| `OvercrowdingMonitor` | Room overcrowding detection | `go.GetSMI<OvercrowdingMonitor.Instance>()` |
| `AgeMonitor` | Critter aging/lifespan | `go.GetSMI<AgeMonitor.Instance>()` |
| `ScaleGrowthMonitor` | Shearing resource growth | `go.GetSMI<ScaleGrowthMonitor.Instance>()` |
| `IncubationMonitor` | Egg incubation progress | `go.GetSMI<IncubationMonitor.Instance>()` |
