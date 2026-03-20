# PLib Options API  - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-16

Source: `PeterHan.PLib.Options` (PLib by Peter Han, MIT license)

## Class-Level Attributes

| Attribute | Target | Parameters | Purpose |
|-|-|-|-|
| `[ModInfo]` | Class | `string url`, `string image = null`, `bool collapse = false` | Sets workshop URL, preview image filename, and whether categories start collapsed |
| `[ConfigFile]` | Class | `string FileName = "config.json"`, `bool IndentOutput = false`, `bool SharedConfigLocation = false` | Overrides config filename, enables pretty-print, or moves config to shared folder. **Constructor is positional**: `[ConfigFile("name.json", false, true)]`. Property `UseSharedConfigLocation` is read-only. **Always use shared config for Workshop mods** to prevent Mod Updater restart loops. |
| `[RestartRequired]` | Class, Property, Field | _(none)_ | Prompts user to restart game after changing options. On properties, compares old/new via `Equals` |
| `[JsonObject]` | Class | `MemberSerialization.OptIn` | Standard Newtonsoft; required if using `[JsonProperty]` on individual fields |

## Property-Level Attributes

| Attribute | Parameters | Purpose | Example |
|-|-|-|-|
| `[Option]` | _(none)_ | Auto-resolve title/tooltip from STRINGS: `STRINGS.{NAMESPACE}.OPTIONS.{PROP}.NAME/TOOLTIP/CATEGORY` | `[Option] public int Foo { get; set; }` |
| `[Option]` | `string title`, `string tooltip = null`, `string category = null` | Explicit title, tooltip, category. All accept STRINGS keys or literal text | `[Option("My Setting", "Tooltip", "General")]` |
| `[Option]` | + `Format` property | Set via named param: `Format = "F1"` for float formatting, `"D"` for int, `"P"` for percent | `[Option("Speed")] { Format = "F1" }` |
| `[Limit]` | `double min, double max` | Clamps numeric value; adds slider when present | `[Limit(0, 100)]` |
| `[Limit]` | `double min, double max, double step` | Clamps + sets slider increment | `[Limit(0.0, 1.0, 0.05)]` |
| `[JsonProperty]` | _(none)_ | Marks property for JSON serialization (required with `OptIn`) | `[JsonProperty]` |
| `[RequireDLC]` | `string dlcID`, `bool required = true` | Show/hide option based on DLC ownership. AllowMultiple. Values preserved when hidden | `[RequireDLC(DlcManager.EXPANSION1_ID)]` |
| `[DynamicOption]` | `Type handler`, `string category = null` | Use custom `IOptionsEntry` handler instead of built-in type mapping | `[DynamicOption(typeof(MyHandler))]` |

## Built-in Entry Types

Mapped automatically from property type. No attribute needed beyond `[Option]`.

| Property Type | Entry Class | UI Control | Notes |
|-|-|-|-|
| `bool` | `CheckboxOptionsEntry` | Checkbox | Klei blue style |
| `int` | `IntOptionsEntry` | Text field + slider (if `[Limit]`) | Format default: `"D"` |
| `int?` | `NullableIntOptionsEntry` | Text field + slider | Nullable; empty = null |
| `float` | `FloatOptionsEntry` | Text field + slider (if `[Limit]`) | Format default: `"F2"`. `"P"` format auto-divides input by 100 |
| `float?` | `NullableFloatOptionsEntry` | Text field + slider | Nullable; empty = null |
| `enum` | `SelectOneOptionsEntry` | Combo box (dropdown) | Enum values get titles from `[Option]` on members or STRINGS auto-lookup |
| `string` | `StringOptionsEntry` | Text field | `[Limit]` sets max length |
| `Color` | `ColorOptionsEntry` | Color picker | Unity `Color` (0-1 range) |
| `Color32` | `Color32OptionsEntry` | Color picker | Unity `Color32` (0-255 range) |
| `Action<object>` | `ButtonOptionsEntry` | Button (full-width) | Read-only property returning handler. Not serialized |
| `LocText` | `TextBlockOptionsEntry` | Static label (full-width) | Read-only property returning null. Word-wrap enabled |
| Custom class | `CompositeOptionsEntry` | Nested fields | Recursively scans properties for `[Option]`. Max depth 16. Category inherited from parent |

## IOptions Interface (Optional)

Implement on the options class for live-reload or dynamic entries.

| Member | Signature | Purpose |
|-|-|-|
| `CreateOptions` | `IEnumerable<IOptionsEntry> CreateOptions()` | Return additional dynamic options entries. Called before dialog shows. Return `null` or empty if unused |
| `OnOptionsChanged` | `void OnOptionsChanged()` | Called after options written to disk, before restart prompt. Use for live-reload |

