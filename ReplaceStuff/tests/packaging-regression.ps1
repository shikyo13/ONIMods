$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$projectFile = Join-Path $projectDir 'ReplaceStuff.csproj'
$optionsFile = Join-Path $projectDir 'Config\ReplaceStuffOptions.cs'
$preview = Join-Path $projectDir 'preview.png'

foreach ($path in @($projectFile, $optionsFile, $preview)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing required packaging file: $path"
    }
}

$project = Get-Content -Raw -LiteralPath $projectFile
$options = Get-Content -Raw -LiteralPath $optionsFile

if ($project -notmatch '<GenerateAssemblyInfo>true</GenerateAssemblyInfo>') {
    throw 'ReplaceStuff.csproj must generate assembly metadata so DLL versions are not 0.0.0.0.'
}

if ($project -notmatch '<GameManaged Condition="''\$\(GameManaged\)'' == ''''">') {
    throw 'GameManaged must be overrideable from MSBuild properties or environment without editing the project file.'
}

if ($project -match '<Import Project="ILRepack\.targets"') {
    throw 'Do not explicitly import ILRepack.targets; the NuGet package imports it and duplicate imports create Release build warnings.'
}

if ($project -notmatch 'Name="DeployMod"') {
    throw 'ReplaceStuff.csproj must deploy Release packages to the ONI dev mod folder.'
}

foreach ($required in @('ReplaceStuff.dll', 'mod.yaml', 'mod_info.yaml', 'CHANGELOG.txt', 'preview.png')) {
    if ($project -notmatch [regex]::Escape($required)) {
        throw "DeployMod must include $required."
    }
}

foreach ($stale in @('PLib.*', 'config.json', 'Mod.yaml', 'workshop-description.*')) {
    if ($project -notmatch [regex]::Escape($stale)) {
        throw "DeployMod must remove stale $stale from the dev mod folder."
    }
}

if ($project -match 'workshop-description\.bbcode.*ModMeta') {
    throw 'workshop-description.bbcode is source-only and must not be copied into the dev mod folder.'
}

if ($options -notmatch '\[ConfigFile\("ReplaceStuff\.json", false, true\)\]') {
    throw 'ReplaceStuff options must use PLib shared config storage for Workshop safety.'
}

if ($options -match 'yourusername' -or $options -notmatch '\[ModInfo\("https://github\.com/shikyo13/ONIMods/tree/master/ReplaceStuff"\)\]') {
    throw 'ReplaceStuff ModInfo must use the real source URL, not a placeholder.'
}

$bytes = [System.IO.File]::ReadAllBytes($preview)
$pngSignature = [byte[]](0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
for ($i = 0; $i -lt $pngSignature.Length; $i++) {
    if ($bytes[$i] -ne $pngSignature[$i]) {
        throw 'preview.png must be a valid PNG file.'
    }
}

Write-Host 'ReplaceStuff packaging regression checks passed'
