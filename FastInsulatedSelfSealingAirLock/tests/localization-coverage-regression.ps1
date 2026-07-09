param(
    [string]$ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

$repoDir = Split-Path -Parent $ProjectDir
$dsbTranslationsDir = Join-Path $repoDir 'DuplicantStatusBar\translations'
$translationsDir = Join-Path $ProjectDir 'translations'
$resolverPath = Join-Path $ProjectDir 'Patches\TranslationFileResolver.cs'
$patchPath = Join-Path $ProjectDir 'Patches\TranslationPatch.cs'
$modLifecyclePath = Join-Path $ProjectDir 'Core\FastInsulatedSelfSealingAirLockMod.cs'

foreach ($path in @($dsbTranslationsDir, $translationsDir)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing translations directory: $path"
    }
}

$dsbLocales = Get-ChildItem -LiteralPath $dsbTranslationsDir -Filter '*.po' |
    ForEach-Object { $_.BaseName } |
    Sort-Object
$airlockLocales = Get-ChildItem -LiteralPath $translationsDir -Filter '*.po' |
    ForEach-Object { $_.BaseName } |
    Sort-Object

$missingLocales = Compare-Object -ReferenceObject $dsbLocales -DifferenceObject $airlockLocales |
    Where-Object { $_.SideIndicator -eq '<=' } |
    ForEach-Object { $_.InputObject }
if ($missingLocales) {
    throw "Airlock translations are missing DSB-supported locale(s): $($missingLocales -join ', ')"
}

$extraLocales = Compare-Object -ReferenceObject $dsbLocales -DifferenceObject $airlockLocales |
    Where-Object { $_.SideIndicator -eq '=>' } |
    ForEach-Object { $_.InputObject }
if ($extraLocales) {
    throw "Airlock translations include locale(s) not currently supported by DSB: $($extraLocales -join ', ')"
}

$requiredKeys = @(
    'FastInsulatedSelfSealingAirLock.ModStrings.Options.MULTIPLIER_TITLE',
    'FastInsulatedSelfSealingAirLock.ModStrings.Options.MULTIPLIER_TOOLTIPS',
    'FastInsulatedSelfSealingAirLock.ModStrings.Options.DEBUG_LOGGING_TITLE',
    'FastInsulatedSelfSealingAirLock.ModStrings.Options.DEBUG_LOGGING_TOOLTIPS',
    'FastInsulatedSelfSealingAirLock.ModStrings.Building.NAME',
    'FastInsulatedSelfSealingAirLock.ModStrings.Building.EFFECT',
    'FastInsulatedSelfSealingAirLock.ModStrings.Building.DESC'
)

foreach ($poFile in Get-ChildItem -LiteralPath $translationsDir -Filter '*.po') {
    $poText = Get-Content -LiteralPath $poFile.FullName -Raw
    foreach ($key in $requiredKeys) {
        if ($poText -notmatch [regex]::Escape("msgctxt `"$key`"")) {
            throw "Missing translation key $key in $($poFile.Name)"
        }
    }
}

if (-not (Test-Path -LiteralPath $resolverPath)) {
    throw 'Missing TranslationFileResolver.cs for locale alias and broken Workshop extraction fallback.'
}

if (-not (Test-Path -LiteralPath $patchPath)) {
    throw 'Missing TranslationPatch.cs for explicit translation loading.'
}

$source = Get-Content -LiteralPath $resolverPath -Raw
$source = $source -replace 'internal static class TranslationFileResolver', 'public static class TranslationFileResolver'
$source = $source -replace 'internal static ', 'public static '
$assembly = Add-Type -TypeDefinition $source -PassThru
$resolver = $assembly | Where-Object { $_.FullName -eq 'FastInsulatedSelfSealingAirLock.TranslationFileResolver' }
if ($null -eq $resolver) {
    throw 'TranslationFileResolver type was not compiled.'
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('fissac-translation-path-test-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path (Join-Path $tempRoot 'translations') | Out-Null

    $normalPath = Join-Path (Join-Path $tempRoot 'translations') 'cs.po'
    Set-Content -LiteralPath $normalPath -Value 'msgid ""' -Encoding UTF8
    if ($resolver::Resolve($tempRoot, 'cs') -ne $normalPath) {
        throw 'Expected normal translation path to resolve before fallback paths.'
    }

    $zhAliasPath = Join-Path (Join-Path $tempRoot 'translations') 'zh.po'
    Set-Content -LiteralPath $zhAliasPath -Value 'msgid ""' -Encoding UTF8
    foreach ($alias in @('zh-CN', 'zh_CN', 'zh-Hans', 'zh_Hans')) {
        if ($resolver::Resolve($tempRoot, $alias) -ne $zhAliasPath) {
            throw "Expected locale alias $alias to resolve to zh.po."
        }
    }

    $ptBrPath = Join-Path (Join-Path $tempRoot 'translations') 'pt_BR.po'
    Set-Content -LiteralPath $ptBrPath -Value 'msgid ""' -Encoding UTF8
    foreach ($alias in @('pt-BR', 'pt_BR')) {
        if ($resolver::Resolve($tempRoot, $alias) -ne $ptBrPath) {
            throw "Expected locale alias $alias to resolve to pt_BR.po."
        }
    }

    Remove-Item -LiteralPath $normalPath -Force
    $brokenPath = Join-Path $tempRoot ($resolver::GetBrokenExtractorFileName('cs'))
    Set-Content -LiteralPath $brokenPath -Value 'msgid ""' -Encoding UTF8
    if ($resolver::Resolve($tempRoot, 'cs') -ne $brokenPath) {
        throw 'Expected broken Workshop extraction path to resolve when normal translations folder is absent.'
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

$patchSource = Get-Content -LiteralPath $patchPath -Raw
if ($patchSource -notmatch 'TranslationFileResolver\.Resolve') {
    throw 'TranslationPatch must use TranslationFileResolver.Resolve.'
}
if ($patchSource -notmatch 'FastInsulatedSelfSealingAirLock\.ModStrings') {
    throw 'TranslationPatch must apply translations to the airlock ModStrings type.'
}
if ($patchSource -notmatch 'STRINGS\.BUILDINGS\.PREFABS') {
    throw 'TranslationPatch must refresh building strings after loading translated ModStrings.'
}

$modLifecycle = Get-Content -LiteralPath $modLifecyclePath -Raw
if ($modLifecycle -notmatch 'PatchClass\(harmony,\s*typeof\(TranslationPatch\)\)') {
    throw 'OnLoad must patch TranslationPatch before Db.Initialize.'
}

Write-Host 'airlock localization coverage regression checks passed'
