# DuplicantStatusBar — Handover

## Purpose & Status
**Version**: v2.6.0
**Branch**: master
**Build**: clean, 0 warnings

RimWorld-style colonist bar showing dupe portraits with stress-colored borders and alert badges. Always visible at top-center of screen.

## Architecture

| File | Purpose |
|-|-|
| `Core/DuplicantStatusBarMod.cs` | UserMod2 entry, PLib init |
| `Config/StatusBarOptions.cs` | PLib options (sort, size, opacity, thresholds, alert toggles) |
| `Data/DupeStatusTracker.cs` | Polls `LiveMinionIdentities` every 0.25s, creates `DupeSnapshot` structs |
| `UI/StatusBarScreen.cs` | MonoBehaviour on Game object; builds uGUI Canvas + manages widgets |
| `UI/DupePortraitWidget.cs` | Individual card: compositor portrait (>=36px) or initials fallback + colored border + alert badge |
| `UI/PortraitCompositor.cs` | Static utility: composites dupe accessories from KAnim atlas into Texture2D/Sprite |
| `UI/AlertEffects.cs` | Alert effect definitions, procedural sprite cache, alpha evaluation |
| `UI/ExpressionResolver.cs` | Maps alert/stress → expression → eye/mouth frame indices (runtime discovery from kanim) |
| `UI/DupeTooltip.cs` | Hover tooltip: name, task, stress/health/breath/temp/calories/bladder, animated alert text |
| `UI/SortFilterPopup.cs` | In-game sort/filter dropdown — sort modes, smart filters, role/dupe visibility |
| `UI/ColorUtil.cs` | Centralized color palette — stat gradients, alert badges, stress tiers, UI chrome |
| `UI/DiagnosticDump.cs` | Debug-only portrait diagnostic output (disabled in production) |
| `Localization/DSBStrings.cs` | All LocString definitions (UI, alerts, options, popup) |
| `Patches/GamePatches.cs` | `Game.OnPrefabInit` postfix — injects `StatusBarScreen` |
| `Patches/TranslationPatch.cs` | Loads .po translation files at game start |
| `API/Experimental/DSBApi.cs` | Public static entry point for extensibility API |
| `API/Experimental/AlertRegistration.cs` | Custom alert registration type + AlertPattern enum |
| `API/Experimental/TooltipContext.cs` | Tooltip hook context |
| `API/Experimental/Events.cs` | Event struct definitions (AlertChanged, Widget, Snapshot) |
| `API/Internal/AlertRegistry.cs` | Registration storage, detection, event dispatch |

## Data Flow

1. `GamePatches` adds `StatusBarScreen` to `Game.gameObject` on prefab init
2. Every 0.25s, `DupeStatusTracker.Update()` polls all dupes on the active world
3. For each dupe: reads Stress, HitPoints, Breath, Bladder, Calories amounts + PrimaryElement temp + ChoreDriver + Sicknesses + JoyBehaviourMonitor + ScaldingMonitor + SuffocationMonitor + RadiationMonitor + CalorieMonitor + Navigator
4. Computes `StressTier` (5 tiers) and `AlertType` (13 types, priority-ordered)
5. Evaluates registered custom alerts via `AlertRegistry.EvaluateCustomAlerts()`
6. Fires `AlertChangedEvent` for built-in and custom alert state transitions
7. Applies sort/filter (sort mode, alerts-only, stressed-only, role filter, per-dupe hide)
8. `StatusBarScreen.RefreshWidgets()` syncs widget count and updates each
9. Width-based row wrapping: columns fit within `MaxBarWidth%` of canvas, overflow rows scroll via `ScrollRect`

## Key Design Decisions

