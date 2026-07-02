$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$modInfo = Join-Path $projectDir 'mod_info.yaml'
$modYaml = Join-Path $projectDir 'mod.yaml'
$workshopDescription = Join-Path $projectDir 'workshop-description.bbcode'
$legacyWorkshopDescription = Join-Path $projectDir 'workshop-description.txt'
$notice = Join-Path $projectDir 'NOTICE.md'
$airlockConstants = Join-Path $projectDir 'Core\AirlockConstants.cs'
$modStrings = Join-Path $projectDir 'Localization\ModStrings.cs'
$zhTranslation = Join-Path $projectDir 'translations\zh.po'

if (-not (Test-Path -LiteralPath $modInfo)) {
    throw "Missing mod_info.yaml at $modInfo"
}

if (-not (Test-Path -LiteralPath $modYaml)) {
    throw "Missing mod.yaml at $modYaml"
}

if (-not (Test-Path -LiteralPath $workshopDescription)) {
    throw "Missing workshop-description.bbcode at $workshopDescription"
}

if (Test-Path -LiteralPath $legacyWorkshopDescription) {
    throw "Use workshop-description.bbcode instead of workshop-description.txt."
}

if (-not (Test-Path -LiteralPath $notice)) {
    throw "Missing NOTICE.md at $notice"
}

if (-not (Test-Path -LiteralPath $airlockConstants)) {
    throw "Missing AirlockConstants.cs at $airlockConstants"
}

if (-not (Test-Path -LiteralPath $modStrings)) {
    throw "Missing ModStrings.cs at $modStrings"
}

$info = Get-Content -Raw -LiteralPath $modInfo
$yaml = Get-Content -Raw -LiteralPath $modYaml
$workshop = Get-Content -Raw -LiteralPath $workshopDescription
$noticeText = Get-Content -Raw -LiteralPath $notice
$constants = Get-Content -Raw -LiteralPath $airlockConstants
$strings = Get-Content -Raw -LiteralPath $modStrings
$zh = if (Test-Path -LiteralPath $zhTranslation) { Get-Content -Raw -LiteralPath $zhTranslation } else { '' }

if ($info -notmatch '(?m)^supportedContent:\s*ALL\s*$') {
    throw 'mod_info.yaml must declare supportedContent: ALL for all current DLC modes.'
}

if ($info -notmatch '(?m)^APIVersion:\s*2\s*$') {
    throw 'mod_info.yaml must declare APIVersion: 2.'
}

if ($info -notmatch '(?m)^minimumSupportedBuild:\s*(\d+)\s*$') {
    throw 'mod_info.yaml must declare minimumSupportedBuild.'
}

$minimumBuild = [int]$Matches[1]
if ($minimumBuild -lt 737790) {
    throw "minimumSupportedBuild must be at least 737790 for the June 2026 SimMessages API change. Found $minimumBuild."
}

if ($yaml -notmatch '(?m)^staticID:\s*zerotheabsolute\.FastInsulatedSelfSealingAirLock\s*$') {
    throw 'mod.yaml must use this continuation staticID to avoid impersonating the original workshop item.'
}

if ($yaml -notmatch '(?m)^title:\s*"Fast Insulated Self Sealing AirLock Continued"\s*$') {
    throw 'mod.yaml title must clearly identify this as the continued version.'
}

if ($strings -notmatch 'public static LocString NAME = "Fast Insulated Self-Sealing Airlock";') {
    throw 'The in-game building name must remain Fast Insulated Self-Sealing Airlock without Continued.'
}

if ($strings -match 'Building\s*\{[\s\S]*Continued') {
    throw 'The in-game building strings must not include Continued; only mod metadata should use the continuation name.'
}

if ($zh -ne '' -and $zh -notmatch 'msgid "Fast Insulated Self-Sealing Airlock"') {
    throw 'zh.po must track the in-game building msgid without Continued.'
}

if ($constants -notmatch 'public const string BuildingId = "FastInsulatedSelfSealingAirLock";') {
    throw 'AirlockConstants.BuildingId must remain FastInsulatedSelfSealingAirLock for save compatibility with existing placed doors.'
}

if ($constants -match 'BuildingId\s*=\s*".*Continued') {
    throw 'AirlockConstants.BuildingId must not include Continued; only display metadata should use the continuation name.'
}

if ($yaml -notmatch 'Community continuation') {
    throw 'mod.yaml description must clearly identify this as a community continuation.'
}

$requiredCredits = @('Neavo', 'Tuna', 'mrcyclo', 'EnemyArea', 'Davkas', 'Triggernometry', 'Chromiumboy', 'gbdickinson')
foreach ($credit in $requiredCredits) {
    if ($workshop -notmatch [regex]::Escape($credit)) {
        throw "workshop-description.bbcode must credit $credit."
    }

    if ($noticeText -notmatch [regex]::Escape($credit)) {
        throw "NOTICE.md must credit $credit."
    }
}

if ($workshop -notmatch 'no explicit license file' -or $noticeText -notmatch 'No explicit license file') {
    throw 'Workshop description and NOTICE.md must document the current license uncertainty.'
}

if ($workshop -notmatch 'Existing doors' -or $workshop -notmatch 'should not need to deconstruct or replace' -or $noticeText -notmatch 'without replacement') {
    throw 'Workshop description and NOTICE.md must document migration without replacing existing doors.'
}

if ($workshop -notmatch '\[b\]Overview\[/b\]' -or $workshop -notmatch '\[list\]' -or $workshop -notmatch '\[url=https://steamcommunity\.com/sharedfiles/filedetails/\?id=3231839363\]') {
    throw 'workshop-description.bbcode must use Steam BBCode headings, lists, and links.'
}

if ($workshop -match '(?m)^\s*\* ' -or $workshop -match '`') {
    throw 'workshop-description.bbcode must not contain markdown bullets or backtick formatting.'
}

Write-Host 'airlock metadata regression checks passed'
