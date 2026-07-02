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

function Get-DecompiledType([string] $typeName) {
    $output = & $ilspy.Source -t $typeName $dll 2>$null
    if ($LASTEXITCODE -ne 0) {
        return ''
    }

    return $output -join "`n"
}

$doorPatches = Get-DecompiledType 'FastInsulatedSelfSealingAirlockContinued.DoorSetSimStatePatch'
$doorPatches += "`n" + (Get-DecompiledType 'FastInsulatedSelfSealingAirlockContinued.DoorOnSpawnPatch')
$doorPatches += "`n" + (Get-DecompiledType 'FastInsulatedSelfSealingAirlockContinued.DoorCleanUpPatch')
$helpers = Get-DecompiledType 'FastInsulatedSelfSealingAirlockContinued.DoorPatchHelpers'
$animPatch = Get-DecompiledType 'FastInsulatedSelfSealingAirlockContinued.DoorAnimPatch'

if ($doorPatches -notmatch 'void Postfix\(Door __instance, IList<int> cells\)') {
    throw 'Door.SetSimState patch must be a postfix so vanilla sim work runs before self-sealing state is re-applied.'
}

if ($doorPatches -match 'void Prefix\(Door __instance, bool is_door_open, IList<int> cells\)') {
    throw 'Door.SetSimState patch must not be the old prefix form.'
}

if ($doorPatches -notmatch 'HarmonyPatch\(typeof\(Door\), "OnSpawn"\)') {
    throw 'Door.OnSpawn must re-apply self-sealing sim state after vanilla door spawn grid setup.'
}

if ($doorPatches -notmatch 'DoorPatchHelpers\.IsFastAirlockDoor\(__instance\)') {
    throw 'Door cleanup/spawn patches must guard by this mod door prefab before touching sim state.'
}

if ($helpers -notmatch 'PrefabTag' -or $helpers -notmatch 'FastInsulatedSelfSealingAirLock') {
    throw 'DoorPatchHelpers must identify this mod door by KPrefabID.PrefabTag.'
}

if ($helpers -notmatch 'door\.CurrentState == Door\.ControlState\.Opened' -and $helpers -notmatch '\(int\)door\.CurrentState == 1') {
    throw 'Self-sealing logic must use the player control state, not transient animation/open state.'
}

if ($helpers -notmatch 'SimMessages\.SetInsulation\(cell, 1f\)' -or $helpers -notmatch 'SimMessages\.SetInsulation\(cell, 0f\)') {
    throw 'Self-sealing logic must restore normal insulation when explicitly opened and zero insulation when sealed.'
}

if ($animPatch -notmatch 'DoorPatchHelpers\.IsFastAirlockDoor\(__instance\)' -or ($animPatch -notmatch 'remoteAnim != null' -and $animPatch -notmatch 'anim != .*null')) {
    throw 'Door animation patch must be scoped to this mod door and must not install a null animation override.'
}

Write-Host 'airlock door lifecycle regression checks passed'
