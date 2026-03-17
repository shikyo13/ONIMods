# Building a Tiered Reference System for AI Coding Assistants

A practical guide to structuring project documentation so AI assistants (Claude Code, Cursor, Copilot, etc.) consume minimal tokens while retaining full codebase understanding.

## The Problem

AI coding assistants have a context window. Every token of documentation they read costs money (API) or eats into your usage cap (subscription). Most projects handle this badly:

- **No docs**: The AI reads source files on every session, re-discovering the same architecture
- **One big doc**: A 2000-line reference gets loaded every time, even when the task only needs 50 lines
- **Scattered docs**: The AI doesn't know which doc to read, so it reads all of them or none

The result: wasted tokens on irrelevant context, or missing context that causes bugs.

## The Solution: Tiered Documentation

Structure docs into tiers by **frequency of access**, then teach the AI when to read each tier.

```
Tier 0: CLAUDE.md          Always loaded (auto-injected by tooling)
Tier 1: Quick Reference     Read every session (~100-150 lines)
Tier 2: Domain References   Read only when editing that domain
Tier 3: Full API/Reference  Read specific sections on demand
```

### Token Budget Comparison

| Scenario | Flat Docs | Tiered | Savings |
|-|-|-|-|
| Routine session | ~1750 lines | ~230 lines | 87% |
| Domain-specific edit | ~1750 lines | ~430 lines | 75% |
| API lookup | ~1750 lines | ~280 lines | 84% |

## Tier 0: CLAUDE.md (The Router)

**Purpose**: Project identity + routing table. Always in context.
**Budget**: 50-80 lines. Every line here costs tokens on *every* interaction.

### What Goes Here

1. **One-line project description**  - What is this?
2. **Key directories**  - 5-8 entries max, table format
3. **Build commands**  - Copy-pasteable
4. **Coding conventions**  - The 5-10 rules that prevent bugs
5. **Documentation routing table**  - The critical piece:

```markdown
## Documentation
| When | Read |
|-|-|
| Every session | `docs/tier1-quickref.md` (~120 lines) |
| Editing module structure | `docs/tier2-architecture.md` |
| Editing hero configs | `docs/tier2-patch-notes.md` |
| Specific API questions | `docs/tier3-api-guide.md` -- section index, read only relevant section |
```

### What Does NOT Go Here

- Architecture explanations (tier 2)
- API details (tier 3)
- Patch notes or changelogs (tier 2)
- Anything longer than one line per topic

### Anti-Pattern: Stuffing CLAUDE.md

Every extra line in CLAUDE.md is read on every single message. A 500-line CLAUDE.md costs you ~500 tokens per exchange. Put it in a lower tier.

## Tier 1: Quick Reference (~100-150 lines)

**Purpose**: The "cheat sheet" that prevents the most common mistakes. Read once per session.
**Budget**: 100-150 lines. Hard cap. If it grows past 150, demote content to tier 2.

### What Goes Here

Content that prevents **repeated mistakes across sessions**:

1. **Dependency graph** (compact)  - What loads what. Not every file, just the key load paths. Table format.
2. **Language/framework gotchas**  - One-liner bullet list of the 15-20 most critical pitfalls. Things the AI gets wrong repeatedly.
3. **Breaking changes**  - If you're on a new version/patch, the top 10 changes that affect the codebase.
4. **Anti-patterns**  - Lessons learned from past mistakes. "Don't do X because Y."

### How to Write Gotchas

Bad (too verbose, wastes tokens):
```markdown
### GetNearbyHeroes() Can Return Nil
The `GetNearbyHeroes()` function sometimes returns nil instead of an empty
table. Always check for nil before using the `#` operator on the result,
or you'll get a runtime error.
```

Good (one line, scannable):
```markdown
- `GetNearbyHeroes()` can return nil -- nil-check before `#`
```

### How to Write the Dependency Graph

Bad (prose):
```markdown
The bot_generic.lua file is the entry point for each hero. It loads the
hero-specific configuration from the BotLib directory using dofile()...
```

Good (table):
```markdown
| File | Requires |
|-|-|
| `bot_generic.lua` | utils, BotLib/hero_* (via dofile) |
| `item_purchase_generic.lua` | jmz_func, aba_item, aba_role, utils, BotLib/hero_* |
```

## Tier 2: Domain References (~150-200 lines each)

**Purpose**: Deep reference for a specific domain. Read only when working in that domain.
**Budget**: 150-200 lines per file. Create multiple tier-2 files for different domains.

### How to Split Domains

Each tier-2 file covers one **editing context**. Ask: "When I'm editing X, what do I need to know?"

Examples from this project:
- **Architecture** (tier2-architecture.md)  - Module structure, require graph, file layout. Read when editing module structure or adding new modules.
- **Patch notes** (tier2-patch-notes.md)  - Hero/item changes mapped to config files. Read when editing hero configs or item builds.

Other projects might split differently:
- **Database schema**  - Read when editing models or migrations
- **API contracts**  - Read when editing endpoints or clients
- **Component library**  - Read when building UI
- **Test patterns**  - Read when writing tests

### Key Principle: Map Changes to Files

Every entry in a tier-2 doc should map to a **specific file or module** in the codebase. Don't just document what changed  - document what to edit.

Bad:
```markdown
- Ethereal Blade reworked: new recipe, different stats
```

Good:
```markdown
- **Ethereal Blade** [REWORKED]: Now Kaya + Ghost Scepter + Recipe(900) = 5200g -> all Ethereal Blade builders in sBuyList
```

### Superseding Rule for Changelogs

If you track multiple versions, use a superseding rule: if version C changed something from version B, only keep the version C entry. This prevents the AI from reading outdated information first.

## Tier 3: Full Reference (any length)

**Purpose**: Complete API docs, full specifications. Never read in full.
**Budget**: Unlimited length, but **must have a section index**.

### The Section Index

This is what makes tier 3 work. At the top of the file, add a table mapping section names to line ranges:

```markdown
## Section Index

