# PLib Integration Guide - ONI U58-720697+

## Problem
PLib 4.18+ crashes with TypeLoadException on ONI U58-720697 when using Enum options:
```
TypeLoadException: Could not resolve type with token 010001d0 from typeref 
(expected class 'TMPro.FaceInfo' in assembly 'Unity.TextMeshPro')
```

## Root Cause
- PLib 4.19+ uses `UnityEngine.TextCore.FaceInfo` for font calculations
- Must be ILMerged into mod DLL, not shipped as separate PLib.dll
- Shipping separate PLib.dll causes version conflicts across mods

## Correct Solution: ILMerge with LibraryPath

### 1. Update .csproj

```xml
<PropertyGroup>
  <TargetFramework>net48</TargetFramework>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  <!-- other properties -->
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="PLib" Version="4.24.0">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="ILRepack.Lib.MSBuild.Task" Version="2.0.40">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>

<ItemGroup>
  <Reference Include="Newtonsoft.Json">
    <HintPath>$(GameManaged)\Newtonsoft.Json.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="UnityEngine.TextCoreFontEngineModule">
    <HintPath>$(GameManaged)\UnityEngine.TextCoreFontEngineModule.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <!-- other ONI references -->
</ItemGroup>

<Import Project="ILRepack.targets" />
```

### 2. Create ILRepack.targets

File: `ILRepack.targets` (same directory as .csproj)

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Target Name="ILRepacking" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
    <PropertyGroup>
      <WorkingDirectory>$(MSBuildThisFileDirectory)bin\$(Configuration)</WorkingDirectory>
    </PropertyGroup>

    <ItemGroup>
      <InputAssemblies Include="$(WorkingDirectory)\YourModName.dll" />
      <InputAssemblies Include="$(WorkingDirectory)\PLib*.dll" />
    </ItemGroup>

    <ILRepack
      Parallel="true"
      Internalize="true"
      InputAssemblies="@(InputAssemblies)"
      TargetKind="Dll"
      LibraryPath="$(GameManaged)"
      OutputFile="$(WorkingDirectory)\YourModName.dll" />
  </Target>
</Project>
```

**Replace `YourModName` with your actual mod DLL name.**

### 3. Key Points

- **PrivateAssets=all**: Prevents PLib from being copied as separate DLL
- **CopyLocalLockFileAssemblies**: Required for ILRepack to find dependencies
- **LibraryPath=$(GameManaged)**: Tells ILRepack where to find ONI DLLs (Newtonsoft.Json, etc.)
- **Internalize=true**: Makes PLib types internal to avoid conflicts
- **UnityEngine.TextCoreFontEngineModule**: Required reference for FaceInfo type

### 4. Verify Merge

After build, check that:
- Only `YourModName.dll` exists in output folder (no separate PLib.dll)
- Build log shows: `Merged 2 assemblies` and `Merge succeeded`

### 5. Deploy

Deploy only the merged `YourModName.dll` - no PLib.dll should be included.

## What NOT To Do

❌ Ship PLib.dll as separate file alongside mod DLL
❌ Use PackageReference without PrivateAssets=all  
❌ Omit LibraryPath in ILRepack (causes Newtonsoft.Json resolution errors)
❌ Reference PLib as local file instead of NuGet package

## Migration from Older Setup

If you have:
```xml
<Reference Include="PLib">
  <HintPath>$(ProjectDir)PLib.dll</HintPath>
</Reference>
```

Replace with NuGet package + ILRepack as shown above.

## Tested Versions

- ONI Build: U58-720697-SCRP
- PLib: 4.24.0
- ILRepack: 2.0.40
- Target Framework: net48
