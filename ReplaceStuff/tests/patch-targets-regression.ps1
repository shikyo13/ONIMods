$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
$assemblyPath = 'D:\SteamLibrary\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed\Assembly-CSharp.dll'
$checkScript = Join-Path $repoRoot 'tools\check-patch-targets.ps1'

if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw "Missing ONI Assembly-CSharp.dll at $assemblyPath"
}

$pairs = @(
    'BuildingDef::IsValidReplaceLocation',
    'Constructable::OnCompleteWork',
    'SelectToolHoverTextCard::UpdateHoverElements',
    'BedConfig::CreateBuildingDef',
    'BedConfig::DoPostConfigureComplete',
    'LadderBedConfig::CreateBuildingDef',
    'LadderBedConfig::DoPostConfigureComplete',
    'DoorConfig::CreateBuildingDef',
    'DoorConfig::DoPostConfigureComplete',
    'ManualPressureDoorConfig::CreateBuildingDef',
    'ManualPressureDoorConfig::DoPostConfigureComplete',
    'PressureDoorConfig::CreateBuildingDef',
    'PressureDoorConfig::DoPostConfigureComplete',
    'ManualGeneratorConfig::CreateBuildingDef',
    'ManualGeneratorConfig::DoPostConfigureComplete',
    'WoodGasGeneratorConfig::CreateBuildingDef',
    'WoodGasGeneratorConfig::DoPostConfigureComplete',
    'HydrogenGeneratorConfig::CreateBuildingDef',
    'HydrogenGeneratorConfig::DoPostConfigureComplete',
    'MethaneGeneratorConfig::CreateBuildingDef',
    'MethaneGeneratorConfig::DoPostConfigureComplete',
    'StorageLockerConfig::CreateBuildingDef',
    'StorageLockerConfig::DoPostConfigureComplete',
    'StorageLockerSmartConfig::CreateBuildingDef',
    'StorageLockerSmartConfig::DoPostConfigureComplete',
    'OuthouseConfig::CreateBuildingDef',
    'OuthouseConfig::DoPostConfigureComplete',
    'FlushToiletConfig::CreateBuildingDef',
    'FlushToiletConfig::DoPostConfigureComplete',
    'WashBasinConfig::CreateBuildingDef',
    'WashBasinConfig::DoPostConfigureComplete',
    'WashSinkConfig::CreateBuildingDef',
    'WashSinkConfig::DoPostConfigureComplete'
)

& $checkScript -AssemblyPath $assemblyPath -Pairs $pairs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host 'ReplaceStuff patch target regression checks passed'
