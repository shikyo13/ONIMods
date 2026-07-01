# compare-memberrefs.ps1 - Diff external member references between two builds of the same assembly.
# Usage: .\compare-memberrefs.ps1 -OldDll <path> -NewDll <path>
# Purpose: after a game update (or a refactor), prove a mod DLL still references the exact
# same external API surface. Identical name-resolved MemberRef sets = binary-level compatibility.
# Signatures are DECODED to type names (not raw blob bytes): raw blobs embed TypeRef row
# indexes, which renumber whenever compilation units change, producing false diffs.
param(
    [Parameter(Mandatory = $true)][string]$OldDll,
    [Parameter(Mandatory = $true)][string]$NewDll
)

$ErrorActionPreference = 'Stop'

function Resolve-ReferenceAssembly([string]$AssemblyName) {
    $loaded = [AppDomain]::CurrentDomain.GetAssemblies() |
        Where-Object { $_.GetName().Name -eq $AssemblyName -and $_.Location } |
        Select-Object -First 1
    if ($loaded) {
        return $loaded.Location
    }

    try {
        $loaded = [System.Reflection.Assembly]::Load($AssemblyName)
        if ($loaded.Location) {
            return $loaded.Location
        }
    }
    catch {
        # Fall through to runtime and NuGet path probing below.
    }

    $tpa = [AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES')
    if ($tpa) {
        $match = ($tpa -split [IO.Path]::PathSeparator) |
            Where-Object { [IO.Path]::GetFileNameWithoutExtension($_) -eq $AssemblyName } |
            Select-Object -First 1
        if ($match) {
            return $match
        }
    }

    $packageName = $AssemblyName.ToLowerInvariant()
    $packageRoot = Join-Path $env:USERPROFILE ".nuget\packages\$packageName"
    if (Test-Path -LiteralPath $packageRoot) {
        $match = Get-ChildItem -LiteralPath $packageRoot -Recurse -Filter "$AssemblyName.dll" |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($match) {
            return $match.FullName
        }
    }

    throw "Unable to resolve compiler reference assembly: $AssemblyName"
}

function Get-CompilerReferences() {
    $refPackRoot = Join-Path $env:ProgramFiles 'dotnet\packs\Microsoft.NETCore.App.Ref'
    if (Test-Path -LiteralPath $refPackRoot) {
        $refDir = Get-ChildItem -LiteralPath $refPackRoot -Directory |
            Sort-Object { [version]$_.Name } -Descending |
            ForEach-Object {
                Get-ChildItem -LiteralPath (Join-Path $_.FullName 'ref') -Directory |
                    Sort-Object Name -Descending |
                    Select-Object -First 1
            } |
            Select-Object -First 1

        if ($refDir) {
            return Get-ChildItem -LiteralPath $refDir.FullName -Filter '*.dll' |
                ForEach-Object FullName |
                Sort-Object -Unique
        }
    }

    $names = @(
        'System.Private.CoreLib',
        'System.Runtime',
        'netstandard',
        'System.Collections',
        'System.Collections.Immutable',
        'System.IO',
        'System.Linq',
        'System.Memory',
        'System.Reflection',
        'System.Reflection.Metadata',
        'System.Runtime.Extensions'
    )

    $refs = foreach ($name in $names) {
        try {
            Resolve-ReferenceAssembly $name
        }
        catch {
            if ($name -in @('System.Reflection.Metadata', 'System.Collections.Immutable', 'System.Memory')) {
                throw
            }
        }
    }

    $refs | Where-Object { $_ } | Sort-Object -Unique
}

if (-not ('MemberRefDumper' -as [type])) {
    Add-Type -ReferencedAssemblies (Get-CompilerReferences) -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

public class NameTypeProvider : ISignatureTypeProvider<string, object>
{
    public string GetPrimitiveType(PrimitiveTypeCode code) { return code.ToString(); }
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var td = reader.GetTypeDefinition(handle);
        var ns = reader.GetString(td.Namespace);
        var name = reader.GetString(td.Name);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var tr = reader.GetTypeReference(handle);
        var name = reader.GetString(tr.Name);
        if (tr.ResolutionScope.Kind == HandleKind.TypeReference)
            return GetTypeFromReference(reader, (TypeReferenceHandle)tr.ResolutionScope, 0) + "/" + name;
        var ns = reader.GetString(tr.Namespace);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }
    public string GetTypeFromSpecification(MetadataReader reader, object ctx, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        return reader.GetTypeSpecification(handle).DecodeSignature(this, ctx);
    }
    public string GetSZArrayType(string elementType) { return elementType + "[]"; }
    public string GetArrayType(string elementType, ArrayShape shape) { return elementType + "[" + new string(',', shape.Rank - 1) + "]"; }
    public string GetPointerType(string elementType) { return elementType + "*"; }
    public string GetByReferenceType(string elementType) { return elementType + "&"; }
    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
    {
        return genericType + "<" + string.Join(",", typeArguments) + ">";
    }
    public string GetGenericMethodParameter(object ctx, int index) { return "!!" + index; }
    public string GetGenericTypeParameter(object ctx, int index) { return "!" + index; }
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) { return unmodifiedType + " mod(" + modifier + ")"; }
    public string GetPinnedType(string elementType) { return elementType + " pinned"; }
    public string GetFunctionPointerType(MethodSignature<string> signature) { return "fnptr"; }
}

public static class MemberRefDumper
{
    public static string[] GetExternalRefs(string path)
    {
        var provider = new NameTypeProvider();
        var refs = new List<string>();
        using (var stream = File.OpenRead(path))
        using (var pe = new PEReader(stream))
        {
            var md = pe.GetMetadataReader();
            foreach (var h in md.MemberReferences)
            {
                var mr = md.GetMemberReference(h);
                var name = md.GetString(mr.Name);
                string parent;
                switch (mr.Parent.Kind)
                {
                    case HandleKind.TypeReference:
                        parent = provider.GetTypeFromReference(md, (TypeReferenceHandle)mr.Parent, 0); break;
                    case HandleKind.TypeDefinition:
                        parent = provider.GetTypeFromDefinition(md, (TypeDefinitionHandle)mr.Parent, 0); break;
                    case HandleKind.TypeSpecification:
                        parent = provider.GetTypeFromSpecification(md, null, (TypeSpecificationHandle)mr.Parent, 0); break;
                    case HandleKind.MethodDefinition:
                        parent = "<vararg-self>"; break;
                    default:
                        parent = "<" + mr.Parent.Kind + ">"; break;
                }
                string sig;
                if (mr.GetKind() == MemberReferenceKind.Method)
                {
                    var ms = mr.DecodeMethodSignature(provider, null);
                    sig = ms.ReturnType + " (" + string.Join(", ", ms.ParameterTypes) + ")"
                        + (ms.Header.IsInstance ? " instance" : "")
                        + (ms.GenericParameterCount > 0 ? " generic:" + ms.GenericParameterCount : "");
                }
                else
                {
                    sig = "field:" + mr.DecodeFieldSignature(provider, null);
                }
                refs.Add(parent + "::" + name + " :: " + sig);
            }
        }
        return refs.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
    }
}
'@
}

Write-Host "Old: $OldDll"
Write-Host "New: $NewDll"
$old = [MemberRefDumper]::GetExternalRefs($OldDll)
$new = [MemberRefDumper]::GetExternalRefs($NewDll)
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
    Write-Host "memberref comparison passed: name-resolved external reference sets are identical" -ForegroundColor Green
    exit 0
}
