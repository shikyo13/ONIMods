$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dll = Join-Path $projectDir 'bin\Release\FastInsulatedSelfSealingAirLock.dll'

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Missing Release DLL at $dll. Build the project first."
}

$ilspy = Get-Command ilspycmd -ErrorAction SilentlyContinue
if (-not $ilspy) {
    throw 'ilspycmd is required for this regression check.'
}

function Get-DecompiledType([string] $typeName) {
    try {
        $output = & $ilspy.Source -t $typeName $dll 2>$null
        if ($LASTEXITCODE -ne 0) {
            return ''
        }

        return $output -join "`n"
    }
    catch {
        return ''
    }
}

$doorPatches = Get-DecompiledType 'FastInsulatedSelfSealingAirLock.DoorSetSimStatePatch'
$doorPatches += "`n" + (Get-DecompiledType 'FastInsulatedSelfSealingAirLock.DoorOnSpawnPatch')
$doorPatches += "`n" + (Get-DecompiledType 'FastInsulatedSelfSealingAirLock.DoorCleanUpPatch')
$helpers = Get-DecompiledType 'FastInsulatedSelfSealingAirLock.DoorPatchHelpers'
$animPatch = Get-DecompiledType 'FastInsulatedSelfSealingAirLock.DoorAnimPatch'
$getAnimPatch = Get-DecompiledType 'FastInsulatedSelfSealingAirLock.DoorWorkableGetAnimPatch'
$temperaturePatch = Get-DecompiledType 'FastInsulatedSelfSealingAirLock.StructureTemperatureGetPatch'

if ($doorPatches -notmatch 'void Postfix\(Door __instance, IList<int> cells\)') {
    throw 'Door.SetSimState patch must be a postfix so vanilla sim work runs before self-sealing state is re-applied.'
}

if ($doorPatches -notmatch 'void Prefix\(Door __instance, IList<int> cells\)' -or $doorPatches -notmatch 'EnsureValidDoorTemperature\(__instance, cells, "SetSimState\.Prefix"\)') {
    throw 'Door.SetSimState patch must repair invalid fast-airlock PrimaryElement temperatures before vanilla closes door cells.'
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

if ($helpers -notmatch 'EnsureValidDoorTemperature' -or $helpers -notmatch 'PrimaryElement' -or $helpers -notmatch 'Temperature <= 0f' -or $helpers -notmatch 'Grid\.Temperature' -or $helpers -notmatch 'ElementLoader\.FindElementByHash') {
    throw 'DoorPatchHelpers must repair zero PrimaryElement temperature using sampled door cells or element defaults before vanilla SetSimState.'
}

if ($temperaturePatch -notmatch 'HarmonyPatch\(typeof\(StructureTemperatureComponents\), "OnGetTemperature"\)' -or $temperaturePatch -notmatch 'void Postfix\(PrimaryElement primary_element, ref float __result\)' -or $temperaturePatch -notmatch 'DoorPatchHelpers\.RepairInvalidTemperatureRead\(primary_element, ref __result, "StructureTemperature\.OnGetTemperature"\)') {
    throw 'StructureTemperatureComponents.OnGetTemperature must repair zero delegated temperature reads for this airlock before vanilla Door.SetSimState submits close sim messages.'
}

if ($helpers -notmatch 'RepairInvalidTemperatureRead' -or $helpers -notmatch 'primaryElement\.InternalTemperature' -or $helpers -notmatch '__result = .*Temperature') {
    throw 'DoorPatchHelpers must repair zero delegated structure-temperature reads using InternalTemperature, valid cells, or element defaults.'
}

if ($helpers -notmatch 'SimMessages\.SetInsulation\(cell, 1f\)' -or $helpers -notmatch 'SimMessages\.SetInsulation\(cell, 0f\)') {
    throw 'Self-sealing logic must restore normal insulation when explicitly opened and zero insulation when sealed.'
}

if ($animPatch -notmatch 'DoorPatchHelpers\.IsFastAirlockDoor\(__instance\)' -or ($animPatch -notmatch 'remoteAnim != null' -and $animPatch -notmatch 'anim != .*null' -and $animPatch -notmatch 'Assets\.TryGetAnim')) {
    throw 'Fast-airlock-specific animation override assignment must stay scoped to this mod door and must not install a null animation override.'
}

if ($animPatch -match 'Assets\.GetAnim\("anim_use_remote_kanim"\)' -or $animPatch -notmatch 'Assets\.TryGetAnim' -or $animPatch -notmatch 'anim_use_remote_kanim') {
    throw 'Optional remote door work animation lookup must use Assets.TryGetAnim so missing vanilla anims do not log Missing Anim warnings.'
}

if ($animPatch -notmatch 'EnsureSafeDoorOverrideAnims\(__instance\)') {
    throw 'Door animation patch must sanitize null vanilla door override animations before a worker can start door work.'
}

if ($getAnimPatch -notmatch 'HarmonyPatch\(typeof\(Workable\), "GetAnim"\)' -or $getAnimPatch -notmatch '__instance is Door' -or $getAnimPatch -notmatch 'GetSafeDoorOverrideAnims\(__result\.overrideAnims\)') {
    throw 'Door Workable.GetAnim patch must sanitize null door override animations before StandardWorker.AttachOverrideAnims consumes them.'
}

if ($getAnimPatch -notmatch 'TryGetDoorOverrideAnims\(out var .*OverrideAnims\)' -or $getAnimPatch -notmatch '__result\.overrideAnims == null \|\| __result\.overrideAnims\.Length == 0' -or $getAnimPatch -notmatch '__result\.overrideAnims = .*OverrideAnims') {
    throw 'Door Workable.GetAnim patch must restore the vanilla remote/tablet door override when a poisoned Door static override only contains null.'
}

Write-Host 'airlock door lifecycle regression checks passed'
