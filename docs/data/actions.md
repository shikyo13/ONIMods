# Actions (Hotkeys) - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

## Section Index
Use `Read` tool with `offset` and `limit` to load specific sections only.

| # | Topic | Lines |
|-|-|-|
| 1 | Mouse / Core Input | 34-44 |
| 2 | Camera / Zoom | 46-56 |
| 3 | Speed Control | 58-65 |
| 4 | View / Misc | 67-72 |
| 5 | User Navigation Bookmarks | 74-79 |
| 6 | Build Menu Categories (Plan Tabs) | 81-89 |
| 7 | Build Category Shortcuts | 91-126 |
| 8 | Build Menu Hotkeys (A-Z) | 127-131 |
| 9 | Management Screens | 133-147 |
| 10 | Overlays | 149-167 |
| 11 | Building Interaction | 169-177 |
| 12 | Tool Actions (Commands) | 179-196 |
| 13 | Building Toggle / Tool | 198-206 |
| 14 | Screenshots | 207-211 |
| 15 | Debug Actions | 213-272 |
| 16 | Dialog | 273-277 |
| 17 | Sandbox Tools | 279-298 |
| 18 | Cinematic Camera | 299-316 |
| 19 | World Switching (DLC1) | 318-322 |
| 20 | Misc / Late Additions | 324-338 |
| 21 | Binding Contexts | 340-356 |
| 22 | Modifier Enum | 358-369 |

Source: `Action` enum in `Assembly-CSharp-firstpass.dll`, default bindings from `KInputHandler.GenerateDefaultBindings()`.

## Mouse / Core Input

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 0 | Invalid | - | - | Sentinel |
| 1 | Escape | Escape | No | Start (gamepad) |
| 2 | Help | - | - | |
| 3 | MouseLeft | Mouse0 | No | |
| 4 | ShiftMouseLeft | Shift+Mouse0 | No | |
| 5 | MouseRight | Mouse1 | No | |
| 6 | MouseMiddle | Mouse2 | No | |

## Camera / Zoom

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 7 | ZoomIn | ScrollUp | Yes | RT (gamepad) |
| 8 | ZoomOut | ScrollDown | Yes | LT (gamepad) |
| 134 | PanUp | W | Yes | |
| 135 | PanDown | S | Yes | |
| 136 | PanLeft | A | Yes | |
| 137 | PanRight | D | Yes | |
| 138 | CameraHome | H | Yes | |

## Speed Control

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 9 | SpeedUp | Numpad+ | Yes | |
| 10 | SlowDown | Numpad- | Yes | |
| 11 | TogglePause | Space | Yes | Back (gamepad) |
| 12 | CycleSpeed | Tab | Yes | |

## View / Misc

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 13 | AlternateView | LeftAlt / RightAlt | Yes | |
| 14 | DragStraight | LeftShift / RightShift | Yes | |

## User Navigation Bookmarks

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 15-24 | SetUserNav1-10 | Ctrl+1 through Ctrl+0 | Yes | Save camera position |
| 25-34 | GotoUserNav1-10 | Shift+1 through Shift+0 | Yes | Recall camera position |

## Build Menu Categories (Plan Tabs)

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 35 | BUILD_MENU_START_INTERCEPT | - | - | Sentinel for build menu range |
| 36-46 | Plan1-Plan11 | 1-9, 0, - | Yes | Plan tabs 1-11 |
| 47-48 | Plan12-Plan13 | =, Shift+- | Yes | |
| 49-50 | Plan14-Plan15 | Shift+=, Shift+Backspace | Yes | |
| 51 | CopyBuilding | B | Yes | |

