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

$il = & $ilspy.Source -il $dll

$required = @(
    "SimMessages::SetCellProperties(int32, uint8, int32)",
    "SimMessages::ClearCellProperties(int32, uint8, int32)"
)

foreach ($memberRef in $required) {
    if (($il | Select-String -SimpleMatch $memberRef).Count -eq 0) {
        throw "Missing rebuilt three-argument member reference: $memberRef"
    }
}

$forbidden = @(
    "SimMessages::SetCellProperties(int32, uint8)",
    "SimMessages::ClearCellProperties(int32, uint8)"
)

foreach ($memberRef in $forbidden) {
    if (($il | Select-String -SimpleMatch $memberRef).Count -gt 0) {
        throw "Found stale two-argument member reference: $memberRef"
    }
}

Write-Host 'airlock SimMessages signature regression checks passed'
