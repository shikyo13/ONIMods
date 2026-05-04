param(
    [string]$ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

$resolverPath = Join-Path $ProjectDir 'Patches\TranslationFileResolver.cs'
$patchPath = Join-Path $ProjectDir 'Patches\TranslationPatch.cs'
$translationsDir = Join-Path $ProjectDir 'translations'
$workshopDescriptionPath = Join-Path $ProjectDir 'workshop-description.txt'
$workshopTitlePath = Join-Path $ProjectDir 'workshop-title.txt'

if (-not (Test-Path -LiteralPath $resolverPath)) {
    throw "Missing TranslationFileResolver.cs"
}

$source = Get-Content -LiteralPath $resolverPath -Raw
$source = $source -replace 'internal static class TranslationFileResolver', 'public static class TranslationFileResolver'
$source = $source -replace 'internal static ', 'public static '
$assembly = Add-Type -TypeDefinition $source -PassThru
$resolver = $assembly | Where-Object { $_.FullName -eq 'DuplicantStatusBar.Patches.TranslationFileResolver' }
if ($null -eq $resolver) {
    throw "TranslationFileResolver type was not compiled"
}

$brokenName = $resolver::GetBrokenExtractorFileName('cs')
if ($brokenName -ne 'translations\cs.po') {
    throw "Expected broken extractor filename translations\cs.po, got $brokenName"
}

$candidates = $resolver::GetCandidatePaths((Join-Path ([System.IO.Path]::GetTempPath()) 'dsb-mod'), 'cs')
if ($candidates.Count -ne 2) {
    throw "Expected exactly two translation path candidates, got $($candidates.Count)"
}
if (-not $candidates[1].EndsWith($brokenName, [System.StringComparison]::Ordinal)) {
    throw "Expected fallback candidate to end with $brokenName, got $($candidates[1])"
}

$ptLowerCandidates = $resolver::GetCandidatePaths((Join-Path ([System.IO.Path]::GetTempPath()) 'dsb-mod'), 'pt-br')
$ptBrIndex = -1
$ptBaseIndex = -1
for ($i = 0; $i -lt $ptLowerCandidates.Count; $i++) {
    if ($ptLowerCandidates[$i].EndsWith('translations\pt_BR.po', [System.StringComparison]::Ordinal)) {
        $ptBrIndex = $i
    }
    if ($ptLowerCandidates[$i].EndsWith('translations\pt.po', [System.StringComparison]::Ordinal)) {
        $ptBaseIndex = $i
    }
}
if ($ptBrIndex -lt 0) {
    throw "Expected lowercase pt-br locale to include a correctly cased pt_BR.po candidate"
}
if ($ptBaseIndex -ge 0 -and $ptBrIndex -gt $ptBaseIndex) {
    throw "Expected pt_BR.po to be checked before generic pt.po for pt-br"
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('dsb-translation-path-test-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path (Join-Path $tempRoot 'translations') | Out-Null
    $normalPath = Join-Path (Join-Path $tempRoot 'translations') 'cs.po'
    Set-Content -LiteralPath $normalPath -Value 'msgid ""' -Encoding UTF8

    $resolved = $resolver::Resolve($tempRoot, 'cs')
    if ($resolved -ne $normalPath) {
        throw "Expected normal path to resolve first. Expected $normalPath, got $resolved"
    }

    $zhAliasPath = Join-Path (Join-Path $tempRoot 'translations') 'zh.po'
    Set-Content -LiteralPath $zhAliasPath -Value 'msgid ""' -Encoding UTF8
    foreach ($alias in @('zh-CN', 'zh_CN', 'zh-Hans', 'zh_Hans')) {
        $resolvedAlias = $resolver::Resolve($tempRoot, $alias)
        if ($resolvedAlias -ne $zhAliasPath) {
            throw "Expected locale alias $alias to resolve to $zhAliasPath, got $resolvedAlias"
        }
    }

    $ptBrPath = Join-Path (Join-Path $tempRoot 'translations') 'pt_BR.po'
    Set-Content -LiteralPath $ptBrPath -Value 'msgid ""' -Encoding UTF8
    foreach ($alias in @('pt-BR', 'pt_BR')) {
        $resolvedAlias = $resolver::Resolve($tempRoot, $alias)
        if ($resolvedAlias -ne $ptBrPath) {
            throw "Expected locale alias $alias to resolve to $ptBrPath, got $resolvedAlias"
        }
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

$patchSource = Get-Content -LiteralPath $patchPath -Raw
if ($patchSource -notmatch 'TranslationFileResolver\.Resolve') {
    throw "TranslationPatch does not use TranslationFileResolver.Resolve"
}

$requiredTranslationKeys = @(
    'STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_OXYGEN_TANK',
    'STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_SKILLPOINTS',
    'STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_SKILLPOINTS_AVAILABLE',
    'STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_STAMINA',
    'STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_BATTERY',
    'STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_GUNK',
    'STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_GEAR_OIL',
    'STRINGS.DUPLICANTSTATUSBAR.ALERTS.LOWBATTERY',
    'STRINGS.DUPLICANTSTATUSBAR.ALERTS.LOWGEAROIL',
    'STRINGS.DUPLICANTSTATUSBAR.ALERTS.GRINDINGGEARS',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.SHOWSKILLPOINTBADGES.NAME',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.SHOWSKILLPOINTBADGES.DESC',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.SHOWSKILLPOINTSTOOLTIP.NAME',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.SHOWSKILLPOINTSTOOLTIP.DESC',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.ALERTLOWBATTERY.NAME',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.ALERTLOWBATTERY.DESC',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.ALERTLOWGEAROIL.NAME',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.ALERTLOWGEAROIL.DESC',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.ALERTGRINDINGGEARS.NAME',
    'STRINGS.DUPLICANTSTATUSBAR.OPTIONS.ALERTGRINDINGGEARS.DESC'
)

foreach ($poFile in Get-ChildItem -LiteralPath $translationsDir -Filter '*.po') {
    $poText = Get-Content -LiteralPath $poFile.FullName -Raw
    foreach ($key in $requiredTranslationKeys) {
        if ($poText -notmatch [regex]::Escape("msgctxt `"$key`"")) {
            throw "Missing translation key $key in $($poFile.Name)"
        }
    }
}

if (-not (Test-Path -LiteralPath $workshopTitlePath)) {
    throw "Missing workshop-title.txt"
}
$workshopTitle = Get-Content -LiteralPath $workshopTitlePath -Raw
if ($workshopTitle -notmatch 'DLC') {
    throw "Workshop title should advertise DLC support"
}

$workshopDescription = Get-Content -LiteralPath $workshopDescriptionPath -Raw
if ($workshopDescription -notmatch 'Bionic Booster') {
    throw "Workshop description must explicitly mention Bionic Booster compatibility"
}
if ($workshopDescription -notmatch 'Prehistoric Planet') {
    throw "Workshop description must explicitly mention Prehistoric Planet compatibility"
}

Write-Output 'translation path fallback checks passed'
