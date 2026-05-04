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

function Require-Regex([string]$Source, [string]$Pattern, [string]$Message) {
    if ($Source -notmatch $Pattern) {
        throw $Message
    }
}

$widget = Read-ProjectFile 'UI\DupePortraitWidget.cs'

Require-Contains $widget 'private const float ALERT_BADGE_MIN_SIZE = 9f;' `
    'Alert badges must define a minimum size.'
Require-Contains $widget 'private const float ALERT_BADGE_MAX_SIZE = 22f;' `
    'Alert badges must define a maximum size so large card layouts do not create oversized badges.'
Require-Contains $widget 'private const float ALERT_BADGE_GROWTH_RATE = 0.55f;' `
    'Alert badges must grow slower than a direct card-size multiplier.'
Require-Contains $widget 'private const float ALERT_BADGE_CORNER_OFFSET_FACTOR = 0.15f;' `
    'Alert badges must keep the original corner offset behavior.'
Require-Regex $widget 'float badgeSize = Mathf\.Clamp\(\s*ALERT_BADGE_MIN_SIZE \+ Mathf\.Max\(0f,\s*cardSz - 20f\) \* badgeFrac \* ALERT_BADGE_GROWTH_RATE,\s*ALERT_BADGE_MIN_SIZE,\s*ALERT_BADGE_MAX_SIZE\);' `
    'Alert badge scaling must use slower growth and clamp between min and max sizes.'
Require-Regex $widget 'float xOff = -\(badgeSize \* ALERT_BADGE_CORNER_OFFSET_FACTOR\) - slot \* \(badgeSize \+ gap\);' `
    'Alert badge horizontal position must preserve the original corner offset and badge slot spacing.'
Require-Contains $widget 'brt.anchoredPosition = new Vector2(xOff, -badgeSize * ALERT_BADGE_CORNER_OFFSET_FACTOR);' `
    'Alert badge vertical position must preserve the original corner offset.'

Write-Output 'badge scaling regression checks passed'
