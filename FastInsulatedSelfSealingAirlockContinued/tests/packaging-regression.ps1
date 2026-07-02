$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$projectFile = Join-Path $projectDir 'FastInsulatedSelfSealingAirlockContinued.csproj'

if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Missing project file at $projectFile"
}

$project = Get-Content -Raw -LiteralPath $projectFile

if ($project -notmatch '<GameManaged Condition="''\$\(GameManaged\)'' == ''''">') {
    throw 'GameManaged must be overrideable from MSBuild properties or environment without editing the project file.'
}

if ($project -notmatch '<Delete Files="\$\(ModDeploy\)PLib\.\*"') {
    throw 'DeployMod must remove stale standalone PLib artifacts before copying the merged mod package.'
}

if ($project -notmatch 'NOTICE\.md') {
    throw 'DeployMod must include NOTICE.md so credits and license status ship with the package.'
}

if ($project -notmatch '<Delete Files="\$\(ModDeploy\)workshop-description\.\*"') {
    throw 'DeployMod must remove stale workshop-description files from the dev deploy folder.'
}

if ($project -match 'ModMeta Include="[^"]*workshop-description') {
    throw 'DeployMod must not copy workshop-description source files into the dev mod folder.'
}

Write-Host 'airlock packaging regression checks passed'
