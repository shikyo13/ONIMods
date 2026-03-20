# Parent workspace: See `../CLAUDE.md` for shared RE toolkit, engine docs, and modding conventions.

# ONI Mods  - Multi-Mod Workspace

C# class libraries using Harmony 2.0 to patch Oxygen Not Included at runtime. .NET Framework 4.7.1, `using HarmonyLib;`, `APIVersion: 2`.

## Key Directories

| Path | Purpose |
|-|-|
| `ReplaceStuff/` | Furniture replacement mod  - active |
| `BuildThrough/` | Build/deconstruct through walls |
| `OniProfiler/` | Real-time performance profiler (F8 toggle) |
| `GCBudget/` | Alloc-gated GC collection mod (POC) |
| `DuplicantStatusBar/` | Persistent dupe status bar HUD (colonist bar) |
| `docs/` | Tiered documentation (see routing table below) |

## Build & Deploy

```bash
# Build any mod (from workspace root)
dotnet build <ModName>/<ModName>.csproj

# Game DLLs referenced from:
D:\SteamLibrary\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed\

# Deploy to:
%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\dev\<ModName>\

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
- `AddOrGet<T>()` for attaching components  - never raw `AddComponent` without null-check
- KSerialization: `[SerializationConfig(MemberSerialization.OptIn)]` + `[Serialize]` on fields

## Game Data Library

**Before decompiling any game type, check `docs/data/_index.md` first.** 28 indexed reference tables cover amounts, elements, buildings, foods, techs, critters, plants, geysers, equipment, overlays, traits, skills, chore types, effects, rooms, status items, expressions, accessories, diseases, schedules, notifications, actions, UI layers, components, and PLib options. Only decompile when the data library doesn't cover what you need.

## Documentation

| When | Read |
|-|-|
| Every session | `docs/tier1-quickref.md` (~106 lines) |
| Need game data (stats, IDs, enums, configs) | `docs/data/_index.md` - check index, read specific file. Do NOT decompile |
| Editing ReplaceStuff | `ReplaceStuff/HANDOVER.md` + `docs/tier2-replacestuff-design.md` |
| Editing BuildThrough | `BuildThrough/HANDOVER.md` + `docs/tier2-buildthrough-design.md` |
| Editing OniProfiler | `OniProfiler/HANDOVER.md` |
| Editing GCBudget | `GCBudget/HANDOVER.md` |
| Editing DuplicantStatusBar | `DuplicantStatusBar/HANDOVER.md` |
| Lag spike investigation context | `docs/tier2-lag-investigation.md` (summary) |
| Lag spike full recording/experiment data | `docs/tier3-lag-investigation-data.md` - use section index |
| ONI API/Harmony/building questions | `docs/tier3-modding-guide.md` - use section index at top, read only relevant section |
| Creating a new HANDOVER | `docs/HANDOVER-TEMPLATE.md` |

## When Releasing a Version

### Versioning (SemVer)

All version numbers use `MAJOR.MINOR.PATCH` (e.g., v2.8.3). Keep these in sync:
- `<AssemblyVersion>` and `<FileVersion>` in the `.csproj` (append `.0` for 4-part: `2.8.3.0`)
- HANDOVER.md version header
- CHANGELOG.txt newest entry
- `docs/tier1-quickref.md` mod index

Increment rules:
- **PATCH** (2.8.2 -> 2.8.3): bugfixes, crash fixes, threshold tweaks
- **MINOR** (2.8.x -> 2.9.0): new features, new alerts, UI additions
- **MAJOR** (2.x -> 3.0.0): breaking API changes, architecture rewrites

**Never ship a DLL with a stale AssemblyVersion.** Mod Updater reads `AssemblyVersion` from the DLL for version tracking. A mismatch causes update detection confusion and can trigger restart loops.

### PLib Config

Always use `[ConfigFile("ModName.json", false, true)]` (SharedConfigLocation) on the options class. Never let PLib write `config.json` into the mod's own folder - it triggers Mod Updater/Steam change detection for Workshop mods.

### Release Checklist

- [ ] Bump `AssemblyVersion` + `FileVersion` in `.csproj`
- [ ] Update mod's `HANDOVER.md` (architecture + version)
- [ ] Update/create `CHANGELOG.txt` (BBCode, newest first)
- [ ] Update/create `workshop-description.txt` (Steam BBCode)
- [ ] Bump version in `docs/tier1-quickref.md`
- [ ] Build Release config, verify DLL version matches
- [ ] Update `MEMORY.md` project state entry
