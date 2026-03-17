$SpikeFile = $args[0]
$csv = Import-Csv $SpikeFile
$props = $csv[0].psobject.properties.name | Where-Object { $_ -match 'alloc_kb' }

Write-Host "Allocation profile for $SpikeFile"
$results = @()
foreach ($p in $props) {
    # sum values
    $sum = 0
    foreach ($row in $csv) {
        $val = $row.$p
        if (-not [string]::IsNullOrEmpty($val)) {
            $sum += [double]$val
        }
    }
    if ($sum -gt 0) {
        $results += [PSCustomObject]@{
            System = $p
            TotalKB = $sum
        }
    }
}
$results | Sort-Object TotalKB -Descending | Format-Table -AutoSize
