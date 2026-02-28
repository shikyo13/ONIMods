# ONI Replace Stuff — Mod Design Document

## Concept

A single-action replacement system for Oxygen Not Included. The player selects a new building or tile type, places it over an existing one, and the mod auto-queues a deconstruct on the old thing followed by a build order for the new thing. One click, two tasks, zero micromanagement.

The closest analog is fluffy's "Replace Stuff" mod for RimWorld, which is considered essential QoL by most of that game's playerbase. ONI has no equivalent despite years of forum requests.

---

## Core Behavior

When the player is in build mode and clicks on a tile occupied by an existing building or tile:

1. Check if the new building can legally occupy that cell (size, rotation, foundation requirements, etc.)
2. If yes, place a **deconstruct errand** on the existing building/tile
3. Immediately place a **build errand** for the new building in the same cell(s)
4. The build errand should be **blocked/paused** until deconstruction completes
5. Both errands inherit the priority the player has selected in the build tool

The player should see a distinct visual overlay on the cell indicating "queued for replacement" rather than just the normal build ghost, so it's clear what's happening.

---

## Supported Replacement Categories

### Tier 1 — Launch (simplest, highest value)

These are the replacements players want most and have the fewest edge cases.

**Tile-for-tile replacements:**
- Regular Tile → Insulated Tile, Metal Tile, Mesh Tile, Airflow Tile, Carpet Tile, etc.
- Any tile type → any other tile type
- Same tile type with different material (sandstone tile → granite tile)

**Furniture upgrades:**
- Cot → Comfy Bed → Luxury Bed (if modded)
- Outhouse → Lavatory
- Wash Basin → Sink / Hand Sanitizer
- Manual Generator → any other generator in the same footprint
- Pitcher Pump → Bottle Emptier (same 1x2 footprint)

**Door swaps:**
- Pneumatic Door → Mechanized Airlock → Bunker Door
- Any door type → any other door type (same cell size)

**Lighting:**
- Ceiling Light → Sun Lamp (both 1x1)

### Tier 2 — Second release

**Pipes and wires (trickier because of connectivity):**
- Regular Pipe → Insulated Pipe → Radiant Pipe
- Wire → Heavi-Watt Wire → Conductive Wire variants
- Gas Pipe equivalents
- Conveyor Rail → variants

**Storage:**
- Storage Bin → Smart Storage Bin
- Liquid Reservoir → (same footprint variants)

**Farm tiles:**
- Farm Tile → Hydroponic Farm (already partially supported by a small mod, can expand)

### Tier 3 — Stretch goals

**Cross-size replacements** (these are hard):
- Small Battery → Smart Battery → Large Battery (different footprints)
- Replacing a 1x1 with a 2x2 that *contains* the original cell

**Multi-building swaps:**
- Select a range and replace all tiles of type X with type Y (batch mode)

---

## Architecture

### ONI Modding Context

ONI mods are C# class libraries using Harmony 2 to patch game methods at runtime. The game loads mods via its built-in mod loader. Key game systems to interact with:

- `BuildTool` — handles player build placement input
- `BuildingDef` — defines building properties (size, placement rules, materials)
- `Deconstructable` — component on buildings that can be torn down
- `Constructable` — component managing the build errand lifecycle
- `PrioritySetting` / `Prioritizable` — work priority system
- `Grid` — the cell-based world grid
- `GameSceneLooper` — manages per-frame updates

### Project Structure

