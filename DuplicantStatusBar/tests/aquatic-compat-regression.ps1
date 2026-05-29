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

$compat = Read-ProjectFile 'Core\GameCompatibility.cs'
Require-Contains $compat 'AquaticBreathDisplayBuild = 731233' `
    'Aquatic compatibility must key integer breath display from build 731233.'
Require-Contains $compat 'BuildWatermark.GetBuildText()' `
    'Aquatic compatibility must parse the real ONI build watermark.'
Require-Contains $compat 'UsesIntegerBreathDisplay' `
    'Aquatic compatibility must expose the breath display mode.'
Require-Regex $compat 'parsedBuild\s*>=\s*AquaticBreathDisplayBuild' `
    'Integer breath display must enable only at or after the Aquatic build.'

$tracker = Read-ProjectFile 'Data\DupeStatusTracker.cs'
Require-NotContains $tracker 'cache.SuffocationSMI.sm.noOxygen.suffocating' `
    'Suffocation detection must not hard-reference the old state machine member chain.'
Require-Contains $tracker 'IsSuffocating(cache.SuffocationSMI, snap.BreathPercent)' `
    'DupeStatusTracker must route suffocation detection through the guarded helper.'
Require-Contains $tracker 'TryIsInsideLegacySuffocationState' `
    'DupeStatusTracker must try the legacy suffocation state through a guarded helper.'
Require-Contains $tracker 'GameCompatibility.UsesIntegerBreathDisplay' `
    'DupeStatusTracker must branch suffocation fallback by compatibility mode.'
Require-Regex $tracker '(?s)if \(GameCompatibility\.UsesIntegerBreathDisplay\).*?return breathPercent <= 0f;' `
    'Aquatic builds must not use the old 30 percent low-breath suffocation fallback.'
Require-Regex $tracker '(?s)return breathPercent < 30f;' `
    'Stable builds must preserve the existing 30 percent low-breath suffocation fallback.'
Require-Contains $tracker 'public bool HasBreath;' `
    'DupeSnapshot must track whether breath data exists.'
Require-Contains $tracker 'public float BreathAmount;' `
    'DupeSnapshot must store raw breath amount for Aquatic integer display.'
Require-Contains $tracker 'public float BreathMax;' `
    'DupeSnapshot must store breath max while keeping percent calculations.'
Require-Contains $tracker 'snap.HasBreath = true;' `
    'DupeStatusTracker must mark breath data available when the amount exists.'
Require-Contains $tracker 'snap.BreathAmount = cache.Breath.value;' `
    'DupeStatusTracker must capture raw breath amount.'

$tooltip = Read-ProjectFile 'UI\DupeTooltip.cs'
Require-Contains $tooltip 'GameCompatibility.UsesIntegerBreathDisplay' `
    'DupeTooltip must choose the breath format through the compatibility helper.'
Require-Contains $tooltip 'snap.BreathAmount:F0' `
    'Aquatic normal breath tooltip must render an integer amount.'
Require-Contains $tooltip 'snap.BreathPercent:F0}%' `
    'Stable normal breath tooltip must keep rendering a percentage.'
Require-Contains $tooltip 'snap.OxygenTankPercent:F0}%' `
    'Bionic oxygen tank tooltip must remain percentage-based.'

$modInfo = Read-ProjectFile 'mod_info.yaml'
Require-Contains $modInfo 'minimumSupportedBuild: 642695' `
    'Aquatic compatibility patch must not raise minimumSupportedBuild.'
Require-Contains $modInfo 'supportedContent: ALL' `
    'Aquatic compatibility patch must preserve the current broad loader metadata.'
Require-NotContains $modInfo 'requiredDlcIds' `
    'Aquatic compatibility patch must not require any specific DLC.'
Require-NotContains $modInfo 'forbiddenDlcIds' `
    'Aquatic compatibility patch must not forbid any DLC.'

Write-Output 'aquatic compatibility regression checks passed'
