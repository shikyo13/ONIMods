# GameModding Workspace & RE Toolkit Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate 8 game mod projects under a shared `GameModding/` workspace with project-local RE toolkit, tiered documentation, and enhanced agents/skills.

**Architecture:** Parent git repo (`GameModding/`) owns the RE plugin, MCP config, shared docs, and CLAUDE.md routing. Each game folder is an independent nested git repo (git-ignored by parent). RE tools only load in GameModding sessions.

**Tech Stack:** Claude Code plugins (markdown agents/skills), MCP servers (Python), tiered markdown documentation, git

**Spec:** `docs/superpowers/specs/2026-03-11-game-modding-workspace-design.md`

---

## Chunk 1: Workspace Creation & Folder Migration (Phase 1)

### Task 1: Create GameModding workspace

**Files:**
- Create: `D:\Dev\Projects\GameModding\.gitignore`

- [ ] **Step 1: Create directory and initialize git**

```bash
mkdir -p "D:/Dev/Projects/GameModding"
cd "D:/Dev/Projects/GameModding"
git init
```

- [ ] **Step 2: Create .gitignore**

Write `D:\Dev\Projects\GameModding\.gitignore`:

```gitignore
# Game mod projects (independent git repos) — wildcard catches future additions
*Mods/

# OS / IDE
.vs/
*.suo
*.user
Thumbs.db
Desktop.ini
```

- [ ] **Step 3: Initial commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add .gitignore
git commit -m "Initialize GameModding workspace"
```

### Task 2: Move and rename game mod folders

**Context:** Each folder is an independent git repo. Moving doesn't affect git history. The `.git/` directory inside each folder stays intact.

- [ ] **Step 1: Move ONI Mods (rename to ONIMods)**

```bash
mv "D:/Dev/Projects/ONI Mods" "D:/Dev/Projects/GameModding/ONIMods"
```

- [ ] **Step 2: Move PhasmoMods**

```bash
mv "D:/Dev/Projects/PhasmoMods" "D:/Dev/Projects/GameModding/PhasmoMods"
```

- [ ] **Step 3: Move MC Mods (rename to MCMods)**

```bash
mv "D:/Dev/Projects/MC Mods" "D:/Dev/Projects/GameModding/MCMods"
```

- [ ] **Step 4: Move RimThreaded (rename to RimWorldMods)**

```bash
mv "D:/Dev/Projects/RimThreaded" "D:/Dev/Projects/GameModding/RimWorldMods"
```

- [ ] **Step 5: Move StardewMods**

```bash
mv "D:/Dev/Projects/StardewMods" "D:/Dev/Projects/GameModding/StardewMods"
```

- [ ] **Step 6: Move SubnauticaMods**

```bash
mv "D:/Dev/Projects/SubnauticaMods" "D:/Dev/Projects/GameModding/SubnauticaMods"
```

- [ ] **Step 7: Merge subnautica-efool-seaglide-sprint into SubnauticaMods**

```bash
# Move the standalone mod folder into SubnauticaMods as a subfolder
mv "D:/Dev/Projects/subnautica-efool-seaglide-sprint" "D:/Dev/Projects/GameModding/SubnauticaMods/seaglide-sprint"
```

If `subnautica-efool-seaglide-sprint` has its own `.git/` directory, remove it before moving (`rm -rf .git` inside it) so it becomes a plain folder within SubnauticaMods. Then `git add` and commit it into SubnauticaMods' repo. If the user wants to preserve its history, use `git log --oneline` to capture it in a commit message before removing `.git/`.

- [ ] **Step 8: Move ZomboidMods**

```bash
mv "D:/Dev/Projects/ZomboidMods" "D:/Dev/Projects/GameModding/ZomboidMods"
```

### Task 3: Verify moved repos

- [ ] **Step 1: Verify git log works in each moved repo**

```bash
for dir in ONIMods PhasmoMods MCMods RimWorldMods StardewMods SubnauticaMods ZomboidMods; do
  echo "=== $dir ==="
  cd "D:/Dev/Projects/GameModding/$dir"
  git log --oneline -3
  cd ..
done
```

Expected: Each repo shows its recent commits, confirming history is intact.

- [ ] **Step 2: Audit ONIMods for absolute path references**

Use the Grep tool (not bash grep) to search `D:/Dev/Projects/GameModding/ONIMods` for patterns:
- `D:\\Dev\\Projects\\ONI Mods` and `D:/Dev/Projects/ONI Mods`
- Search in `*.csproj`, `*.md`, `*.json` files

Fix any found references to use the new path `D:\Dev\Projects\GameModding\ONIMods`. Game DLL HintPaths point to Steam (`D:\SteamLibrary\...`) and should be unaffected.

- [ ] **Step 3: Update ONIMods MEMORY.md path references**

Edit `C:\Users\Zero\.claude\projects\d--Dev-Projects-ONI-Mods\memory\MEMORY.md` — update any path references from `D:\Dev\Projects\ONI Mods` to `D:\Dev\Projects\GameModding\ONIMods`. Note: The Claude Code project memory directory name may also need updating (Claude Code uses the working directory path as the project key).

---

## Chunk 2: RE Plugin Migration (Phase 2)

### Task 4: Set up RE plugin for GameModding project

**Context:** Claude Code uses `~/.claude/plugins/installed_plugins.json` as its plugin registry. Each entry has a `projectPath` that scopes it. The current RE plugin is scoped to `C:\Users\Zero` (effectively global). We need to:
1. Create the plugin source in the GameModding project
2. Register it in `installed_plugins.json` scoped to `D:\Dev\Projects\GameModding`

**Files:**
- Create: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\` (entire plugin tree)
- Modify: `C:\Users\Zero\.claude\plugins\installed_plugins.json`

- [ ] **Step 1: Create plugin directory structure**

```bash
mkdir -p "D:/Dev/Projects/GameModding/.claude/plugins/local/re-game-hacking/.claude-plugin"
mkdir -p "D:/Dev/Projects/GameModding/.claude/plugins/local/re-game-hacking/agents"
mkdir -p "D:/Dev/Projects/GameModding/.claude/plugins/local/re-game-hacking/skills"
```

- [ ] **Step 2: Copy agent files**

```bash
cp "C:/Users/Zero/.claude/plugins/cache/local/re-game-hacking/1.0.0/agents/"*.md \
   "D:/Dev/Projects/GameModding/.claude/plugins/local/re-game-hacking/agents/"
