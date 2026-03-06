<#
.SYNOPSIS
    Analyzes OniProfiler spike CSV files, categorizes spikes, and highlights mystery spikes.

.DESCRIPTION
    Reads *_spikes.csv files from the OniProfiler recording directory.
    Categorizes each spike into: GC Pause, PathAsync Storm, Mystery, or Minor.
    Shows BulkUpdateTop5 data for mystery spikes to identify coroutine vs Update() culprits.

.PARAMETER Path
    Path to a specific spike CSV file. If omitted, processes all spike CSVs in the default directory.

.PARAMETER MinUnaccounted
    Minimum unaccounted ms to classify as a mystery spike (default: 100).

.EXAMPLE
    .\analyze-spikes.ps1
    .\analyze-spikes.ps1 -Path "C:\path\to\profiler_20260305_151650_spikes.csv"
#>
param(
    [string]$Path,
    [double]$MinUnaccounted = 100
)

$defaultDir = Join-Path $env:USERPROFILE "Documents\Klei\OxygenNotIncluded\mods\local\OniProfiler"

function Categorize-Spike {
    param($row, $headers)

    $gc2     = [double]$row.GC_Gen2
    $unacct  = [double]$row.Unaccounted_ms
    $frameMs = [double]$row.FrameMs

    # Find PathProbe_Async column
    $pathAsync = 0.0
    if ($headers -contains "PathProbe_Async_ms") {
        $pathAsync = [double]$row.PathProbe_Async_ms
    }

    if ($frameMs -lt $MinUnaccounted) {
        return "Minor"
    }
    if ($gc2 -gt 0) {
        return "GC_Pause"
    }
    if ($pathAsync -gt 1) {
        return "PathAsync_Storm"
    }
    if ($unacct -gt $MinUnaccounted) {
        return "Mystery"
    }
    return "Minor"
}

