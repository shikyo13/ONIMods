$ErrorActionPreference = 'Stop'

$modRoot = Split-Path -Parent $PSScriptRoot
$screenFiles = Get-ChildItem -LiteralPath (Join-Path $modRoot 'UI') -Filter 'StatusBarScreen*.cs' -File
if (-not $screenFiles) {
    throw 'No StatusBarScreen source files found.'
}

$combinedSource = ($screenFiles | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw
}) -join "`n"

if ($combinedSource -match '\.enableWordWrapping\s*=') {
    throw 'StatusBarScreen must use textWrappingMode instead of obsolete TMP_Text.enableWordWrapping.'
}

if ($combinedSource -match '(?<!Resources\.)FindObjectOfType\s*<') {
    throw 'StatusBarScreen must use a compatibility helper instead of direct obsolete Object.FindObjectOfType<T>().'
}

if ($combinedSource -match '(?<!Resources\.)FindObjectsOfType\s*<') {
    throw 'StatusBarScreen must use a compatibility helper instead of direct obsolete Object.FindObjectsOfType<T>().'
}

if ($combinedSource -notmatch 'FindFirstObjectByType' -or
    $combinedSource -notmatch 'FindObjectsByType' -or
    $combinedSource -notmatch 'FindObjectsSortMode') {
    throw 'StatusBarScreen must probe modern Unity object lookup APIs and retain compatibility fallbacks.'
}

Write-Host 'DSB Unity API warning regression checks passed'
