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

$screen = Read-ProjectFile 'UI\StatusBarScreen.cs'

Require-Contains $screen 'DebugHandler.HideUI' `
    'The status bar must respect the game UI hide state used by Alt+S screenshot mode.'
Require-Regex $screen '(?s)private void LateUpdate\(\).*?bool hideForGameUI = DebugHandler\.HideUI;.*?canvasRT\.gameObject\.SetActive\(!screenOpen && !hideForGameUI\);.*?if \(screenOpen \|\| hideForGameUI\) return;' `
    'LateUpdate must hide the DSB canvas while the game UI is hidden and skip resize tracking while hidden.'
Require-Regex $screen '(?s)private void LogVisibilityDiagnostic\(\).*?DebugHandler\.HideUI.*?DebugHandler\.ScreenshotMode.*?DebugHandler\.TimelapseMode' `
    'Visibility diagnostics should report the game UI hide flags used to suppress the DSB canvas.'

Write-Output 'screenshot mode regression checks passed'
