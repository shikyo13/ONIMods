# Room Types — ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

## Washroom

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| Latrine | Latrine | 12 | 64 | Outhouse (primary), Wash Basin | Morale bonus |
| PlumbedBathroom | Washroom | 12 | 64 | Flush Toilet (primary), Shower | Morale bonus |

## Sleep

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| Barracks | Barracks | 12 | 64 | Cot/Comfy Bed (primary) | Morale bonus |
| Bedroom | Luxury Barracks | 12 | 64 | Comfy Bed (primary), Decor Item, 4-high ceiling | Morale bonus |
| PrivateBedroom | Private Bedroom | 24 | 64 | Luxury Bed single (primary), 2x Decor Items, 4-high ceiling, Backwall | Morale bonus |

## Dining

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| MessHall | Mess Hall | 12 | 64 | Mess Table (primary) | Morale bonus |
| GreatHall | Great Hall | 32 | 120 | Mess Table (primary), Decor Item, Rec Building | Morale bonus |
| BanquetHall | Banquet Hall | 32 | 120 | Dining Table (primary), Decor Item, Rec Building, Ornament | Morale bonus |
| Kitchen | Kitchen | 12 | 96 | Spice Grinder (primary), Cook Top, Refrigerator | Enables Spice Grinder use |

## Medical

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| MassageClinic | Massage Clinic | 12 | 64 | Massage Table (primary), Decor Item | Massage stress relief bonus |
| Hospital | Hospital | 12 | 96 | Clinic (primary), Toilet, Mess Table | Quarantine sick Duplicants |

## Recreation

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| RecRoom | Recreation Room | 12 | 96 | Rec Building (primary), Decor Item | Morale bonus |

## Parks

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| Park | Park | 12 | 64 | Nature Decoration (primary), 1 Wild Plant | Morale bonus |
| NatureReserve | Nature Reserve | 32 | 120 | Nature Decoration (primary), 2+ Wild Plants | Morale bonus |

## Industrial

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| PowerPlant | Power Plant | 12 | 120 | Power Control Station (primary) | Enables tune-ups on generators |
| MachineShop | Machine Shop | 12 | 96 | Craft Station (primary) | Increased fabrication efficiency |

## Agriculture

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| Farm | Greenhouse | 12 | 96 | Farm Station (primary) | Enables Farm Station fertilizer |
| CreaturePen | Stable | 12 | 96 | Grooming Station (primary) | Critter taming and mood bonus |

## Science

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| Laboratory | Laboratory | 32 | 120 | Science Building (primary) | Efficiency bonus |

## Uncategorized

| ID | Display Name | Min Size | Max Size | Required Buildings | Bonus Effect |
|-|-|-|-|-|-|
| Neutral | Miscellaneous Room | — | — | None | No effect |

## Constraint Reference

All rooms exclude Industrial Machinery unless otherwise noted (PowerPlant, MachineShop, Farm, CreaturePen, Laboratory are exceptions).

| Constraint | Meaning |
|-|-|
| MINIMUM_SIZE_12 | At least 12 tiles |
| MINIMUM_SIZE_24 | At least 24 tiles |
| MINIMUM_SIZE_32 | At least 32 tiles |
| MAXIMUM_SIZE_64 | No more than 64 tiles |
| MAXIMUM_SIZE_96 | No more than 96 tiles |
| MAXIMUM_SIZE_120 | No more than 120 tiles |
| CEILING_HEIGHT_4 | Minimum 4-tile ceiling height |
| NO_INDUSTRIAL_MACHINERY | No refineries, generators, etc. |
| IS_BACKWALLED | All background tiles must be filled |
| DECORATIVE_ITEM | At least 1 decor-positive building |
| DECORATIVE_ITEM_2 | At least 2 decor-positive buildings |

## Upgrade Paths

| From | To |
|-|-|
| Latrine | Washroom |
| Barracks | Luxury Barracks, Private Bedroom |
| Bedroom | Private Bedroom |
| Mess Hall | Great Hall, Banquet Hall |
| Great Hall | Banquet Hall |
| Park | Nature Reserve |