function Process-SpikeFile {
    param([string]$FilePath)

    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found: $FilePath"
        return
    }

    $fileName = Split-Path $FilePath -Leaf
    $spikes = Import-Csv $FilePath
    $headers = $spikes[0].PSObject.Properties.Name

    if ($spikes.Count -eq 0) {
        Write-Host "  No spikes in $fileName"
        return
    }

    # Categorize all spikes
    $categories = @{ GC_Pause = @(); PathAsync_Storm = @(); Mystery = @(); Minor = @() }
    foreach ($spike in $spikes) {
        $cat = Categorize-Spike $spike $headers
        $categories[$cat] += $spike
    }

    # Summary
    Write-Host ""
    Write-Host "=== $fileName ($($spikes.Count) spikes) ===" -ForegroundColor Cyan
    Write-Host ""

    $total = $spikes.Count
    foreach ($cat in @("GC_Pause", "PathAsync_Storm", "Mystery", "Minor")) {
        $count = $categories[$cat].Count
        $pct = if ($total -gt 0) { [math]::Round($count / $total * 100) } else { 0 }
        $color = switch ($cat) {
            "GC_Pause"       { "Yellow" }
            "PathAsync_Storm" { "Magenta" }
            "Mystery"        { "Red" }
            "Minor"          { "Gray" }
        }
        Write-Host ("  {0,-20} {1,3} ({2,3}%)" -f $cat, $count, $pct) -ForegroundColor $color
    }

    # Detail mystery spikes
    if ($categories["Mystery"].Count -gt 0) {
        Write-Host ""
        Write-Host "  --- Mystery Spike Details ---" -ForegroundColor Red
        Write-Host ("  {0,-12} {1,8} {2,10} {3,-50}" -f "WallTime", "FrameMs", "Unacct_ms", "BulkUpdateTop5")
        Write-Host ("  {0,-12} {1,8} {2,10} {3,-50}" -f "--------", "-------", "---------", "--------------")

        foreach ($spike in $categories["Mystery"]) {
            $bulk = if ($spike.PSObject.Properties["BulkUpdateTop5"]) { $spike.BulkUpdateTop5 } else { "(no data)" }
            if ([string]::IsNullOrWhiteSpace($bulk)) { $bulk = "(empty)" }
            Write-Host ("  {0,-12} {1,8} {2,10} {3,-50}" -f $spike.WallTime, $spike.FrameMs, $spike.Unaccounted_ms, $bulk) -ForegroundColor Red
        }

        # Check if BulkUpdateTop5 has useful data
        $hasData = $categories["Mystery"] | Where-Object {
            $_.PSObject.Properties["BulkUpdateTop5"] -and
            -not [string]::IsNullOrWhiteSpace($_.BulkUpdateTop5) -and
            $_.BulkUpdateTop5 -ne "(empty)"
        }

        Write-Host ""
        if ($hasData.Count -gt 0) {
            # Parse BulkUpdateTop5 and aggregate across mystery spikes
            $typeTotals = @{}
            foreach ($spike in $hasData) {
                $entries = $spike.BulkUpdateTop5 -split '\|'
                foreach ($entry in $entries) {
                    $parts = $entry -split ':'
                    if ($parts.Count -eq 2) {
                        $typeName = $parts[0]
                        $ms = [double]$parts[1]
                        if ($typeTotals.ContainsKey($typeName)) {
                            $typeTotals[$typeName] += $ms
                        } else {
                            $typeTotals[$typeName] = $ms
                        }
                    }
                }
            }

            $sorted = $typeTotals.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 10
            $totalBulkMs = ($sorted | Measure-Object -Property Value -Sum).Sum
            $avgUnacct = ($categories["Mystery"] | ForEach-Object { [double]$_.Unaccounted_ms } | Measure-Object -Average).Average

            Write-Host "  Top Update() types across mystery spikes:" -ForegroundColor Yellow
            foreach ($kv in $sorted) {
                Write-Host ("    {0,-30} {1,8:F1} ms total" -f $kv.Key, $kv.Value)
            }
            Write-Host ""
            Write-Host ("  Total BulkUpdate: {0:F1} ms | Avg Unaccounted: {1:F1} ms" -f $totalBulkMs, $avgUnacct) -ForegroundColor Yellow

            if ($totalBulkMs -lt 5 * $hasData.Count) {
                Write-Host "  >> VERDICT: Update() methods are NOT the culprit (<5ms avg)." -ForegroundColor Red
                Write-Host "  >> Time is likely in COROUTINES (ScriptRunDelayedDynamicFrameRate)." -ForegroundColor Red
            } else {
                $topType = $sorted | Select-Object -First 1
                Write-Host ("  >> VERDICT: Update() method '{0}' is the likely culprit." -f $topType.Key) -ForegroundColor Green
            }
        } else {
            Write-Host "  >> No BulkUpdateTop5 data on mystery spikes yet." -ForegroundColor Yellow
            Write-Host "  >> Need a longer recording to capture mystery spikes with bulk data." -ForegroundColor Yellow
        }

        # Show phase data if available
        $phaseColumns = $headers | Where-Object { $_ -like "Phase_*_ms" }
        if ($phaseColumns.Count -gt 0) {
            Write-Host ""
            Write-Host "  --- Phase Breakdown (mystery spikes) ---" -ForegroundColor Yellow
            foreach ($spike in $categories["Mystery"]) {
                Write-Host "  Spike @ $($spike.WallTime):" -ForegroundColor Gray
                foreach ($col in $phaseColumns) {
                    $val = [double]$spike.$col
                    if ($val -gt 1.0) {
                        $label = $col -replace '^Phase_' -replace '_ms$'
                        $color = if ($val -gt 100) { "Red" } elseif ($val -gt 10) { "Yellow" } else { "White" }
                        Write-Host ("    {0,-25} {1,8:F1} ms" -f $label, $val) -ForegroundColor $color
                    }
                }
            }
        }
    }
}

