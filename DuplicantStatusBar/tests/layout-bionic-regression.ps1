param(
    [string]$ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

function Read-ProjectFile([string]$RelativePath) {
    $path = Join-Path $ProjectDir $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing expected file: $RelativePath"
    }
    Get-Content -LiteralPath $path -Raw
}

function Require-Contains([string]$Source, [string]$Needle, [string]$Message) {
    if (-not $Source.Contains($Needle)) {
        throw $Message
    }
}

function Require-NotContains([string]$Source, [string]$Needle, [string]$Message) {
    if ($Source.Contains($Needle)) {
        throw $Message
    }
}

function Require-Regex([string]$Source, [string]$Pattern, [string]$Message) {
    if ($Source -notmatch $Pattern) {
        throw $Message
    }
}

function Require-Order([string]$Source, [string[]]$Needles, [string]$Message) {
    $cursor = -1
    foreach ($needle in $Needles) {
        $next = $Source.IndexOf($needle, $cursor + 1, [System.StringComparison]::Ordinal)
        if ($next -lt 0) {
            throw $Message
        }
        $cursor = $next
    }
}

$options = Read-ProjectFile 'Config\StatusBarOptions.cs'
Require-Regex $options '(?s)\[Option\("STRINGS\.DUPLICANTSTATUSBAR\.OPTIONS\.MAXDUPESPERROW\.NAME".*?\)\]\s*\[Limit\(0,\s*100\)\]\s*\[JsonProperty\]\s*public int MaxDupesPerRow' `
    'MaxDupesPerRow must be exposed as a PLib option with a sane limit.'
Require-Regex $options '(?s)\[Option\("STRINGS\.DUPLICANTSTATUSBAR\.OPTIONS\.MAXBARWIDTH\.NAME".*?\)\]\s*\[Limit\(10,\s*100\)\]\s*\[JsonProperty\]\s*public int MaxBarWidth' `
    'MaxBarWidth must be exposed as a PLib option with a sane limit.'
Require-Regex $options '(?s)\[Option\("STRINGS\.DUPLICANTSTATUSBAR\.OPTIONS\.MAXROWS\.NAME".*?\)\]\s*\[Limit\(0,\s*20\)\]\s*\[JsonProperty\]\s*public int MaxBarRows' `
    'MaxBarRows must be exposed as a PLib option with a sane limit.'

$screen = Read-ProjectFile 'UI\StatusBarScreen.cs'
Require-NotContains $screen 'if (dupeCount <= 0) return;' `
    'Zero-dupe active worlds must compact the bar instead of keeping the previous layout.'
Require-Contains $screen 'ApplyEmptyLayout();' `
    'StatusBarScreen must apply an explicit empty-world layout.'
Require-Contains $screen 'ApplyColumnLimit' `
    'StatusBarScreen must honor MaxDupesPerRow when computing columns.'
Require-Contains $screen 'ApplyRowLimit' `
    'StatusBarScreen must honor MaxBarRows when computing visible rows.'
Require-Regex $screen 'ApplyRowLimit\([^;]+,\s*!hConstrained\)' `
    'MaxBarRows must not cap layouts with an explicit vertical drag height.'
Require-Contains $screen 'GetMaxAutoWidth' `
    'StatusBarScreen must honor MaxBarWidth in automatic layout.'
Require-Contains $screen 'HandleActiveWorldChanged(CurrentActiveWorldId());' `
    'StatusBarScreen must treat asteroid switches as explicit layout reload events.'
Require-Contains $screen 'worldBoxSizes' `
    'StatusBarScreen must keep manual resize state scoped by active world during asteroid transitions.'
Require-Regex $screen 'barWidthPx\s*=\s*-1f;\s*barHeightPx\s*=\s*-1f;' `
    'Unknown asteroid layouts must start from auto sizing so previous-world dimensions do not leak.'
Require-Contains $screen 'lastDupeCount = -1;' `
    'Asteroid switches must force a widget count/layout refresh even when two worlds have the same visible count.'
Require-Contains $screen 'ResetScrollForWorldChange();' `
    'Asteroid switches must reset scroll state so a smaller world does not inherit a blank scrolled viewport.'
Require-Order $screen @(
    'HandleActiveWorldChanged(CurrentActiveWorldId());',
    'DupeStatusTracker.Update();',
    'RefreshWidgets();'
) 'Asteroid switch handling must run before tracker refresh and widget/layout refresh in the update tick.'
Require-Order $screen @(
    'StoreCurrentWorldBoxSize();',
    'lastActiveWorldId = currentWorldId;',
    'worldBoxSizes.TryGetValue(currentWorldId, out Vector2 savedBox)',
    'barWidthPx = -1f;'
) 'Asteroid switch handling must save the previous world before selecting the new world, then restore or auto-size.'
Require-Contains $screen 'scrollRect.StopMovement();' `
    'Asteroid switches must stop inherited scrolling momentum.'
Require-Contains $screen 'contentRT.anchoredPosition = Vector2.zero;' `
    'Asteroid switches must return the content viewport to the top.'
Require-Regex $screen '(?s)private void ApplyEmptyLayout\(\).*?scrollViewLayout\.preferredWidth = 0f;.*?scrollViewLayout\.preferredHeight = 0f;.*?MarkBarLayoutForRebuild\(\);' `
    'Empty active worlds must zero the portrait viewport and force a layout rebuild.'
Require-Regex $screen '(?s)public void OnPointerUp\(PointerEventData e\).*?screen\.StoreCurrentWorldBoxSize\(\);.*?PlayerPrefs\.SetFloat\(PW, screen\.barWidthPx\);' `
    'Manual resize completion must update the current world size cache before persisting the box size.'

$tracker = Read-ProjectFile 'Data\DupeStatusTracker.cs'
Require-Contains $tracker 'OxygenTankPercent' `
    'Dupe snapshots must carry bionic oxygen tank percentage.'
Require-Contains $tracker 'HasOxygenTank' `
    'Dupe snapshots must track whether bionic oxygen tank data was available.'
Require-Contains $tracker 'BionicOxygenTank' `
    'DupeStatusTracker must read the ONI bionic oxygen tank amount.'

$tooltip = Read-ProjectFile 'UI\DupeTooltip.cs'
Require-Contains $tooltip 'TOOLTIP_OXYGEN_TANK' `
    'Bionic tooltips must label oxygen tank separately from normal breath.'
Require-Contains $tooltip 'snap.OxygenTankPercent' `
    'Bionic tooltips must render the oxygen tank percentage.'

$strings = Read-ProjectFile 'Localization\DSBStrings.cs'
Require-Contains $strings 'TOOLTIP_OXYGEN_TANK' `
    'Localization strings must include an oxygen tank tooltip label.'

Write-Output 'layout and bionic regression checks passed'
