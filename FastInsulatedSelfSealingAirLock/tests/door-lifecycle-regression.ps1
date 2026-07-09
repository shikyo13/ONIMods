$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dll = Join-Path $projectDir 'bin\Release\FastInsulatedSelfSealingAirLock.dll'
$sourcePath = Join-Path $projectDir 'Patches\DoorPatches.cs'

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Missing Release DLL at $dll. Build the project first."
}

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Missing source file at $sourcePath"
}

$source = Get-Content -Raw -LiteralPath $sourcePath

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

foreach ($vanillaDoorId in @('Door', 'ManualPressureDoor', 'PressureDoor', 'BunkerDoor', 'WoodenDoor', 'InsulatedDoor', 'GravitasDoor', 'POIFacilityDoor', 'POIDlc2ShowroomDoor', 'POIDoorInternal')) {
    if ($helpers -notmatch [regex]::Escape($vanillaDoorId)) {
        throw "Door remote animation fallback must explicitly include vanilla/Klei door prefab '$vanillaDoorId'."
    }
}

if ($helpers -notmatch 'ShouldUseDoorRemoteOverrideFallback') {
    throw 'DoorPatchHelpers must centralize vanilla-plus-airlock remote animation fallback scoping.'
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

if ($animPatch -notmatch 'RepairDoorOverrideAnims' -or $animPatch -notmatch 'GetSafeDoorOverrideAnims' -or $animPatch -notmatch 'ShouldUseDoorRemoteOverrideFallback' -or ($animPatch -notmatch 'remoteAnim != null' -and $animPatch -notmatch 'anim != .*null' -and $animPatch -notmatch 'Assets\.TryGetAnim')) {
    throw 'Door prefab animation override repair must sanitize null entries and restore the tablet override without installing a null animation override.'
}

if ($animPatch -match 'Assets\.GetAnim\("anim_use_remote_kanim"\)' -or $animPatch -notmatch 'Assets\.TryGetAnim' -or $animPatch -notmatch 'anim_use_remote_kanim') {
    throw 'Optional remote door work animation lookup must use Assets.TryGetAnim so missing vanilla anims do not log Missing Anim warnings.'
}

if ($animPatch -notmatch 'DoorDiagnostics\.LogOverrideAnimRepair') {
    throw 'Door prefab animation override repair must log before/after override state when debug logging is enabled.'
}

if ($getAnimPatch -notmatch 'HarmonyPatch\(typeof\(Workable\), "GetAnim"\)' -or $getAnimPatch -notmatch '__instance is Door' -or $getAnimPatch -notmatch 'GetSafeDoorOverrideAnims\(__result\.overrideAnims\)') {
    throw 'Door Workable.GetAnim patch must sanitize null door override animations before StandardWorker.AttachOverrideAnims consumes them.'
}

$workableGuardIndex = $getAnimPatch.IndexOf('DoorPatchHelpers.ShouldUseDoorRemoteOverrideFallback')
$workableFallbackIndex = $getAnimPatch.IndexOf('DoorAnimPatch.TryGetDoorOverrideAnims')
if ($workableGuardIndex -lt 0 -or $workableFallbackIndex -lt 0 -or $workableFallbackIndex -lt $workableGuardIndex -or $getAnimPatch -notmatch '__result\.overrideAnims == null \|\| __result\.overrideAnims\.Length == 0' -or $getAnimPatch -notmatch '__result\.overrideAnims = .*OverrideAnims') {
    throw 'Door Workable.GetAnim patch must only synthesize the remote/tablet override for vanilla/Klei doors and this mod door, while arbitrary modded doors only get null override sanitization.'
}

if ($source -match 'throw;') {
    throw 'Runtime door patches must log patch exceptions without rethrowing into ONI, so diagnostics do not become game crashes.'
}

foreach ($patchClass in @('StructureTemperatureGetPatch', 'DoorAnimPatch', 'DoorWorkableGetAnimPatch', 'DoorSetSimStatePatch', 'DoorOnSpawnPatch', 'DoorCleanUpPatch', 'GameLeakSamplerPatch')) {
    $pattern = 'class\s+' + [regex]::Escape($patchClass) + '[\s\S]*?catch\s*\(Exception ex\)'
    if ($source -notmatch $pattern) {
        throw "$patchClass must catch and log runtime patch exceptions instead of allowing Harmony to surface them as game crashes."
    }
}

Write-Host 'airlock door lifecycle regression checks passed'