function Cross-Validate {
    param([string]$SpikeFile)

    $mainFile = $SpikeFile -replace '_spikes\.csv$', '.csv'
    if (-not (Test-Path $mainFile)) {
        Write-Host "  (No main CSV found for cross-validation)" -ForegroundColor Gray
        return
    }

    $spikes = Import-Csv $SpikeFile
    $mainRows = Import-Csv $mainFile
    if ($spikes.Count -eq 0 -or $mainRows.Count -eq 0) { return }

    $mainHeaders = $mainRows[0].PSObject.Properties.Name
    $hasGameUpdateMax = $mainHeaders -contains "GameUpdate_max"
    if (-not $hasGameUpdateMax) { return }

    # Build lookup: timestamp (HH:mm:ss) → main row
    $mainByTime = @{}
    foreach ($row in $mainRows) {
        $ts = $row.Timestamp
        if ($ts) { $mainByTime[$ts] = $row }
    }

    Write-Host ""
    Write-Host "  --- Cross-Validation (spike vs main CSV, best of +/-1s) ---" -ForegroundColor Yellow
    Write-Host ("  {0,-12} {1,10} {2,12} {3,10} {4,12} {5,8}" -f "SpikeTime", "Spike_GU", "Main_GU_max", "MatchSec", "Mismatch%", "Offset?")

    $offsetCount = 0
    foreach ($spike in $spikes) {
        $spikeTime = $spike.WallTime
        $sec = if ($spikeTime.Length -ge 8) { $spikeTime.Substring(0, 8) } else { $spikeTime }

        # Try same second and +/-1 second, pick best match
        $candidates = @($sec)
        try {
            $ts = [datetime]::ParseExact($sec, "HH:mm:ss", $null)
            $candidates += $ts.AddSeconds(-1).ToString("HH:mm:ss")
            $candidates += $ts.AddSeconds(1).ToString("HH:mm:ss")
        } catch {}

        $spikeGU = [double]$spike.GameUpdate_ms
        $bestMatch = $null
        $bestMismatch = [double]::MaxValue
        $bestSec = ""

        foreach ($candidate in $candidates) {
            $mainRow = $mainByTime[$candidate]
            if (-not $mainRow) { continue }
            $mainGU = [double]$mainRow.GameUpdate_max
            if ($mainGU -lt 1) { continue }
            $m = [math]::Abs($spikeGU - $mainGU) / [math]::Max($spikeGU, $mainGU) * 100
            if ($m -lt $bestMismatch) {
                $bestMismatch = $m
                $bestMatch = $mainGU
                $bestSec = $candidate
            }
        }

        if ($bestMatch -eq $null) { continue }

        $isOffset = $bestMismatch -gt 50
        if ($isOffset) { $offsetCount++ }

        $color = if ($isOffset) { "Red" } else { "Green" }
        $tag = if ($isOffset) { "YES" } else { "no" }
        Write-Host ("  {0,-12} {1,10:F1} {2,12:F1} {3,10} {4,11:F0}% {5,8}" -f $spikeTime, $spikeGU, $bestMatch, $bestSec, $bestMismatch, $tag) -ForegroundColor $color
    }

    if ($offsetCount -gt 0) {
        Write-Host "  >> $offsetCount spike(s) show >50% mismatch — likely offset-affected" -ForegroundColor Red
    } else {
        Write-Host "  >> All spikes match main CSV — offset fix is working" -ForegroundColor Green
    }
}

function Phase-Coverage {
    param([string]$SpikeFile)

    $spikes = Import-Csv $SpikeFile
    if ($spikes.Count -eq 0) { return }

    $headers = $spikes[0].PSObject.Properties.Name
    # Top-level phases only — sub-phases (UpdateScriptRun, UpdateDirector, UpdateCoroutines)
    # are nested inside Update, so including them would double-count
    $topPhases = @(
        "Phase_Initialization_ms", "Phase_EarlyUpdate_ms", "Phase_FixedUpdate_ms",
        "Phase_PreUpdate_ms", "Phase_Update_ms", "Phase_PreLateUpdate_ms", "Phase_PostLateUpdate_ms"
    )
    $phaseColumns = $headers | Where-Object { $_ -in $topPhases }
    if ($phaseColumns.Count -eq 0) { return }

    Write-Host ""
    Write-Host "  --- Phase Coverage (top-level only) ---" -ForegroundColor Yellow
    Write-Host ("  {0,-12} {1,8} {2,10} {3,10}" -f "WallTime", "FrameMs", "PhaseSum", "Coverage%")

    $lowCount = 0
    foreach ($spike in $spikes) {
        $frameMs = [double]$spike.FrameMs
        if ($frameMs -lt 1) { continue }

        $phaseSum = 0.0
        foreach ($col in $phaseColumns) {
            $phaseSum += [double]$spike.$col
        }
        $coverage = $phaseSum / $frameMs * 100
        $isLow = $coverage -lt 50
        if ($isLow) { $lowCount++ }

        $color = if ($isLow) { "Red" } else { "Green" }
        Write-Host ("  {0,-12} {1,8:F1} {2,10:F1} {3,9:F0}%" -f $spike.WallTime, $frameMs, $phaseSum, $coverage) -ForegroundColor $color
    }

    if ($lowCount -gt 0) {
        Write-Host "  >> $lowCount spike(s) with <50% phase coverage (offset-affected)" -ForegroundColor Red
    }
}

