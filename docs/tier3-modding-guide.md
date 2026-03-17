# The complete guide to Oxygen Not Included modding

## Section Index
Use `Read` tool with `offset` and `limit` to load specific sections only.

| # | Topic | Lines |
|-|-|-|
| - | Overview & prerequisites | 23-26 |
| 1 | Development environment setup | 30-112 |
| 2 | Mod loading & folder structure | 113-283 |
| 3 | Harmony patching - core technique | 284-486 |
| 4 | Gameplay tweaks with examples | 487-616 |
| 5 | Creating new buildings from scratch | 617-809 |
| 6 | New items, elements, creatures + kanim pipeline | 810-995 |
| 7 | UI modding techniques | 996-1127 |
| 8 | Mod configuration & settings (PLib) | 1128-1228 |
| 9 | Debugging and testing | 1229-1303 |
| 10 | Publishing to Steam Workshop | 1304-1338 |
| 11 | Key API reference | 1339-1519 |
| 12 | Community resources and ecosystem | 1520-1571 |

---

**ONI mods are C# class libraries that use Harmony 2.0 to patch the game's methods at runtime.** There is no official modding API - you decompile `Assembly-CSharp.dll`, find methods to intercept, and write Harmony patches that alter game behavior. The game ships Harmony built-in and provides a mod loading pipeline that scans your mod folder, reads `mod_info.yaml`, loads your DLL, and applies patches. This guide covers everything from environment setup through publishing, with production-ready code templates throughout.

ONI targets **.NET Framework 4.7.1** and **Harmony 2.0** since the 2021 "Mergedown" update that unified the base game and Spaced Out DLC codebases. Any tutorial referencing .NET 3.5 or `using Harmony;` (v1 namespace) is outdated. Current mods must use `using HarmonyLib;` and set `APIVersion: 2` in `mod_info.yaml`.

---

## 1. Development environment setup

### Required tools

**IDE**: Visual Studio Community 2022+ (Windows, recommended), JetBrains Rider, or VS Code with C# extensions. For Visual Studio, install the `.NET desktop development` workload and the `.NET Framework 4.7.1 SDK` + targeting pack as individual components. For VS Code on Linux/Mac, install `dotnetcore` and grab the reference assemblies via NuGet: `Microsoft.NETFramework.ReferenceAssemblies.net471`.