- **uGUI, not IMGUI**: portraits need proper layout, sprites, pointer events
- **MonoBehaviour + own Canvas**, not KScreen: independent of game's screen stack, won't conflict
- **Texture compositing portraits with initials fallback**: reads KAnim atlas textures directly, composites accessory sprites layer-by-layer (bypasses broken KAnim batch pipeline for ScreenSpaceOverlay); falls back to 2-letter initials below 36px
- **Stress border**: 5-tier color gradient (green→lime→yellow→orange→red) with pulse on critical
- **Alert badges**: 13-type priority system (incapacitated > suffocating > lowHP > scalding > hypothermia > stuck > irradiated > starving > overstressed > bladder > diseased > overjoyed > idle)
- **Drag via header**: position saved to PlayerPrefs, survives restarts
- **Collapse button**: minimizes to just the header bar
- **Width-based wrapping**: `MaxBarWidth%` of canvas width determines columns per row; excess rows scroll
- **ScrollRect overflow**: when rows exceed `MaxBarRows`, a vertical `ScrollRect` with thin rounded scrollbar activates (clamped movement, 20 scroll sensitivity)
- **Min card size 16px**: drag-resize and options allow down to 16px (initials at ~8px font, portraits auto-disabled below 36px)

## Stress Tiers

| Tier | Default Range | Color |
|-|-|-|
| Calm | 0–20% | #4ade80 (green) |
| Mild | 20–40% | #a3e635 (lime) |
| Stressed | 40–60% | #fbbf24 (yellow) |
| High | 60–80% | #f97316 (orange) |
| Critical | 80%+ | #ef4444 (red) + pulse |

## Alert Detection

| Alert | Detection Method |
|-|-|
| Suffocating | `Amounts.Breath` < 30% of max |
| Low HP | `Amounts.HitPoints` < 30% of max |
| Scalding | `PrimaryElement.Temperature` > 348.15K (75°C) |
| Hypothermia | `PrimaryElement.Temperature` < 263.15K (-10°C) |
| Overstressed | Stress >= 100% |
| Diseased | `Sicknesses.IsInfected()` |
| Overjoyed | `JoyBehaviourMonitor.Instance` in `overjoyed` state |
| Stuck | `Navigator.GetNavigationCost(podCell) < 0` for 10s (checked every 2s) |
| Idle | `ChoreDriver` chore name == "Idle" for 30s continuous |

## Portrait Rendering — Texture Compositing

**Approach**: Static `Sprite` portraits generated by reading accessory symbols from KAnim texture atlases and compositing them layer-by-layer onto a `Texture2D`. Displayed via standard `UnityEngine.UI.Image`.

**Why not KAnim canvas rendering**: `KBatchedAnimCanvasRenderer` (materialType=UI) creates the batch and renderer correctly but `KAnimBatchManager.UpdateDirty()` never populates mesh/material data for ScreenSpaceOverlay canvases. Mesh vertices stay uninitialized and all material textures remain NULL. The vanilla `MinionPortrait` prefab also produces invisible portraits. Root cause: batch processing depends on the game's screen management system (ScreenSpaceCamera mode).

**Key files**:
- `UI/PortraitCompositor.cs` — static utility: `ComposePortrait(MinionIdentity, int size)` → `Sprite`
- `UI/DupePortraitWidget.cs` — displays compositor sprite via `Image`, falls back to initials below 36px