## SingletonOptions<T> Base Class

| Member | Type | Purpose |
|-|-|-|
| `Instance` | `static T` | Lazy-loads from `config.json` via `POptions.ReadSettings<T>()`, caches result. Returns `new T()` if file missing |
| `instance` | `protected static T` | Backing field. Set manually in `OnOptionsChanged` for live-reload |

## IOptionsEntry Interface (Custom Entries)

For `[DynamicOption]` handlers or `IOptions.CreateOptions()` return values.

| Member | Signature | Purpose |
|-|-|-|
| `Category` | `string` (from `IOptionSpec`) | Category tab name |
| `Title` | `string` (from `IOptionSpec`) | Display label |
| `Tooltip` | `string` (from `IOptionSpec`) | Hover tooltip |
| `Format` | `string` (from `IOptionSpec`) | Number format string |
| `RestartRequired` | `bool { get; set; }` | Set by PLib if `[RestartRequired]` present |
| `CreateUIEntry` | `void CreateUIEntry(PGridPanel parent, ref int row)` | Build UI into the grid panel |
| `ReadFrom` | `void ReadFrom(object settings)` | Read value from settings object into UI |
| `WriteTo` | `bool WriteTo(object settings)` | Write UI value back. Return true if changed |

Constructor must match one of: `(string field, IOptionSpec spec)`, `(string field, IOptionSpec spec, LimitAttribute limit)`, or `(string field, IOptionSpec spec, Type fieldType)`.

## Static API (POptions Class)

| Method | Signature | Purpose |
|-|-|-|
| `ReadSettings<T>` | `static T ReadSettings<T>() where T : class` | Read config from disk. Returns null if missing/corrupt |
| `WriteSettings<T>` | `static void WriteSettings<T>(T settings) where T : class` | Write config to disk |
| `GetConfigFilePath` | `static string GetConfigFilePath(Type optionsType)` | Resolve config path for a type |
| `ShowDialog` | `static void ShowDialog(Type optionsType, Action<object> onClose = null)` | Open options dialog programmatically |
| `RegisterOptions` | `void RegisterOptions(KMod.UserMod2 mod, Type optionsType)` | Register options type (called automatically by `new POptions().RegisterOptions(this, typeof(T))`) |

## Registration Pattern

```csharp
// In UserMod2.OnLoad:
public override void OnLoad(Harmony harmony)
{
    base.OnLoad(harmony);
    PUtil.InitLibrary();
    new POptions().RegisterOptions(this, typeof(MyOptions));
}
```

## Enum Option Pattern

```csharp
public enum MyMode
{
    [Option("STRINGS.MYMOD.OPTIONS.MODE.FAST")] Fast,
    [Option("STRINGS.MYMOD.OPTIONS.MODE.SLOW")] Slow
}
```

Enum members also support `[EnumMember(Value = "Display Name")]` as fallback.

## STRINGS Auto-Lookup Convention

When `[Option]` has no arguments, PLib resolves from STRINGS:

- Title: `STRINGS.{NAMESPACE}.OPTIONS.{PROPERTY}.NAME`
- Tooltip: `STRINGS.{NAMESPACE}.OPTIONS.{PROPERTY}.TOOLTIP`
- Category: `STRINGS.{NAMESPACE}.OPTIONS.{PROPERTY}.CATEGORY`

Namespace is uppercased. All string parameters (title, tooltip, category) are checked against the string database; if found, the localized value is used.

## Live-Reload Pattern (No Restart)

```csharp
[JsonObject(MemberSerialization.OptIn)]
public sealed class MyOptions : SingletonOptions<MyOptions>, IOptions
{
    [Option("Setting", "Desc")] [JsonProperty]
    public int MySetting { get; set; } = 42;

    public void OnOptionsChanged()
    {
        instance = POptions.ReadSettings<MyOptions>() ?? new MyOptions();
    }

    public IEnumerable<IOptionsEntry> CreateOptions() => null;
}
```

## Button Option Pattern

```csharp
[Option("Reset All", "Resets settings to defaults")]
public Action<object> ResetButton => _ => DoReset();
// Do NOT add [JsonProperty]  - buttons are not serialized
```

## Constants

| Constant | Value | Purpose |
|-|-|-|
| `POptions.CONFIG_FILE_NAME` | `"config.json"` | Default config filename |
| `POptions.MAX_SERIALIZATION_DEPTH` | `8` | Max nesting depth for JSON serialization |
| `POptions.SHARED_CONFIG_FOLDER` | `"config"` | Subfolder name for shared config location |