**Decompiler** (essential - this is how you read the game's source): Use **ILSpy** (github.com/icsharpcode/ILSpy), **dnSpy** (best for viewing IL alongside C#), or **dotPeek** (JetBrains, free). Open `Assembly-CSharp.dll` to browse every game class and method.

### Required DLL references

All DLLs live in your game installation at:
```
<Steam Library>/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed/
```

Copy these to a `lib/` folder next to your solution:

| DLL | Purpose |
|-|-|
| `Assembly-CSharp.dll` | All game logic - buildings, creatures, UI, simulation |
| `Assembly-CSharp-firstpass.dll` | First-pass compiled game code |
| `0Harmony.dll` | Harmony 2.0 patching library (bundled with game) |
| `UnityEngine.dll` | Unity engine core |
| `UnityEngine.CoreModule.dll` | `Debug.Log`, `MonoBehaviour`, core types |
| `UnityEngine.UI.dll` | Unity UI components (needed for UI mods) |
| `Newtonsoft.Json.dll` | JSON serialization (bundled with game) |

### Project configuration (.csproj)

Create a **Class Library** project targeting **.NET Framework 4.7.1**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net471</TargetFramework>
    <OutputType>Library</OutputType>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Assembly-CSharp">
      <HintPath>..\lib\Assembly-CSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Assembly-CSharp-firstpass">
      <HintPath>..\lib\Assembly-CSharp-firstpass.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>..\lib\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine">
      <HintPath>..\lib\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>..\lib\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

Set **`<Private>false</Private>`** (Copy Local = false) on every game DLL - they already exist in the game directory and must not be bundled with your mod.

### Build output and deployment

Add a post-build event to auto-deploy your compiled DLL:

```batch
mkdir "%HOMEPATH%\Documents\Klei\OxygenNotIncluded\mods\local\$(ProjectName)"
copy "$(TargetPath)" "%HOMEPATH%\Documents\Klei\OxygenNotIncluded\mods\local\$(ProjectName)"
copy "$(ProjectDir)mod_info.yaml" "%HOMEPATH%\Documents\Klei\OxygenNotIncluded\mods\local\$(ProjectName)"
```

### Community templates

- **Cairath's ONI Mod template**: Download from github.com/Cairath/Oxygen-Not-Included-Modding - place the ZIP in `%USERPROFILE%\Documents\Visual Studio 2022\Templates\ProjectTemplates\`
- **O-n-y's template**: github.com/O-n-y/OxygenNotIncludedModTemplate - includes step-by-step guide
- **PLib** (Peter Han): NuGet package `PLib` - modular library for mod options, UI helpers, building registration. Must be ILMerged into your output DLL

---

## 2. How ONI loads mods and the mod folder structure

### Mod directory locations

```
Windows:  %USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\
macOS:    ~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/
Linux:    ~/.config/unity3d/Klei/Oxygen Not Included/mods/
```

Each location contains three subdirectories: **`dev/`** (development mods, always loaded), **`local/`** (manually installed), and **`Steam/`** (Workshop subscriptions, managed by Steam).

### Individual mod folder structure

```
MyMod/
├── mod_info.yaml           ← REQUIRED: compatibility metadata
├── mod.yaml                ← Recommended: display metadata
├── MyMod.dll               ← Your compiled assembly
├── anim/                   ← Custom kanim animations
│   └── assets/
│       └── mybuilding/
│           ├── mybuilding_0.png
│           ├── mybuilding_anim.bytes
│           └── mybuilding_build.bytes
├── worldgen/               ← Custom world generation templates
├── strings/                ← Localization .po files
└── archived_versions/      ← Backward-compatible builds
    └── vanilla_469369/
        ├── mod_info.yaml
        └── MyMod.dll
```

### mod_info.yaml - required for all mods

```yaml
supportedContent: ALL          # VANILLA_ID, EXPANSION1_ID, or ALL
minimumSupportedBuild: 526233  # Minimum game build number
version: 1.0.0                 # Your mod version (semantic versioning)
APIVersion: 2                  # REQUIRED for DLL mods (Harmony 2.0)
```

| Field | Required | Values |
|-|-|-|
| `supportedContent` | Yes | `VANILLA_ID`, `EXPANSION1_ID`, `ALL`, or comma-separated |
| `minimumSupportedBuild` | Yes | Game build number from main menu |
| `version` | Recommended | Displayed in mod list |
| `APIVersion` | Yes (DLL mods) | Must be `2` - without this, DLL mods are silently disabled |

### mod.yaml - display metadata

```yaml
title: "My Cool Mod"
description: "A description of what this mod does"
staticID: "MyMod.AuthorName"
```

The **`staticID`** is used as the Harmony instance ID and enables cross-install-method mod detection.

### The mod loading pipeline

The game follows this sequence on startup:

1. Scans `Steam/`, `local/`, and `dev/` directories for mod folders
2. Reads `mod_info.yaml` from each folder and checks `supportedContent` against current game mode and `minimumSupportedBuild` against current build
3. If the root mod doesn't match, scans `archived_versions/` subfolders for a compatible version
4. Loads `.dll` files from the matched mod folder
5. Scans each DLL for classes extending **`KMod.UserMod2`**
6. If found, calls **`OnLoad(Harmony harmony)`** - you control patching
7. If no `UserMod2` subclass exists, automatically calls `harmony.PatchAll()` on the assembly

### Entry points

**Option A - UserMod2 (recommended for control):**

```csharp
using KMod;
using HarmonyLib;

namespace MyMod
{
    public class MyModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Debug.Log("MyMod: Loading...");
            base.OnLoad(harmony);  // Calls harmony.PatchAll()
            Debug.Log("MyMod: All patches applied!");
        }
    }
}
```

Rules: at most **one** `UserMod2` per DLL, must not be abstract. Calling `base.OnLoad(harmony)` invokes `harmony.PatchAll()`.

**Option B - Auto-patching (simplest):**

Just annotate patch classes with `[HarmonyPatch]` - no entry point class needed. The game discovers and applies all patches automatically.

### Hello world mod - complete example

**Patches.cs:**
```csharp
using HarmonyLib;

namespace HelloWorldMod
{
    [HarmonyPatch(typeof(Db), "Initialize")]
    public class Db_Initialize_Patch
    {
        public static void Postfix()
        {
            Debug.Log("Hello World from ONI mod!");
        }
    }
}
```

**mod_info.yaml:**
```yaml
supportedContent: ALL
minimumSupportedBuild: 526233
version: 1.0.0
APIVersion: 2
```

Deploy the DLL + YAML to `mods/local/HelloWorldMod/` and check `Player.log` at `%APPDATA%\..\LocalLow\Klei\Oxygen Not Included\Player.log`.

### Base game vs Spaced Out DLC compatibility

Use `supportedContent: ALL` if your mod works with both. For runtime DLC detection, call **`DlcManager.IsExpansion1Active()`**. The `archived_versions/` system lets you ship separate builds for different game versions - the root folder holds the current version while archived subfolders serve older builds.

### UserMod2 full lifecycle

`UserMod2` provides two override points:

**`OnLoad(Harmony harmony)`** - called when your mod DLL loads. If you override this, call `base.OnLoad(harmony)` to trigger automatic `PatchAll()`. You can run code before or after the base call:

```csharp
public override void OnLoad(Harmony harmony)
{
    Debug.Log("Before patches");
    base.OnLoad(harmony);
    Debug.Log("After patches");
}
```

**`OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)`** - called after ALL mods finish loading. Use for mod compatibility checks:

```csharp
public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
{
    foreach (var mod in mods)
        Debug.Log("Found mod: " + mod.title);
}
```

**Available properties:**
- `assembly` - your mod's Assembly
- `path` - your mod's folder path
- `mod` - the Mod instance
- `mod.title`, `mod.staticID`, `mod.description` - from mod.yaml
- `mod.packageModInfo` - from mod_info.yaml

**Constraints:**
- Maximum one UserMod2 per DLL (but a Mod with multiple DLLs can have multiple UserMod2 classes)
- Cannot be abstract
- Access all instances via `Mod.loaded_mod_data.userMod2Instances`

---

## 3. Harmony patching - the core modding technique

### Fundamentals

ONI ships **Harmony 2.0.4.0**. Import with `using HarmonyLib;` (never the old `using Harmony;`). Mods do not bundle their own `0Harmony.dll` - the game provides it.

**Attribute-based patching** (most common):
```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
// or with parameter types for overloaded methods:
[HarmonyPatch(typeof(TargetClass), "MethodName", new Type[] { typeof(int), typeof(string) })]
// or using nameof for compile-time safety:
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.MethodName))]
```

**Manual patching** (for dynamic targets):
```csharp
var original = typeof(ManualGeneratorConfig).GetMethod("CreateBuildingDef");
var postfix = typeof(MyPatch).GetMethod("Postfix");
harmony.Patch(original, postfix: new HarmonyMethod(postfix));
```

### Prefix patches - run before the original method

Prefixes can inspect/modify arguments, skip the original entirely (return `false`), or set the return value early. All patch methods must be `static`.

**Injected parameters:**
- `__instance` - the object instance (`this`) for instance methods
- `__result` - the return value (use `ref` to set it)
- `__state` - data passed from prefix to postfix
- Named parameters matching original method parameters (use `ref` to modify)
- `___privateFieldName` - access private fields (three underscores + field name)

**Example - modifying a return value and skipping the original:**
```csharp
[HarmonyPatch(typeof(SomeClass), "GetValue")]
public static class SomeClass_GetValue_Patch
{
    public static bool Prefix(ref float __result)
    {
        __result = 42f;   // Set the return value
        return false;      // Skip the original method
    }
}
```

**Real ONI example - suppressing camera setup:**
```csharp
[HarmonyPatch(typeof(CameraController), "OnPrefabInit")]
public static class CameraController_OnPrefabInit_Patch
{
    public static bool Prefix()
    {
        return false; // Skip the entire original method
    }
}
```

### Postfix patches - run after the original method

Postfixes always run (even if a prefix skipped the original) and are the **preferred patch type** for mod compatibility.

**Real ONI example - changing Manual Generator wattage from 400W to 600W:**
```csharp
[HarmonyPatch(typeof(ManualGeneratorConfig), "CreateBuildingDef")]
public static class ManualGeneratorConfig_CreateBuildingDef_Patch
{
    public static void Postfix(BuildingDef __result)
    {
        __result.GeneratorWattageRating = 600f;
    }
}
```

**Real ONI example - enriching geyser tooltips with average output:**
```csharp
[HarmonyPatch(typeof(Geyser), nameof(Geyser.GetDescriptors))]
public static class Geyser_GetDescriptors_Patch
{
    public static void Postfix(Geyser __instance, ref List<Descriptor> __result)
    {
        var studyable = __instance.GetComponent<Studyable>();
        if (studyable != null && studyable.Studied)
        {
            float emitRate = __instance.configuration.GetEmitRate() * 1000f;
            float onDuration = __instance.configuration.GetOnDuration();
            float iterLen = __instance.configuration.GetIterationLength();
            float avgOutput = emitRate * (onDuration / iterLen);
            __result.Add(new Descriptor(
                $"Average Output: {avgOutput:F1} g/s",
                $"Calculated average continuous output"));
        }
    }
}
```

### Transpiler patches - IL code manipulation

Transpilers rewrite the method's CIL bytecode at patch time. They run **once**, add no runtime overhead, and compose with other transpilers. Use them when you need to change logic **in the middle** of a method.

```csharp
using System.Collections.Generic;
using System.Reflection.Emit;

[HarmonyPatch(typeof(ClusterMapScreen), nameof(ClusterMapScreen.OnKeyDown))]
public static class ClusterMapScreen_OnKeyDown_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        bool patched = false;

        for (int i = 0; i < codes.Count; i++)
        {
            // Find the float constant 50f (default max zoom) and replace it
            if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 50f)
            {
                codes[i].operand = 150f;  // New max zoom
                patched = true;
            }
        }

        if (!patched)
            Debug.LogWarning("[MyMod] Transpiler failed - game version may have changed.");

        return codes;
    }
}
```

**When to use transpilers vs prefix/postfix:**

- Change return value → **Postfix** with `ref __result`
- Change arguments → **Prefix** with `ref` parameters
- Skip method entirely → **Prefix** returning `false`
- Modify logic **mid-method** → **Transpiler**
- Change a specific constant deep inside a method → **Transpiler**

### Finding methods to patch with decompilers

Open `Assembly-CSharp.dll` in ILSpy or dnSpy and use these strategies:

1. **Search by keyword** - search for "calories", "stress", "wattage" to find relevant code
2. **Follow UI strings** - search for in-game text like "Wheezewort" to find the code name ("Coldbreather")
3. **Look at `*Config` classes** - every building has a `[BuildingName]Config.cs` with `CreateBuildingDef()`
4. **Browse TUNING namespace** - all balance constants are organized here
5. **Check existing mods on GitHub** - search for similar functionality

**Commonly patched methods:**

| Class.Method | Purpose |
|-|-|
| `Db.Initialize()` | Register buildings, techs, traits, strings |
| `GeneratedBuildings.LoadGeneratedBuildings()` | Add buildings to build menus |
| `*Config.CreateBuildingDef()` | Modify building stats |
| `*Config.ConfigureBuildingTemplate()` | Modify building components |
| `ElementLoader.Load()` | Modify element properties |
| `Immigration.*` | Printing pod / duplicant selection |
| `MinionConfig.CreatePrefab()` | Duplicant stats and traits |

### Harmony best practices

**Prefer Postfix over destructive Prefix** - postfixes compose naturally across mods. Returning `false` from a prefix blocks all subsequent prefixes and the original method.

**Use unique Harmony IDs** following reverse domain notation: `"com.myname.mymod"`. In ONI's built-in system, the ID defaults from `staticID` in `mod.yaml`.

**Always wrap patch logic in try/catch** - uncaught exceptions in patches crash the game:
```csharp
[HarmonyPatch(typeof(SomeClass), "SomeMethod")]
public static class SafePatch
{
    public static void Postfix(SomeClass __instance)
    {
        try
        {
            var component = __instance.GetComponent<SomeComponent>();
            if (component == null) return;
            // ... patch logic ...
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MyMod] Error in patch: {e}");
        }
    }
}
```

**Use `Prepare()` to conditionally skip patches:**
```csharp
[HarmonyPatch(typeof(SomeClass), "SomeMethod")]
public static class ConditionalPatch
{
    static bool Prepare() => MyModConfig.FeatureEnabled;
    static void Postfix() { /* ... */ }
}
```

**Priority system** for ordering patches: `Priority.First` (0) through `Priority.Last` (900), with `Priority.Normal` (400) as default. Apply with `[HarmonyPriority(Priority.High)]`.

**Debug Harmony patches** by setting `Harmony.DEBUG = true;` (creates `harmony.log.txt` on Desktop) or use the `[HarmonyDebug]` attribute on a specific patch class.

---

## 4. Gameplay tweaks with concrete examples

### Modifying building stats via postfix

The cleanest approach patches the building's `CreateBuildingDef()` and modifies the returned `BuildingDef`:

```csharp
// Double battery capacity
[HarmonyPatch(typeof(BatteryConfig), "CreateBuildingDef")]
public static class BatteryConfig_Patch
{
    public static void Postfix(BuildingDef __result)
    {
        __result.GeneratorBaseCapacity = 20000f;  // Was 10000f
    }
}

// Make Electrolyzer produce more oxygen
[HarmonyPatch(typeof(ElectrolyzerConfig), "CreateBuildingDef")]
public static class ElectrolyzerConfig_Patch
{
    public static void Postfix(BuildingDef __result)
    {
        __result.EnergyConsumptionWhenActive = 100f;  // Was 120f
        __result.SelfHeatKilowattsWhenActive = 1.0f;  // Was 1.75f
    }
}
```

### Modifying recipes

Add a new recipe to an existing fabricator by patching `ConfigureBuildingTemplate`:

```csharp
[HarmonyPatch(typeof(RockCrusherConfig), "ConfigureBuildingTemplate")]
public static class RockCrusher_AddRecipe_Patch
{
    public static void Postfix()
    {
        ComplexRecipe.RecipeElement[] inputs = new ComplexRecipe.RecipeElement[]
        {
            new ComplexRecipe.RecipeElement(SimHashes.Regolith.CreateTag(), 100f)
        };
        ComplexRecipe.RecipeElement[] outputs = new ComplexRecipe.RecipeElement[]
        {
            new ComplexRecipe.RecipeElement(SimHashes.Sand.CreateTag(), 100f)
        };

        string recipeId = ComplexRecipeManager.MakeRecipeID(
            RockCrusherConfig.ID, inputs, outputs);

        new ComplexRecipe(recipeId, inputs, outputs)
        {
            time = 40f,
            description = "Crushes Regolith into Sand.",
            nameDisplay = ComplexRecipe.RecipeNameDisplay.IngredientToResult,
            fabricators = new List<Tag> { TagManager.Create(RockCrusherConfig.ID) }
        };
    }
}
```

### Modifying the research tree

```csharp
[HarmonyPatch(typeof(Db), "Initialize")]
public static class Db_ResearchTree_Patch
{
    public static void Postfix()
    {
        // Move ThermalBlock from TemperatureModulation to BasicRefinement
        Tech oldTech = Db.Get().Techs.Get("TemperatureModulation");
        oldTech.unlockedItemIDs.Remove("ThermalBlock");

        Tech newTech = Db.Get().Techs.Get("BasicRefinement");
        newTech.unlockedItemIDs.Add("ThermalBlock");
    }
}
```

### Modifying element properties

```csharp
[HarmonyPatch(typeof(ElementLoader), "Load")]
public static class ElementLoader_Load_Patch
{
    public static void Postfix()
    {
        Element iron = ElementLoader.FindElementByHash(SimHashes.Iron);
        if (iron != null)
        {
            iron.thermalConductivity = 100f;
            iron.specificHeatCapacity = 0.5f;
        }
    }
}
```

### Modifying geyser outputs

```csharp
[HarmonyPatch(typeof(GeyserConfigurator), "CreateConfiguration")]
public static class GeyserPatch
{
    public static void Postfix(GeyserConfigurator __instance)
    {
        // Modify geyser emit rate, temperature, cycle timing, etc.
        // Properties: element, temperature, minRatePerCycle, maxRatePerCycle,
        // minIterationLength, maxIterationLength, minYearLength, maxYearLength
    }
}
```

### Modifying duplicant traits

```csharp
[HarmonyPatch(typeof(Db), "Initialize")]
public static class ModifyTraits_Patch
{
    public static void Postfix()
    {
        // Access the trait database
        var traits = Db.Get().traits;
        // Modify specific trait attribute modifiers
    }
}
```

---

## 5. Creating new buildings from scratch

Every custom building requires a class implementing **`IBuildingConfig`** with three methods: `CreateBuildingDef()`, `ConfigureBuildingTemplate()`, and `DoPostConfigureComplete()`. You also need Harmony patches to register strings, add the building to the build menu, and assign it to a research node.

### Complete building template

```csharp
using TUNING;
using UnityEngine;
using System.Collections.Generic;

namespace MyMod
{
    public class MyCustomBuildingConfig : IBuildingConfig
    {
        public const string ID = "MyCustomBuilding";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id:                     ID,
                width:                  2,
                height:                 3,
                anim:                   "mybuilding_kanim",
                hitpoints:              BUILDINGS.HITPOINTS.TIER2,          // 50 HP
                construction_time:      BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER3, // 60s
                construction_mass:      BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,      // 200kg
                construction_materials: MATERIALS.ALL_METALS,
                melting_point:          BUILDINGS.MELTING_POINT_KELVIN.TIER2,       // 2400K
                build_location_rule:    BuildLocationRule.OnFloor,
                decor:                  BUILDINGS.DECOR.PENALTY.TIER2,
                noise:                  NOISE_POLLUTION.NOISY.TIER2
            );

            // Power consumption
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 240f;
            def.PowerInputOffset = new CellOffset(0, 0);

            // Liquid pipe input
            def.InputConduitType = ConduitType.Liquid;
            def.UtilityInputOffset = new CellOffset(0, 0);

            // Liquid pipe output
            def.OutputConduitType = ConduitType.Liquid;
            def.UtilityOutputOffset = new CellOffset(1, 0);

            // Automation port
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(
                new CellOffset(0, 0));

            // Thermal properties
            def.ExhaustKilowattsWhenActive = 0.5f;
            def.SelfHeatKilowattsWhenActive = 2.0f;
            def.Overheatable = true;
            def.OverheatTemperature = 348.15f;  // 75°C

            // Misc
            def.Floodable = false;
            def.Entombable = true;
            def.ViewMode = OverlayModes.LiquidConduits.ID;
            def.AudioCategory = "HollowMetal";
            def.PermittedRotations = PermittedRotations.Unrotatable;

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(
                RoomConstraints.ConstraintTags.IndustrialMachinery);

            // Storage
            Storage storage = go.AddOrGet<Storage>();
            storage.showInUI = true;
            storage.capacityKg = 200f;
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);

            // Pipe input consumer
            ConduitConsumer consumer = go.AddOrGet<ConduitConsumer>();
            consumer.conduitType = ConduitType.Liquid;
            consumer.consumptionRate = 10f;
            consumer.capacityKG = 200f;
            consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            consumer.forceAlwaysSatisfied = true;

            // Pipe output dispenser
            ConduitDispenser dispenser = go.AddOrGet<ConduitDispenser>();
            dispenser.conduitType = ConduitType.Liquid;
            dispenser.elementFilter = new SimHashes[] { SimHashes.DirtyWater };
            dispenser.invertElementFilter = false;

            // Element conversion (Water → Oxygen + Hydrogen)
            ElementConverter converter = go.AddOrGet<ElementConverter>();
            converter.consumedElements = new ElementConverter.ConsumedElement[]
            {
                new ElementConverter.ConsumedElement(new Tag("Water"), 1.0f)
            };
            converter.outputElements = new ElementConverter.OutputElement[]
            {
                new ElementConverter.OutputElement(0.888f, SimHashes.Oxygen, 0, false,
                    true, 0f, 0.5f, 1f, byte.MaxValue, 0),
                new ElementConverter.OutputElement(0.112f, SimHashes.Hydrogen, 0, false,
                    false, 0f, 0.5f, 1f, byte.MaxValue, 0)
            };
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGetDef<PoweredActiveController.Def>();
        }
    }
}
```

### TUNING constant tiers reference

```
HITPOINTS:          TIER0=10  TIER1=30  TIER2=50  TIER3=100  TIER4=250
CONSTRUCTION_TIME:  TIER0=3   TIER1=10  TIER2=30  TIER3=60   TIER4=120  TIER5=180
CONSTRUCTION_MASS:  TIER0=25  TIER1=50  TIER2=100 TIER3=200  TIER4=400  TIER5=800
MELTING_POINT:      TIER0=800 TIER1=1600 TIER2=2400 TIER3=9999
```

**Material arrays:** `MATERIALS.RAW_MINERALS`, `MATERIALS.ALL_METALS`, `MATERIALS.REFINED_METALS`, `MATERIALS.PLASTICS`, `MATERIALS.TRANSPARENTS`

**BuildLocationRule values:** `OnFloor`, `OnCeiling`, `OnWall`, `Anywhere`, `Tile`, `Conduit`, `NotInTiles`, `Behind`, `BuildingAttachPoint`

### Registering the building - strings, build menu, and tech tree

```csharp
[HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
public static class RegisterBuilding_Patch
{
    public static void Prefix()
    {
        // Register display strings
        Strings.Add(
            $"STRINGS.BUILDINGS.PREFABS.{MyCustomBuildingConfig.ID.ToUpperInvariant()}.NAME",
            UI.FormatAsLink("My Custom Building", MyCustomBuildingConfig.ID));
        Strings.Add(
            $"STRINGS.BUILDINGS.PREFABS.{MyCustomBuildingConfig.ID.ToUpperInvariant()}.DESC",
            "A custom building that processes water.");
        Strings.Add(
            $"STRINGS.BUILDINGS.PREFABS.{MyCustomBuildingConfig.ID.ToUpperInvariant()}.EFFECT",
            "Converts Water into Oxygen and Hydrogen.");

        // Add to build menu - categories: "Base", "Oxygen", "Power", "Food",
        // "Plumbing", "HVAC", "Refining", "Medical", "Furniture", "Equipment",
        // "Utilities", "Automation", "Shipping", "Rocketry"
        ModUtil.AddBuildingToPlanScreen("Oxygen", MyCustomBuildingConfig.ID);
    }
}

[HarmonyPatch(typeof(Db), "Initialize")]
public static class AddToTechTree_Patch
{
    public static void Postfix()
    {
        Db.Get().Techs.Get("ImprovedOxygen")
            .unlockedItemIDs.Add(MyCustomBuildingConfig.ID);
    }
}
```

### Power generator pattern

For buildings that generate power instead of consuming it:

```csharp
// In CreateBuildingDef:
def.GeneratorWattageRating = 600f;
def.GeneratorBaseCapacity = 2000f;
def.RequiresPowerOutput = true;
def.PowerOutputOffset = new CellOffset(0, 0);

// In ConfigureBuildingTemplate:
EnergyGenerator.Formula formula = default;
formula.inputs = new EnergyGenerator.InputItem[]
{
    new EnergyGenerator.InputItem(new Tag("Carbon"), 1f, 600f)
};
formula.outputs = new EnergyGenerator.OutputItem[]
{
    new EnergyGenerator.OutputItem(SimHashes.CarbonDioxide, 0.02f, false,
        new CellOffset(0, 1), 383.15f)
};
go.AddOrGet<EnergyGenerator>().formula = formula;
```

---

## 6. Creating new items, elements, and creatures

### New elements

ONI's element system is built on the **`SimHashes`** enum (every element has a unique hash), the **`ElementLoader`** static class, and YAML definition files in `StreamingAssets/elements/`. Element properties include thermal conductivity, specific heat capacity, state transitions, hardness, viscosity, and sublimation behavior.

**Modifying existing elements** is straightforward via an `ElementLoader.Load` postfix (shown in Section 4). **Adding entirely new elements** is substantially harder because you must extend the `SimHashes` enum, register with the native C++ simulation, and provide a `Substance` with rendering data. Most mods modify existing elements rather than adding new ones.

**Critical pitfall:** `ElementLoader` is not available during early initialization. Never call `ElementLoader.FindElementByHash()` in static field initializers - use `SimHashes.Water.CreateTag()` instead.

### New food items

Food items implement **`IEntityConfig`** and use **`EdiblesManager.FoodInfo`** for nutritional properties:

```csharp
public class MyFoodConfig : IEntityConfig
{
    public const string ID = "MyCustomFood";

    public static readonly EdiblesManager.FoodInfo FOOD_INFO =
        new EdiblesManager.FoodInfo(
            ID, "",
            4000000f,       // 4000 kcal
            3,              // Quality: GREAT (-1=Awful, 0=Terrible, 1=Mediocre,
                            //          2=Good, 3=Great, 4=Amazing, 5=Wonderful)
            255.15f,        // Preserve temperature (K)
            277.15f,        // Rot temperature (K)
            2400f,          // Spoil time (seconds; 600 = 1 cycle)
            true);          // Can rot from age

    public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

    public GameObject CreatePrefab()
    {
        GameObject prefab = EntityTemplates.CreateLooseEntity(
            ID, "My Custom Food", "A delicious custom dish.",
            1f, false,
            Assets.GetAnim("mycustomfood_kanim"),
            "object", Grid.SceneLayer.Front,
            EntityTemplates.CollisionShape.RECTANGLE,
            0.8f, 0.4f, true);

        EntityTemplates.ExtendEntityToFood(prefab, FOOD_INFO);
        return prefab;
    }

    public void OnPrefabInit(GameObject inst) { }
    public void OnSpawn(GameObject inst) { }
}
```

Register a cooking recipe with:
```csharp
new ComplexRecipe(recipeId, inputs, outputs)
{
    time = 50f,
    description = "Cook a custom dish.",
    nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
    fabricators = new List<Tag> { CookingStationConfig.ID }
};
```

**Key recipe building IDs:** `CookingStation`, `GourmetCookingStation`, `MicrobeMusher`, `RockCrusher`, `MetalRefinery`, `GlassForge`, `Kiln`, `SupermaterialRefinery`, `Apothecary`

### New creatures

Creatures use `IEntityConfig` combined with `EntityTemplates` helper methods for movement, health, diet, and reproduction:

```csharp
public class MyCreatureConfig : IEntityConfig
{
    public const string ID = "MyCreature";
    public const string EGG_ID = "MyCreatureEgg";
    public const string BASE_TRAIT_ID = "MyCreatureBaseTrait";

    public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

    public GameObject CreatePrefab()
    {
        GameObject prefab = EntityTemplates.CreatePlacedEntity(
            ID, "My Creature", "A custom creature.",
            50f, Assets.GetAnim("my_creature_kanim"),
            "idle_loop", Grid.SceneLayer.Creatures,
            width: 1, height: 1,
            new EffectorValues { amount = 0, radius = 0 },
            NOISE_POLLUTION.NONE);

        // Define diet: eats CO2, produces Petroleum
        Diet.Info[] dietInfos = new Diet.Info[]
        {
            new Diet.Info(
                new HashSet<Tag> { SimHashes.CarbonDioxide.CreateTag() },
                SimHashes.Petroleum.CreateTag(),
                TUNING.CREATURES.CALORIES_PER_KG_OF_ORE,
                TUNING.CREATURES.CONVERSION_EFFICIENCY.GOOD_1,
                null, 0, false, false)
        };

        EntityTemplates.ExtendEntityToBasicCreature(
            prefab,
            FactionManager.FactionID.Prey,
            BASE_TRAIT_ID,
            "FlyerNavGrid",    // or "WalkerNavGrid", "SwimmerNavGrid"
            NavType.Hover,     // or NavType.Floor, NavType.Swim
            32, 2f,            // Probing radius, move speed
            "Meat", 1,         // Death drop item, count
            true, false,       // Can drown, can crush
            253.15f, 373.15f); // Min/max lethal temperature

        EntityTemplates.ExtendEntityToWildCreature(prefab,
            TUNING.CREATURES.SPACE_REQUIREMENTS.TIER3,
            TUNING.CREATURES.LIFESPAN.TIER4);

        // Attach diet
        prefab.AddOrGetDef<CreatureCalorieMonitor.Def>().diet = new Diet(dietInfos);
        prefab.AddOrGetDef<SolidConsumerMonitor.Def>().diet = new Diet(dietInfos);

        return prefab;
    }

    public void OnPrefabInit(GameObject inst) { }
    public void OnSpawn(GameObject inst) { }
}
```

**Navigation types:** `NavType.Floor` (Hatch), `NavType.Hover` (Slickster, Puft), `NavType.Swim` (Pacu). **Nav grids:** `"WalkerNavGrid"`, `"FlyerNavGrid"`, `"SwimmerNavGrid"`, `"DreckoNavGrid"`.

**Key creature components:** `CreatureCalorieMonitor.Def` (hunger), `FertilityMonitor.Def` (egg laying), `IncubationMonitor.Def` (egg hatching), `OvercrowdingMonitor.Def` (crowding), `BabyMonitor.Def` (growing up).

### Kanim animations

ONI uses **Klei Animation (kanim)** - a proprietary cut-out animation format consisting of three files per animation:

| File | Content |
|-|-|
| `name_0.png` | Texture atlas with all sprite pieces packed together |
| `name_build.bytes` | Build data mapping sprite names to atlas regions |
| `name_anim.bytes` | Animation banks with frame-by-frame transforms |

Place custom kanims in `YourMod/anim/assets/subfolder/` (files must be in a subfolder, not directly in `anim/`). Reference in code as `Assets.GetAnim("name_kanim")` - note the `_kanim` suffix.

**Common animation bank names for buildings:** `"idle"`, `"off"`, `"on"`, `"working_pre"`, `"working_loop"`, `"working_pst"`, `"broken"`. **For creatures:** `"idle_loop"`, `"walk_pre"`, `"walk_loop"`, `"eat_pre"`, `"eat_loop"`. **For items:** `"object"` (single static frame).

**Tools for creating kanims:**

- **Kanim Explorer** (github.com/romen-h/kanim-explorer) - best current GUI tool for viewing, editing, and creating kanim files. Supports SCML import/export
- **kanimal-SE** (github.com/skairunner/kanimal-SE) - CLI converter between kanim and Spriter SCML format
- **kparser** (github.com/daviscook477/kparser) - original Java converter, established the kanim↔SCML workflow

**Workflow:** Extract existing kanim → edit in Spriter (free 2D animation tool) → convert back with kanimal-SE → place output files in mod's `anim/assets/` folder.

### Conversion commands

**Extract game animation (kanim → Spriter):**
```bash
kanimal-cli.exe scml - -output <folder> <texture_0.png> <build.bytes> <anim.bytes>
```

**Build mod animation (Spriter → kanim):**
```bash
kanimal-cli.exe kanim <file.scml> - -output <folder> - -interpolate
```

### Critical constraints
1. **No bones** - kanim format does not support skeletal animation; use sprite-based animation only
2. **No non-linear tweens** - only linear interpolation between keyframes
3. **Never resize sprites** - do not resize or move sprite contents within their bounding box
4. **33ms frame duration** - set frame duration to 33ms with snapping enabled in Spriter
5. **Always include anim.bytes** - even static graphics (like building artwork) need an anim.bytes file

### Mod animation folder structure
Place animation files in your mod's anim folder:
```
<mod>/anim/assets/<animname>/<animname>_0.png
<mod>/anim/assets/<animname>/<animname>_build.bytes
<mod>/anim/assets/<animname>/<animname>_anim.bytes
```

### Animation tips
- Use existing game kanims as reference - extract them with kanimal-SE first
- Symbol names in the build file must match what the game code expects
- Banks define animation sequences (e.g., "idle", "working", "off")
- Each bank can have multiple frames with different symbol visibility

---

## 7. UI modding techniques

### ONI's UI framework

ONI wraps Unity's UGUI system in Klei-specific classes. The key base classes are:

- **`KScreen`** - base class for all screens, with lifecycle methods `OnPrefabInit`, `OnActivate`, `OnDeactivate`, `OnKeyDown`
- **`KModal`** - modal dialog that blocks input to screens beneath it
- **`KButton`**, **`KScrollRect`**, **`LocText`** - Klei wrappers for buttons, scrolling, and localized text
- **`DetailsScreen`** - the right-side panel showing selected entity details, hosts side screens
- **`OverlayScreen`** - manages overlay modes (temperature, power, plumbing)
- **`ScreenManager`** - manages the active screen stack

### Modifying existing UI

Patch the target screen's lifecycle methods and navigate the Transform hierarchy:

```csharp
[HarmonyPatch(typeof(SomeUIScreen), "OnPrefabInit")]
public static class SomeUIScreen_Patch
{
    static void Postfix(SomeUIScreen __instance)
    {
        var panel = __instance.gameObject.transform.Find("SomePanel");
        // Add new buttons, labels, etc. to the panel
    }
}
```

### Adding a custom side screen for buildings

```csharp
[HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
public static class DetailsScreen_Patch
{
    static void Postfix()
    {
        PUIUtils.AddSideScreenContent<MyCustomSideScreen>();
    }
}

public class MyCustomSideScreen : SideScreenContent
{
    protected override void OnPrefabInit()
    {
        base.OnPrefabInit();
        var panel = new PPanel("MainPanel")
        {
            Direction = PanelDirection.Vertical,
            Alignment = TextAnchor.MiddleCenter
        };
        panel.AddChild(new PLabel("MyLabel") { Text = "Hello" });
        ContentContainer = panel.AddTo(gameObject, 0);
    }

    public override bool IsValidForTarget(GameObject target)
    {
        return target.GetComponent<MyBuildingComponent>() != null;
    }

    public override void SetTarget(GameObject target)
    {
        base.SetTarget(target);
        // Update UI with target's data
    }
}
```

**Note:** `SetTarget` is called on the very first selection *before* `OnPrefabInit` runs - handle null cases in your code.

### Adding custom overlays

```csharp
public class MyOverlay : OverlayModes.Mode
{
    public static readonly HashedString ID = "MyCustomOverlay";
    public override HashedString ViewMode() => ID;
    public override string GetSoundName() => "Temperature";
    public override void Enable() { /* Setup visualization */ }
    public override void Disable() { /* Cleanup */ }
    public override void Update() { /* Per-frame update */ }
}
```

Register by patching `OverlayScreen.OnPrefabInit` and adding your mode to its overlay modes list.

### PLib UI components

PLib provides a builder-pattern UI system matching the game's visual style: **`PPanel`** (layout), **`PLabel`** (text), **`PButton`** (button), **`PTextField`** (input), **`PCheckBox`** (toggle), **`PSliderSingle`** (slider), **`PScrollPane`** (scrolling), **`PDialog`** (dialog window). All use `.AddTo(parent, index)` to attach to the hierarchy.

### Standalone Canvas (ScreenSpaceOverlay)

For HUD elements independent of the game's screen stack, create your own Canvas:

```csharp
var canvasGO = new GameObject("MyCanvas");
var canvas = canvasGO.AddComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 100;  // above game UI

var scaler = canvasGO.AddComponent<CanvasScaler>();
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1920, 1080);
scaler.matchWidthOrHeight = 0.5f;

canvasGO.AddComponent<GraphicRaycaster>();
```

**Key gotchas:**
- `new GameObject()` only has `Transform`. You must `AddComponent<Image>()` (or any UI component) before `GetComponent<RectTransform>()` returns non-null - `Image` auto-adds a `RectTransform`.
- `TextMeshProUGUI` and `Image` cannot share the same GameObject - TMP needs its own child GO.
- To respect the game's UI Scale slider: read `KPlayerPrefs.GetFloat(KCanvasScaler.UIScalePrefKey)` (stored as percentage, 100 = 1x), divide by 100, then adjust your `CanvasScaler.referenceResolution` by dividing the base resolution by that scale factor.
- `KCanvasScaler` uses `ConstantPixelSize` mode with a stepped resolution scale. If you use `ScaleWithScreenSize`, you handle resolution adaptation automatically but need to incorporate `userScale` manually via the reference resolution trick.

### Localization with .po files

ONI uses `LocString` fields for localizable text. Fields must be `public static` (not `readonly`):

```csharp
public static class STRINGS
{
    public static class MYMOD
    {
        public static LocString LABEL = "My Label";
    }
}
```

Load translations from `.po` files using `Localization.RegisterForTranslation(typeof(STRINGS))` in your mod's `OnLoad`, then place `<locale>.po` files in a `translations/` folder inside your mod directory.

---

## 8. Mod configuration and settings

### PLib Options - the standard approach

PLib is the de facto standard for mod settings. Initialize in `OnLoad`, then define a settings class with attributes:

```csharp
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

public sealed class ModLoad : KMod.UserMod2
{
    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);
        PUtil.InitLibrary(false);
        new POptions().RegisterOptions(typeof(MyModSettings));
    }
}
```

```csharp
using Newtonsoft.Json;
using PeterHan.PLib.Options;

[JsonObject(MemberSerialization.OptIn)]
[ModInfo("https://github.com/mymod")]
[ConfigFile("config.json", IndentOutput: true)]
public class MyModSettings
{
    [Option("Wattage", "Power consumption in watts.", "Power")]
    [Limit(1, 50000)]
    [JsonProperty]
    public float Watts { get; set; }

    [Option("Enable Feature", "Toggle this feature on/off.")]
    [JsonProperty]
    public bool FeatureEnabled { get; set; }

    [Option("Mode", "Select operating mode.")]
    [JsonProperty]
    public MyEnum OperatingMode { get; set; }

    public MyModSettings()
    {
        Watts = 10000f;
        FeatureEnabled = true;
        OperatingMode = MyEnum.Default;
    }
}
```

**Supported types:** `int`, `float`, `string`, `bool`, `Color`, `Enum`, nullable variants, `System.Action` (rendered as button), `LocText` (rendered as label), and nested types (rendered as categories).

**Reading/writing settings at runtime:**
```csharp
var settings = POptions.ReadSettings<MyModSettings>() ?? new MyModSettings();
POptions.WriteSettings(settings);
```

The third parameter of `[Option]` groups related options into **categories**. Use `[Limit(min, max)]` for numeric ranges. Config files default to `config.json` in the mod assembly directory; set `UseSharedConfigLocation = true` to survive mod updates.

### Manual JSON config without PLib

```csharp
using Newtonsoft.Json;
using System.IO;

public static class ConfigManager
{
    private static string ConfigPath =>
        Path.Combine(KMod.Manager.GetDirectory(), "config.json");

    public static MyConfig Load()
    {
        if (File.Exists(ConfigPath))
            return JsonConvert.DeserializeObject<MyConfig>(
                File.ReadAllText(ConfigPath));
        return new MyConfig();
    }

    public static void Save(MyConfig config) =>
        File.WriteAllText(ConfigPath,
            JsonConvert.SerializeObject(config, Formatting.Indented));
}
```

### PLib additional modules

PLib 4.0+ is modular and must be ILMerged into your mod DLL:

- **PLib.Core** - initialization, logging, utilities
- **PLib.Options** - mod settings UI (shown above)
- **PLib.UI** - builder-pattern UI components
- **PLib.Actions** - custom keybindings rebindable in game options
- **PLib.Buildings** - simplified building registration
- **PLib.Database** - localization (`PLocalization.Register()`), codex entries
- **PLib.Lighting** - custom light shapes

---

## 9. Debugging and testing ONI mods

### Log file locations

| Platform | Path |
|-|-|
| Windows | `%APPDATA%\..\LocalLow\Klei\Oxygen Not Included\Player.log` |
| macOS | `~/Library/Application Support/unity.Klei.Oxygen Not Included/Player.log` |
| Linux | `~/.config/unity3d/Klei/Oxygen Not Included/Player.log` |

The log is **overwritten on each launch** - capture it before restarting. Crash dumps go to `%APPDATA%\..\Local\Temp\Klei\Oxygen Not Included\Crashes`.

### Logging from your mod

```csharp
// Standard Unity logging (always works)
Debug.Log("MyMod: Info message");
Debug.LogWarning("MyMod: Warning");
Debug.LogError("MyMod: Error");
Debug.LogException(ex);

// PLib logging (adds mod name prefix automatically)
PUtil.LogDebug("Debug message");
PUtil.LogWarning("Warning");
PUtil.LogError("Error");
```

### Enabling debug mode

Create an empty file named **`debug_enable.txt`** in `OxygenNotIncluded_Data/` and restart the game. Key debug hotkeys:

- **Backspace** - open debug tools
- **Ctrl+F4** - Instant Build Mode (free, instant construction)
- **Ctrl+F2** - spawn duplicant
- **Alt+M** - reload mods (limited hot-reload)
- **Alt+Z** - super speed
- **Alt+F1** - toggle UI

### Unity Explorer

**UnityExplorer** (github.com/sinai-dev/UnityExplorer) is invaluable for runtime inspection: browse all GameObjects, inspect/modify component properties live, execute C# in an in-game console, and click UI elements to inspect them. Install as a BepInEx plugin.

### Common error patterns and solutions

| Error | Cause | Solution |
|-|-|-|
| `NullReferenceException` in patches | Method signature changed after game update | Re-decompile latest `Assembly-CSharp.dll`, update parameter names |
| `Parameter 'X' not found in method` | Harmony can't match parameter name | Update patch parameter names to match current decompiled code |
| `MissingMethodException` | Referenced method removed or signature changed | Recompile against current game DLLs |
| `TypeLoadException` | Wrong version of referenced assemblies | Ensure all references point to current game's Managed folder |
| `First anim file needs to be non-null` | Building animation not set correctly | Verify `anim` parameter in `CreateBuildingDef()` matches actual kanim files |

### Debugging Harmony patches

Use `[HarmonyDebug]` on a patch class to log only that patch's IL manipulation, or set `Harmony.DEBUG = true;` globally (creates `harmony.log.txt` on Desktop). Use **Finalizer** patches to catch and log exceptions:

```csharp
static Exception Finalizer(Exception __exception)
{
    if (__exception != null)
        Debug.LogError($"[MyMod] Exception: {__exception}");
    return __exception;
}
```

### Testing workflow

1. Use **`mods/dev/`** folder for active development - mods here always load
2. Use **Ctrl+F4** (Instant Build) to quickly test building mods
3. Keep a test save that exercises your mod's features
4. Use **`PUtil.InitLibrary(true)`** to log your mod's version for confirming the correct DLL loaded
5. Start with `Debug.Log` everywhere, then remove when stable

---

## 10. Publishing to Steam Workshop

### Packaging your mod

Your mod folder needs at minimum:

```
MyMod/
├── mod_info.yaml     ← Required
├── mod.yaml          ← Strongly recommended
├── MyMod.dll         ← Your compiled mod
└── preview.png       ← Workshop preview image (recommended)
```

### Upload process

1. Install **"Oxygen Not Included Uploader"** from Steam → Library → Tools (App ID 636750)
2. **Close ONI** (DLLs will be locked otherwise)
3. Launch the uploader, click "Add", browse to your mod folder
4. Fill in title, description, preview image, and tags (`Base Game`, `Spaced Out!`, `Mods`)
5. Click "Publish!" - for updates, re-open, select existing mod, publish again
6. Set visibility to "Public" on your Steam Workshop profile

Your Steam account must have spent at least **$5 USD** to publish Workshop content.

### Versioning best practices

- Use **semantic versioning**: `major.minor.patch`
- Update `version` in `mod_info.yaml` with each release
- Update `minimumSupportedBuild` to the current game build when you test
- When a game update breaks your mod, archive the working version in `archived_versions/` and update the root for the new build - players on older builds automatically get the archived version
- Use consistent `staticID` across versions for reliable mod detection

---

## 11. Key API reference

### GameTags

`GameTags` contains `Tag` constants for categorizing entities:

```csharp
GameTags.Edible, GameTags.Medicine, GameTags.BuildableAny
GameTags.Solid, GameTags.Liquid, GameTags.Gas
GameTags.Metal, GameTags.RefinedMetal
GameTags.Creature, GameTags.LightSource, GameTags.Operational

// Usage
gameObject.GetComponent<KPrefabID>().HasTag(GameTags.Edible);
gameObject.GetComponent<KPrefabID>().AddTag(myTag);

// Custom tags
public static readonly Tag MyTag = TagManager.Create("MyModTag");
```

### Db class - the game database singleton

```csharp
Db db = Db.Get();

db.Techs               // Tech tree entries
db.TechItems           // Individual tech items
db.Skills              // Duplicant skills
db.Effects             // Buff/debuff effects
db.ChoreTypes          // Errand types
db.RoomTypes           // Room definitions
db.Amounts             // Trackable amounts (Stress, Calories, etc.)
db.BuildingStatusItems // Building status indicators
db.CreatureStatusItems // Creature status indicators
db.Accessories         // Duplicant accessories
db.Personalities       // Duplicant personalities
```

### StatusItems

```csharp
var myStatus = new StatusItem(
    "MyStatusId", "BUILDING", "",
    StatusItem.IconType.Info,
    NotificationType.Neutral,
    false, OverlayModes.None.ID);

myStatus.resolveStringCallback = (str, data) =>
    str.Replace("{0}", "some value");

selectable.AddStatusItem(myStatus, null);
```

### Assets class

```csharp
Assets.GetAnim("my_anim_kanim");     // Get animation file
Assets.GetPrefab(tag);               // Get prefab by tag
Assets.GetSprite("my_sprite_name");  // Get UI sprite
Assets.GetBuildingDef("BuildingId"); // Get building definition
```

### TUNING namespace

All game balance constants organized in nested classes:

```csharp
TUNING.BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER3  // 60 seconds
TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER3       // float[] { 200f }
TUNING.BUILDINGS.HITPOINTS.TIER2                   // 50
TUNING.CREATURES.LIFESPAN.TIER4
TUNING.CREATURES.CALORIES_PER_KG_OF_ORE
TUNING.FOOD.FOOD_CALORIES_PER_CYCLE
TUNING.DUPLICANTSTATS.STANDARD
TUNING.EQUIPMENT.SUITS
```

### Strings and localization

The `STRINGS` class hierarchy mirrors game entities. Search for in-game text to find code names - "Wheezewort" maps to `STRINGS.CREATURES.SPECIES.COLDBREATHER.NAME`, revealing the code name "Coldbreather".

```csharp
// Register strings for a custom building
Strings.Add("STRINGS.BUILDINGS.PREFABS.MYBUILDINGID.NAME", "My Building");
Strings.Add("STRINGS.BUILDINGS.PREFABS.MYBUILDINGID.DESC", "Description.");
Strings.Add("STRINGS.BUILDINGS.PREFABS.MYBUILDINGID.EFFECT", "Effect text.");

// PLib localization
new PLocalization().Register();
```

Translation files go in `strings/` as `.po` files named by language code (e.g., `zh-CN.po`).

### Event system - GameHashes

ONI uses a hash-based messaging system for game events:

```csharp
// Subscribe
gameObject.Subscribe((int)GameHashes.OperationalChanged, OnOperationalChanged);
gameObject.Subscribe((int)GameHashes.OnStorageChange, OnStorageChange);

// Common GameHashes
GameHashes.OperationalChanged    // Building operational state changed
GameHashes.OnStorageChange       // Storage contents changed
GameHashes.ActiveChanged         // Active state changed
GameHashes.LogicEvent            // Automation signal received
GameHashes.RefreshUserMenu       // User menu needs refresh
GameHashes.TagsChanged           // Entity tags changed
GameHashes.CopySettings          // Settings being copied
GameHashes.StatusChange          // Status indicator changed
GameHashes.BuildingActivated     // Building toggled on/off

// Trigger
gameObject.Trigger((int)GameHashes.OperationalChanged, data);

// Unsubscribe
gameObject.Unsubscribe(handleId);
```

### SimHashes - element identifiers

```csharp
SimHashes.Water, SimHashes.DirtyWater, SimHashes.SaltWater
SimHashes.Oxygen, SimHashes.CarbonDioxide, SimHashes.Hydrogen
SimHashes.Iron, SimHashes.Copper, SimHashes.Steel
SimHashes.Sand, SimHashes.Regolith, SimHashes.Obsidian

// Convert to Tag
SimHashes.Water.CreateTag()
```

### Entity component system essentials

ONI uses a component architecture on Unity's `GameObject` system:

```csharp
// Base class - use instead of MonoBehaviour
public class MyComponent : KMonoBehaviour
{
    [Serialize] public float myValue;  // Automatically saved/loaded

    protected override void OnPrefabInit() { }  // Called on instantiation
    protected override void OnSpawn() { }        // Called when entering world
    protected override void OnCleanUp() { }      // Called on destruction
}

// Key utility methods
gameObject.AddOrGet<T>()           // Add component or get existing
Grid.PosToCell(position)           // World position → cell index
Grid.CellToPos(cell)               // Cell index → world position
Grid.Element[cell]                 // Element at cell
Grid.Temperature[cell]             // Temperature at cell
Grid.Mass[cell]                    // Mass at cell
```

### Common interfaces

- **`IBuildingConfig`**: `CreateBuildingDef()`, `ConfigureBuildingTemplate()`, `DoPostConfigureComplete()`
- **`IEntityConfig`**: `CreatePrefab()`, `OnPrefabInit()`, `OnSpawn()`, `GetDlcIds()`
- **`KMod.UserMod2`**: `OnLoad(Harmony)`, `OnAllModsLoaded(Harmony, IReadOnlyList<Mod>)`

### Key serialization pattern

```csharp
[SerializationConfig(MemberSerialization.OptIn)]
public class MyComponent : KMonoBehaviour
{
    [Serialize] public float savedValue;
    [Serialize] public string savedString;

    protected override void OnSpawn()
    {
        base.OnSpawn();
        // savedValue and savedString are already deserialized here
    }
}
```

---

## 12. Community resources and ecosystem

### PLib (Peter Han's modding library)

The dominant community library. NuGet package `PLib 4.19.0+` (ILMerged into output automatically via build targets).

| Module | Purpose | Key Types |
|-|-|-|
| PLib.Core | Cross-mod event bus, shared data registry | `PUtil.InitLibrary()`, `PRegistry` |
| PLib.Options | In-game settings UI from attribute-decorated classes | `POptions`, `SingletonOptions<T>`, `[Option]` |
| PLib.Lighting | Custom light shapes for buildings | `PLightShape` |
| PLib.Actions | Custom key bindings registered with the game | `PAction`, `PActionManager` |
| PLib.UI | IMGUI-style UI panel builder | `PUIElements`, `PPanel`, `PButton` |
| PLib.Database | Localization string registration | `PLocalization` |
| PLib.Buildings | Building registration helpers | `PBuilding` |

**Usage pattern**: Call `PUtil.InitLibrary()` first in `OnLoad`, then register options with `new POptions().RegisterOptions(this, typeof(MyOptions))`. Options class uses `[JsonObject(MemberSerialization.OptIn)]` + `[JsonProperty]` on fields, `[Option("Label", "Tooltip")]` for UI, and inherits `SingletonOptions<T>` for static access.

### Reference repositories

| Repository | What It Offers |
|-|-|
| **Peter Han's ONIMods** (github.com/peterhaneve/ONIMods) | PLib source + 20+ production mods. Best reference for patterns |
| **Truinto's SimpleMods** (github.com/Truinto/ONI-Modloader-SimpleMods) | JSON-configurable building/geyser/recipe customization |
| **Sanchozz's Mods** (github.com/SanchozzDepon662/ONIMods) | Advanced Harmony patterns, complex game system patches |
| **Cairath's Modding Wiki** (github.com/Cairath/Oxygen-Not-Included-Modding/wiki) | Most comprehensive community guide, post-Mergedown |
| **Aki's Mods** (github.com/AkisExtraTwitchEvents) | Event-driven architecture, Twitch integration patterns |

### Development tools

| Tool | Best For |
|-|-|
| **ILSpy** (GUI, github.com/icsharpcode/ILSpy) | Browsing game classes interactively |
| **ilspycmd** (CLI) | Scripted decompilation: `ilspycmd - t TypeName - r ManagedDir` |
| **dnSpy** (GUI, no longer maintained) | IL + C# side-by-side viewing. Still works fine |
| **dotPeek** (JetBrains, free) | Rider integration, search across assemblies |
| **Kanim Explorer** (github.com/romen-h/kanim-explorer) | GUI tool for viewing/editing ONI animation files |

### Community channels

- **ONI Modding Discord** (linked from Klei Forums) - most active source of current help, real-time troubleshooting
- **Klei Forums - Modding subforum** (forums.kleientertainment.com) - official venue, slower but archived and searchable
- **Harmony 2.0 docs** (harmony.pardeike.net) - full patching library reference: prefixes, postfixes, transpilers, finalizers

### Modding infrastructure (post-Mergedown)

Since the 2021 Mergedown update, ONI has built-in mod loading. No external mod loader is needed:

- **`mod_info.yaml`**: Declares `supportedContent` (ALL, VANILLA_ID, EXPANSION1_ID) and `APIVersion: 2`
- **`mod.yaml`**: Title, description, `staticID` for Workshop identification
- **Mod load order**: Game scans `Documents/Klei/OxygenNotIncluded/mods/local/` and Steam Workshop folders, loads DLLs matching `mod_info.yaml`, calls `OnLoad(Harmony)` on `UserMod2` subclasses
- **No IL2CPP**: ONI ships standard Mono/.NET assemblies - direct Harmony patching works without extra tooling
- **PLib dominance**: Most mods use PLib for options/settings. Few other shared libraries exist since PLib covers the common needs