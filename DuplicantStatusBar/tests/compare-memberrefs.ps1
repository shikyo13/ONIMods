# compare-memberrefs.ps1 - Diff external member references between two builds of the same assembly.
# Usage: .\compare-memberrefs.ps1 -OldDll <path> -NewDll <path>
# Purpose: after a game update, prove a mod DLL built against new game assemblies
# still references the exact same external API surface as the build against old assemblies.
# Identical MemberRef/TypeRef sets = binary-level backwards compatibility.
param(
    [Parameter(Mandatory = $true)][string]$OldDll,
    [Parameter(Mandatory = $true)][string]$NewDll
)

$ErrorActionPreference = 'Stop'

function Get-ExternalRefs([string]$path) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $stream = New-Object System.IO.MemoryStream(, $bytes)
    $pe = New-Object System.Reflection.PortableExecutable.PEReader($stream)
    try {
        $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        $refs = New-Object System.Collections.Generic.List[string]

        function Resolve-TypeName($reader, $handle) {
            switch ($handle.Kind) {
                'TypeReference' {
                    $tr = $reader.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle]$handle)
                    $ns = $reader.GetString($tr.Namespace)
                    $name = $reader.GetString($tr.Name)
                    # Nested types chain through ResolutionScope
                    if ($tr.ResolutionScope.Kind -eq 'TypeReference') {
                        $parent = Resolve-TypeName $reader $tr.ResolutionScope
                        return "$parent/$name"
                    }
                    if ($ns) { return "$ns.$name" } else { return $name }
                }
                'TypeDefinition' { return '<self>' }
                'TypeSpecification' {
                    $ts = $reader.GetTypeSpecification([System.Reflection.Metadata.TypeSpecificationHandle]$handle)
                    return '<typespec:' + [System.BitConverter]::ToString($reader.GetBlobBytes($ts.Signature)) + '>'
                }
                default { return "<$($handle.Kind)>" }
            }
        }

        foreach ($h in $md.MemberReferences) {
            $mr = $md.GetMemberReference($h)
            $name = $md.GetString($mr.Name)
            $parent = Resolve-TypeName $md $mr.Parent
            $sig = [System.BitConverter]::ToString($md.GetBlobBytes($mr.Signature))
            $refs.Add("$parent::$name($sig)")
        }
        return $refs | Sort-Object -Unique
    }
    finally {
        $pe.Dispose()
        $stream.Dispose()
    }
}

Write-Host "Old: $OldDll"
Write-Host "New: $NewDll"
$old = Get-ExternalRefs $OldDll
$new = Get-ExternalRefs $NewDll
Write-Host "Old refs: $($old.Count)  New refs: $($new.Count)"

$diff = Compare-Object -ReferenceObject $old -DifferenceObject $new
if ($diff) {
    Write-Host "MEMBERREF DIFFERENCES FOUND:" -ForegroundColor Red
    foreach ($d in $diff) {
        $side = if ($d.SideIndicator -eq '<=') { 'ONLY-IN-OLD' } else { 'ONLY-IN-NEW' }
        Write-Host "  [$side] $($d.InputObject)"
    }
    exit 1
}
else {
    Write-Host "memberref comparison passed: external reference sets are identical" -ForegroundColor Green
    exit 0
}
