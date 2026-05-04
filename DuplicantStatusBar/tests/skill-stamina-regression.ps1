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
Require-Contains $tracker 'public int AvailableSkillPoints;' `
    'DupeSnapshot must carry available skill points.'
Require-Contains $tracker 'public bool HasStamina;' `
    'DupeSnapshot must track whether stamina data was available.'
Require-Contains $tracker 'public float StaminaPercent;' `
    'DupeSnapshot must carry stamina percentage.'
Require-Contains $tracker 'private static Klei.AI.Amount staminaAmount;' `
    'DupeStatusTracker must cache the Stamina amount.'
Require-Contains $tracker 'public MinionResume Resume;' `
    'DupeStatusTracker cache must retain MinionResume.'
Require-Contains $tracker 'Resume = go.GetComponent<MinionResume>()' `
    'DupeStatusTracker must cache MinionResume from the dupe GameObject.'
Require-Contains $tracker 'snap.AvailableSkillPoints = cache.Resume.AvailableSkillpoints;' `
    'DupeStatusTracker must read MinionResume.AvailableSkillpoints.'
Require-Contains $tracker 'staminaAmount = db.Amounts.Stamina;' `
    'DupeStatusTracker must resolve Db.Get().Amounts.Stamina.'
Require-Contains $tracker 'cache.Stamina = staminaAmount?.Lookup(go);' `
    'DupeStatusTracker must cache each dupe stamina amount instance.'

$alertEnum = SectionBetween $tracker 'public enum AlertType' 'public struct DupeSnapshot'
Require-NotContains $alertEnum 'Skill' `
    'Skill points must not be added as an AlertType.'
Require-NotContains $alertEnum 'Stamina' `
    'Stamina must not be added as an AlertType.'

$options = Read-ProjectFile 'Config\StatusBarOptions.cs'
Require-Regex $options '(?s)\[Option\("STRINGS\.DUPLICANTSTATUSBAR\.OPTIONS\.SHOWSKILLPOINTBADGES\.NAME".*?\)\]\s*\[JsonProperty\]\s*public bool ShowSkillPointBadges \{ get; set; \} = true;' `
    'ShowSkillPointBadges must be exposed as a default-on PLib option.'
Require-Regex $options '(?s)\[Option\("STRINGS\.DUPLICANTSTATUSBAR\.OPTIONS\.SHOWSKILLPOINTSTOOLTIP\.NAME".*?\)\]\s*\[JsonProperty\]\s*public bool ShowSkillPointsInTooltip \{ get; set; \} = true;' `
    'ShowSkillPointsInTooltip must be exposed as a default-on PLib option.'

$widget = Read-ProjectFile 'UI\DupePortraitWidget.cs'
Require-Contains $widget 'private Image skillPointBadge;' `
    'DupePortraitWidget must have a dedicated skill point badge image.'
Require-Contains $widget 'private TextMeshProUGUI skillPointText;' `
    'DupePortraitWidget must have dedicated skill point badge text.'
Require-Contains $widget 'UpdateSkillPointBadge(snapshot.AvailableSkillPoints' `
    'DupePortraitWidget must update the skill point badge from snapshot data.'
Require-Regex $widget '(?s)private void UpdateSkillPointBadge\(int points, int cardSz\).*?StatusBarOptions\.Instance\.ShowSkillPointBadges.*?points > 0' `
    'Skill point badge visibility must depend on ShowSkillPointBadges and points greater than zero.'
Require-Regex $widget '(?s)skillPointBadge\.rectTransform.*?anchorMin = new Vector2\(0f,\s*0f\).*?anchorMax = new Vector2\(0f,\s*0f\)' `
    'Skill point badge must be anchored bottom-left, separate from alert badges and clear of the top edge.'
Require-Regex $widget '(?s)private void UpdateSkillPointBadge\(int points, int cardSz\).*?Mathf\.Clamp\(\s*ALERT_BADGE_MIN_SIZE \+ Mathf\.Max\(0f,\s*cardSz - 20f\) \* 0\.22f \* ALERT_BADGE_GROWTH_RATE,\s*ALERT_BADGE_MIN_SIZE,\s*ALERT_BADGE_MAX_SIZE\);' `
    'Skill point badge must use the capped slower badge growth to avoid oversized count badges on large cards.'
$updateBadges = SectionBetween $widget 'private void UpdateBadges' 'private static float HoldTime'
Require-NotContains $updateBadges 'SkillPoint' `
    'Skill point badge must not consume alert badge slots.'

$tooltip = Read-ProjectFile 'UI\DupeTooltip.cs'
Require-Contains $tooltip 'StatusBarOptions.Instance.ShowSkillPointsInTooltip' `
    'Skill point tooltip line must respect ShowSkillPointsInTooltip.'
Require-Contains $tooltip 'snap.AvailableSkillPoints > 0' `
    'Skill point tooltip line must render only when points are available.'
Require-Contains $tooltip 'TOOLTIP_SKILLPOINTS' `
    'Tooltip must include localized skill point label.'
Require-Contains $tooltip 'TOOLTIP_STAMINA' `
    'Tooltip must include localized stamina label.'
Require-Contains $tooltip 'snap.StaminaPercent' `
    'Tooltip must render the stamina percentage.'

$strings = Read-ProjectFile 'Localization\DSBStrings.cs'
Require-Contains $strings 'TOOLTIP_SKILLPOINTS' `
    'Localization strings must include a skill point tooltip label.'
Require-Contains $strings 'TOOLTIP_STAMINA' `
    'Localization strings must include a stamina tooltip label.'
Require-Contains $strings 'SHOWSKILLPOINTBADGES' `
    'Localization strings must include the ShowSkillPointBadges option.'
Require-Contains $strings 'SHOWSKILLPOINTSTOOLTIP' `
    'Localization strings must include the ShowSkillPointsInTooltip option.'

$project = Read-ProjectFile 'DuplicantStatusBar.csproj'
Require-Contains $project '<AssemblyVersion>2.10.0.0</AssemblyVersion>' `
    'AssemblyVersion must be bumped to 2.10.0.0.'
Require-Contains $project '<FileVersion>2.10.0.0</FileVersion>' `
    'FileVersion must be bumped to 2.10.0.0.'

Write-Output 'skill and stamina regression checks passed'
