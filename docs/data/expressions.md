# Expressions & Faces  - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

> Frame indices in "Known Frame Indices" section are from runtime kanim parsing
> (`head_master_swap_kanim`), not assembly data. They may change with game updates.
> See `ExpressionResolver.EnsureDiscovered()` for authoritative runtime dump.

## Expression Class Structure

| Field | Type | Notes |
|-|-|-|
| face | Face | The Face this expression uses |
| priority | int | Auto-assigned: `100 * (count - insertionIndex)` |

## Face Class Structure

| Field | Type | Notes |
|-|-|-|
| hash | HashedString | Anim hash  - matches kanim animation name |
| headFXHash | HashedString | VFX overlay hash (null if no VFX) |
| SYMBOL_PREFIX | string (const) | `"headfx_"`  - prepended to build headFX symbol name |

## Expressions

Ordered by constructor insertion (highest priority first = last inserted gets lowest number).

| # | Expression | Face Used | Priority | Notes |
|-|-|-|-|-|
| 1 | Neutral | Neutral | 3200 | Default fallback |
| 2 | Happy | Happy | 3100 | Calm stress tier |
| 3 | Uncomfortable | Uncomfortable | 3000 | |
| 4 | Cold | Cold | 2900 | Has headFX |
| 5 | Hot | Hot | 2800 | |
| 6 | FullBladder | Hungry | 2700 | Reuses Hungry face |
| 7 | Tired | Tired | 2600 | |
| 8 | Hungry | Hungry | 2500 | |
| 9 | Angry | Angry | 2400 | |
| 10 | Unhappy | Uncomfortable | 2300 | Reuses Uncomfortable face |
| 11 | RedAlert | Angry | 2200 | Reuses Angry face |
| 12 | Suffocate | Suffocate | 2100 | Has headFX |
| 13 | RecoverBreath | Suffocate | 2000 | Reuses Suffocate face |
| 14 | Sick | Sick | 1900 | Has headFX |
| 15 | SickSpores | SickSpores | 1800 | Has headFX |
| 16 | Zombie | Zombie | 1700 | Has headFX |
| 17 | SickFierySkin | SickFierySkin | 1600 | Has headFX |
| 18 | SickCold | SickCold | 1500 | Has headFX |
| 19 | Pollen | Pollen | 1400 | Has headFX |
| 20 | Relief | Uncomfortable | 1300 | Reuses Uncomfortable face |
| 21 | Productive | Productive | 1200 | |
| 22 | Determined | Determined | 1100 | |
| 23 | Sticker | Uncomfortable | 1000 | Reuses Uncomfortable face |
| 24 | Balloon | Neutral | 900 | Reuses Neutral face |
| 25 | Sparkle | Sparkle | 800 | Override frames in DSB mod |
| 26 | Music | Music | 700 | |
| 27 | Tickled | Tickled | 600 | |
| 28 | Radiation1 | Radiation1 | 500 | |
| 29 | Radiation2 | Radiation2 | 400 | |
| 30 | Radiation3 | Radiation3 | 300 | |
| 31 | Radiation4 | Radiation4 | 200 | |
| 32 | BionicJoy | Robodancer | 100 | Bionic-only face |

## Faces

| Face ID | HeadFX | Notes |
|-|-|-|
| Neutral | none | |
| Happy | none | |
| Uncomfortable | none | |
| Cold | has headFX | |
| Hot | none | |
| Tired | none | |
| Sleep | none | Used for blink frame discovery |
| Hungry | none | |
| Angry | none | |
| Suffocate | has headFX | |
| Dead | none | Blink fallback #2 |
| Sick | has headFX | |
| SickSpores | has headFX | |
| Zombie | has headFX | |
| SickFierySkin | has headFX | |
| SickCold | has headFX | |
| Pollen | has headFX | |
| Productive | none | |
| Determined | none | |
| Sticker | none | |
| Balloon | none | |
| Sparkle | none | |
| Tickled | none | |
| Music | none | |
| Radiation1 | none | |
| Radiation2 | none | |
| Radiation3 | none | |
| Radiation4 | none | |
| Robodancer | none | Bionic dance face |

## Known Frame Indices (from DuplicantStatusBar runtime discovery)

Source: `ExpressionResolver.GetFrames()` + `head_master_swap_kanim` parsing.

| Expression | Eye Frame | Mouth Frame | Notes |
|-|-|-|-|
| Neutral | 0 | 0 | Default/fallback |
| Happy | 0 | 22 | Smiling mouth; default portrait expression |
| Angry | 6 | 1 | |
| Suffocate | 3 | 7 | |
| Cold | 3 | 2 | |
| Hot | 2 | 3 | |
| Hungry | 5 | 6 | |
| Tired | 4 | 5 | Blink fallback #3 |
| Sick | 19 | 23 | |
| Sparkle | 27 (raw) | 28 | DSB overrides to eye=22 mouth=28 (raw eye is overlay) |
| Uncomfortable | 1 | 1 | |
| Dead | 0 | 0 | Not in kanim  - uses fallback; blink fallback #2 |
| Productive | 12 | 31 | |
| Sleep | 4 |  - | Eye frame 4 = closed eyes; used for blink via `GetBlinkFrame()` |

See `frame-map.md` for full 43-face kanim dump including non-DB faces (veryhappy, raging, insanity, etc.).

## KAnim Symbol Layers

| Symbol | Purpose |
|-|-|
| snapto_eyes | Eye accessory frame reference (FaceGraph.ANIM_HASH_SNAPTO_EYES) |
| snapto_mouth | Mouth accessory frame reference (FaceGraph.ANIM_HASH_SNAPTO_MOUTH) |
| headfx_ prefix | VFX overlay layer (Face.SYMBOL_PREFIX const) |

## Key Runtime Classes

| Class | Role |
|-|-|
| FaceGraph | Drives face animation; owns snapto_eyes/snapto_mouth hashes |
| BlinkMonitor | Reads HASH_SNAPTO_EYES for blink animation |
| SpeechMonitor | Reads HASH_SNAPTO_MOUTH for speech bubble |
| ExpressionResolver (mod) | Maps AlertType/StressTier to ExpressionType, discovers frame indices |
