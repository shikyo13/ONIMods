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

# Main
Write-Host "OniProfiler Spike Analyzer" -ForegroundColor Cyan
Write-Host "Threshold: Unaccounted > ${MinUnaccounted}ms for mystery classification"

if ($Path) {
    Process-SpikeFile $Path
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
}
