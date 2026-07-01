$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$modInfo = Join-Path $projectDir 'mod_info.yaml'
$modYaml = Join-Path $projectDir 'mod.yaml'

if (-not (Test-Path -LiteralPath $modInfo)) {
    throw "Missing mod_info.yaml at $modInfo"
}

if (-not (Test-Path -LiteralPath $modYaml)) {
    throw "Missing mod.yaml at $modYaml"
}

$info = Get-Content -Raw -LiteralPath $modInfo
$yaml = Get-Content -Raw -LiteralPath $modYaml

if ($info -notmatch '(?m)^supportedContent:\s*ALL\s*$') {
    throw 'mod_info.yaml must declare supportedContent: ALL for all current DLC modes.'
}

if ($info -notmatch '(?m)^APIVersion:\s*2\s*$') {
    throw 'mod_info.yaml must declare APIVersion: 2.'
}

if ($info -notmatch '(?m)^minimumSupportedBuild:\s*(\d+)\s*$') {
    throw 'mod_info.yaml must declare minimumSupportedBuild.'
}

$minimumBuild = [int]$Matches[1]
if ($minimumBuild -lt 737790) {
    throw "minimumSupportedBuild must be at least 737790 for the June 2026 SimMessages API change. Found $minimumBuild."
}

if ($yaml -notmatch '(?m)^staticID:\s*zerotheabsolute\.FastInsulatedSelfSealingAirLock\s*$') {
    throw 'mod.yaml must use this fork staticID to avoid impersonating the original workshop item.'
}

Write-Host 'airlock metadata regression checks passed'