## Build Category Shortcuts

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 52 | BuildCategoryLadders | - | - | |
| 53 | BuildCategoryTiles | - | - | |
| 54 | BuildCategoryDoors | - | - | |
| 55 | BuildCategoryStorage | - | - | |
| 56 | BuildCategoryGenerators | - | - | |
| 57 | BuildCategoryWires | - | - | |
| 58 | BuildCategoryPowerControl | - | - | |
| 59 | BuildCategoryPlumbingStructures | - | - | |
| 60 | BuildCategoryPipes | - | - | |
| 61 | BuildCategoryVentilationStructures | - | - | |
| 62 | BuildCategoryTubes | - | - | |
| 63 | BuildCategoryTravelTubes | - | - | |
| 64 | BuildCategoryConveyance | - | - | |
| 65 | BuildCategoryLogicWiring | - | - | |
| 66 | BuildCategoryLogicGates | - | - | |
| 67 | BuildCategoryLogicSwitches | - | - | |
| 68 | BuildCategoryLogicConduits | - | - | |
| 69 | BuildCategoryCooking | - | - | |
| 70 | BuildCategoryFarming | - | - | |
| 71 | BuildCategoryRanching | - | - | |
| 72 | BuildCategoryResearch | - | - | |
| 73 | BuildCategoryHygiene | - | - | |
| 74 | BuildCategoryMedical | - | - | |
| 75 | BuildCategoryRecreation | - | - | |
| 76 | BuildCategoryFurniture | - | - | |
| 77 | BuildCategoryDecor | - | - | |
| 78 | BuildCategoryOxygen | - | - | |
| 79 | BuildCategoryUtilities | - | - | |
| 80 | BuildCategoryRefining | - | - | |
| 81 | BuildCategoryEquipment | - | - | |
| 82 | BuildCategoryRocketry | - | - | |

## Build Menu Hotkeys (A-Z)

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 83-108 | BuildMenuKeyA-Z | A-Z | No | Context: "BuildingsMenu"; ignore_root_conflicts |

## Management Screens

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 109 | ManagePriorities | L | Yes | |
| 110 | ManageConsumables | F | Yes | |
| 111 | ManageVitals | V | Yes | |
| 112 | ManageResources | - | Yes | No default key |
| 113 | ManageResearch | R | Yes | |
| 114 | ManageSchedule | . (Period) | Yes | |
| 115 | ManageReport | E | Yes | |
| 116 | ManageDatabase | U | Yes | |
| 117 | ManageSkills | J | Yes | |
| 118 | ManageStarmap | Z | Yes | |
| 119 | ManageDiagnostics | - | Yes | No default key |

## Overlays

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 120 | Overlay1 | F1 | Yes | Oxygen |
| 121 | Overlay2 | F2 | Yes | Power |
| 122 | Overlay3 | F3 | Yes | Temperature |
| 123 | Overlay4 | F4 | Yes | Materials |
| 124 | Overlay5 | F5 | Yes | Light |
| 125 | Overlay6 | F6 | Yes | Liquid |
| 126 | Overlay7 | F7 | Yes | Gas |
| 127 | Overlay8 | F8 | Yes | Decor |
| 128 | Overlay9 | F9 | Yes | Disease |
| 129 | Overlay10 | F10 | Yes | Crop |
| 130 | Overlay11 | F11 | Yes | Rooms |
| 131 | Overlay12 | Shift+F1 | Yes | Automation |
| 132 | Overlay13 | Shift+F2 | Yes | Conveyor |
| 133 | Overlay14 | Shift+F3 | Yes | Suits |
| 134 | Overlay15 | Shift+F4 | Yes | Radiation (DLC1 only) |

## Building Interaction

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 139 | BuildingUtility1 | \\ (Backslash) | Yes | Context: "Building" |
| 140 | BuildingUtility2 | [ | Yes | |
| 141 | BuildingUtility3 | ] | Yes | |
| 142 | BuildingDeconstruct | X | Yes | |
| 143 | BuildingCancel | C | Yes | |

## Tool Actions (Commands)

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 144 | Dig | G | Yes | |
| 145 | Attack | T | Yes | |
| 146 | Capture | N | Yes | |
| 147 | Harvest | Y | Yes | |
| 148 | EmptyPipe | Insert | Yes | |
| 149 | AccessCleanUpCollection | - | - | |
| 150 | Mop | M | Yes | |
| 151 | Clear | K | Yes | |
| 152 | Disinfect | I | Yes | |
| 153 | AccessPrioritizeCollection | - | - | |
| 154 | Prioritize | P | Yes | |
| 155 | Deprioritize | - | - | |
| 156 | AccessRegionCollection | - | - | |
| 157-163 | SelectRegion through CreateExosuitRegion | - | - | Region tools |