```
ReplaceTool/
├── ReplaceTool.sln
├── ReplaceTool.csproj
├── Mod.yaml                    # ONI mod metadata
├── mod_info.yaml               # DLC compatibility flags
│
├── Core/
│   ├── ReplaceToolMod.cs       # Entry point, Harmony init, config loading
│   ├── ReplacementDef.cs       # Data class: what's being replaced with what
│   └── ReplacementValidator.cs # Rules engine: can X replace Y?
│
├── Patches/
│   ├── BuildToolPatches.cs     # Intercept build placement to detect overlaps
│   ├── DeconstructPatches.cs   # Hook deconstruct completion to trigger build
│   ├── PriorityPatches.cs      # Ensure priority inheritance
│   ├── OverlayPatches.cs       # Visual indicator for pending replacements
│   └── CancelPatches.cs        # Handle cancellation of either half
│
├── Systems/
│   ├── ReplacementTracker.cs   # Tracks active replacement pairs (old → new)
│   ├── ReplacementQueue.cs     # Manages the sequencing (deconstruct then build)
│   ├── MaterialResolver.cs     # Determines material handling during replacement
│   └── TemperatureResolver.cs  # Determines temperature handling
│
├── UI/
│   ├── ReplaceOverlay.cs       # Custom overlay icon/tint for "replacing" cells
│   ├── ReplaceToolTip.cs       # Tooltip showing "Replacing X with Y"
│   └── ReplaceSettings.cs      # In-game settings (if using PeterHan's PLib)
│
└── Config/
    └── ReplaceToolConfig.cs    # User-configurable options
```

### Key Classes in Detail

#### ReplacementDef

```
Fields:
  - cell (int)                    — Grid cell being replaced
  - oldBuildingDef (BuildingDef)  — What's currently there
  - newBuildingDef (BuildingDef)  — What's being placed
  - newMaterials (Tag[])          — Selected materials for new building
  - priority (int)                — Work priority
  - orientation (Orientation)     — Rotation of new building
  - state (ReplacementState)      — Enum: Pending, Deconstructing, 
                                    ReadyToBuild, Building, Complete, Cancelled
```

#### ReplacementValidator

This is where most of the complexity lives. It decides whether a replacement is legal.

```
Rules to check:
  1. Footprint compatibility — do old and new occupy the same cells?
     (Tier 1: same size only. Tier 3: allow size changes.)
  2. Foundation requirements — does the new building need floor below?
  3. Conduit layer — replacing pipes on the same layer?
  4. Tech requirements — has the player researched the new building?
  5. Material availability — are the required materials discovered?
     (Not necessarily available, just discovered, matching "Plan Buildings 
      Without Materials" philosophy)
  6. Blocked combinations — things that make no physical sense
     (e.g., replacing a gas pipe with a liquid pipe should be fine,
      but replacing a generator with a tile probably shouldn't be 
      auto-allowed)
```

#### ReplacementTracker

The central state manager. Stores a `Dictionary<int, ReplacementDef>` keyed by cell ID. Persists across save/load via ONI's serialization system (`KSerialization`).

```
Responsibilities:
  - Register new replacement requests
  - Track state transitions (Pending → Deconstructing → ReadyToBuild → etc.)
  - Handle cancellation (cancel either errand = cancel both)
  - Clean up completed or failed replacements
  - Serialize/deserialize with save data
```

#### ReplacementQueue

Manages the sequencing guarantee. When a deconstruct errand completes on a tracked cell, this system creates the corresponding build errand.

```
Flow:
  1. BuildTool placement intercepted → ReplacementDef created
  2. Deconstruct errand placed on old building (visible to dupes)
  3. Build errand placed but flagged as blocked (NOT visible to dupes yet)
  4. On deconstruct complete callback → unblock build errand
  5. Dupes now see and execute the build errand
  6. On build complete → clean up tracker entry
```

The "blocked build errand" approach is important. If you just wait to place the build order until after deconstruct, the player loses visual feedback about what's planned. The ghost should be visible immediately, just not workable.

---

## Harmony Patches — What Gets Patched and Why

### BuildTool.OnLeftClickDown (Prefix)
Intercept the player's click when in build mode. Check if the target cell(s) contain an existing building. If yes, and the replacement is valid, create a ReplacementDef instead of the normal "can't build here" rejection.

### Deconstructable.OnCompleteWork (Postfix)
When deconstruction finishes, check if this cell is tracked in ReplacementTracker. If yes, transition state and unblock the pending build errand.

