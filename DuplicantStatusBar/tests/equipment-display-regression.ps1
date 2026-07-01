param(
    [string]$ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

function Read-ProjectFile([string]$RelativePath) {
    $path = Join-Path $ProjectDir $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing expected file: $RelativePath"
    }
    Get-Content -LiteralPath $path -Raw
}

function Require-Contains([string]$Source, [string]$Needle, [string]$Message) {
    if (-not $Source.Contains($Needle)) {
        throw $Message
    }
}

function Require-NotContains([string]$Source, [string]$Needle, [string]$Message) {
    if ($Source.Contains($Needle)) {
        throw $Message
    }
}

function Require-Regex([string]$Source, [string]$Pattern, [string]$Message) {
    if ($Source -notmatch $Pattern) {
        throw $Message
    }
}

function Require-NotRegex([string]$Source, [string]$Pattern, [string]$Message) {
    if ($Source -match $Pattern) {
        throw $Message
    }
}

function SectionBetween([string]$Source, [string]$Start, [string]$End) {
    $startIndex = $Source.IndexOf($Start, [System.StringComparison]::Ordinal)
    if ($startIndex -lt 0) {
        throw "Missing section start: $Start"
    }
    $endIndex = $Source.IndexOf($End, $startIndex + $Start.Length, [System.StringComparison]::Ordinal)
    if ($endIndex -lt 0) {
        throw "Missing section end: $End"
    }
    $Source.Substring($startIndex, $endIndex - $startIndex)
}

$tracker = Read-ProjectFile 'Data\DupeStatusTracker.cs'
Require-Contains $tracker 'public string EquippedSuitName;' `
    'DupeSnapshot must carry the equipped suit display name.'
Require-Contains $tracker 'public string EquippedOutfitName;' `
    'DupeSnapshot must carry the equipped outfit display name.'
Require-Contains $tracker 'public string EquippedShoesName;' `
    'DupeSnapshot must carry the equipped shoes display name.'
Require-Contains $tracker 'public bool HasEquippedSuit' `
    'DupeSnapshot must expose a compact suit-equipped check for portrait badges.'
Require-Contains $tracker 'public bool HasEquipment' `
    'DupeSnapshot must expose a compact any-equipment check for tooltip rendering.'
Require-Contains $tracker 'public Equipment Equipment;' `
    'DupeStatusTracker cache must retain the ONI Equipment component from the assignables proxy.'
Require-Contains $tracker 'identity.assignableProxy.Get()' `
    'DupeStatusTracker must resolve equipment from the real minion assignables proxy.'
Require-Contains $tracker 'GetComponent<Equipment>()' `
    'DupeStatusTracker must read ONI Equipment from the proxy instead of inferring from animations.'
Require-Contains $tracker 'ResolveAssignableSlot("Suit")' `
    'DupeStatusTracker must resolve the suit slot by runtime slot ID.'
Require-Contains $tracker 'ResolveAssignableSlot("Outfit")' `
    'DupeStatusTracker must resolve the outfit slot by runtime slot ID.'
Require-Contains $tracker 'ResolveAssignableSlot("Shoes")' `
    'DupeStatusTracker must resolve the Aquatic shoes slot by runtime slot ID.'
Require-Contains $tracker 'equippable.isEquipped' `
    'DupeStatusTracker must report actively equipped items, not merely assigned items.'
Require-Contains $tracker 'equippable.def.Name' `
    'DupeStatusTracker must use ONI localized equipment names for tooltip display.'
Require-NotContains $tracker 'AssignableSlots.Shoes' `
    'Equipment detection must not hard-reference the Aquatic Shoes field, which would break older builds.'

$alertEnum = SectionBetween $tracker 'public enum AlertType' 'public struct DupeSnapshot'
Require-NotContains $alertEnum 'Suit' `
    'Suit equipment must not consume an alert priority slot.'
Require-NotContains $alertEnum 'Equipment' `
    'Equipment display must not be modeled as a built-in alert.'

$options = Read-ProjectFile 'Config\StatusBarOptions.cs'
Require-Regex $options '(?s)\[Option\("STRINGS\.DUPLICANTSTATUSBAR\.OPTIONS\.SHOWSUITEQUIPMENTBADGE\.NAME".*?\)\]\s*\[JsonProperty\]\s*public bool ShowSuitEquipmentBadge \{ get; set; \} = true;' `
    'ShowSuitEquipmentBadge must be exposed as a default-on PLib option.'
Require-Regex $options '(?s)\[Option\("STRINGS\.DUPLICANTSTATUSBAR\.OPTIONS\.SHOWEQUIPMENTTOOLTIP\.NAME".*?\)\]\s*\[JsonProperty\]\s*public bool ShowEquipmentInTooltip \{ get; set; \} = true;' `
    'ShowEquipmentInTooltip must be exposed as a default-on PLib option.'