## Building Toggle / Tool

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 164 | ToggleEnabled | Return | Yes | Context: "Building" |
| 165 | ToggleOpen | / (Slash) | Yes | Context: "Building" |
| 166 | ToggleScreenshotMode | Alt+S | Yes | |
| 167 | RotateBuilding | O | Yes | Context: "Tool" |

## Screenshots

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 168-171 | SreenShot1x-32x | Alt+1 through Alt+4 | Yes | Note: typo "Sreen" is in-game |

## Debug Actions

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 172 | DebugToggle | Backspace | Yes | |
| 173 | DebugSpawnMinion | Ctrl+F2 | Yes | |
| 174 | DebugSpawnStressTest | - | - | |
| 175 | DebugSuperTestMode | - | - | |
| 176 | DebugUltraTestMode | Ctrl+U | Yes | |
| 177 | DebugSlowTestMode | Ctrl+F5 | Yes | |
| 178 | DebugInstantBuildMode | Ctrl+F4 | Yes | |
| 179 | DebugToggleFastWorkers | Ctrl+Backspace | Yes | |
| 180 | DebugExplosion | Ctrl+F8 | Yes | |
| 181 | DebugDiscoverAllElements | Ctrl+F9 | Yes | |
| 182 | DebugTriggerException | Ctrl+F12 | Yes | |
| 183 | DebugTriggerError | Ctrl+Shift+F12 | Yes | Modifier=(Modifier)6 |
| 184 | DebugTogglePersonalPriorityComparison | Alt+0 | Yes | |
| 185 | DebugCheerEmote | Ctrl+C | Yes | |
| 186 | DebugDig | Ctrl+F6 | Yes | |
| 187 | DebugToggleUI | Alt+F1 | Yes | |
| 188 | DebugCollectGarbage | Alt+F3 | Yes | |
| 189 | DebugInvincible | Alt+F7 | Yes | |
| 190 | DebugRefreshNavCell | Alt+N | Yes | |
| 191 | DebugToggleClusterFX | Alt+F | Yes | DLC1 only |
| 192 | DebugApplyHighAudioReverb | - | - | |
| 193 | DebugApplyLowAudioReverb | - | - | |
| 194 | DebugForceLightEverywhere | Alt+F10 | Yes | |
| 195 | DebugPlace | Ctrl+F3 | Yes | |
| 196 | DebugVisualTest | - | - | |
| 197 | DebugGameplayTest | - | - | |
| 198 | DebugElementTest | Shift+F10 | Yes | |
| 199 | DebugRiverTest | - | - | |
| 200 | DebugTileTest | Shift+F12 | Yes | |
| 201 | DebugGotoTarget | Ctrl+Q | Yes | |
| 202 | DebugTeleport | Alt+Q | Yes | |
| 203 | DebugSelectMaterial | Ctrl+S | Yes | |
| 204 | DebugToggleMusic | Ctrl+M | Yes | |
| 205 | DebugToggleSelectInEditor | Alt+T | Yes | |
| 206 | DebugPathFinding | Alt+P | Yes | |
| 207 | DebugQuickDevActions | - | - | |
| 208 | DebugSuperSpeed | Alt+Z | Yes | |
| 209 | DebugGameStep | Alt+= | Yes | |
| 210 | DebugSimStep | Alt+- | Yes | |
| 211 | DebugNotification | Alt+X | Yes | |
| 212 | DebugNotificationMessage | Alt+C | Yes | |
| 213 | DebugReloadLevel | - | - | |
| 214 | DebugReloadMods | - | - | |
| 215 | ToggleProfiler | ` (BackQuote) | Yes | |
| 216 | ToggleChromeProfiler | Alt+` | Yes | |
| 217 | DebugReportBug | - | - | |
| 218 | DebugFocus | Ctrl+T | Yes | |
| 219 | DebugCellInfo | - | - | |
| 220 | DebugDumpGCRoots | Ctrl+F10 | Yes | |
| 221 | DebugDumpGarbageReferences | Ctrl+Shift+F10 | Yes | Modifier=(Modifier)3 |
| 222 | DebugDumpEventData | Ctrl+F11 | Yes | |
| 223 | DebugDumpSceneParitionerLeakData | Ctrl+F1 | Yes | |
| 224 | DebugCrashSim | Ctrl+Shift+F7 | Yes | Modifier=(Modifier)3 |
| 225 | DebugNextCall | Alt+9 | Yes | |
| 226 | DebugLockCursor | Alt+5 | Yes | |

