$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dll = Join-Path $projectDir 'bin\Release\FastInsulatedSelfSealingAirlockContinued.dll'

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Missing Release DLL at $dll. Build the project first."
}

$ilspy = Get-Command ilspycmd -ErrorAction SilentlyContinue
if (-not $ilspy) {
    throw 'ilspycmd is required for this regression check.'
}

$decompiled = (& $ilspy.Source -t FastInsulatedSelfSealingAirlockContinued.FastInsulatedDoorConfig $dll) -join "`n"

if ($decompiled -notmatch 'PermittedRotations\.(R90)|\(PermittedRotations\)1') {
    throw 'FastInsulatedDoorConfig must use PermittedRotations.R90, enum value 1, so ONI adds a Rotatable component required by Door.OnSpawn.'
}

if ($decompiled -match 'PermittedRotations\.Unrotatable|\(PermittedRotations\)0') {
    throw 'FastInsulatedDoorConfig must not use PermittedRotations.Unrotatable; Door.OnSpawn dereferences rotatable.'
}

Write-Host 'airlock door rotation regression checks passed'
