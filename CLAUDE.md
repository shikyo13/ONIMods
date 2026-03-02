# ONI Mods — Multi-Mod Workspace

C# class libraries using Harmony 2.0 to patch Oxygen Not Included at runtime. .NET Framework 4.7.1, `using HarmonyLib;`, `APIVersion: 2`.

## Key Directories

| Path | Purpose |
|-|-|
| `ReplaceStuff/` | Furniture replacement mod — active, source on `v2-vanilla-replacement` branch |
| `BuildThrough/` | Build/deconstruct through walls — source on `build-through` branch |
| `ReplaceTool/` | Legacy replacement mod — superseded by ReplaceStuff |
| `docs/` | Tiered documentation (see routing table below) |

## Build & Deploy

```bash
# Build any mod (from workspace root)
dotnet build <ModName>/<ModName>.csproj

# Game DLLs referenced from:
D:\SteamLibrary\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed\

# Deploy to:
%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\local\<ModName>\

# Logs:
%APPDATA%\..\LocalLow\Klei\Oxygen Not Included\Player.log
```

## Coding Conventions

- Target .NET Framework 4.7.1, LangVersion 8.0
- All game DLL references: `<Private>false</Private>` (never bundle)
- Entry point: subclass `KMod.UserMod2`, override `OnLoad(Harmony)`
- PLib: init with `PUtil.InitLibrary()` in OnLoad, options via `POptions`
- Namespace matches folder structure: `ReplaceStuff.Core`, `BuildThrough.Patches`, etc.
- Harmony patches: use `[HarmonyPatch]` attributes, all patch methods `static`
- Prefer postfix patches over prefix unless you need to skip the original
- `AddOrGet<T>()` for attaching components — never raw `AddComponent` without null-check
- KSerialization: `[SerializationConfig(MemberSerialization.OptIn)]` + `[Serialize]` on fields

## Documentation

| When | Read |
|-|-|
| Every session | `docs/tier1-quickref.md` (~100 lines) |
| Editing ReplaceStuff | `docs/tier2-replacestuff-design.md` |
| Editing BuildThrough | `docs/tier2-buildthrough-design.md` |
| ONI API/Harmony/building questions | `docs/ONI-modding-guide.md` — use section index at top, read only relevant section |
