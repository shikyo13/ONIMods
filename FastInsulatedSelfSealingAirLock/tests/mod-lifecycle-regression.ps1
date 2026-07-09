$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$modLifecycle = Join-Path $projectDir 'Core\FastInsulatedSelfSealingAirLockMod.cs'

if (-not (Test-Path -LiteralPath $modLifecycle)) {
    throw "Missing mod lifecycle source at $modLifecycle"
}

$source = Get-Content -Raw -LiteralPath $modLifecycle

if ($source -match 'base\.OnLoad\(harmony\)') {
    throw 'OnLoad must not call base.OnLoad(harmony), because it PatchAlls Door patches before mod animations are loaded.'
}

if ($source -match '\.PatchAll\(') {
    throw 'This mod must patch classes explicitly so Door patches can be deferred without delaying database registration.'
}

if ($source -notmatch 'PatchClass\(harmony,\s*typeof\(DatabasePatches\)\)') {
    throw 'OnLoad must patch DatabasePatches early so strings, tech unlocks, and plan screen registration are ready for Db.Initialize.'
}

if ($source -notmatch 'override\s+void\s+OnAllModsLoaded\s*\(\s*Harmony\s+harmony\s*,\s*IReadOnlyList<Mod>\s+mods\s*\)') {
    throw 'Door and Workable patches must be installed from OnAllModsLoaded after mod animation content is available.'
}

$latePatchTypes = @(
    'StructureTemperatureGetPatch',
    'DoorAnimPatch',
    'DoorWorkableGetAnimPatch',
    'DoorSetSimStatePatch',
    'DoorOnSpawnPatch',
    'DoorCleanUpPatch',
    'GameLeakSamplerPatch'
)

foreach ($patchType in $latePatchTypes) {
    if ($source -notmatch ('PatchClass\(harmony,\s*typeof\(' + [regex]::Escape($patchType) + '\)\)')) {
        throw "OnAllModsLoaded must patch $patchType."
    }
}

if ($source -notmatch 'PUtil\.InitLibrary\(\)' -or $source -notmatch 'POptions\(\)\.RegisterOptions') {
    throw 'OnLoad must still initialize PLib and register options.'
}

Write-Host 'airlock mod lifecycle regression checks passed'
