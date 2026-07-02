$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$entryPoint = Join-Path $projectDir 'Core\ReplaceStuffMod.cs'

if (-not (Test-Path -LiteralPath $entryPoint)) {
    throw "Missing ReplaceStuffMod.cs at $entryPoint"
}

$source = Get-Content -Raw -LiteralPath $entryPoint

if ($source -notmatch '"FastInsulatedSelfSealingAirlockContinued\.FastInsulatedDoorConfig"') {
    throw 'ReplaceStuff must soft-patch the FastInsulatedSelfSealingAirlockContinued.FastInsulatedDoorConfig type.'
}

if ($source -match '"FastInsulatedSelfSealingAirLock\.Door"') {
    throw 'ReplaceStuff must not use the stale FastInsulatedSelfSealingAirLock.Door type name.'
}

Write-Host 'ReplaceStuff modded door compatibility regression checks passed'
