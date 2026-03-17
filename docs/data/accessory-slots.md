# Accessory Slots  - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

## Slot Registry (Database.AccessorySlots)

22 slots total. Constructor: `AccessorySlots(ResourceSet parent)`.

### Simple Slots (targetSymbolId = `snapTo_{id.ToLower()}`)

| Slot Field | ID (string) | KAnim Source | Override Layer | Notes |
|-|-|-|-|-|
| Eyes | `Eyes` | `head_swap_kanim` | 0 | |
| Hair | `Hair` | `hair_swap_kanim` | 0 | |
| HeadShape | `HeadShape` | `head_swap_kanim` | 0 | |
| Mouth | `Mouth` | `head_swap_kanim` | 0 | |
| Hat | `Hat` | `hat_swap_kanim` | 4 | Only slot with non-zero overrideLayer |
| HatHair | `Hat_Hair` | `hair_swap_kanim` | 0 | Hair compressed under hats |
| HeadEffects | `HeadFX` | `head_swap_kanim` | 0 | Visual FX layer on head |

### Custom Symbol Slots (explicit KAnimHashedString targetSymbolId)

| Slot Field | ID (string) | Symbol Name | KAnim Source | Default Anim | Override Layer |
|-|-|-|-|-|-|
| Body | `Torso` | `torso` | `body_swap_kanim` | (same) | 0 |
| Arm | `Arm_Sleeve` | `arm_sleeve` | `body_swap_kanim` | (same) | 0 |
| ArmLower | `Arm_Lower_Sleeve` | `arm_lower_sleeve` | `body_swap_kanim` | (same) | 0 |
| Belt | `Belt` | `belt` | `body_comp_default_kanim` | (same) | 0 |
| Neck | `Neck` | `neck` | `body_comp_default_kanim` | (same) | 0 |
| Pelvis | `Pelvis` | `pelvis` | `body_comp_default_kanim` | (same) | 0 |
| Foot | `Foot` | `foot` | `body_comp_default_kanim` | `shoes_basic_black_kanim` | 0 |
| Leg | `Leg` | `leg` | `body_comp_default_kanim` | (same) | 0 |
| Necklace | `Necklace` | `necklace` | `body_comp_default_kanim` | (same) | 0 |
| Cuff | `Cuff` | `cuff` | `body_comp_default_kanim` | (same) | 0 |
| Hand | `Hand` | `hand_paint` | `body_comp_default_kanim` | (same) | 0 |
| Skirt | `Skirt` | `skirt` | `body_swap_kanim` | (same) | 0 |
| ArmLowerSkin | `Arm_Lower` | `arm_lower` | `body_swap_kanim` | (same) | 0 |
| ArmUpperSkin | `Arm_Upper` | `arm_upper` | `body_swap_kanim` | (same) | 0 |
| LegSkin | `Leg_Skin` | `leg_skin` | `body_swap_kanim` | (same) | 0 |

### KAnim Source Files

| Local Var | Asset Name | Used By |
|-|-|-|
| anim | `head_swap_kanim` | Eyes, HeadShape, Mouth, HeadEffects |
| anim2 | `body_comp_default_kanim` | Belt, Neck, Pelvis, Foot, Leg, Necklace, Cuff, Hand |
| anim3 | `body_swap_kanim` | Body, Arm, ArmLower, Skirt, ArmLowerSkin, ArmUpperSkin, LegSkin |
| anim4 | `hair_swap_kanim` | Hair, HatHair |
| anim5 | `hat_swap_kanim` | Hat |

### Custom Accessories (post-init)

After iterating all slots, two additional builds are loaded:
- `body_lonelyminion_kanim`  - Lonely Minion NPC accessories
- `body_sena_kanim`  - Sena NPC accessories

## AccessorySlot Class

Inherits: `Resource`

### Fields

| Name | Type | Access | Notes |
|-|-|-|-|
| targetSymbolId | `KAnimHashedString` | public (get) / private (set) | Symbol to override in kanim |
| accessories | `List<Accessory>` | public (get) / private (set) | All accessories in this slot |
| file | `KAnimFile` | private | The swap build kanim |
| AnimFile | `KAnimFile` | public (get) | Property exposing `file` |
| defaultAnimFile | `KAnimFile` | public (get) / private (set) | Fallback anim file |
| overrideLayer | `int` | public (get) / private (set) | SymbolOverrideController priority |

### Constructors

```csharp
// Simple: targetSymbolId auto-generated as "snapTo_{id.ToLower()}"
AccessorySlot(string id, ResourceSet parent, KAnimFile swap_build, int overrideLayer = 0)

// Custom symbol: explicit targetSymbolId
AccessorySlot(string id, ResourceSet parent, KAnimHashedString target_symbol_id,
              KAnimFile swap_build, KAnimFile defaultAnimFile = null, int overrideLayer = 0)
```

### Key Methods

| Method | Signature | Notes |
|-|-|-|
| AddAccessories | `void AddAccessories(KAnimFile default_build, ResourceSet parent)` | Scans build symbols matching `Id.ToLower()`, creates `Accessory` entries |
| Lookup | `Accessory Lookup(string id)` | Find accessory by string ID |
| Lookup | `Accessory Lookup(HashedString full_id)` | Find accessory by hash |

## Accessory Class

Inherits: `Resource`

| Property | Type | Notes |
|-|-|-|
| symbol | `KAnim.Build.Symbol` | The kanim symbol data |
| batchSource | `HashedString` | Batch tag from swap build |
| slot | `AccessorySlot` | Parent slot reference |
| animFile | `KAnimFile` | Source anim file |

```csharp
bool IsDefault()  // Returns true if animFile == slot.defaultAnimFile
```

## Common Access Patterns

```csharp
// Get slot reference
AccessorySlot eyeSlot = Db.Get().AccessorySlots.Eyes;

// Get a dupe's accessory for a slot
Accessory acc = accessorizer.GetAccessory(eyeSlot);

// Add/apply accessory
accessorizer.AddAccessory(accessory);
accessorizer.ApplyAccessories();

// Override symbol on anim controller
symbolOverrideCtrl.AddSymbolOverride(acc.slot.targetSymbolId, acc.symbol, acc.slot.overrideLayer);

// Lookup accessory by name within a slot
Accessory hair3 = Db.Get().AccessorySlots.Hair.Lookup("hair_003");

// Iterate all slots
foreach (AccessorySlot slot in Db.Get().AccessorySlots.resources) { ... }

// Find slot by symbol name
AccessorySlot slot = Db.Get().AccessorySlots.Find(symbolHash);
```

## Symbol Naming Convention

- Simple slots: symbol target = `snapTo_{id.ToLower()}` (e.g., `snapTo_eyes`, `snapTo_hair`)
- Custom slots: explicit symbol name (e.g., `torso`, `arm_sleeve`, `belt`)
- Accessory IDs: match kanim symbol names starting with `slot.Id.ToLower()` (e.g., `eyes_001`, `hair_003`)