### Constructable.OnCompleteWork (Postfix)
When construction finishes on a tracked cell, mark the replacement as complete and clean up.

### Cancelable.OnCancel (Postfix)
If either the deconstruct or build errand is cancelled, cancel the other one too. The pair should always be atomic — you don't get one without the other.

### BuildingDef.IsValidBuildLocation (Postfix)
Modify the validity check to return true for cells where a replacement is queued, even though the cell is currently occupied. This is the critical patch that makes the whole thing work; normally ONI rejects build orders on occupied cells.

### PlanScreen / BuildMenu (Prefix or Postfix)
Possibly needed to add a visual indicator in the build menu showing "this will replace the existing building" when hovering over an occupied cell with a compatible building selected.

---

## Edge Cases and Decisions

### Temperature handling
When you deconstruct a tile, the stored thermal energy disperses. The new tile gets built at whatever temperature the materials are. This is vanilla behavior and the mod should NOT try to preserve temperature — that would be a gameplay change, not QoL. Just let it work normally.

### Material drops from deconstruction
Deconstructing returns 100% materials in ONI. These drop on the ground as usual. The new building requires its own materials delivered. No special handling needed, but worth documenting so players understand they're not "upgrading in place" for free — they still pay full material cost for the new thing and get full refund from the old thing.

### Structural dependencies
If a tile is holding up buildings above it, deconstructing it can cause collapses or pressure breaches. The mod should show a warning icon on replacements where the old tile has structural dependents, but NOT block the action. The player might know what they're doing (replacing one tile type with another while dupes are elsewhere). A tooltip saying "Warning: structures above depend on this tile" is sufficient.

### Liquid/gas pressure
Replacing a wall tile that's holding back a liquid column is dangerous. Same approach: warn, don't block. Players replacing tiles in pressurized areas are usually doing it deliberately. The deconstruct will briefly expose the cell, same as manual deconstruct would.

### Pipe/wire contents
Replacing a pipe segment means the contents spill during deconstruct. This is the same as manual pipe replacement. No special handling, but the tooltip should note "Pipe contents will be released during replacement."

### Multi-cell buildings
A 3x2 generator occupies 6 cells. The replacement check needs to validate ALL cells, and the deconstruct/build pair targets the building as a whole, not individual cells. Use the building's primary cell (anchor) for tracking, but validate footprint overlap.

### Save/Load
All active ReplacementDefs must serialize into the save file. Use ONI's `KSerialization` system. On load, restore the tracker state and re-validate all pending replacements (in case a game update changed building defs).

### Mod compatibility
Should play nicely with:
- **Blueprints mod** — blueprint placement should trigger replacements if placing over existing buildings
- **Plan Buildings Without Materials** — replacements should be plannable even without materials on hand
- **Build Over Plants** (base game) — plant uprooting should still work; replacement system should not interfere with it

---

## Configuration Options