## Dialog

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 227 | DialogSubmit | Return | No | |

## Sandbox Tools

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 228 | SandboxBrush | Shift+B | Yes | |
| 229 | SandboxSprinkle | Shift+N | Yes | |
| 230 | SandboxFlood | Shift+F | Yes | |
| 231 | SandboxMarquee | - | - | |
| 232 | SandboxSample | Shift+K | Yes | |
| 233 | SandboxHeatGun | Shift+H | Yes | |
| 234 | SandboxClearFloor | Shift+C | Yes | |
| 235 | SandboxDestroy | Shift+X | Yes | |
| 236 | SandboxSpawnEntity | Shift+E | Yes | |
| 237 | ToggleSandboxTools | Shift+S | Yes | |
| 238 | SandboxReveal | Shift+R | Yes | |
| 239 | SandboxRadsTool | - | - | |
| 240 | SandboxCritterTool | Shift+Z | Yes | |
| 241 | SandboxCopyElement | Ctrl+Mouse0 | Yes | |
| 242 | SandboxStressTool | Shift+J | Yes | |

## Cinematic Camera

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 243 | CinemaCamEnable | C | Yes | Context: "CinematicCamera"; ignore_root_conflicts |
| 244 | CinemaPanLeft | A | Yes | |
| 245 | CinemaPanRight | D | Yes | |
| 246 | CinemaPanUp | W | Yes | |
| 247 | CinemaPanDown | S | Yes | |
| 248 | CinemaZoomIn | I | Yes | |
| 249 | CinemaZoomOut | O | Yes | |
| 250 | CinemaPanSpeedMinus | - | - | |
| 251 | CinemaPanSpeedPlus | - | - | |
| 252 | CinemaZoomSpeedMinus | Shift+Z | Yes | |
| 253 | CinemaZoomSpeedPlus | Z | Yes | |
| 254 | CinemaToggleLock | T | Yes | |
| 255 | CinemaToggleEasing | E | Yes | |
| 256 | CinemaUnpauseOnMove | P | Yes | |

## World Switching (DLC1)

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 257-266 | SwitchActiveWorld1-10 | `+1 through `+0 | Yes | Modifier=Backtick; DLC1 only |

## Misc / Late Additions

| Value | Name | Default Key | Rebindable | Notes |
|-|-|-|-|-|
| 267 | DebugSpawnMinionAtmoSuit | Alt+F2 | Yes | |
| 268 | BuildMenuUp | - | - | |
| 269 | BuildMenuDown | - | - | |
| 270 | BuildMenuLeft | - | - | |
| 271 | BuildMenuRight | - | - | |
| 272 | AnalogCamera | None | No | Gamepad analog |
| 273 | AnalogCursor | None | No | Gamepad analog |
| 274 | Disconnect | Shift+D | Yes | |
| 275 | SandboxStoryTraitTool | Shift+T | Yes | |
| 276 | Find | Ctrl+F | Yes | |
| 277 | NumActions | - | - | Sentinel, total count |

## Binding Contexts

Actions are scoped to input contexts that determine when they are active:

| Context | Description |
|-|-|
| Root | Always active during gameplay |
| Tool | Active when a tool is selected |
| Building | Active when a building is selected |
| BuildingsMenu | Active inside the build menu |
| Management | Management screen hotkeys |
| Navigation | Camera bookmark shortcuts |
| Debug | Debug/dev mode only |
| Sandbox | Sandbox mode tools |
| CinematicCamera | Cinematic camera mode |
| SwitchActiveWorld | Colony switching (DLC1) |
| Analog | Gamepad analog inputs |

## Modifier Enum

| Value | Name |
|-|-|
| 0 | None |
| 1 | Ctrl |
| 2 | Alt |
| 3 | Ctrl+Shift (cast as (Modifier)3) |
| 4 | Shift |
| 5 | Backtick |
| 6 | Ctrl+Alt (cast as (Modifier)6) |
