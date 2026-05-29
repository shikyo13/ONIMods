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

$tooltip = Read-ProjectFile 'UI\DupeTooltip.cs'
Require-Contains $tooltip 'GameUtil.GetFormattedTemperature' `
    'Body temperature tooltip must use the game temperature formatter so main game unit settings are honored.'
Require-Contains $tooltip 'GameUtil.TemperatureInterpretation.Absolute' `
    'Body temperature tooltip must format absolute temperatures, not relative temperature deltas.'
Require-Contains $tooltip 'GameUtil.TimeSlice.None' `
    'Body temperature tooltip must not add per-second or per-cycle suffixes.'
Require-NotContains $tooltip 'tempC:F1' `
    'Body temperature tooltip must not render the hardcoded Celsius conversion.'
Require-NotContains $tooltip '\u00B0C' `
    'Body temperature tooltip must not hardcode the Celsius suffix.'

Write-Output 'temperature unit regression checks passed'
