# Schedules  - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

## Schedule Block Types

`Database.ScheduleBlockTypes`  - the atomic activity types that fill schedule hours.

| ID | Color (R,G,B) | Hex | Visual |
|-|-|-|-|
| Sleep | 0.984, 0.992, 0.271 | #FBFD45 | Yellow |
| Eat | 0.808, 0.529, 0.114 | #CE871D | Orange |
| Work | 0.937, 0.129, 0.129 | #EF2121 | Red |
| Hygiene | 0.459, 0.176, 0.345 | #752D58 | Purple |
| Recreation | 0.459, 0.373, 0.188 | #755F30 | Brown |

## Schedule Groups

`Database.ScheduleGroups`  - groups define which block types are allowed and their default segment count.

| Group ID | Default Segments | Allowed Block Types | Has Alarm |
|-|-|-|-|
| Hygene | 1 | Hygiene, Work | No |
| Worktime | 18 | Work | Yes |
| Recreation | 2 | Hygiene, Eat, Recreation, Work | No |
| Sleep | 3 | Sleep | No |

Note: "Hygene" is the actual field name in code (typo preserved from game source).

## Default Schedule Pattern (24 hours)

Created by `ScheduleManager.AddDefaultSchedule` using `ScheduleGroups.allGroups` ordering.
Groups are laid out sequentially: each group fills `defaultSegments` consecutive hours.

| Hour | Group | Allowed Block Types |
|-|-|-|
| 0 | Hygene | Hygiene, Work |
| 1 | Worktime | Work |
| 2 | Worktime | Work |
| 3 | Worktime | Work |
| 4 | Worktime | Work |
| 5 | Worktime | Work |
| 6 | Worktime | Work |
| 7 | Worktime | Work |
| 8 | Worktime | Work |
| 9 | Worktime | Work |
| 10 | Worktime | Work |
| 11 | Worktime | Work |
| 12 | Worktime | Work |
| 13 | Worktime | Work |
| 14 | Worktime | Work |
| 15 | Worktime | Work |
| 16 | Worktime | Work |
| 17 | Worktime | Work |
| 18 | Worktime | Work |
| 19 | Recreation | Hygiene, Eat, Recreation, Work |
| 20 | Recreation | Hygiene, Eat, Recreation, Work |
| 21 | Sleep | Sleep |
| 22 | Sleep | Sleep |
| 23 | Sleep | Sleep |

## Default Bionic Schedule Override

When DLC (Bionic Booster) is active, `AddDefaultSchedule` also creates a bionic schedule with:

| Hour | Group |
|-|-|
| 0–20 | Worktime |
| 21 | Recreation |
| 22 | Recreation |
| 23 | Sleep |

## Key Classes

| Class | Namespace | Purpose |
|-|-|-|
| ScheduleBlockType | (global) | Atomic activity type (Sleep, Eat, Work, Hygiene, Recreation) |
| ScheduleGroup | (global) | Grouping of allowed block types with default segment count and UI color |
| ScheduleBlock | (global) | Single hour slot in a schedule; has `name` (group ID) and `_groupId` |
| Schedule | (global) | Full 24-hour schedule: list of ScheduleBlock + assigned Schedulables |
| ScheduleManager | (global) | Singleton managing all schedules; ticks hourly via `Sim33ms` |
| Database.ScheduleBlockTypes | Database | ResourceSet holding all 5 block type instances |
| Database.ScheduleGroups | Database | ResourceSet holding all 4 group instances + `allGroups` list |

## Schedule Structure

| Field | Type | Description |
|-|-|-|
| Schedule.blocks | List\<ScheduleBlock\> | 24 blocks, one per hour |
| Schedule.name | string | User-editable schedule name |
| Schedule.alarmActivated | bool | Whether transition alarm plays |
| Schedule.isDefaultForBionics | bool | Bionic default flag |
| Schedule.tones | int[] | Musical tones for alarm |
| ScheduleManager.schedules | List\<Schedule\> | All colony schedules |
| ScheduleManager.lastHour | int | Last processed hour (0–23) |

## API Notes

- `ScheduleManager.Instance`  - singleton access
- `ScheduleManager.GetSchedule(Schedulable)`  - get a dupe's schedule
- `ScheduleManager.IsAllowed(Schedulable, ScheduleBlockType)`  - check if dupe's current block allows a type
- `ScheduleManager.GetCurrentHour()`  - returns `GameClock.Instance.GetCurrentCycleAsPercentage() * 24`
- `Schedule.SetBlockGroup(int idx, ScheduleGroup group)`  - change a single hour's group
- `Schedule.RotateBlocks(bool directionLeft, int timetableIdx)`  - shift schedule left/right
- `ScheduleGroup.Allowed(ScheduleBlockType type)`  - check if block type is permitted in group
