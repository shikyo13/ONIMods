# ONI Mods

Mods for [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included), built with Harmony 2.0 and PLib.

---

## Duplicant Status Bar

> RimWorld-style colonist bar for ONI. Always visible, always watching your dupes.

![DuplicantStatusBar Preview](DuplicantStatusBar/preview_gh.gif)

A persistent bar showing every duplicant on the current asteroid — stress-colored borders, expression-driven portraits, health bars, alert badges, and detailed hover tooltips. Click any widget to snap the camera to that dupe. Does not affect steam achievements.

### Highlights

- **Expression-driven portraits** — dupes show contextual facial expressions with periodic blinking
- **Stress-colored borders** — 5-tier gradient from green to pulsing red, with configurable thresholds
- **Health fill bar** — vertical drain with green-to-red gradient and damage overlay
- **13 alert types** — suffocating, low HP, incapacitated, scalding, hypothermia, irradiated, starving, diseased, bladder, overstressed, overjoyed, stuck, idle
- **Animated tooltip** — stats with color gradients, active alerts with animated text, rainbow cycling for overjoyed dupes
- **In-game Sort/Filter popup** — sort by stress, calories, name, or job role; filter by alerts, stress level, job role, or individual dupes
- **Multi-row layout with scroll** — wraps into rows automatically, scrollbar when rows exceed limit
- **Draggable, collapsible, resizable** — drag header to reposition, resize grip for portrait size, all persisted
- **Respects game UI scale** — scales with Options > Graphics > UI Scale slider
- **Mod API** — other mods can register custom alerts, hook tooltips, and listen to events ([API Guide](DuplicantStatusBar/docs/api-guide.md))
- **7 languages** — Chinese, Korean, Russian, Japanese, German, Spanish, French

### Install

[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3450387042)

---

## ReplaceStuff

Replace existing buildings with upgraded versions without deconstructing first. Supports tiles, doors, ladders, fire poles, and more — including modded doors (e.g., Fast Insulated Self Sealing AirLock).

- Footprint-strict replacement groups (same width x height only)
- Vanilla replacement pipeline integration (`TryReplaceTile`, `ReplacementTags`)
- Multi-cell tile cleanup for non-anchor cells

---

## BuildThrough

Build and deconstruct through walls. Transpiler-based patching of the offset table to bypass solid cell checks.

---

## OniProfiler

In-game performance profiler toggled with backtick/F8. Instruments 29+ game systems, PlayerLoop phases, 193 bulk Update methods, and coroutine census. Zero overhead when closed.

- Spike capture with pre-allocated pools (zero GC)
- Data recording to CSV with interval accumulation
- Stopwatch-based frame timing (fixes one-frame offset bug)

---

## GCBudget

Alloc-gated garbage collection — triple-gate system (frame budget, cooldown, alloc threshold) to reduce GC stutter spikes. Proof of concept for mitigating Unity Mono's non-incremental GC pauses.

---

## Building

```bash
dotnet build <ModName>/<ModName>.csproj
```

Requires game DLLs from your ONI installation. See individual `.csproj` files for reference paths.

## License

MIT
