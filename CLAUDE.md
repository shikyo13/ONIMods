# ONI Mods — Multi-Mod Workspace

C# class libraries using Harmony 2.0 to patch Oxygen Not Included at runtime. .NET Framework 4.7.1, `using HarmonyLib;`, `APIVersion: 2`.

## Key Directories

| Path | Purpose |
|-|-|
| `ReplaceStuff/` | Furniture replacement mod — active |
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
| Editing ReplaceStuff | `ReplaceStuff/HANDOVER.md` + `docs/tier2-replacestuff-design.md` |
| Editing BuildThrough | `BuildThrough/HANDOVER.md` + `docs/tier2-buildthrough-design.md` |
| Editing OniProfiler | `OniProfiler/HANDOVER.md` |
| Editing GCBudget | `GCBudget/HANDOVER.md` |
| Editing DuplicantStatusBar | `DuplicantStatusBar/HANDOVER.md` |
| Lag spike investigation context | `docs/tier2-lag-investigation.md` |
| ONI API/Harmony/building questions | `docs/ONI-modding-guide.md` — use section index at top, read only relevant section |
| Creating a new HANDOVER | `docs/HANDOVER-TEMPLATE.md` |

## When Releasing a Version

- [ ] Update mod's `HANDOVER.md` (architecture + version)
- [ ] Update/create `CHANGELOG.txt` (BBCode, newest first)
- [ ] Update/create `workshop-description.txt` (Steam BBCode)
- [ ] Bump version in `docs/tier1-quickref.md`
- [ ] Update `MEMORY.md` project state entry
