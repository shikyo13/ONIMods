$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$sourcePath = Join-Path $projectDir 'Buildings\FastInsulatedDoorConfig.cs'
$dll = Join-Path $projectDir 'bin\Release\FastInsulatedSelfSealingAirLock.dll'

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Missing source file at $sourcePath"
}

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Missing Release DLL at $dll. Build the project first."
}

$source = Get-Content -Raw -LiteralPath $sourcePath
$standardDoorPortPattern = 'def\.LogicInputPorts\s*=\s*DoorConfig\.CreateSingleInputPortList\(new CellOffset\(0,\s*0\)\);'

if ($source -notmatch $standardDoorPortPattern) {
    throw 'Fast airlock must use the standard vanilla door automation input at cell offset (0, 0).'
}

$ilspy = Get-Command ilspycmd -ErrorAction SilentlyContinue
if (-not $ilspy) {
    throw 'ilspycmd is required for this regression check.'
}

$decompiled = & $ilspy.Source -t 'FastInsulatedSelfSealingAirLock.FastInsulatedDoorConfig' $dll 2>$null
if ($LASTEXITCODE -ne 0) {
    throw 'Could not decompile FastInsulatedDoorConfig from the Release DLL.'
}

$decompiledText = $decompiled -join "`n"
if ($decompiledText -notmatch 'LogicInputPorts\s*=\s*DoorConfig\.CreateSingleInputPortList\(new CellOffset\(0,\s*0\)\)') {
    throw 'Release DLL does not contain the standard vanilla door automation input.'
}

Write-Host 'airlock door automation regression checks passed'
