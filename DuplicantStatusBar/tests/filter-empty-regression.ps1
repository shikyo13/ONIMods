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

function Require-Regex([string]$Source, [string]$Pattern, [string]$Message) {
    if ($Source -notmatch $Pattern) {
        throw $Message
    }
}

$options = Read-ProjectFile 'Config\StatusBarOptions.cs'
Require-Regex $options '(?s)private static void ResetBarState\(\).*?PlayerPrefs\.DeleteKey\("DSB_HiddenDupes"\).*?PlayerPrefs\.DeleteKey\("DSB_AlertsOnly"\).*?PlayerPrefs\.DeleteKey\("DSB_StressedOnly"\).*?PlayerPrefs\.DeleteKey\("DSB_HiddenRoles"\).*?SortFilterPopup\.ResetFilters\(\);' `
    'The PLib reset button must clear all saved and runtime filter state.'
Require-Regex $options '(?s)SortFilterPopup\.ResetFilters\(\);.*?var screen = UI\.StatusBarScreen\.Instance;' `
    'The PLib reset button must clear runtime filters before resetting the live screen layout.'

$popup = Read-ProjectFile 'UI\SortFilterPopup.cs'
Require-Regex $popup '(?s)public static void ResetFilters\(\).*?HiddenDupes\.Clear\(\);.*?HiddenRoles\.Clear\(\);.*?AlertsOnly = false;.*?StressedOnly = false;' `
    'SortFilterPopup.ResetFilters must clear every runtime filter.'
Require-Regex $popup '(?s)private static void Reset\(\).*?pendingAlertsOnly = false;.*?pendingStressedOnly = false;.*?ShowAllDupes\(\);.*?Apply\(\);' `
    'The popup Reset button must commit the cleared filters instead of only changing pending checkbox state.'
Require-Regex $popup '(?s)private static void RebuildFilterList\(\).*?var dupes = GetAllDupeNames\(\);' `
    'The popup dupe checklist must be rebuilt from all active-world names, not only visible snapshots.'
Require-Regex $popup '(?s)private static List<string> GetAllDupeNames\(\).*?Components\.LiveMinionIdentities\.GetWorldItems\(worldId\).*?GetProperName\(\)' `
    'The popup must source hidden-dupe recovery names from live active-world identities.'

$tracker = Read-ProjectFile 'Data\DupeStatusTracker.cs'
Require-Contains $tracker 'public static int ActiveWorldDupeCount { get; private set; }' `
    'DupeStatusTracker must expose the unfiltered active-world dupe count.'
Require-Regex $tracker '(?s)snapshots\.Clear\(\);\s*ActiveWorldDupeCount = 0;' `
    'ActiveWorldDupeCount must reset at the start of each tracker update.'
Require-Regex $tracker '(?s)foreach \(var identity in dupes\).*?if \(identity == null \|\| identity\.gameObject == null\) continue;.*?ActiveWorldDupeCount\+\+;.*?if \(filtered\) continue;' `
    'ActiveWorldDupeCount must count valid active-world dupes before portrait filters are applied.'

$screen = Read-ProjectFile 'UI\StatusBarScreen.cs'
Require-Regex $screen 'ApplyEmptyLayout\(DupeStatusTracker\.ActiveWorldDupeCount > 0\);' `
    'Zero visible portraits must distinguish filtered-empty from truly empty active worlds.'
Require-Regex $screen '(?s)private void ApplyEmptyLayout\(bool keepFilterAvailable\).*?scrollViewLayout\.preferredWidth = 0f;.*?scrollViewLayout\.preferredHeight = 0f;.*?if \(keepFilterAvailable && !isCollapsed\).*?ShowFullFilterButton\(\);.*?else.*?HideFilterButton\(\);.*?MarkBarLayoutForRebuild\(\);' `
    'Filtered-empty layout must keep the filter button reachable while keeping the portrait viewport compact.'
Require-Regex $screen '(?s)private void ShowFullFilterButton\(\).*?filterBtnGO\.SetActive\(true\).*?filterTMP\.text = \(string\)DSB\.UI\.POPUP_SORTFILTER.*?headerHLG\.padding = new RectOffset\(\(int\)\(filterFullWidth \+ 6f\)' `
    'Filtered-empty layout must reserve header room for the full Sort/Filter control.'
Require-Regex $screen '(?s)private void HideFilterButton\(\).*?filterBtnGO\.SetActive\(false\).*?ResetHeaderFilterPadding\(\);' `
    'Truly empty worlds must still hide the filter button and reset header padding.'
Require-Regex $screen '(?s)private void ToggleCollapse\(\).*?bool hasActiveDupes = DupeStatusTracker\.ActiveWorldDupeCount > 0;.*?UpdateGridLayout\(lastDupeCount\);' `
    'Expanding a filtered-empty bar must restore filter control visibility without requiring a tracker tick.'
Require-Regex $screen '(?s)internal void ResetToDefaults\(\).*?DupeStatusTracker\.ActiveWorldDupeCount > 0' `
    'ResetToDefaults must use the unfiltered active-world count when deciding whether controls are recoverable.'

Write-Output 'filter-empty regression checks passed'