```

This copies: `re-analyst.md`, `memory-hunter.md`, `mod-builder.md`

- [ ] **Step 3: Write plugin.json**

Write `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\.claude-plugin\plugin.json` (marketplace.json is NOT needed for project-local plugins — it's only for marketplace registry):

```json
{
  "name": "re-game-hacking",
  "version": "2.0.0",
  "description": "Reverse engineering agents for game hacking: binary analysis, memory hunting, and mod generation. Designed for solo use or as an agent team.",
  "author": {
    "name": "Zero"
  },
  "agents": [
    "./agents/re-analyst.md",
    "./agents/memory-hunter.md",
    "./agents/mod-builder.md"
  ]
}
```

- [ ] **Step 4: Register plugin in installed_plugins.json**

Edit `C:\Users\Zero\.claude\plugins\installed_plugins.json`. In the `plugins` object, update the `re-game-hacking@local` entry to point to GameModding:

```json
"re-game-hacking@local": [
  {
    "scope": "project",
    "projectPath": "D:\\Dev\\Projects\\GameModding",
    "installPath": "D:\\Dev\\Projects\\GameModding\\.claude\\plugins\\local\\re-game-hacking",
    "version": "2.0.0",
    "installedAt": "2026-03-11T00:00:00.000Z",
    "lastUpdated": "2026-03-11T00:00:00.000Z"
  }
]
```

This scopes the plugin to only load when Claude Code is opened at or under `D:\Dev\Projects\GameModding`.

- [ ] **Step 5: Verify plugin structure**

```bash
find "D:/Dev/Projects/GameModding/.claude/plugins/local/re-game-hacking" -type f
```

Expected files:
```
.claude-plugin/plugin.json
agents/re-analyst.md
agents/memory-hunter.md
agents/mod-builder.md
```

### Task 5: Move skills into plugin

**Files:**
- Create: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\skills\` (5 skill folders)
- Modify: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\.claude-plugin\plugin.json`

- [ ] **Step 1: Copy skills from global into plugin**

```bash
for skill in analyze-assembly find-value trace-to-code generate-mod new-project; do
  mkdir -p "D:/Dev/Projects/GameModding/.claude/plugins/local/re-game-hacking/skills/$skill"
  cp "C:/Users/Zero/.claude/skills/$skill/SKILL.md" \
     "D:/Dev/Projects/GameModding/.claude/plugins/local/re-game-hacking/skills/$skill/SKILL.md"
done
```

- [ ] **Step 2: Update plugin.json to register skills**

Write updated `plugin.json`:

```json
{
  "name": "re-game-hacking",
  "version": "2.0.0",
  "description": "Reverse engineering agents for game hacking: binary analysis, memory hunting, and mod generation. Designed for solo use or as an agent team.",
  "author": {
    "name": "Zero"
  },
  "agents": [
    "./agents/re-analyst.md",
    "./agents/memory-hunter.md",
    "./agents/mod-builder.md"
  ],
  "skills": [
    "./skills/analyze-assembly",
    "./skills/find-value",
    "./skills/trace-to-code",
    "./skills/generate-mod",
    "./skills/new-project"
  ]
}
```

- [ ] **Step 3: Verify skill files exist**

```bash
find "D:/Dev/Projects/GameModding/.claude/plugins/local/re-game-hacking/skills" -name "SKILL.md"
```

Expected: 5 SKILL.md files.

### Task 6: Create project-local MCP config

**Files:**
- Create: `D:\Dev\Projects\GameModding\.mcp.json`

- [ ] **Step 1: Write .mcp.json**

Copy from `C:\Users\Zero\re-mcp-toolkit\.mcp.json` — the exact same server definitions:

```json
{
  "mcpServers": {
    "cheatengine": {
      "command": "C:/Python313/python.exe",
      "args": ["D:/AI/MCP Servers/cheatengine-mcp-bridge/MCP_Server/mcp_cheatengine.py"]
    },
    "ghidra": {
      "command": "C:/Users/Zero/re-mcp-toolkit/venv/Scripts/python.exe",
      "args": [
        "C:/Users/Zero/re-mcp-toolkit/GhidraMCP/bridge_mcp_ghidra.py",
        "--ghidra-server", "http://127.0.0.1:8080/"
      ]
    },
    "frida-game-hacking": {
      "command": "C:/Users/Zero/re-mcp-toolkit/venv/Scripts/python.exe",
      "args": ["-m", "frida_game_hacking_mcp"],
      "cwd": "C:/Users/Zero/re-mcp-toolkit"
    },
    "x64dbg": {
      "command": "C:/Users/Zero/re-mcp-toolkit/venv/Scripts/python.exe",
      "args": ["C:/Users/Zero/re-mcp-toolkit/x64dbgMCP/src/x64dbg.py"]
    },
    "re-orchestrator": {
      "command": "C:/Users/Zero/re-mcp-toolkit/venv/Scripts/python.exe",
      "args": ["C:/Users/Zero/re-mcp-toolkit/orchestrator/run_server.py"]
    }
  }
}
```

- [ ] **Step 2: Commit plugin and MCP config**

```bash
cd "D:/Dev/Projects/GameModding"
git add .claude/ .mcp.json
git commit -m "Add RE plugin (local) and MCP server config"
```

- [ ] **Step 3: Verify — open Claude Code at GameModding and test**

Open a new Claude Code session at `D:\Dev\Projects\GameModding\`. Verify:
1. Agents appear: re-analyst, memory-hunter, mod-builder
2. Skills appear: /analyze-assembly, /find-value, /trace-to-code, /generate-mod, /new-project
3. MCP servers listed in deferred tools

If anything fails, check plugin directory structure matches Claude Code's expected layout. Keep global copies as fallback until confirmed.

---

## Chunk 3: Shared Documentation — CLAUDE.md + Tier 1 (Phase 3, part 1)

### Task 7: Create GameModding CLAUDE.md

**Files:**
- Create: `D:\Dev\Projects\GameModding\CLAUDE.md`

- [ ] **Step 1: Write CLAUDE.md**

```markdown
# GameModding — RE & Modding Workspace