$widget = Read-ProjectFile 'UI\DupePortraitWidget.cs'
Require-Contains $widget 'private Image equipmentBadge;' `
    'DupePortraitWidget must have a dedicated equipment badge image.'
Require-Contains $widget 'private TextMeshProUGUI equipmentBadgeText;' `
    'DupePortraitWidget must have dedicated equipment badge text.'
Require-Contains $widget 'UpdateEquipmentBadge(snapshot, cardSz)' `
    'DupePortraitWidget must update the equipment badge from snapshot data.'
Require-Regex $widget '(?s)private void UpdateEquipmentBadge\(DupeSnapshot snapshot, int cardSz\).*?StatusBarOptions\.Instance\.ShowSuitEquipmentBadge.*?snapshot\.HasEquippedSuit' `
    'Equipment badge visibility must depend on ShowSuitEquipmentBadge and an actively equipped suit.'
Require-Regex $widget '(?s)equipmentBadge\.rectTransform.*?anchorMin = new Vector2\(1f,\s*0f\).*?anchorMax = new Vector2\(1f,\s*0f\)' `
    'Equipment badge must be anchored bottom-right, separate from alert badges and skill badges.'
$updateBadges = SectionBetween $widget 'private void UpdateBadges' 'private static float HoldTime'
Require-NotContains $updateBadges 'equipmentBadge' `
    'Equipment badge must not consume alert badge slots.'

$tooltip = Read-ProjectFile 'UI\DupeTooltip.cs'
Require-Contains $tooltip 'StatusBarOptions.Instance.ShowEquipmentInTooltip' `
    'Equipment tooltip line must respect ShowEquipmentInTooltip.'
Require-Contains $tooltip 'snap.HasEquipment' `
    'Equipment tooltip line must use the snapshot equipment state.'
Require-Contains $tooltip 'AppendEquipmentLines(snap)' `
    'Tooltip must route equipment through direct equipment line rendering.'
Require-NotContains $tooltip 'CountEquipmentParts' `
    'Tooltip should not branch into compact and stacked equipment layouts.'
Require-NotContains $tooltip 'AppendCompactEquipmentLine' `
    'Tooltip should not render a compact generic Equipment line for single equipped items.'
Require-NotContains $tooltip 'AppendStackedEquipmentLines' `
    'Tooltip should render equipment slot lines directly without a separate stacked helper.'
Require-Contains $tooltip 'AppendEquipmentPartLine' `
    'DupeTooltip must render equipment parts one per line.'
Require-Regex $tooltip '(?s)private static void AppendEquipmentLines\(DupeSnapshot snap\).*?AppendEquipmentPartLine\(\(string\)DSB\.UI\.TOOLTIP_EQUIPMENT_SUIT, snap\.EquippedSuitName, valueColor\).*?AppendEquipmentPartLine\(\(string\)DSB\.UI\.TOOLTIP_EQUIPMENT_OUTFIT, snap\.EquippedOutfitName, valueColor\).*?AppendEquipmentPartLine\(\(string\)DSB\.UI\.TOOLTIP_EQUIPMENT_SHOES, snap\.EquippedShoesName, valueColor\)' `
    'Tooltip must render suit, outfit, and shoes as direct slot label lines.'
Require-NotContains $tooltip 'sb.AppendLine($"{DSB.UI.TOOLTIP_EQUIPMENT}")' `
    'Stacked equipment tooltip output must not spend a separate line on an Equipment header.'
Require-Contains $tooltip 'sb.AppendLine($"{label}: <color=#{color}>{value}</color>")' `
    'Equipment tooltip output must render each equipped slot as a normal tooltip label line.'
Require-NotContains $tooltip 'sb.AppendLine($"  {label}: <color=#{color}>{value}</color>")' `
    'Equipment tooltip output must not indent equipment slot lines.'
Require-NotRegex $tooltip 'DSB\.UI\.TOOLTIP_EQUIPMENT(?!_)' `
    'Tooltip must not render the generic Equipment label anywhere.'
Require-NotContains $tooltip 'TOOLTIP_EQUIPMENT_NONE' `
    'Tooltip should omit the equipment block when no equipment is equipped instead of spending space on a none line.'
Require-Contains $tooltip 'TOOLTIP_EQUIPMENT_SUIT' `
    'Tooltip must include a localized suit label.'
Require-Contains $tooltip 'TOOLTIP_EQUIPMENT_OUTFIT' `
    'Tooltip must include a localized outfit label.'
Require-Contains $tooltip 'TOOLTIP_EQUIPMENT_SHOES' `
    'Tooltip must include a localized shoes label.'

$strings = Read-ProjectFile 'Localization\DSBStrings.cs'
Require-Contains $strings 'TOOLTIP_EQUIPMENT' `
    'Localization strings must include the equipment tooltip label.'
Require-Contains $strings 'SHOWSUITEQUIPMENTBADGE' `
    'Localization strings must include the suit equipment badge option.'
Require-Contains $strings 'SHOWEQUIPMENTTOOLTIP' `
    'Localization strings must include the equipment tooltip option.'

Write-Output 'equipment display regression checks passed'