**Compositing pipeline**:
1. Get `Accessorizer` component → iterate accessory slots
2. For each slot: `symbol.GetFrame(0)` → `uvMin/uvMax` → atlas rect → `Sprite.Create()`
3. `GetReadableCopy()` via `RenderTexture` + `ReadPixels` (GPU textures aren't CPU-readable by default)
4. Extract sprite region pixels → alpha-blend onto output texture with pivot-based positioning
5. Layer order (back to front): HeadShape → Eyes (3° CCW rotation, bottom-anchored) → Mouth (top-anchored) → Hair (or HatHair+Hat)

**Caching**: Readable atlas copies cached in `Dictionary<Texture2D, Texture2D>`. Sprite region textures cached per `Sprite`. Caches cleared on `StatusBarScreen.OnDestroy()`.

**Memory**: ~64 KB per portrait (128×128 RGBA32). 19 dupes ≈ 1.2 MB. Old textures destroyed on identity/hat change via `DestroyPortraitSprite()`.

## v2.0 — ONI Visual Overhaul

Adopted ONI's native color palette, rounded panels, and game fonts:

- **Panel bg**: `#2A3545` (blue-gray, was dark gray)
- **Card fill**: `#1E2A38` with stress-color lerp at 0.15
- **Card border**: `#3D5060` base for calm, tier colors for elevated stress
- **Header text**: `#A0ADB8`, **name labels**: `#E8EDF2`
- **Collapse button**: `#3D5060`
- **Tooltip**: `#1E2A38 @ 0.95` bg, `#E8EDF2` text
- **Rounded corners**: procedural `MakeRoundedRect(32, 8)` → 9-slice sprite on panel, card, tooltip
- **Game font**: runtime discovery of `GRAYSTROKE REGULAR SDF` → NotoSans fallback → TMP default
- **Badge scaling**: `Mathf.Max(10f, cardSz * 0.30f)` (was fixed 14×14), position `(3, 3)`

## v2.1 — Multi-Row Wrap + Scroll

- **MaxBarWidth** option (20–100%, default 50%): replaces hard-coded `Screen.width * 0.8f` with `canvasRT.rect.width * MaxBarWidth%`
- **MaxBarRows** option (0–10, default 3): vertical scroll threshold (0 = unlimited)
- **PortraitSize** lower limit: 24 → 16px
- **ScrollRect hierarchy**: ScrollView (ScrollRect + LayoutElement) → Viewport (Mask) → Content (grid) + VScrollbar (6px, `RoundedRect` handle at 35% opacity)
- **LayoutElement bridge**: `UpdateGridLayout` computes `preferredWidth`/`preferredHeight` explicitly since ScrollRect absorbs child preferred-size signals
- **Collapse**: toggles `scrollViewGO` visibility (grid stays active inside viewport)

## Stuck & Idle Detection (v2.1.2)

- **Stuck**: `Navigator.GetNavigationCost(cachedPodCell)` returns -1 → unreachable. Checked every 2s, threshold 10s. Pod cell cached from `Components.Telepads.GetWorldItems()` every 30s.
- **Idle**: chore name string comparison `== "Idle"`, accumulated via `Time.unscaledDeltaTime`, threshold 30s.
- Timer dictionaries keyed by `GameObject.GetInstanceID()`, pruned when dead dupe count exceeds live count + 5.
- Badge colors: Stuck = amber `#F59E0B` ("!"), Idle = gray `#9CA3AF` ("?").
- Priority: Stuck after Hypothermia (near life-threatening), Idle lowest (after Overjoyed).
- FastTrack compatible: reads cached PathProber data, no new pathfind triggered.

## v2.1.3 — Pause-Safe Timers + Distinct Alert Colors

- **Pause-safe `dt`**: Idle and stuck timers now use `Time.deltaTime` (pauses when game pauses) instead of `Time.unscaledDeltaTime`, preventing false alerts from reading tooltips while paused
- **Alert color palette redesign**: 12 alerts now use distinct colors spanning the full color wheel:
  - Suffocating: `#3B82F6` (blue)
  - Low HP: `#EF4444` (red)
  - Scalding: `#F97316` (orange)
  - Hypothermia: `#06B6D4` (cyan)
  - Irradiated: `#A855F7` (purple)
  - Starving: `#F59E0B` (amber)
  - Bladder: `#EC4899` (pink)
  - Diseased: `#84CC16` (lime)
  - Overstressed: `#EF4444` (red, pulsing)
  - Overjoyed: `#FACC15` (gold)
  - Stuck: `#D97706` (dark amber)
  - Idle: `#9CA3AF` (gray)

## v2.2.0 — Per-Alert Visual Effects

- **Animated tooltip text**: replaced rich-text alert lines with 5 pooled TMP elements. Each alert gets per-character vertex color animation with glow. Overjoyed uses smooth rainbow cycling; other alerts pulse monochromatically.
- **New file `UI/AlertEffects.cs`**: static data layer — `AlertEffect` struct, `EvaluateAlpha()` with three animation patterns (Pulse/Heartbeat/Flicker), used by tooltip animation
- **Renamed**: `TooltipRainbowDriver` → `TooltipAnimationDriver`, `AnimateRainbow()` → `AnimateTexts()`
- **Bug fixes**: removed `✦` characters from Overjoyed tooltip (game font lacks glyph), fixed blank line always appearing before alert section
- **Removed portrait overlay**: per-alert color overlay on cards obscured the health bar — tooltip-only animated text is the current approach

## v2.3.0 — Expression-Driven Portraits

- **Runtime face discovery**: `ExpressionResolver.EnsureDiscovered()` parses `head_master_swap_kanim` at first use — iterates animation frame elements for `snapto_eyes`/`snapto_mouth` symbols, builds `HashedString→ExpressionFrames` dict matching ONI's `Database.Faces` hashes
- **Alert→expression mapping**: `ExpressionResolver.Resolve(alert, tier)` — highest alert determines expression (Suffocating→Suffocate, LowHP→Angry, etc.); no alert falls back to stress tier (Calm→Happy, Mild→Neutral, Stressed→Uncomfortable, High/Critical→Angry)
- **13 expression types**: Neutral, Happy, Angry, Suffocate, Cold, Hot, Hungry, Tired, Sick, Sparkle, Uncomfortable, Dead, Productive
- **Compositor changes**: `ComposePortrait(identity, eyeFrame, mouthFrame)` — base cache validates against frames, rebuilds when expression changes (evicts old texture)
- **Blink system**: per-widget timer (3-8s random interval, 0.15s duration), closed-eye frame discovered from Sleep face in kanim. Skips if `GetBlinkFrame()` returns -1 or matches current eye frame. Recomposites via `RecomposeWithEyes()`.
- **Option**: `EnableExpressions` (bool, default true) — disabling reverts to static portraits (eyes=0, mouth=22)

## v2.3.1 — Expression Sprite Sizing & Blink Fallback

**Root cause**: Different expression frames extract to different pixel dimensions from the atlas. All sprites share PPU=50, so larger sprites (e.g., Sparkle mouth ~50×30 vs Happy mouth ~27×15) have intentionally larger bboxes. The center-offset compositor (`xStart = center - spriteWidth/2 + offset`) extends oversized sprites equally in all directions, pushing top edges into eye areas.

**Fix — proportional size clamping**: `WriteSymbolDirect` accepts optional `maxWidth`/`maxHeight` parameters. Oversized sprites are uniformly scaled down via `ScaleTexture` (GPU `RenderTexture` + `Graphics.Blit` downsampling). Max dimensions: eyes 68×40 px, mouth 45×22 px (~84%/50% and ~55%/27% of head's ~81×80 px).

**Blink fallback**: Sleep face not found in some kanim variants → `blinkEyeFrame = -1`. Added Tired face as third fallback after Sleep and Dead (droopy/half-closed eyes as blink substitute).

**Diagnostic enhancement**: Section 4 now exports sprites for ALL 13 expression types (`eyes_sparkle.png`, `mouth_angry.png`, etc.) with pixel dimensions logged, enabling visual verification of clamping effectiveness.

**Previous approaches tried and rejected**:
- Bbox-relative positioning (commit b347224, reverted): computed bbox-center offsets but still centered at target — didn't solve sizing. Mixed KAnim units with pixels in `usePivot` math.
- Per-expression offset tables: fragile — would need tuning per expression AND per sprite size. All expressions share nearly identical snapto transforms.
- Anchor-based positioning: anchor fractions vary per-dupe (0.04–0.31), introducing per-dupe vertical variation.

**Variant system note**: `head_master_swap_kanim` contains 3 body type variants with 7/5/8 elements each. The `ContainsKey` first-variant fix (commit 1f7020c) resolved wrong-frame-index bugs by taking the first matching animation hash.

## v2.3.2 — Y-Offset Tuning + Sparkle Eye Fix

**Y-offset tuning**: Eyes shifted 4px lower (`yOffset: -4 + PORTRAIT_Y_SHIFT`), mouth shifted 6px lower (`yOffset: -18 + PORTRAIT_Y_SHIFT`, was -12). Net positions from canvas center (62): eyes ≈ y=50, mouth ≈ y=36.

**Sparkle white-circle fix**: ONI's Sparkle expression renders sparkle effects as overlays on normal eyes. The `snapto_eyes` frame discovered from `head_master_swap_kanim` for Sparkle is the overlay base shape (white circles), not standalone eyes. `GetFrames()` now overrides Sparkle with explicit frame indices (eye=22, mouth=28 per sgt_imalas), with diagnostic logging of discovered vs overridden values.

**Hat clipping variance**: per-hat bbox pivots produce different vertical offsets — some hats clip eyes more than others. This is inherent to the per-hat pivot system and partly matches in-game appearance (hats cover foreheads). No code change needed.

## v2.3.3 — Edge-Anchored Feature Positioning

**Root cause**: Center-offset positioning (`yStart = center - spriteH/2 + offset`) makes the bottom edge of eyes height-dependent. A 35px eye sprite extends 5px further toward the mouth than a 25px eye sprite, creating per-dupe eye-mouth spacing variation (e.g., Hassan's mouth crowds his eye while Ditto/Burt look correct).

**Fix — `VerticalAnchor` enum**: `WriteSymbolDirect` accepts a `VerticalAnchor` parameter (`Center`/`Bottom`/`Top`). `Bottom` anchors the bottom edge at `center + yOffset`; `Top` anchors the top edge at `center + yOffset - spriteHeight`. Eyes use `Bottom`, mouth uses `Top`, all other layers remain `Center`.

**Net positions** (canvas center=62, `PORTRAIT_Y_SHIFT=-8`):
- Eye bottom edge: 62 + (-14 + -8) = **y=40** — constant for all dupes
- Mouth top edge: 62 + (-20 + -8) = **y=34** — constant for all dupes
- Gap: **6px** — consistent regardless of sprite dimensions

**Growth direction**: taller eyes extend upward (into hat/hair area — covered by hair/hat layer drawn last), taller mouths extend downward (toward chin — harmless).

**Tuning**: adjust `-14` (eye yOffset) and `-20` (mouth yOffset) to move anchor positions. The gap equals `eyeAnchor - mouthAnchor` in offset terms (currently 6px).

## v2.3.4 — Portrait Offset Tuning Round 2 + Eye Rotation

**Offset tuning**: multiple rounds of visual tuning to fix eye/mouth spacing, hat positioning, and eye tilt asymmetry from ONI's 3/4 perspective sprites.

**Final offsets** (all relative to canvas center 62, `PORTRAIT_Y_SHIFT=-8`):

| Layer | xOffset | yOffset | Notes |
|-|-|-|-|
| Head | 0 | 0 (shifted by PORTRAIT_Y_SHIFT) | — |
| Eyes | 4 | -16 | Bottom-anchored, 3° CCW rotation |
| Mouth | 6 | -12 | Top-anchored |
| Hair (no hat) | 8 | 28 | Pivot-based |
| HatHair (under hat) | 8 | 30 | Pivot-based |
| Hat | 6 | 30 | Pivot-based |

**Eye rotation**: `WriteSymbolDirect` gained a `rotation` parameter (float, degrees). When non-zero, uses inverse-mapped pixel sampling (iterate output pixels, reverse-rotate to find source coords) to avoid gaps. Eyes use 3° CCW to counteract the perceived tilt from ONI's 3/4 perspective eye sprites where the right eye appears lower than the left.

**Key learnings**:
- ONI eye sprites are a single image containing both eyes — no independent left/right control
- The perceived eye tilt is baked into the sprite's 3/4 perspective art, not a positioning bug
- Positive yOffset = up on texture (Unity texture Y=0 is bottom)
- Hat, HatHair, and Hair are three independent layers: Hat is the headwear, HatHair is hair visible under a hat, Hair is the full hairstyle when no hat is worn

## v2.5.1 — Game UI Scale Support

DSB matches the game's UI scale by using `ConstantPixelSize` mode and reading the game's `KCanvasScaler.GetCanvasScale()` directly. This mirrors the game's own scaling approach (which combines user scale preference with a resolution-based step table via `ScreenRelativeScale()`), ensuring correct sizing on all displays including Mac Retina / high-DPI.

**Previous approach (pre-fix)**: used `ScaleWithScreenSize` mode with adjusted `referenceResolution`. This caused oversized bars on high-DPI displays (GitHub issue #1) because Unity's automatic screen÷reference division produces different scale factors than the game's `ConstantPixelSize` + manual `scaleFactor` approach, especially when `Screen.height` ≠ 1080.

**Fix**: switched to `ConstantPixelSize` and reading `KCanvasScaler.GetCanvasScale()` from the game's own canvas scaler instance (cached on first access, with `FindObjectOfType` fallback). Zero maintenance if Klei changes their scale steps.

## v2.6.0 — Extensibility API

Public API in `DuplicantStatusBar.API.Experimental` namespace. External mods can:

- **Register custom alerts** via `DSBApi.RegisterAlert()` — detector function called every 0.25s per dupe, results displayed as badges and tooltip entries alongside built-in alerts
- **Hook tooltip construction** via `DSBApi.RegisterTooltipHook()` — append/modify tooltip text
- **Listen to events** — `OnAlertChanged`, `OnWidgetCreated/Destroyed`, `OnSnapshotUpdated`, `OnBarVisibilityChanged`

### Internal Architecture

| File | Purpose |
|-|-|
| `API/Experimental/DSBApi.cs` | Public static entry point — validation + delegation |
| `API/Experimental/AlertRegistration.cs` | Registration type + AlertPattern enum |
| `API/Experimental/TooltipContext.cs` | Tooltip hook context |
| `API/Experimental/Events.cs` | Event struct definitions |
| `API/Internal/AlertRegistry.cs` | Registration storage, detection, event dispatch |

### Key Design Decisions

- **No enum changes**: custom alerts use string IDs, not `AlertType` enum values. Built-in `AlertMask` bitmask unchanged.
- **Separate storage**: custom alert state stored in `Dictionary<string, bool>` per snapshot (`DupeSnapshot.CustomAlerts`), not in the bitmask.
- **Error isolation**: every external callback is try/caught. A broken mod never crashes the status bar.
- **Copy-on-iterate**: event dispatch snapshots listener lists into arrays before iteration, so `Dispose()` during a callback is safe.
- **`System.Action` qualification**: Unity's `PlayerLoop` namespace shadows `System.Action` — fully qualified in AlertRegistry.
- **Snapshot semantics**: `AlertRegistration` fields are snapshotted at registration time via `Snapshot()`. Mutations after `RegisterAlert()` have no effect.

### Integration Points

1. **Alert detection**: `DupeStatusTracker.Update()` → after `ComputeAlerts()`, calls `AlertRegistry.EvaluateCustomAlerts()` per dupe
2. **Alert change events**: built-in alerts diffed via `previousAlertMasks` dictionary; custom alerts diffed inside `AlertRegistry`
3. **Badge rendering**: `DupePortraitWidget.UpdateBadges()` renders custom alert badges in slots after built-in badges
4. **Tooltip**: `DupeTooltip.Show()` adds custom alert text slots after built-in slots, then invokes tooltip hooks
5. **Widget events**: `StatusBarScreen.RefreshWidgets()` fires `WidgetCreated`/`WidgetDestroyed`
6. **Visibility events**: `StatusBarScreen.ToggleCollapse()` fires `BarVisibilityChanged`

See `docs/api-guide.md` for the external mod author guide.

## Not Yet Implemented

- Phase 2: Animated KBAC portraits (ScreenSpaceCamera canvas with KAnimBatchManager, staggered RT capture)