function GC-Mode-Comparison {
    param([string]$Dir)

    $txtFiles = Get-ChildItem $Dir -Filter "profiler_*.txt" | Sort-Object LastWriteTime
    if ($txtFiles.Count -eq 0) { return }

    $enabled = @()
    $manual = @()

    foreach ($txt in $txtFiles) {
        $content = Get-Content $txt.FullName -Raw
        $mode = if ($content -match 'GC_Mode:\s*(\S+)') { $Matches[1] } else { "Unknown" }
        $timestamp = if ($txt.Name -match 'profiler_(\d{8}_\d{6})') { $Matches[1] } else { $txt.Name }

        # Find matching spike file
        $spikeFile = Join-Path $Dir ($txt.Name -replace '\.txt$', '_spikes.csv')
        $spikeCount = 0
        if (Test-Path $spikeFile) {
            $spikeCount = (Import-Csv $spikeFile).Count
        }

        $entry = [PSCustomObject]@{ Timestamp = $timestamp; Mode = $mode; Spikes = $spikeCount; File = $txt.Name }
        if ($mode -eq "Manual") { $manual += $entry } else { $enabled += $entry }
    }

    Write-Host ""
    Write-Host "=== GC Mode Comparison ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "  GC Enabled recordings: $($enabled.Count)" -ForegroundColor Yellow
    foreach ($e in $enabled) {
        Write-Host ("    {0}  {1,3} spikes" -f $e.Timestamp, $e.Spikes)
    }
    $enabledTotal = ($enabled | Measure-Object -Property Spikes -Sum).Sum
    Write-Host "    Total: $enabledTotal spikes" -ForegroundColor Yellow

    Write-Host ""
    Write-Host "  GC Manual (GCBudget) recordings: $($manual.Count)" -ForegroundColor Cyan
    foreach ($e in $manual) {
        Write-Host ("    {0}  {1,3} spikes" -f $e.Timestamp, $e.Spikes)
    }
    $manualTotal = ($manual | Measure-Object -Property Spikes -Sum).Sum
    Write-Host "    Total: $manualTotal spikes" -ForegroundColor Cyan
}

# Main
Write-Host "OniProfiler Spike Analyzer" -ForegroundColor Cyan
Write-Host "Threshold: Unaccounted > ${MinUnaccounted}ms for mystery classification"

if ($Path) {
    Process-SpikeFile $Path
    Cross-Validate $Path
    Phase-Coverage $Path
} else {
    if (-not (Test-Path $defaultDir)) {
        Write-Error "Default directory not found: $defaultDir"
        exit 1
    }

    $files = Get-ChildItem $defaultDir -Filter "*_spikes.csv" | Sort-Object LastWriteTime
    if ($files.Count -eq 0) {
        Write-Host "No spike CSV files found in $defaultDir"
        exit 0
    }

    Write-Host "Found $($files.Count) spike files in $defaultDir"

    # Cross-file aggregation
    $allSpikes = @{ GC_Pause = 0; PathAsync_Storm = 0; Mystery = 0; Minor = 0 }
    $grandTotal = 0

    foreach ($file in $files) {
        Process-SpikeFile $file.FullName
        Cross-Validate $file.FullName
        Phase-Coverage $file.FullName

        # Count for grand total
        $spikes = Import-Csv $file.FullName
        $headers = if ($spikes.Count -gt 0) { $spikes[0].PSObject.Properties.Name } else { @() }
        foreach ($spike in $spikes) {
            $cat = Categorize-Spike $spike $headers
            $allSpikes[$cat]++
            $grandTotal++
        }
    }

    # Grand summary
    Write-Host ""
    Write-Host "=== GRAND TOTAL ($grandTotal spikes across $($files.Count) recordings) ===" -ForegroundColor Green
    foreach ($cat in @("GC_Pause", "PathAsync_Storm", "Mystery", "Minor")) {
        $count = $allSpikes[$cat]
        $pct = if ($grandTotal -gt 0) { [math]::Round($count / $grandTotal * 100) } else { 0 }
        Write-Host ("  {0,-20} {1,3} ({2,3}%)" -f $cat, $count, $pct)
    }

    GC-Mode-Comparison $defaultDir
}
