param(
    [string]$ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

$resolverPath = Join-Path $ProjectDir 'Patches\TranslationFileResolver.cs'
$patchPath = Join-Path $ProjectDir 'Patches\TranslationPatch.cs'
$screenPath = Join-Path $ProjectDir 'UI\StatusBarScreen.cs'
$translationsDir = Join-Path $ProjectDir 'translations'
$workshopDescriptionPath = Join-Path $ProjectDir 'workshop-description.txt'
$workshopTitlePath = Join-Path $ProjectDir 'workshop-title.txt'

function SectionBetween([string]$Source, [string]$Start, [string]$End) {
    $startIndex = $Source.IndexOf($Start, [System.StringComparison]::Ordinal)
    if ($startIndex -lt 0) {
        throw "Missing section start: $Start"
    }
    $endIndex = $Source.IndexOf($End, $startIndex + $Start.Length, [System.StringComparison]::Ordinal)
    if ($endIndex -lt 0) {
        throw "Missing section end: $End"
    }
    $Source.Substring($startIndex, $endIndex - $startIndex)
}

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

$expectedTranslationLocales = @('cs', 'de', 'es', 'fr', 'ja', 'ko', 'pt', 'pt_BR', 'ru', 'zh')
foreach ($locale in $expectedTranslationLocales) {
    $poPath = Join-Path $translationsDir "$locale.po"
    if (-not (Test-Path -LiteralPath $poPath)) {
        throw "Missing shipped translation file for locale $locale"
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

$screenSource = Get-Content -LiteralPath $screenPath -Raw
$portraitSource = Get-Content -LiteralPath (Join-Path $ProjectDir 'UI\DupePortraitWidget.cs') -Raw
if ($screenSource -notmatch 'FindLocalizedGameFont') {
    throw "StatusBarScreen must use localized font selection instead of a hard-coded single font."
}
if ($screenSource -notmatch 'FontForText\(string text\)') {
    throw "StatusBarScreen must expose a shared font helper for DSB text."
}
if ($screenSource -notmatch 'ApplyFontForText\(TMPro\.TMP_Text tmp, string text\)') {
    throw "StatusBarScreen must expose a helper that applies ONI's primary UI font to each TMP label."
}
if ($screenSource -match 'ApplyFontForPortraitName\(TMPro\.TMP_Text tmp, string text\)') {
    throw "Portrait names must not use a separate per-string primary-font helper."
}
if ($screenSource -match 'FontForPortraitName\(string text\)') {
    throw "Portrait names must not switch primary fonts based on the current string contents."
}
if ($screenSource -notmatch 'FindDirectFallbackFontForText\(string text\)') {
    throw "StatusBarScreen diagnostics must still be able to report a direct runtime fallback font for field debugging."
}
$fontForTextSection = SectionBetween $screenSource 'internal static TMPro.TMP_FontAsset FontForText(string text)' 'internal static void ApplyFontForText'
if ($fontForTextSection -match 'FindDirectFallbackFontForText|ContainsNonAscii|SupportsText\(.*?,\s*text,\s*false\)') {
    throw "FontForText must not choose a primary font based on the current string contents."
}
$applyFontSection = SectionBetween $screenSource 'internal static void ApplyFontForText(TMPro.TMP_Text tmp, string text)' 'private static TMPro.TMP_FontAsset FindDirectFallbackFontForText'
if ($applyFontSection -match 'FindDirectFallbackFontForText|ContainsNonAscii|SupportsText\(.*?,\s*text,\s*false\)') {
    throw "ApplyFontForText must not choose a primary font based on the current string contents."
}
if ($screenSource -notmatch 'if\s*\(gameFont != null\)\s*return gameFont;') {
    throw "Per-text font selection must keep ONI's primary font for DSB labels whenever it is available."
}
if ($screenSource -match 'if\s*\(SupportsTextDirect\(gameFont, text\)\)\s*return gameFont;') {
    throw "Per-text font selection must not bypass the primary ONI font just because a glyph comes from a fallback language-pack font."
}
if ($screenSource -match 'SupportsTextDirect\(localizedFont, text\)') {
    throw "Per-text font selection must not switch DSB labels directly to the localized font when ONI's primary font is available."
}
if ($screenSource -match 'System\.Array\.Find\(all, f => IsNotoSans\(f\) && SupportsTextDirect\(f, text\)\)') {
    throw "Per-text font selection must not switch DSB labels directly to a Noto font when ONI's primary font is available."
}
if ($screenSource -notmatch 'FindLocaleFontByName\(all\)\s*\?\?\s*global::Localization\.FontAsset') {
    throw "Localized font lookup must prefer the locale-declared font name before generic Localization.FontAsset."
}
if ($screenSource -notmatch 'TMPro\.TMP_Settings\.defaultFontAsset') {
    throw "Font fallback setup must use TMP_Settings.defaultFontAsset, matching ONI's runtime menu font setup."
}
if ($screenSource -notmatch 'TMPro\.TMP_Settings\.fallbackFontAssets') {
    throw "Font fallback setup must use TMP_Settings.fallbackFontAssets, matching ONI's runtime menu fallback chain."
}
if ($screenSource -notmatch 'TryAddRuntimeFallbackFont') {
    throw "Font fallback setup must add ONI/TMP runtime fallback fonts without rejecting language-pack fonts outside DSB's probe list."
}
if ($screenSource -notmatch 'LogFontSetupSummary') {
    throw "StatusBarScreen must log a once-per-session font setup summary for issue diagnostics."
}
if ($screenSource -notmatch 'fallbackFonts=') {
    throw "Font diagnostics must include the fallback font assets attached to the portrait text primary font."
}
if ($screenSource -notmatch 'tmpSettingsFallbackFonts=') {
    throw "Font diagnostics must include ONI TMP_Settings fallback font assets."
}
if ($screenSource -notmatch 'sampleNames=') {
    throw "Font diagnostics must include sample mixed-language name glyph support."
}
foreach ($label in @('EnglishName', 'CzechName', 'GermanName', 'SpanishName', 'FrenchName', 'PortugueseName', 'RussianName', 'ChineseName', 'JapaneseName', 'KoreanName')) {
    if ($screenSource -notmatch [regex]::Escape($label + '=')) {
        throw "Font diagnostics must include mixed Latin + localized script sample support for $label."
    }
}
if ($screenSource -notmatch 'BuildLocalizedFontSample') {
    throw "StatusBarScreen must build the font probe from the active localized UI text."
}
if ($screenSource -notmatch 'GetCyrillicFallbackProbe') {
    throw "StatusBarScreen must keep a Cyrillic fallback probe available for Russian text."
}
if ($screenSource -match '(?s)string localizedSample = string\.Concat\(.*?\\u0414\\u0443\\u043f\\u043b\\u0438\\u043a\\u0430\\u043d\\u0442\\u044b') {
    throw "StatusBarScreen must not force Cyrillic into the normal localized sample for Latin locales."
}
if ($screenSource -notmatch 'SupportsText\(oniFont, localizedSample\)') {
    throw "StatusBarScreen must keep ONI's native font when it supports the active localized UI text."
}
if ($screenSource -notmatch 'RequiresFallbackFont\(localizedSample\)') {
    throw "StatusBarScreen must only force a fallback font for localized samples that need it."
}
if ($screenSource -notmatch 'SupportsText\(f, fallbackSample\)') {
    throw "StatusBarScreen font selection must verify glyph coverage for localized UI text."
}
if ($screenSource -notmatch 'IsNotoSans\(f\) && SupportsText\(f, fallbackSample\)') {
    throw "StatusBarScreen should prefer NotoSans only when it can render the fallback UI sample."
}
if ($screenSource -notmatch 'EnsureGameFontFallbacks\(oniFont, all\)') {
    throw "StatusBarScreen must keep ONI's native font as primary and attach localized fallback fonts."
}
if ($screenSource -notmatch 'fallbackFontAssetTable') {
    throw "StatusBarScreen must use TMP font fallback assets instead of replacing ONI's primary font."
}
if ($screenSource -notmatch 'BuildDynamicTextFallbackSample') {
    throw "StatusBarScreen must include dynamic dupe-name scripts in its fallback probe independent of UI locale."
}
if ($screenSource -notmatch 'BuildSupportedLocalizationFallbackSample') {
    throw "StatusBarScreen must probe every shipped DSB localization language, not only the active locale."
}
if ($screenSource -notmatch 'BuildSupportedNameFallbackSample') {
    throw "StatusBarScreen must include mixed Latin + localized name probes for every shipped non-ASCII localization language."
}
if ($screenSource -notmatch 'GetLatinExtendedFallbackProbe') {
    throw "StatusBarScreen must include Latin-extended glyph coverage for Czech, German, Spanish, French, and Portuguese names."
}
if ($screenSource -notmatch '\\u0414\\u0443\\u043f\\u043b\\u0438\\u043a\\u0430\\u043d\\u0442\\u044b') {
    throw "StatusBarScreen font probe must include Cyrillic coverage for Russian UI text."
}
if ($screenSource -notmatch '\\u590d\\u5236\\u4eba') {
    throw "StatusBarScreen font probe must include Chinese coverage for mixed-language dupe names."
}
$requiredFontProbeEscapes = @{
    Czech = '\\u0158'
    German = '\\u00df'
    Spanish = '\\u00f1'
    French = '\\u0153'
    Portuguese = '\\u00e3'
    Japanese = '\\u8907'
    Korean = '\\ubcf5'
}
foreach ($probe in $requiredFontProbeEscapes.GetEnumerator()) {
    if ($screenSource -notmatch $probe.Value) {
        throw "StatusBarScreen font probe must include $($probe.Key) localization glyph coverage."
    }
}
$requiredNameProbeEscapes = @{
    CzechName = '\\u0159'
    GermanName = '\\u00fc'
    SpanishName = '\\u00fa'
    FrenchName = '\\u00c9'
    PortugueseName = '\\u00e3'
    RussianName = '\\u0418\\u0432\\u0430\\u043d'
    ChineseName = '\\u5c0f\\u660e'
    JapaneseName = '\\u5c71\\u7530\\u592a\\u90ce'
    KoreanName = '\\uae40\\ubbfc\\uc900'
}
foreach ($probe in $requiredNameProbeEscapes.GetEnumerator()) {
    if ($screenSource -notmatch $probe.Value) {
        throw "StatusBarScreen mixed-name fallback probes must include $($probe.Key)."
    }
}
foreach ($probeName in @('GetCzechNameFallbackProbe', 'GetGermanNameFallbackProbe', 'GetSpanishNameFallbackProbe', 'GetFrenchNameFallbackProbe', 'GetPortugueseNameFallbackProbe', 'GetRussianNameFallbackProbe', 'GetChineseNameFallbackProbe', 'GetJapaneseNameFallbackProbe', 'GetKoreanNameFallbackProbe')) {
    if ($screenSource -notmatch $probeName) {
        throw "StatusBarScreen must include $probeName for dynamic duplicant names."
    }
}
if ($screenSource -match 'GRAYSTROKE REGULAR SDF"\)\s*\?\?\s*System\.Array\.Find\(all, f => f\.name\.Contains\("NotoSans"\)\)') {
    throw "StatusBarScreen must not prefer GRAYSTROKE before checking localized NotoSans glyph coverage."
}
if ($portraitSource -notmatch 'ApplyFontForText\(nameLabel, displayName\)') {
    throw "DupePortraitWidget portrait names must use the same primary/fallback font helper as tooltips."
}
if ($portraitSource -match 'nameLabel\s*=\s*AddText\(transform, "Name"\)') {
    throw "DupePortraitWidget portrait names must not use the raw TextMeshProUGUI AddText helper inside the masked status bar grid."
}
if ($portraitSource -notmatch 'nameLabel\s*=\s*AddLocText\(transform, "Name"\)') {
    throw "DupePortraitWidget portrait names must use Klei's LocText wrapper so ONI text setup is applied inside the masked status bar grid."
}
if ($portraitSource -notmatch 'AddComponent<LocText>\(\)') {
    throw "DupePortraitWidget must instantiate Klei LocText for the portrait name label."
}
if ($portraitSource -notmatch 'Util\.KInstantiateUI') {
    throw "DupePortraitWidget must first try to clone an existing Klei UI LocText label before falling back to constructing one."
}
if ($portraitSource -notmatch 'FindLocTextTemplate') {
    throw "DupePortraitWidget must find a Klei LocText template for the portrait name label."
}
if ($portraitSource -notmatch 'allowOverride\s*=\s*true') {
    throw "Portrait LocText must allow dynamic duplicant names rather than treating text as a localization key."
}
if ($portraitSource -match 'ApplyFontForPortraitName\(nameLabel, displayName\)') {
    throw "DupePortraitWidget portrait names must not use the failed portrait-specific primary-font switching helper."
}
if ($portraitSource -notmatch 'ApplyFontForText\(initialText, initials\)') {
    throw "DupePortraitWidget must apply a per-text font to initials mode for non-Latin names."
}
if ($portraitSource -match 'nameLabel\.overflowMode\s*=\s*TextOverflowModes\.Ellipsis') {
    throw "Portrait name labels must not use TMP Ellipsis overflow because it can blank labels that render through fallback CJK fonts."
}
if ($portraitSource -notmatch 'nameLabel\.overflowMode\s*=\s*TextOverflowModes\.Truncate') {
    throw "Portrait name labels must truncate instead of using ellipsis so fallback CJK glyphs do not blank the whole label."
}
if ($portraitSource -notmatch 'nameLabel\.verticalAlignment\s*=\s*VerticalAlignmentOptions\.Middle') {
    throw "Portrait name labels must vertically center fallback glyph metrics in the fixed label area."
}
if ($portraitSource -notmatch 'nameLabel\.textWrappingMode\s*=\s*TextWrappingModes\.NoWrap') {
    throw "Portrait name labels must keep no-wrap behavior while avoiding obsolete TMP word-wrap APIs."
}
if ($portraitSource -notmatch 'nameLayout\.preferredHeight\s*=\s*20') {
    throw "Portrait name labels need enough height for CJK fallback font metrics; field diagnostics showed CJK preferred height 17.8px in a 16px label, producing zero TMP vertices."
}
if ($portraitSource -notmatch 'int totalH = size \+ 26;') {
    throw "DupePortraitWidget total height must grow with the 20px CJK-safe name label instead of squeezing the portrait label back down."
}
if ($portraitSource -notmatch 'LogTextRenderDiagnostics\("PortraitName", nameLabel, displayName\)') {
    throw "Portrait names must use the shared text-render diagnostic dump so failed field reports include the same evidence as tooltips."
}
$tooltipSource = Get-Content -LiteralPath (Join-Path $ProjectDir 'UI\DupeTooltip.cs') -Raw
if ($tooltipSource -notmatch 'LogTextRenderDiagnostics\("Tooltip", tooltipText, tooltip\)') {
    throw "Tooltips must log the same text-render diagnostic shape as portrait names for working-vs-broken comparison."
}
foreach ($diagToken in @(
    'textObjectPath=',
    'component=',
    'activeInHierarchy=',
    'canvas=',
    'worldCorners=',
    'stencilDepth=',
    'materialProps=',
    'materialSlots=',
    'charDetails=',
    'subMeshes=',
    'canvasRenderer=',
    'fallbackSourceMaterial=',
    'GetComponentsInChildren<TMPro.TMP_SubMeshUI>',
    'MaskUtilities.GetStencilDepth',
    'ShaderPropertySummary')) {
    if ($screenSource -notmatch [regex]::Escape($diagToken)) {
        throw "Shared text-render diagnostics must include $diagToken to isolate fallback submesh, material, stencil, and culling failures."
    }
}

Write-Output 'translation path fallback checks passed'
