param(
    [string]$ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

$resolverPath = Join-Path $ProjectDir 'Patches\TranslationFileResolver.cs'
$patchPath = Join-Path $ProjectDir 'Patches\TranslationPatch.cs'

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

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('dsb-translation-path-test-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path (Join-Path $tempRoot 'translations') | Out-Null
    $normalPath = Join-Path (Join-Path $tempRoot 'translations') 'cs.po'
    Set-Content -LiteralPath $normalPath -Value 'msgid ""' -Encoding UTF8

    $resolved = $resolver::Resolve($tempRoot, 'cs')
    if ($resolved -ne $normalPath) {
        throw "Expected normal path to resolve first. Expected $normalPath, got $resolved"
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

Write-Output 'translation path fallback checks passed'
