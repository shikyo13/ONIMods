$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$projectFile = Join-Path $projectDir 'FastInsulatedSelfSealingAirLock.csproj'
$sourceConfig = Join-Path $projectDir 'config.json'

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

if ($project -notmatch '<Delete Files="\$\(ModDeploy\)CHANGELOG\.\*"') {
    throw 'DeployMod must remove stale CHANGELOG files from the dev deploy folder.'
}

if ($project -notmatch '<Delete Files="\$\(ModDeploy\)config\.json"') {
    throw 'DeployMod must remove stale config.json from the dev deploy folder so local rebuilds do not reset user options.'
}

if ($project -match 'ModMeta Include="[^"]*workshop-description') {
    throw 'DeployMod must not copy workshop-description source files into the dev mod folder.'
}

if ($project -match 'ModMeta Include="[^"]*CHANGELOG') {
    throw 'DeployMod must not copy CHANGELOG source files into the dev mod folder.'
}

if ($project -match 'ModMeta Include="[^"]*config\.json') {
    throw 'DeployMod must not copy config.json into the dev mod folder because it overwrites runtime user settings.'
}

if (Test-Path -LiteralPath $sourceConfig) {
    throw 'The airlock project must not track a default config.json; runtime options belong in PLib shared config storage.'
}

if ($project -notmatch 'FastInsulatedSelfSealingAirlockContinued') {
    throw 'DeployMod must clean the legacy pre-rename dev folder so local testing does not load two continuation builds.'
}

if ($project -notmatch '<RemoveDir Directories="\$\(LegacyModDeploy\)"') {
    throw 'DeployMod must remove the legacy pre-rename dev folder when it exists.'
}

Write-Host 'airlock packaging regression checks passed'
