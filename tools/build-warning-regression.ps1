$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectFiles = @(
    'BuildThrough/BuildThrough.csproj',
    'GCBudget/GCBudget.csproj',
    'OniProfiler/OniProfiler.csproj',
    'ReplaceStuff/ReplaceStuff.csproj',
    'DuplicantStatusBar/DuplicantStatusBar.csproj',
    'FastInsulatedSelfSealingAirLock/FastInsulatedSelfSealingAirLock.csproj'
)

foreach ($relativePath in $projectFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing expected project file: $relativePath"
    }

    $project = Get-Content -LiteralPath $path -Raw
    if ($project -match '<Import\s+Project="ILRepack\.targets"\s*/>') {
        throw "$relativePath must not manually import ILRepack.targets. ILRepack.Lib.MSBuild.Task imports project-local targets automatically, and the manual import causes MSB4011."
    }
}

Write-Host 'build warning regression checks passed'
