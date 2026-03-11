# GameModding Workspace & RE Toolkit Design

**Date:** 2026-03-11
**Status:** Approved

## Problem

RE toolkit (3 agents, 5 skills, 5 MCP servers) is installed globally — loads into every Claude Code session including webapps and desktop apps. No shared documentation for engine-specific RE workflows. Game mod projects scattered across `D:\Dev\Projects\` with inconsistent naming.

## Solution

Create `D:\Dev\Projects\GameModding\` as a parent workspace with the RE toolkit installed project-locally. Shared engine/tool docs at the workspace level, game-specific docs at each game folder level. 8 existing game mod projects consolidated under it.

---

## 1. Workspace Structure

```
D:\Dev\Projects\GameModding\                    ← git repo (shared toolkit)
├── .claude/
│   └── plugins/local/re-game-hacking/          ← RE plugin (moved from global)
│       ├── .claude-plugin/plugin.json
│       ├── agents/
│       │   ├── re-analyst.md                   ← enhanced with .NET workflows
│       │   ├── memory-hunter.md
│       │   ├── mod-builder.md                  ← enhanced with UserMod2/PLib
│       │   └── asset-explorer.md               ← NEW
│       └── skills/
│           ├── analyze-assembly/SKILL.md
│           ├── find-value/SKILL.md
│           ├── trace-to-code/SKILL.md
│           ├── generate-mod/SKILL.md
│           ├── new-project/SKILL.md
│           ├── compare-assemblies/SKILL.md      ← NEW
│           ├── dump-type/SKILL.md               ← NEW
│           └── find-hooks/SKILL.md              ← NEW
├── .mcp.json                                   ← RE MCP servers (moved from global)
├── CLAUDE.md                                   ← shared conventions + doc routing
├── .gitignore                                  ← `*Mods/` wildcard + explicit un-ignores for tracked files
├── docs/
│   ├── tier1-re-quickref.md                    ← tool inventory, decision tree (~100 lines)
│   ├── engines/
│   │   ├── unity-mono.md                       ← ONI, Phasmo, RimWorld, Subnautica
│   │   ├── unity-il2cpp.md                     ← IL2CPP backend games
│   │   ├── unity-assets.md                     ← AssetStudio, kanim, textures
│   │   ├── unity-runtime.md                    ← UnityExplorer, BepInEx, runtime inspection
│   │   ├── unreal.md                           ← Unreal Engine games
│   │   ├── godot.md                            ← Godot engine games
│   │   ├── java-minecraft.md                   ← Fabric/Forge, MCP mappings
│   │   ├── java-zomboid.md                     ← Zomboid Lua + Java modding
│   │   ├── source2.md                          ← Source 2 engine (future)
│   │   └── monogame-smapi.md                   ← Stardew Valley / SMAPI
│   ├── frameworks/
│   │   ├── harmony.md                          ← Harmony 2.0 deep-dive
│   │   ├── bepinex.md                          ← BepInEx plugin lifecycle
│   │   └── frida.md                            ← Frida scripting patterns
│   ├── tools/
│   │   ├── ghidra.md                           ← static analysis workflows
│   │   ├── cheat-engine.md                     ← memory scanning, AOB, tables
│   │   ├── dotnet-decompilation.md             ← ilspycmd, dnSpy, dotPeek
│   │   └── x64dbg.md                           ← dynamic debugging
│   └── toolkit/
│       ├── agents.md                           ← agent usage guide for Claude
│       ├── skills.md                           ← slash command reference for Claude
│       ├── workflows.md                        ← end-to-end recipes for Claude
│       ├── mcp-servers.md                      ← MCP server inventory for Claude
│       └── commands.md                         ← helper scripts/CLI commands
│
├── ONIMods/                                    ← independent git repo
├── PhasmoMods/                                 ← independent git repo
├── MCMods/                                     ← independent git repo
├── RimWorldMods/                               ← independent git repo
├── StardewMods/                                ← independent git repo
├── SubnauticaMods/                             ← independent git repo
├── ZomboidMods/                                ← independent git repo
└── (future games)/
```

## 2. Documentation Architecture

### Two-level split

**GameModding level** — engine-specific RE, modding frameworks, tool workflows, toolkit usage. Shared across all games.

**Game level** — game-specific APIs, modding guides, gotchas, breaking changes. Each game folder owns its own docs.

### Decision rule

| Content type | Level |
|-|-|
| "How do I decompile a Unity Mono DLL?" | GameModding/docs/engines/ |
| "How does Harmony transpiler work?" | GameModding/docs/frameworks/ |
| "How do I scan memory for a value?" | GameModding/docs/tools/ |
| "What is ONI's BuildingDef API?" | ONIMods/docs/ |
| "How do I add a building to ONI?" | ONIMods/docs/ |
| "What broke in the last ONI update?" | ONIMods/docs/ |

### Tiering

- **Tier 1** (~100 lines, every session): `tier1-re-quickref.md` — tool inventory, agent/skill decision tree
- **Tier 2** (~100-200 lines each, on demand): engine/framework/tool/toolkit docs, loaded via CLAUDE.md routing
- **Tier 3** (reference only): full MCP tool catalogs, detailed API docs

### GameModding CLAUDE.md routing table

```markdown
| When | Read |
|-|-|
| Every session | docs/tier1-re-quickref.md |
| Unity Mono game | docs/engines/unity-mono.md |
| Unity IL2CPP game | docs/engines/unity-il2cpp.md |
| Unity assets/animations | docs/engines/unity-assets.md |
| Unity runtime inspection | docs/engines/unity-runtime.md |
| Unreal Engine game | docs/engines/unreal.md |
| Godot engine game | docs/engines/godot.md |
| Minecraft modding | docs/engines/java-minecraft.md |
| Project Zomboid modding | docs/engines/java-zomboid.md |
| Stardew Valley modding | docs/engines/monogame-smapi.md |
| Source 2 engine game | docs/engines/source2.md |
| Writing Harmony patches | docs/frameworks/harmony.md |
| Setting up BepInEx | docs/frameworks/bepinex.md |
| Writing Frida scripts | docs/frameworks/frida.md |
| Static analysis (Ghidra) | docs/tools/ghidra.md |
| Memory scanning (CE) | docs/tools/cheat-engine.md |
| .NET decompilation | docs/tools/dotnet-decompilation.md |
| Dynamic debugging (x64dbg) | docs/tools/x64dbg.md |
| Using an agent | docs/toolkit/agents.md |
| Looking up a slash command | docs/toolkit/skills.md |
| Full workflow recipe | docs/toolkit/workflows.md |
| MCP server setup/issues | docs/toolkit/mcp-servers.md |
| Helper scripts | docs/toolkit/commands.md |
| Working on a specific game | <GameFolder>/CLAUDE.md |
```

### Cairath wiki gaps → destinations

| Gap | Destination |
|-|-|
| Modding ethics / IP policy | GameModding/CLAUDE.md |
| Decompiling workflow (step-by-step) | GameModding/docs/engines/unity-mono.md |
| UserMod2 full lifecycle | ONIMods/docs/ONI-modding-guide.md |
| Animations (kanim pipeline) | ONIMods/docs/ONI-modding-guide.md |
| API breaking changes | ONIMods/docs/tier1-quickref.md |

## 3. Plugin Enhancements

### New skills

| Skill | Purpose |
|-|-|
| `/compare-assemblies` | Diff two DLL versions after game update — show added/removed/changed types and methods |
| `/dump-type` | Single-type deep dive — decompile with all fields, methods, base classes, interfaces |
| `/find-hooks` | Given a gameplay goal, search assemblies for candidate methods to patch |

### New agent

| Agent | Purpose |
|-|-|
| `asset-explorer` | Unity asset extraction/inspection — AssetStudio, kanim analysis, texture/animation workflows |

### Agent upgrades

| Agent | Enhancement |
|-|-|
| `re-analyst` | Add .NET/Mono workflow as first-class path (ilspycmd, re-orchestrator .NET tools) |
| `mod-builder` | Add UserMod2 + PLib template alongside BepInEx; detect framework from project context |

### Not adding (YAGNI)

- No anti-obfuscation agent (no actual obfuscation encountered)
- No dedicated patcher agent (mod-builder covers this)
- No IL2CPP-specific agent (re-analyst's IL2CPP workflow is sufficient)

## 4. MCP Server Configuration

### Project-local `.mcp.json`

All 5 MCP servers move from global config to `GameModding/.mcp.json`:

| Server | Used by |
|-|-|
| re-orchestrator | All agents, all skills |
| ghidra | re-analyst |
| cheatengine | memory-hunter, re-analyst |
| frida-game-hacking | memory-hunter, mod-builder |
| x64dbg | re-analyst |

MCP tool schemas are already deferred (lazy-loaded via ToolSearch). The win is not connecting servers at all outside GameModding sessions.

## 5. Game Folder Migration

### Moves and renames

| Current path | New path |
|-|-|
| `D:\Dev\Projects\ONI Mods\` | `GameModding\ONIMods\` |
| `D:\Dev\Projects\PhasmoMods\` | `GameModding\PhasmoMods\` |
| `D:\Dev\Projects\MC Mods\` | `GameModding\MCMods\` |
| `D:\Dev\Projects\RimThreaded\` | `GameModding\RimWorldMods\` |
| `D:\Dev\Projects\StardewMods\` | `GameModding\StardewMods\` |
| `D:\Dev\Projects\SubnauticaMods\` | `GameModding\SubnauticaMods\` |
| `D:\Dev\Projects\subnautica-efool-seaglide-sprint\` | merged into `GameModding\SubnauticaMods\` |
| `D:\Dev\Projects\ZomboidMods\` | `GameModding\ZomboidMods\` |

### Not moving (game dev, not modding)

`Dota2AI/`, `PokeAI/`, `GODOT Games/`, `MetalSnake/`, `Asteroids/`

### Post-move fixups

- Update absolute paths in `.csproj` files (if any reference old location)
- Update MEMORY.md and CLAUDE.md path references
- Verify `git log` works in each moved repo
- Reopen IDE/projects from new paths

## 6. Migration Phases

### Phase 1: Create workspace
1. Create `D:\Dev\Projects\GameModding\`, `git init`
2. Create `.gitignore` ignoring all game subfolders
3. Move and rename 7 game mod folders (see Section 5 table)
4. Merge `subnautica-efool-seaglide-sprint/` into `SubnauticaMods/` (8th source → existing destination)
5. Verify `git log` in each moved repo

### Phase 2: Install RE plugin locally
1. Copy `re-game-hacking` plugin from `~/.claude/plugins/cache/local/re-game-hacking/1.0.0/` into `GameModding/.claude/plugins/local/re-game-hacking/`
2. Move 5 skills from `~/.claude/skills/{analyze-assembly,find-value,trace-to-code,generate-mod,new-project}/` into plugin `skills/` folder
3. Update `plugin.json` to register skills
4. Create `GameModding/.mcp.json` with all 5 MCP server configs
5. Verify: open Claude Code at `GameModding/`, confirm agents and skills load

### Phase 3: Write shared docs
1. Create `GameModding/CLAUDE.md` with routing table
2. Create `docs/tier1-re-quickref.md`
3. Create engine docs (10 files)
4. Create framework docs (3 files)
5. Create tool docs (4 files)
6. Create toolkit docs (5 files)

### Phase 4: Enhance plugin
1. Add 3 new skills: `compare-assemblies`, `dump-type`, `find-hooks`
2. Add new agent: `asset-explorer`
3. Enhance `re-analyst` with .NET-first workflows
4. Enhance `mod-builder` with UserMod2/PLib templates

### Phase 5: Enhance ONI Mods docs
1. Fill Cairath wiki gaps in `ONI-modding-guide.md` (UserMod2 lifecycle, animations)
2. Add modding ethics reference to CLAUDE.md (pointing to parent)
3. Populate Breaking Changes section in `tier1-quickref.md`
4. Update ONIMods CLAUDE.md to reference parent routing

### Phase 6: Clean up global config
1. Remove RE MCP servers from global config
2. Remove `~/.claude/skills/` RE skills (5 folders)
3. Remove `~/.claude/agents/re-analyst.md`
4. Remove global plugin reference to `re-game-hacking`
5. Verify: non-game sessions have no RE tools loaded

### Ordering
Phases 1-2 first (structure working), then 3-5 in parallel (docs + enhancements independent), then 6 last (only after verified).

### Risk mitigation

| Risk | Mitigation |
|-|-|
| Git history broken by folder move | Git tracks content by hash — history follows. Verify with `git log` |
| Absolute paths in .csproj break | Audit HintPaths after move; game DLL refs use Steam path (unchanged) |
| Skills don't load from plugin folder | Test after Phase 2; keep global copies until confirmed |
| MCP servers fail from project-local config | Format identical to global — test in isolation |