Expose these via PLib settings (PeterHan's options framework, standard for ONI mods):

| Setting | Default | Description |
|---------|---------|-------------|
| Enable tile replacement | true | Allow tile → tile swaps |
| Enable furniture replacement | true | Allow building → building swaps |
| Enable pipe replacement | false | Pipes are risky; off by default for Tier 2 |
| Enable wire replacement | false | Same rationale |
| Show structural warnings | true | Warning icons on load-bearing replacements |
| Show pipe content warnings | true | Warning about fluid release |
| Require same footprint | true | Only allow same-size replacements |
| Replacement overlay color | #FFD700 (gold) | Tint color for pending replacement ghosts |

---

## Visual Feedback

### Placement Phase
When the player hovers a building over an occupied cell that's eligible for replacement:
- Cell border tints gold/yellow instead of the normal green "valid placement"
- Tooltip shows: "Replace [Old Building] with [New Building]"
- The existing building gets a subtle "marked for replacement" shimmer or icon

### In-Progress Phase
- Cell shows the new building's ghost (semi-transparent) overlaid on the old building
- A small swap icon (two arrows) appears on the cell
- The errand list shows "Replace: [Old] → [New]" instead of separate deconstruct/build entries

### Completion
- Normal build-complete behavior, no special feedback needed

---

## Development Phases

### Phase 1: Proof of concept
Get tile-for-tile replacement working. One tile type replacing another tile type in a single cell. No UI polish, just the core flow: intercept placement → queue deconstruct → on complete, queue build. This proves the Harmony patches work and the sequencing is reliable.

### Phase 2: Expand building support
Add furniture, doors, and same-footprint buildings. Build out ReplacementValidator with proper rule checking. Add the cancellation linkage so cancelling one errand cancels the pair.

### Phase 3: Visual polish
Add the overlay tint, tooltips, and warning icons. Integrate with PLib for the settings screen. Add the "Replace" label to errand entries.

### Phase 4: Save/load and stability
Implement serialization. Test edge cases: save mid-replacement, load, verify state restores correctly. Test cancellation, structural warnings, multi-cell buildings.

### Phase 5: Pipe and wire support (Tier 2)
Add conduit replacement with content warnings. This is the highest-risk feature due to fluid simulation interactions.

### Phase 6: Batch mode (Tier 3)
Drag-select to replace all tiles of type X with type Y in a region. Basically the replacement equivalent of the deconstruct drag tool.

---

## Technical Risks

**Risk: BuildingDef.IsValidBuildLocation is too restrictive to patch cleanly.**
The vanilla method has a lot of internal checks. A Harmony postfix that flips the result from false to true might have unintended side effects. Mitigation: study the decompiled method carefully, and only override the specific "cell is occupied" check, not other validity checks.

**Risk: Errand system doesn't support "blocked" errands natively.**
ONI's chore system might not have a clean way to say "this build errand exists but isn't workable yet." Mitigation: instead of a blocked errand, we could delay creating the build errand entirely and only create it in the Deconstructable.OnCompleteWork postfix. The ghost visual would be managed separately by the mod, not by the game's normal build ghost system.

**Risk: Serialization across game updates.**
If Klei changes BuildingDef IDs or building properties, deserialized ReplacementDefs might reference stale data. Mitigation: validate on load and discard any replacements that no longer make sense, with a notification to the player.

**Risk: Multiplayer/async timing.**
ONI is single-player, so this isn't a concern. But the deconstruct→build transition needs to happen in the same game tick to prevent the cell from being "empty" and causing physics issues (liquid flowing into the gap). Mitigation: hook the completion callback and place the build errand synchronously in the same frame.

---

## Dependencies

- **Harmony 2.x** — patching framework (bundled with ONI modding)
- **PLib** (optional but recommended) — PeterHan's shared library for options screens, logging, and mod utilities. Most serious ONI mods use it.
- **ONI Common Lib** — only if needed for shared utilities

---

## Workshop Metadata

**Name:** Replace Stuff  
**Tags:** QoL, Building, Utility  
**Description pitch:** "Replace buildings and tiles in one click. Select what you want to build, place it over an existing building, and the mod handles the rest — queuing deconstruct and rebuild automatically. No more manual two-step dance for every upgrade."  
**Compatibility:** Base Game + Spaced Out DLC  
**Preview image:** Side-by-side: left shows manual 4-step process (select deconstruct, click tile, select build, click tile), right shows 1-step with Replace Stuff.

---

## Revenue / Distribution Notes

This is the kind of mod that could anchor a Nexus Mods + BuyMeACoffee strategy. High subscriber count potential on Steam Workshop (this solves a universal pain point), and a Nexus mirror captures the donation-capable audience. Patreon tier could offer early access to new phases (pipe replacement, batch mode) before Workshop release.

Given that Peter Han's mods and Sanchozz's mods are the most popular ONI mods and neither covers this use case, there's a clear gap in the market.
