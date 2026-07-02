$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$modInfo = Join-Path $projectDir 'mod_info.yaml'
$modYaml = Join-Path $projectDir 'mod.yaml'
$actualFiles = Get-ChildItem -LiteralPath $projectDir -File | Select-Object -ExpandProperty Name
$workshopDescription = Join-Path $projectDir 'workshop-description.bbcode'
$legacyWorkshopDescription = Join-Path $projectDir 'workshop-description.txt'
$changelog = Join-Path $projectDir 'CHANGELOG.txt'

foreach ($path in @($modInfo, $modYaml, $workshopDescription, $changelog)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing required publishing file: $path"
    }
}

if (-not ($actualFiles | Where-Object { $_ -ceq 'mod.yaml' })) {
    throw 'mod.yaml must exist with lowercase casing for portable Workshop packaging.'
}

if ($actualFiles | Where-Object { $_ -ceq 'Mod.yaml' }) {
    throw 'Use lowercase mod.yaml for portable Workshop packaging.'
}

if (Test-Path -LiteralPath $legacyWorkshopDescription) {
    throw 'Use workshop-description.bbcode instead of workshop-description.txt.'
}

$info = Get-Content -Raw -LiteralPath $modInfo
$yaml = Get-Content -Raw -LiteralPath $modYaml
$workshop = Get-Content -Raw -LiteralPath $workshopDescription
$changeLogText = Get-Content -Raw -LiteralPath $changelog

if ($yaml -notmatch '(?m)^title:\s*"Replace Stuff"\s*$') {
    throw 'mod.yaml title must be Replace Stuff.'
}

if ($yaml -notmatch '(?m)^staticID:\s*"ReplaceStuff"\s*$') {
    throw 'mod.yaml staticID must stay ReplaceStuff.'
}

if ($yaml -notmatch 'One-click building replacement') {
    throw 'mod.yaml description must stay short and describe replacement, not full Workshop copy.'
}

if ($info -notmatch '(?m)^supportedContent:\s*ALL\s*$') {
    throw 'mod_info.yaml must declare supportedContent: ALL.'
}

if ($info -notmatch '(?m)^APIVersion:\s*2\s*$') {
    throw 'mod_info.yaml must declare APIVersion: 2.'
}

if ($info -notmatch '(?m)^minimumSupportedBuild:\s*(\d+)\s*$') {
    throw 'mod_info.yaml must declare minimumSupportedBuild.'
}

if ([int]$Matches[1] -lt 737790) {
    throw "minimumSupportedBuild must reflect the current verified ONI build. Found $($Matches[1])."
}

if ($info -notmatch '(?m)^version:\s*1\.0\.0\s*$') {
    throw 'mod_info.yaml version must match the current Workshop release version 1.0.0.'
}

if ($workshop -notmatch '\[h1\]Replace Stuff\[/h1\]' -or $workshop -notmatch '\[list\]' -or $workshop -notmatch '\[url=https://github\.com/shikyo13/ONIMods/tree/master/ReplaceStuff\]') {
    throw 'workshop-description.bbcode must use Steam BBCode heading, list, and source URL tags.'
}

foreach ($required in @('Cot', 'Comfy Bed', 'Manual Airlock', 'Mechanized Airlock', 'Storage Bin', 'Smart Storage Bin', 'Outhouse', 'Lavatory')) {
    if ($workshop -notmatch [regex]::Escape($required)) {
        throw "workshop-description.bbcode must mention supported replacement target: $required"
    }
}

if ($workshop -notmatch 'does not let duplicants build through walls') {
    throw 'Workshop description must distinguish Replace Stuff from Build Through.'
}

if ($workshop -match '(?m)^\s*\* ' -or $workshop -match '`' -or $workshop -match '—') {
    throw 'workshop-description.bbcode must not contain markdown bullets, backticks, or em dash characters.'
}

if ($changeLogText -notmatch '\[h2\]v1\.0\.0 - Initial Workshop Release\[/h2\]') {
    throw 'CHANGELOG.txt must describe the current 1.0.0 Workshop release.'
}

Write-Host 'ReplaceStuff metadata regression checks passed'