Use `Read` tool with `offset` and `limit` to load specific sections only.

| # | Topic | Lines |
|-|-|-|
| 1 | Environment Setup | 39-85 |
| 2 | Architecture | 86-123 |
| 3 | File Naming & Overrides | 124-226 |
| ...| ... | ... |
| 15 | Complete API Reference | 954-1151 |
```

The AI reads the index (~15 lines), identifies the relevant section, then reads only those 50-100 lines. A 1300-line doc costs 15 + ~75 = ~90 tokens instead of 1300.

### Maintaining Line Ranges

When you edit the tier-3 doc, update the section index line ranges. This is the one maintenance cost of the system. Keep sections in stable order to minimize churn.

## Implementation Checklist

### 1. Audit Your Current Docs

- List every doc file, its line count, and how often it's needed
- Identify content that's read every session vs. occasionally vs. rarely
- Find duplicate information across files

### 2. Write CLAUDE.md First

- Project identity (1-2 lines)
- Key directories (table, 5-8 rows)
- Build commands (3-5 lines)
- Coding conventions (5-10 bullet points)
- Documentation routing table (the tier index)
- **Target: under 80 lines**

### 3. Extract Tier 1

- Pull the top 15-20 gotchas/pitfalls from experience or existing docs
- Build a compact dependency graph (table format)
- Summarize breaking changes (if applicable)
- List anti-patterns from past mistakes
- **Target: under 150 lines**

### 4. Build Tier 2 Files

- One file per editing domain
- Every entry maps to a specific file/module
- Use superseding rules for changelogs
- **Target: under 200 lines each**

### 5. Add Section Index to Tier 3

- If you have large reference docs, add line-range index at top
- Keep the index itself under 20 lines
- Instruct the AI to use offset/limit reads

### 6. Wire It Up

- Add the routing table to CLAUDE.md
- Add to auto-memory: "Read tier1 every session"
- Delete or archive the old flat docs

### 7. Maintain

- When tier 1 grows past 150 lines, demote the least-referenced content to tier 2
- When tier 2 grows past 200 lines, split into two domain files
- Update tier 3 section index when editing that doc
- Review quarterly: is every line in tier 1 still earning its keep?

## Design Principles

**1. Token cost is proportional to access frequency.**
Tier 0 is read on every message. Tier 1 once per session. Tier 2 once per task. Tier 3 once per question. Size them accordingly.

**2. Routing over reading.**
The AI should know *where* to find information, not memorize it all. CLAUDE.md is a routing table, not an encyclopedia.

**3. One-liners over paragraphs.**
AI assistants scan for patterns, not prose. A bullet point with a code snippet beats a paragraph of explanation.

**4. Map to files, not concepts.**
"Ethereal Blade was reworked" is useless. "Ethereal Blade reworked -> update sBuyList in hero_morphling.lua" is actionable.

**5. Prune aggressively.**
If a gotcha hasn't prevented a bug in 3 sessions, demote it. If a tier-2 entry references deleted code, remove it. Dead docs are negative value  - they cost tokens and mislead.

## Adapting to Other Projects

The tier structure is universal. The content is project-specific. Here's how different project types might organize:

| Project Type | Tier 1 | Tier 2 Files | Tier 3 |
|-|-|-|-|
| Web app (React + API) | Component patterns, API conventions, common bugs | `frontend-patterns.md`, `api-contracts.md`, `db-schema.md` | Full API spec |
| CLI tool | Arg parsing patterns, exit codes, test patterns | `command-reference.md`, `plugin-system.md` | Man page / full docs |
| Game mod (this project) | Dependency graph, Lua gotchas, patch changes | `architecture.md`, `patch-notes.md` | Bot scripting API |
| ML pipeline | Data formats, model configs, common failures | `data-pipeline.md`, `training-config.md` | Framework API docs |
| Monorepo | Package boundaries, shared lib usage | One per package | Full API per package |

## Cost Analysis

Assuming ~1 token per line of documentation:

| Approach | Tokens/session | Tokens/100 sessions |
|-|-|-|
| No docs (AI reads source) | ~15,000 | 1,500,000 |
| One big doc | ~2,000 | 200,000 |
| **Tiered (routine task)** | **~230** | **23,000** |
| **Tiered (domain edit)** | **~430** | **43,000** |

The tiered approach uses 85-98% fewer tokens than alternatives over time.
