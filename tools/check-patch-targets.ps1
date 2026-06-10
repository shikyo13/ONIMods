# check-patch-targets.ps1 - Verify Harmony patch targets exist in a game assembly.
# Usage: .\check-patch-targets.ps1 -AssemblyPath <dll> -Pairs "Type::Method","Outer/Nested::Method","Type::#fieldName"
# Method pairs use Type::Name. Field pairs prefix the name with '#'.
# Nested types use 'Outer/Nested'. Matching is by simple type name (no namespace).
# Metadata-level (System.Reflection.Metadata) - no assembly loading, no dependency resolution.
param(
    [Parameter(Mandatory = $true)][string]$AssemblyPath,
    [Parameter(Mandatory = $true)][string[]]$Pairs
)

$ErrorActionPreference = 'Stop'

$bytes = [System.IO.File]::ReadAllBytes($AssemblyPath)
$stream = New-Object System.IO.MemoryStream(, $bytes)
$pe = New-Object System.Reflection.PortableExecutable.PEReader($stream)
$md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)

# Build map: typeName -> @{ Methods = set; Fields = set }
# Nested types keyed as 'Outer/Nested' AND bare nested name for convenience.
$typeMap = @{}
foreach ($th in $md.TypeDefinitions) {
    $td = $md.GetTypeDefinition($th)
    $name = $md.GetString($td.Name)
    if ($td.IsNested) {
        $outer = $md.GetTypeDefinition($td.GetDeclaringType())
        $name = "$($md.GetString($outer.Name))/$name"
    }
    $methods = New-Object System.Collections.Generic.HashSet[string]
    foreach ($mh in $td.GetMethods()) {
        [void]$methods.Add($md.GetString($md.GetMethodDefinition($mh).Name))
    }
    $fields = New-Object System.Collections.Generic.HashSet[string]
    foreach ($fh in $td.GetFields()) {
        [void]$fields.Add($md.GetString($md.GetFieldDefinition($fh).Name))
    }
    $typeMap[$name] = @{ Methods = $methods; Fields = $fields }
}
$pe.Dispose(); $stream.Dispose()

$missing = 0
foreach ($pair in $Pairs) {
    $parts = $pair -split '::', 2
    $typeName = $parts[0]; $member = $parts[1]
    $isField = $member.StartsWith('#')
    if ($isField) { $member = $member.Substring(1) }

    if (-not $typeMap.ContainsKey($typeName)) {
        Write-Host "[MISSING TYPE]   $pair" -ForegroundColor Red
        $missing++
        continue
    }
    $entry = $typeMap[$typeName]
    $found = if ($isField) { $entry.Fields.Contains($member) } else { $entry.Methods.Contains($member) }
    if ($found) {
        Write-Host "[OK]             $pair"
    }
    else {
        Write-Host "[MISSING MEMBER] $pair" -ForegroundColor Red
        $missing++
    }
}

Write-Host ""
if ($missing -gt 0) {
    Write-Host "$missing of $($Pairs.Count) patch targets MISSING" -ForegroundColor Red
    exit 1
}
Write-Host "all $($Pairs.Count) patch targets present" -ForegroundColor Green
exit 0