Multi-game modding workspace with shared RE toolkit. Engine-specific docs at this level; game-specific APIs and guides live in each game folder.

## Modding Ethics

- Never distribute decompiled game code or assets
- Never charge money for mods (donations OK if they don't gate features)
- Respect other modders' work — check licenses before reusing code
- When uncertain, ask the community first

## Documentation Routing

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

## Game Folders

| Folder | Game | Engine |
|-|-|-|
| ONIMods/ | Oxygen Not Included | Unity (Mono) |
| PhasmoMods/ | Phasmophobia | Unity |
| MCMods/ | Minecraft | Java (Fabric/Forge) |
| RimWorldMods/ | RimWorld | Unity (Mono) |
| StardewMods/ | Stardew Valley | .NET (SMAPI) |
| SubnauticaMods/ | Subnautica | Unity (Mono) |
| ZomboidMods/ | Project Zomboid | Java (Lua modding) |

## Conventions

- Save RE findings via `re-orchestrator:save_finding` for cross-session persistence
- Use `/new-project` skill when starting work on a new game target
- Each game folder has its own CLAUDE.md with game-specific routing and conventions
```

- [ ] **Step 2: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add CLAUDE.md
git commit -m "Add CLAUDE.md with routing table and conventions"
```

### Task 8: Create tier1-re-quickref.md

**Files:**
- Create: `D:\Dev\Projects\GameModding\docs\tier1-re-quickref.md`

- [ ] **Step 1: Create docs directory**

```bash
mkdir -p "D:/Dev/Projects/GameModding/docs"
```

- [ ] **Step 2: Write tier1-re-quickref.md**

Target: ~100 lines. Decision tree for which tool/agent/skill to use. Note: asset-explorer agent and 3 new skills (compare-assemblies, dump-type, find-hooks) are listed here but created in Chunk 6. This is intentional — the quickref is the canonical inventory and should be complete from day one.

```markdown
# Tier 1 — RE Quick Reference (read once per session)

Hard cap: 100 lines. Tool inventory + decision trees.

## Tool Inventory

| Tool | Type | Purpose |
|-|-|-|
| re-analyst | Agent | Static/dynamic binary analysis, decompilation, xref tracing |
| memory-hunter | Agent | Memory scanning, pointer chains, AOB signatures |
| mod-builder | Agent | Code generation: BepInEx, Harmony, CE tables, Frida scripts |
| asset-explorer | Agent | Unity asset extraction, kanim/texture/animation inspection |
| /new-project | Skill | Initialize RE project, detect engine, run initial analysis |
| /analyze-assembly | Skill | Deep .NET assembly analysis — types, methods, findings |
| /find-value | Skill | Guided memory scanning workflow with CE |
| /trace-to-code | Skill | Map memory address → source code method |
| /generate-mod | Skill | Auto-generate mod from project findings |
| /compare-assemblies | Skill | Diff two DLL versions after game update |
| /dump-type | Skill | Single-type deep dive with full decompilation |
| /find-hooks | Skill | Search assemblies for candidate methods to patch |

## Decision Tree — "What do I use?"

### "I have a new game to mod"
1. `/new-project` — detects engine, creates project, runs initial analysis
2. If Unity Mono → `/analyze-assembly` on Assembly-CSharp.dll
3. If Unity IL2CPP → re-analyst agent (IL2CPP dumper workflow)
4. If Java → check game-specific docs (MCMods, ZomboidMods)

### "I want to find where X is implemented"
1. `/analyze-assembly` — broad search by keyword
2. `/dump-type` — deep dive on a specific type
3. `/find-hooks` — find patchable methods for a gameplay goal
4. re-analyst agent — complex multi-step analysis with Ghidra

### "I want to change a runtime value"
1. `/find-value` — guided memory scan workflow
2. memory-hunter agent — complex multi-step scanning with pointer chains

### "I want to build a mod from findings"
1. `/generate-mod` — auto-generate from saved project findings
2. mod-builder agent — custom mod with specific requirements

### "A game updated and my mod broke"
1. `/compare-assemblies` — diff old vs new DLL
2. `/analyze-assembly` — find renamed/moved methods
3. re-analyst agent — deep investigation of changes

### "I need to inspect game assets"
1. asset-explorer agent — Unity assets, textures, animations

## MCP Servers

| Server | Tools for | Required by |
|-|-|-|
| re-orchestrator | Project mgmt, .NET inspection, mod gen | All agents/skills |
| ghidra | Static binary analysis, decompilation | re-analyst |
| cheatengine | Memory scanning, breakpoints, AOB | memory-hunter, re-analyst |
| frida-game-hacking | Runtime hooking, cross-platform scanning | memory-hunter, mod-builder |
| x64dbg | Dynamic debugging (Windows native) | re-analyst |

## Engine Quick Reference

| Engine | Key DLL | Decompilation Tool | Mod Framework |
|-|-|-|-|
| Unity Mono | Assembly-CSharp.dll | ilspycmd / dnSpy | BepInEx + Harmony or UserMod2 |
| Unity IL2CPP | GameAssembly.dll | IL2CPP Dumper + Ghidra | BepInEx (Il2CppInterop) |
| Java (MC) | game JARs | JD-GUI / fernflower | Fabric / Forge |
| Java (Zomboid) | game JARs | JD-GUI | Lua API + Java patches |
| .NET (Stardew) | game DLLs | ilspycmd / dnSpy | SMAPI |
| Unreal | game .exe | Ghidra / x64dbg | UE4SS |
| Godot | .pck files | Godot RE tools | GDScript patches |

## Decompiler CLI Cheat Sheet

ilspycmd (installed globally):
- List types: `ilspycmd "path.dll" -l -r "ManagedDir"`
- Decompile type: `ilspycmd "path.dll" -t TypeName -r "ManagedDir"`
- Use `\\` path separators on Windows
```

- [ ] **Step 3: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add docs/tier1-re-quickref.md
git commit -m "Add tier1 RE quick reference"
```

---

## Chunk 4: Shared Documentation — Engine Docs (Phase 3, part 2)

### Task 9: Create engine documentation files

**Files:**
- Create: `D:\Dev\Projects\GameModding\docs\engines\unity-mono.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\unity-il2cpp.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\unity-assets.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\unity-runtime.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\unreal.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\godot.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\java-minecraft.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\java-zomboid.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\source2.md`
- Create: `D:\Dev\Projects\GameModding\docs\engines\monogame-smapi.md`

Each doc should be 100-200 lines, covering:
- Engine identification markers (file/folder patterns)
- Key DLLs/binaries and where to find them
- Decompilation workflow (step-by-step)
- Common modding framework(s) for this engine
- Gotchas and pitfalls specific to this engine

**Implementation note:** These 10 files are independent — they can be written in parallel by subagents. Each agent should research the engine and write a self-contained guide following the structure above.

- [ ] **Step 1: Create engines directory**

```bash
mkdir -p "D:/Dev/Projects/GameModding/docs/engines"
```

- [ ] **Step 2: Write unity-mono.md**

Cover: Assembly-CSharp.dll location, ilspycmd/dnSpy workflow, Harmony patching, BepInEx setup, UserMod2 pattern (ONI-specific variant), common pitfalls (version mismatches, private fields, protected methods).

Reference: Cairath wiki decompiling section, ONI modding guide Section 1, existing MEMORY.md decompiler notes.

- [ ] **Step 3: Write unity-il2cpp.md**

Cover: GameAssembly.dll + global-metadata.dat identification, IL2CPP Dumper workflow, cross-referencing dump offsets with Ghidra, BepInEx Il2CppInterop, limitations vs Mono.

- [ ] **Step 4: Write unity-assets.md**

Cover: AssetStudio for extraction, kanim format (texture + build + anim .bytes files), kanimal-SE for conversion, Spriter workflow, adding animations to mods. Critical rules about editing textures.

Reference: Cairath wiki Animations page content (already fetched during brainstorming).

- [ ] **Step 5: Write unity-runtime.md**

Cover: UnityExplorer installation (BepInEx plugin), runtime GameObject inspection, component modification, in-game C# console, debug hotkeys.

- [ ] **Step 6: Write unreal.md**

Cover: UE4SS framework, Unreal asset formats (.uasset, .pak), Ghidra for native analysis, Blueprint vs C++ modding.

- [ ] **Step 7: Write godot.md**

Cover: .pck file extraction, GDScript decompilation, Godot RE patterns, asset inspection.

- [ ] **Step 8: Write java-minecraft.md**

Cover: Fabric vs Forge, MCP mappings (Mojang/Yarn/SRG), JD-GUI decompilation, mixin framework, mod structure.

- [ ] **Step 9: Write java-zomboid.md**

Cover: Lua modding API, Java decompilation for core patches, mod structure, Workshop publishing.

- [ ] **Step 10: Write source2.md**

Cover: Source 2 engine identification, Lua/VScript scripting, asset formats, Dota 2 modding specifics. Stub — expand when actively working on a Source 2 game.

- [ ] **Step 11: Write monogame-smapi.md**

Cover: SMAPI framework, mod lifecycle (Entry method), content patcher, Harmony integration, mod manifest format.

- [ ] **Step 12: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add docs/engines/
git commit -m "Add engine-specific RE documentation (10 engines)"
```

---

## Chunk 5: Shared Documentation — Frameworks, Tools, Toolkit (Phase 3, part 3)

### Task 10: Create framework documentation

**Files:**
- Create: `D:\Dev\Projects\GameModding\docs\frameworks\harmony.md`
- Create: `D:\Dev\Projects\GameModding\docs\frameworks\bepinex.md`
- Create: `D:\Dev\Projects\GameModding\docs\frameworks\frida.md`

- [ ] **Step 1: Create frameworks directory**

```bash
mkdir -p "D:/Dev/Projects/GameModding/docs/frameworks"
```

- [ ] **Step 2: Write harmony.md**

Cover: Harmony 2.0 deep-dive — prefix/postfix/transpiler/finalizer patches, `[HarmonyPatch]` attribute patterns, `__instance`/`__result`/`__state` parameters, `ref` keyword for modification, `AccessTools`, `CodeMatcher` for transpilers, `[HarmonyDebug]`, common mistakes.

Reference: ONI modding guide Section 3 (lines 247-449) for existing Harmony content to extract and generalize.

- [ ] **Step 3: Write bepinex.md**

Cover: BepInEx 5.x vs 6.x, installation, plugin lifecycle (`Awake`/`Start`/`Update`), `ConfigEntry<T>`, logging, Harmony integration within BepInEx, chainloader, preloader patchers.

- [ ] **Step 4: Write frida.md**

Cover: Frida attach/spawn workflow, script structure, `Interceptor.attach`, `NativeFunction`, `Memory.scan`, RPC methods, common patterns for game hacking.

- [ ] **Step 5: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add docs/frameworks/
git commit -m "Add framework documentation (Harmony, BepInEx, Frida)"
```

### Task 11: Create tool documentation

**Files:**
- Create: `D:\Dev\Projects\GameModding\docs\tools\ghidra.md`
- Create: `D:\Dev\Projects\GameModding\docs\tools\cheat-engine.md`
- Create: `D:\Dev\Projects\GameModding\docs\tools\dotnet-decompilation.md`
- Create: `D:\Dev\Projects\GameModding\docs\tools\x64dbg.md`

- [ ] **Step 1: Create tools directory**

```bash
mkdir -p "D:/Dev/Projects/GameModding/docs/tools"
```

- [ ] **Step 2: Write ghidra.md**

Cover: Ghidra setup for game RE, auto-analysis settings, function discovery, decompilation tips, cross-reference tracing, structure recovery, annotation best practices, scripting.

- [ ] **Step 3: Write cheat-engine.md**

Cover: Scan-narrow-find workflow, value types, data breakpoints, pointer chain resolution, AOB signature generation, auto-assemble scripts, table creation, DBVM.

- [ ] **Step 4: Write dotnet-decompilation.md**

Cover: ilspycmd CLI reference (flags, examples), dnSpy GUI workflow, dotPeek export-to-project, comparing decompilers, when to use which, common decompilation artifacts (compiler-generated names, state machines).

Reference: Existing MEMORY.md notes on ilspycmd usage.

- [ ] **Step 5: Write x64dbg.md**

Cover: x64dbg setup, breakpoint types, stepping, register/memory inspection, pattern scanning, scripting, conditional breakpoints, tracing.

- [ ] **Step 6: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add docs/tools/
git commit -m "Add tool documentation (Ghidra, CE, .NET decompilation, x64dbg)"
```

### Task 12: Create toolkit documentation

**Files:**
- Create: `D:\Dev\Projects\GameModding\docs\toolkit\agents.md`
- Create: `D:\Dev\Projects\GameModding\docs\toolkit\skills.md`
- Create: `D:\Dev\Projects\GameModding\docs\toolkit\workflows.md`
- Create: `D:\Dev\Projects\GameModding\docs\toolkit\mcp-servers.md`
- Create: `D:\Dev\Projects\GameModding\docs\toolkit\commands.md`

- [ ] **Step 1: Create toolkit directory**

```bash
mkdir -p "D:/Dev/Projects/GameModding/docs/toolkit"
```

- [ ] **Step 2: Write agents.md**

Document each agent for Claude's reference:
- **re-analyst**: When to use (static analysis, decompilation, xref tracing), key MCP tools it uses, example prompts
- **memory-hunter**: When to use (value scanning, pointer chains), key MCP tools, example prompts
- **mod-builder**: When to use (code generation from findings), supported frameworks, example prompts
- **asset-explorer**: When to use (Unity assets, textures, animations), tools, example prompts
- Team mode: how agents communicate via re-orchestrator findings

- [ ] **Step 3: Write skills.md**

Slash command cheat sheet for Claude:
- Each skill: name, one-line description, required input, output format
- `/new-project` → `/analyze-assembly` → `/find-hooks` → `/generate-mod` pipeline
- When to use a skill vs when to spawn an agent

- [ ] **Step 4: Write workflows.md**

End-to-end recipes:
- "New Unity Mono game → first Harmony mod"
- "New Unity IL2CPP game → first mod"
- "Game updated, mod broke → fix it"
- "Find and modify a runtime value"
- "Build a comprehensive trainer"

Each recipe: ordered steps with which skill/agent to use at each point.

- [ ] **Step 5: Write mcp-servers.md**

For Claude's reference:
- Each server: name, what it provides, Python executable path, how to verify it's running
- Troubleshooting: common connection failures, restart procedures
- Which agent/skill depends on which server

- [ ] **Step 6: Write commands.md**

Stub file — placeholder for future helper scripts and CLI commands.

```markdown
# Commands & Helper Scripts

No custom commands yet. This file will document helper scripts as they're added.
```

- [ ] **Step 7: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add docs/toolkit/
git commit -m "Add toolkit documentation (agents, skills, workflows, MCP servers)"
```

---

## Chunk 6: Plugin Enhancements (Phase 4)

### Task 13: Add new skills

**Files:**
- Create: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\skills\compare-assemblies\SKILL.md`
- Create: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\skills\dump-type\SKILL.md`
- Create: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\skills\find-hooks\SKILL.md`
- Modify: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\.claude-plugin\plugin.json`

- [ ] **Step 1: Write compare-assemblies skill**

```markdown
---
name: compare-assemblies
description: "Diff two versions of a .NET assembly after a game update — show added, removed, and changed types and methods"
---

# /compare-assemblies — Assembly Version Diff

When the user runs `/compare-assemblies`, diff two versions of a .NET assembly to identify what changed after a game update.

## Required Input
- **Old assembly path**: Path to the previous version .dll
- **New assembly path**: Path to the updated version .dll

## Workflow

### Phase 1: Enumerate Both Versions
1. Use `re-orchestrator:enumerate_dotnet_types` on the OLD assembly.
2. Use `re-orchestrator:enumerate_dotnet_types` on the NEW assembly.
3. Build a set of all type names from each version.

### Phase 2: Identify Changes
4. **Added types**: Types present in NEW but not OLD.
5. **Removed types**: Types present in OLD but not NEW.
6. **Potentially changed types**: Types present in both — compare method counts.

### Phase 3: Method-Level Diff (for changed types)
7. For each potentially changed type, use `re-orchestrator:enumerate_dotnet_methods` on both versions.
8. Compare method signatures — identify added, removed, renamed methods.
9. For renamed methods: look for methods with same parameter types but different names.

### Phase 4: Report
10. Present summary:
    - Added types (with namespace grouping)
    - Removed types
    - Changed types (with method-level diff)
    - Impact assessment: which existing Harmony patches might break

### Phase 5: Save Findings
11. Use `re-orchestrator:save_finding` to persist the diff for future reference.
```

- [ ] **Step 2: Write dump-type skill**

```markdown
---
name: dump-type
description: "Single-type deep dive — decompile a type with all fields, methods, base classes, and interfaces"
---

# /dump-type — Deep Type Inspection

When the user runs `/dump-type`, perform a comprehensive inspection of a single .NET type.

## Required Input
- **Assembly path**: Path to the .dll containing the type
- **Type name**: Full or partial type name to inspect

## Workflow

### Phase 1: Find the Type
1. If partial name given, use `re-orchestrator:search_dotnet_assembly` to find matches.
2. If multiple matches, present them and ask user to pick one.
3. Confirm the full type name (with namespace).

### Phase 2: Type Overview
4. Use `re-orchestrator:enumerate_dotnet_fields` to get all fields (name, type, visibility, static/instance).
5. Use `re-orchestrator:enumerate_dotnet_methods` to get all methods (name, return type, parameters, visibility).
6. Identify: base class, implemented interfaces, nested types.

### Phase 3: Method Decompilation
7. For each method, use `re-orchestrator:disassemble_dotnet_method` to get decompiled C# source.
8. Present methods grouped by visibility (public → protected → private).
9. Flag interesting patterns: virtual methods (patchable), event handlers, serialization attributes.

### Phase 4: Cross-Reference Analysis
10. Identify which other types reference this type (consumers).
11. Identify which types this type references (dependencies).

### Phase 5: Save & Report
12. Use `re-orchestrator:save_finding` with type "structure" to persist the full type analysis.
13. Present a summary with the type's role in the codebase and suggested hook points.
```

- [ ] **Step 3: Write find-hooks skill**

```markdown
---
name: find-hooks
description: "Given a gameplay goal, search assemblies for candidate methods to Harmony-patch"
---

# /find-hooks — Find Patchable Methods

When the user runs `/find-hooks`, search assemblies for methods that can be patched to achieve a gameplay goal.

## Required Input
- **Goal description**: What the user wants to change (e.g., "modify building cost", "change movement speed")
- **Assembly path**: Path to the .dll to search (or auto-detect from project)

## Workflow

### Phase 1: Keyword Extraction
1. Extract search keywords from the goal description.
2. Generate synonyms and related terms (e.g., "cost" → "Cost", "Price", "Resource", "Expense").
3. Generate likely class name patterns (e.g., "movement speed" → "Movement", "Locomotion", "Navigator", "Speed").

### Phase 2: Broad Search
4. Use `re-orchestrator:search_dotnet_assembly` with each keyword set.
5. Collect all matching types and methods.
6. Deduplicate and rank by relevance (exact match > partial match > related term).

### Phase 3: Method Analysis
7. For the top 10 candidates, use `re-orchestrator:disassemble_dotnet_method` to decompile.
8. Analyze each method for:
   - Does it read/write the target value?
   - Is it virtual (easier to patch)?
   - What are its parameters and return type?
   - Is it called frequently (per-frame) or infrequently (on-event)?

### Phase 4: Recommend Hook Points
9. Present ranked list of recommended hook points:
   - Method signature
   - Patch type recommendation (prefix/postfix/transpiler)
   - What to modify (parameter, return value, internal logic)
   - Risk assessment (per-frame performance, side effects)

### Phase 5: Save Findings
10. Use `re-orchestrator:save_finding` to persist hook recommendations.
```

- [ ] **Step 4: Update plugin.json with new skills**

Add the 3 new skills to the `skills` array in `plugin.json`:

```json
"skills": [
  "./skills/analyze-assembly",
  "./skills/find-value",
  "./skills/trace-to-code",
  "./skills/generate-mod",
  "./skills/new-project",
  "./skills/compare-assemblies",
  "./skills/dump-type",
  "./skills/find-hooks"
]
```

- [ ] **Step 5: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add .claude/plugins/local/re-game-hacking/skills/ .claude/plugins/local/re-game-hacking/.claude-plugin/plugin.json
git commit -m "Add 3 new skills: compare-assemblies, dump-type, find-hooks"
```

### Task 14: Add asset-explorer agent

**Files:**
- Create: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\agents\asset-explorer.md`
- Modify: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\.claude-plugin\plugin.json`

- [ ] **Step 1: Write asset-explorer agent**

```markdown
---
name: asset-explorer
description: "Unity asset extraction and inspection specialist. Extracts textures, animations, and other assets using AssetStudio. Analyzes kanim files, inspects Unity asset bundles, and helps understand game art pipelines."
model: inherit
color: purple
---

# Asset Explorer Agent

You are a Unity asset extraction and inspection specialist. Your job is to extract, inspect, and understand game assets — textures, animations, models, audio, and UI elements. You help modders understand a game's art pipeline and create compatible custom assets.

You have access to MCP tool servers for the **RE Orchestrator** and filesystem tools. Use `ToolSearch` to load any MCP tool before calling it.

## Core Workflows

### 1. Unity Asset Extraction
- Use `re-orchestrator:list_unity_assets` to enumerate asset bundles
- Identify asset types: textures, sprites, animations, prefabs, audio
- Guide the user through AssetStudio extraction

### 2. Kanim Analysis (ONI-style animations)
ONI and some Unity games use Klei's kanim format:
- **Texture** (`name_0.png`): sprite sheet with all graphics
- **Build** (`name_build.bytes`): sprite organization, symbol data, pivot points
- **Anim** (`name_anim.bytes`): animation keyframes and sequencing

Tools:
- kanimal-SE: convert between Spriter SCML and kanim formats
- Kanim Explorer: inspect and edit kanim file contents

Conversion workflow:
- Kanim → Spriter: `kanimal-cli.exe scml --output <folder> <texture> <build> <anim>`
- Spriter → Kanim: `kanimal-cli.exe kanim <scml> --output <folder> --interpolate`

Critical rules:
- Never use bones or non-linear tweens in Spriter (kanim doesn't support them)
- Never resize or move sprite contents within their bounding box
- Frame duration must be 33ms with snapping enabled
- Even static graphics need an anim.bytes file in mod kanim folders

### 3. Texture Inspection
- Identify sprite atlases and their contents
- Analyze texture formats and compression
- Guide creation of compatible replacement textures

### 4. Asset Modding
- Help create mod-compatible asset folder structures:
  ```
  <mod>/anim/assets/<animname>/<animname>_0.png
  <mod>/anim/assets/<animname>/<animname>_build.bytes
  <mod>/anim/assets/<animname>/<animname>_anim.bytes
  ```
- Verify asset naming conventions
- Check for conflicts with existing game assets

## Working as a Teammate

### Communicating Findings
- Save discovered asset structures via `re-orchestrator:save_finding`
- When you identify animation or texture patterns, document them for mod-builder
- Report to the lead with: asset inventory, formats found, modding approach

### What to Report
- Asset inventory (types, counts, formats)
- Animation structure (kanim symbols, banks, frame counts)
- Texture atlas layouts
- Recommended mod asset structure
- Blockers (encrypted assets, custom formats)
```

- [ ] **Step 2: Update plugin.json agents array**

Add `"./agents/asset-explorer.md"` to the agents array.

- [ ] **Step 3: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add .claude/plugins/local/re-game-hacking/agents/asset-explorer.md .claude/plugins/local/re-game-hacking/.claude-plugin/plugin.json
git commit -m "Add asset-explorer agent for Unity asset inspection"
```

### Task 15: Enhance re-analyst with .NET workflows

**Files:**
- Modify: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\agents\re-analyst.md`

- [ ] **Step 1: Read current re-analyst.md**

Read the file to understand current structure (already read during brainstorming — it's at `.claude/plugins/local/re-game-hacking/agents/re-analyst.md`).

- [ ] **Step 2: Add .NET/Mono workflow section**

Insert a new section after "### 6. .NET / Mono Games" (which is currently only 3 lines). Expand it to be a first-class workflow:

```markdown
### 6. .NET / Mono Games (Primary Workflow for Unity Mono)

For Unity Mono or other .NET games, managed DLLs give you source-level decompilation — this is your fastest path.

**Step-by-step workflow:**

1. `re-orchestrator:list_dotnet_assemblies` — find all managed DLLs in the game's Managed folder
2. `re-orchestrator:inspect_assembly` on `Assembly-CSharp.dll` — get metadata, framework, type count
3. `re-orchestrator:get_dotnet_assembly_refs` — understand dependency graph
4. `re-orchestrator:enumerate_dotnet_types` — list all types, identify key namespaces
5. `re-orchestrator:search_dotnet_assembly` with gameplay keywords — find relevant types
6. `re-orchestrator:enumerate_dotnet_methods` on target type — get method signatures
7. `re-orchestrator:enumerate_dotnet_fields` on target type — get field layouts
8. `re-orchestrator:disassemble_dotnet_method` — get full C# decompilation of specific methods

**CLI alternative (ilspycmd):**
- List types: `ilspycmd "Assembly-CSharp.dll" -l -r "ManagedDir"`
- Decompile type: `ilspycmd "Assembly-CSharp.dll" -t TypeName -r "ManagedDir"`
- Use `\\` path separators on Windows

**When to escalate to Ghidra:**
- Native plugins (C++ DLLs loaded by the game)
- IL2CPP builds (use IL2CPP workflow instead)
- Obfuscated assemblies where decompilation fails
- Need to trace into Unity engine internals (UnityEngine.dll is native)

**Common .NET game patterns:**
- MonoBehaviour subclasses: game logic attached to GameObjects
- ScriptableObject subclasses: data definitions (items, recipes, configs)
- Static managers: singletons holding global state
- Serialization attributes: `[SerializeField]`, `[Serialize]`, `[SerializationConfig]`
```

- [ ] **Step 3: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add .claude/plugins/local/re-game-hacking/agents/re-analyst.md
git commit -m "Enhance re-analyst with first-class .NET/Mono workflow"
```

### Task 16: Enhance mod-builder with UserMod2/PLib templates

**Files:**
- Modify: `D:\Dev\Projects\GameModding\.claude\plugins\local\re-game-hacking\agents\mod-builder.md`

- [ ] **Step 1: Read current mod-builder.md**

Read the file (already read during brainstorming).

- [ ] **Step 2: Add UserMod2/PLib framework section**

Insert after the BepInEx section. Add framework auto-detection logic:

```markdown
### 2b. UserMod2 Plugin Generation (ONI and KMod-based games)

For games using Klei's mod loading pipeline (ONI, potentially other Klei games):

**Key differences from BepInEx:**
- Entry point: `KMod.UserMod2` subclass (not `BaseUnityPlugin`)
- Patching: `base.OnLoad(harmony)` calls `PatchAll()` automatically
- Config: PLib `SingletonOptions<T>` (not BepInEx ConfigEntry)
- Logging: `PUtil.LogDebug/Warning/Error` (not BepInEx Logger)
- No BepInEx installation needed — game has built-in mod loading

**Template:**
```csharp
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace ModName
{
    public class ModNameMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModNameOptions));
            base.OnLoad(harmony); // applies all [HarmonyPatch] classes
        }
    }
}
```

**mod_info.yaml:**
```yaml
supportedContent: ALL
minimumSupportedBuild: 469112
version: 1.0.0
APIVersion: 2
```

**Framework auto-detection:**
When generating a mod, check the project context:
1. If project has `mod_info.yaml` or references `KMod` → use UserMod2 template
2. If project has `BepInEx` references or `doorstop_config.ini` → use BepInEx template
3. If game is Unity but no framework detected → recommend BepInEx
4. If game is non-Unity → check engine-specific framework
```

- [ ] **Step 3: Commit**

```bash
cd "D:/Dev/Projects/GameModding"
git add .claude/plugins/local/re-game-hacking/agents/mod-builder.md
git commit -m "Enhance mod-builder with UserMod2/PLib templates and framework auto-detection"
```

---

## Chunk 7: ONI Docs Enhancement & Global Cleanup (Phases 5-6)

### Task 17: Fill Cairath wiki gaps in ONI modding guide

**Files:**
- Modify: `D:\Dev\Projects\GameModding\ONIMods\docs\ONI-modding-guide.md`

**Context:** The following gaps were identified by comparing against the Cairath wiki:
1. UserMod2 full lifecycle (`OnAllModsLoaded`, properties, constraints)
2. Animations (kanim pipeline, kanimal-SE, Spriter workflow)

- [ ] **Step 1: Read current Section 2 of ONI modding guide**

```bash
# Read the mod loading section to find where UserMod2 is documented
```

Read `ONIMods/docs/ONI-modding-guide.md` lines 113-246 to find where to add UserMod2 details.

- [ ] **Step 2: Add UserMod2 lifecycle details**

After the existing UserMod2 section, add:

```markdown
### UserMod2 full lifecycle

`UserMod2` provides two override points:

**`OnLoad(Harmony harmony)`** — called when your mod DLL loads. If you override this, call `base.OnLoad(harmony)` to trigger automatic `PatchAll()`. You can run code before or after the base call:

```csharp
public override void OnLoad(Harmony harmony)
{
    Debug.Log("Before patches");
    base.OnLoad(harmony);
    Debug.Log("After patches");
}
```

**`OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)`** — called after ALL mods finish loading. Use for mod compatibility checks:

```csharp
public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
{
    foreach (var mod in mods)
        Debug.Log("Found mod: " + mod.title);
}
```

**Available properties:**
- `assembly` — your mod's Assembly
- `path` — your mod's folder path
- `mod` — the Mod instance
- `mod.title`, `mod.staticID`, `mod.description` — from mod.yaml
- `mod.packageModInfo` — from mod_info.yaml

**Constraints:**
- Maximum one UserMod2 per DLL (but a Mod with multiple DLLs can have multiple UserMod2 classes)
- Cannot be abstract
- Access all instances via `Mod.loaded_mod_data.userMod2Instances`
```

- [ ] **Step 3: Add animations section**

Add a new section to the modding guide (or expand Section 6) covering the kanim pipeline. Content source: Cairath wiki Animations page (fetched during brainstorming).

Cover:
- Kanim format (texture + build.bytes + anim.bytes)
- Required tools (Spriter, kanimal-SE, Kanim Explorer)
- Conversion workflow (kanim ↔ Spriter SCML)
- Adding animations to mods (folder structure)
- Critical constraints (no bones, no resize, 33ms frames, snapping)

- [ ] **Step 4: Commit**

```bash
cd "D:/Dev/Projects/GameModding/ONIMods"
git add docs/ONI-modding-guide.md
git commit -m "Add UserMod2 lifecycle and animations section (Cairath wiki gaps)"
```

### Task 18: Update ONIMods CLAUDE.md to reference parent

**Files:**
- Modify: `D:\Dev\Projects\GameModding\ONIMods\CLAUDE.md`

- [ ] **Step 1: Read current ONIMods CLAUDE.md**

Read `ONIMods/CLAUDE.md`.

- [ ] **Step 2: Add parent reference**

Add at the top of the file, before the existing content:

```markdown
# Parent workspace: See `../CLAUDE.md` for shared RE toolkit, engine docs, and modding conventions.
```

- [ ] **Step 3: Commit**

```bash
cd "D:/Dev/Projects/GameModding/ONIMods"
git add CLAUDE.md
git commit -m "Add parent workspace reference to ONIMods CLAUDE.md"
```

### Task 19: Populate ONIMods breaking changes section

**Files:**
- Modify: `D:\Dev\Projects\GameModding\ONIMods\docs\tier1-quickref.md`

- [ ] **Step 1: Read current tier1-quickref.md**

Read the Breaking Changes section at the bottom of `ONIMods/docs/tier1-quickref.md` (currently says "None tracked yet").

- [ ] **Step 2: Add breaking changes tracking template**

Replace the empty section with a structured template:

```markdown
## Breaking Changes

Track game updates that break mod behavior here.

| Date | Game Build | What Broke | Fix |
|-|-|-|-|
| (none yet) | | | |
```

- [ ] **Step 3: Commit**

```bash
cd "D:/Dev/Projects/GameModding/ONIMods"
git add docs/tier1-quickref.md
git commit -m "Add breaking changes tracking template"
```

### Task 20: Clean up global config

**Context:** Only do this AFTER verifying everything works in the GameModding workspace (Task 6 Step 3 passed).

- [ ] **Step 1: Remove global RE skills**

```bash
rm -rf "C:/Users/Zero/.claude/skills/analyze-assembly"
rm -rf "C:/Users/Zero/.claude/skills/find-value"
rm -rf "C:/Users/Zero/.claude/skills/trace-to-code"
rm -rf "C:/Users/Zero/.claude/skills/generate-mod"
rm -rf "C:/Users/Zero/.claude/skills/new-project"
```

Note: Keep `C:/Users/Zero/.claude/skills/tiered-docs/` — that's a general skill, not RE-specific.

- [ ] **Step 2: Remove global RE agent copies**

```bash
rm -f "C:/Users/Zero/.claude/agents/re-analyst.md"
rm -rf "C:/Users/Zero/.claude/re-game-hacking/"
```

- [ ] **Step 3: Remove or relocate global MCP config**

The MCP config at `C:\Users\Zero\re-mcp-toolkit\.mcp.json` may still be needed if `re-mcp-toolkit` is used independently. Options:
- If `re-mcp-toolkit` is only used via Claude Code → rename to `.mcp.json.bak` (keep as backup)
- If `re-mcp-toolkit` has its own workflows → leave it, the GameModding copy is independent

Ask the user which approach they prefer.

- [ ] **Step 4: Remove global plugin reference**

Check if `re-game-hacking` is referenced in `settings.json` under `enabledPlugins`. If so, remove the entry. Currently it's listed under `extraKnownMarketplaces.local` which points to `C:\Users\Zero\.claude` — removing the source files (Step 2) should be sufficient.

- [ ] **Step 5: Verify — open Claude Code in a non-game project**

Open Claude Code at a non-game project (e.g., `D:\Dev\Projects\Downloads-cleaner\`). Verify:
1. No RE agents appear
2. No RE skills appear
3. No RE MCP tools in deferred tools list

- [ ] **Step 6: Update MEMORY.md**

Update `C:\Users\Zero\.claude\projects\d--Dev-Projects-ONI-Mods\memory\MEMORY.md` (or the new project key if it changed after the move):
- Update all path references from `D:\Dev\Projects\ONI Mods` to `D:\Dev\Projects\GameModding\ONIMods`
- Note the new workspace structure
- Note that RE tools are now project-local to GameModding

---

## Execution Notes

### Parallelism
- **Chunk 4 (engine docs)**: All 10 files are independent — can be written in parallel by subagents
- **Chunk 5 (frameworks + tools + toolkit)**: All 12 files are independent — can be parallelized
- **Chunks 3, 4, 5, and 6** are all independent of each other — can run in parallel after Chunk 2
- **Chunk 7 (ONI docs + cleanup)**: Must wait until Chunks 1-2 are verified working

### Task dependencies
```
Chunk 1 (workspace) → Chunk 2 (plugin) → Chunk 3 (CLAUDE.md + tier1)
                                        ↘ Chunk 4 (engine docs)
                                        ↘ Chunk 5 (framework/tool docs)   → Chunk 7 (ONI docs + cleanup)
                                        ↘ Chunk 6 (plugin enhancements)  ↗
```

Chunks 3-6 all branch from Chunk 2 and can run in parallel. Chunk 7 waits for all of them.

### Risk checkpoints
- After Task 3: verify all git repos have intact history
- After Task 6: verify plugin loads in GameModding session (CRITICAL — don't proceed if this fails)
- After Task 20 Step 5: verify non-game sessions are clean
