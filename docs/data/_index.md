# ONI Game Data Maps — Index
Game build: U58-717001 | Generated: 2026-03-16

Indexed reference tables extracted from Assembly-CSharp.dll. Use instead of ad-hoc decompilation lookups.

## File Index

| File | Category | Entries | Lines |
|-|-|-|-|
| [amounts.md](amounts.md) | Dupe/Critter/Plant amounts (Stress, Calories, etc.) | 38 | 65 |
| [skills.md](skills.md) | Duplicant skills by group | 52 | 134 |
| [choretypes.md](choretypes.md) | Chore types with priority tiers | 142 | 195 |
| [rooms.md](rooms.md) | Room types with requirements + bonuses | 20 | 101 |
| [elements.md](elements.md) | SimHashes — all elements by state | 191 | 213 |
| [effects.md](effects.md) | Common effects with modifiers + durations | ~120 | 188 |
| [components.md](components.md) | Curated monitors/components with key methods | 53 | 91 |
| [status-items-dupe.md](status-items-dupe.md) | Duplicant status items | 151 | 119 |
| [status-items-bldg.md](status-items-bldg.md) | Building status items | 324 | 86 |
| [status-items-misc.md](status-items-misc.md) | Misc/Robot/Creature status items | 141 | 250 |
| [expressions.md](expressions.md) | Expressions, faces, priority, headFX, frame indices | 32+29 | 125 |
| [frame-map.md](frame-map.md) | Runtime kanim frame dump (all eye/mouth frames + transforms) | 43 | 74 |
| [accessory-slots.md](accessory-slots.md) | Accessory slots, symbols, kanim sources | 22 | 135 |
| [traits.md](traits.md) | Dupe traits by category with stat modifiers | 105 | 182 |
| [diseases-sicknesses.md](diseases-sicknesses.md) | Germs, sicknesses, medicines, cures | 19 | 89 |
| [schedules.md](schedules.md) | Schedule block types + default 24-hour pattern | 5+24 | 104 |
| [buildings.md](buildings.md) | Curated building configs by category | 148 | 250 |
| [plib-options.md](plib-options.md) | PLib options API — attributes, interfaces, patterns | — | 156 |

## Usage

1. Check this index for the right file
2. Open the file → Ctrl+F for the ID or display name
3. Cross-ref string paths: `STRINGS.DUPLICANTS.STATS.<UPPER_ID>.NAME`

## Runtime-Generated Files

`frame-map.md` is written by `ExpressionResolver.DumpFrameMap()` on first portrait render in-game.
It contains the authoritative eye/mouth frame indices from `head_master_swap_kanim` parsing.
After capture, remove the `DumpFrameMap(data)` call in `ExpressionResolver.EnsureDiscovered()`.

## Refresh Checklist

When game updates break mods:
- [ ] Re-extract affected category using re-orchestrator MCP tools
- [ ] Update game build version in file header
- [ ] Update entry counts in this index
