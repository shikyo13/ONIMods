$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$optionsPath = Join-Path $projectDir 'Config\Options.cs'
$stringsPath = Join-Path $projectDir 'Localization\ModStrings.cs'
$loggerPath = Join-Path $projectDir 'Core\FISSACLog.cs'
$diagnosticsPath = Join-Path $projectDir 'Core\DoorDiagnostics.cs'
$doorPatchesPath = Join-Path $projectDir 'Patches\DoorPatches.cs'
$modLifecyclePath = Join-Path $projectDir 'Core\FastInsulatedSelfSealingAirLockMod.cs'
$projectFile = Join-Path $projectDir 'FastInsulatedSelfSealingAirLock.csproj'

foreach ($path in @($optionsPath, $stringsPath, $doorPatchesPath, $modLifecyclePath, $projectFile)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing required source file: $path"
    }
}

if (-not (Test-Path -LiteralPath $loggerPath)) {
    throw 'Missing dedicated FISSACLog.cs logger.'
}

if (-not (Test-Path -LiteralPath $diagnosticsPath)) {
    throw 'Missing DoorDiagnostics.cs cell-state diagnostic logger.'
}

$options = Get-Content -Raw -LiteralPath $optionsPath
$strings = Get-Content -Raw -LiteralPath $stringsPath
$logger = Get-Content -Raw -LiteralPath $loggerPath
$diagnostics = Get-Content -Raw -LiteralPath $diagnosticsPath
$doorPatches = Get-Content -Raw -LiteralPath $doorPatchesPath
$modLifecycle = Get-Content -Raw -LiteralPath $modLifecyclePath
$project = Get-Content -Raw -LiteralPath $projectFile

if ($options -notmatch '\[JsonProperty\]\s*public bool DebugLogging \{ get; set; \}') {
    throw 'Options must expose a JsonProperty DebugLogging bool.'
}

if ($options -match '\[RestartRequired\]\s*\[JsonObject') {
    throw 'Options class must not be marked RestartRequired; DebugLogging must be live-toggleable.'
}

if ($options -notmatch 'public sealed class Options : SingletonOptions<Options>, IOptions') {
    throw 'Options must inherit SingletonOptions<Options> and implement IOptions so option changes live-reload.'
}

if ($options -notmatch '\[ConfigFile\("FastInsulatedSelfSealingAirLock\.json", false, true\)\]') {
    throw 'Options must use PLib shared config storage so settings survive mod updates and local redeploys.'
}

if ($options -notmatch '\[RestartRequired\]\s*\[Option\("ModStrings\.Options\.MULTIPLIER_TITLE"' -or $options -match '\[RestartRequired\]\s*\[Option\("ModStrings\.Options\.DEBUG_LOGGING_TITLE"') {
    throw 'Only the speed multiplier should require restart; DebugLogging must not.'
}

if ($options -notmatch 'public void OnOptionsChanged\(\)' -or $options -notmatch 'POptions\.ReadSettings<Options>\(\)' -or $options -notmatch 'FISSACLog\.OnOptionsChanged\(\)') {
    throw 'Options.OnOptionsChanged must refresh cached options and notify FISSACLog.'
}

if ($options -notmatch 'public IEnumerable<IOptionsEntry> CreateOptions\(\)' -or $options -notmatch 'return null;') {
    throw 'Options must implement the PLib IOptions.CreateOptions no-op required by this PLib version.'
}

if ($options -notmatch 'DebugLogging\s*=\s*false') {
    throw 'DebugLogging must default to false.'
}

if ($options -notmatch 'DEBUG_LOGGING_TITLE' -or $strings -notmatch 'Debug Logging' -or $strings -notmatch 'FISSAC\.log') {
    throw 'Debug Logging option strings must name the option and FISSAC.log destination.'
}

if ($logger -notmatch 'FISSAC\.log' -or $logger -notmatch 'Application\.persistentDataPath' -or $logger -notmatch 'StreamWriter') {
    throw 'FISSACLog must write FISSAC.log next to Player.log using Application.persistentDataPath.'
}

if ($logger -notmatch 'SingletonOptions<Options>\.Instance\.DebugLogging') {
    throw 'FISSACLog must be gated by the mod DebugLogging option.'
}

if ($logger -notmatch 'public static void OnOptionsChanged\(\)' -or $logger -notmatch 'Debug logging enabled via mod options' -or $logger -notmatch 'Close\(\)') {
    throw 'FISSACLog must open or close the dedicated log when DebugLogging changes at runtime.'
}

if ($logger -notmatch 'Debug\.LogWarning' -or $logger -notmatch 'Debug\.LogError') {
    throw 'FISSACLog warnings and errors must still surface in Player.log.'
}

if ($diagnostics -notmatch 'Grid\.Insulation\[cell\]' -or $diagnostics -notmatch 'Grid\.Temperature\[cell\]' -or $diagnostics -notmatch 'Grid\.Mass\[cell\]') {
    throw 'DoorDiagnostics must log actual insulation, temperature, and mass for sampled cells.'
}

if ($diagnostics -notmatch 'Grid\.IsGas\(cell\)' -or $diagnostics -notmatch 'Grid\.IsLiquid\(cell\)' -or $diagnostics -notmatch 'Grid\.LiquidImpermeable\[cell\]') {
    throw 'DoorDiagnostics must log gas/liquid state and liquid impermeability for sampled cells.'
}

if ($diagnostics -notmatch 'Grid\.CellLeft\(cell\)' -or $diagnostics -notmatch 'Grid\.CellRight\(cell\)' -or $diagnostics -notmatch 'Grid\.CellAbove\(cell\)' -or $diagnostics -notmatch 'Grid\.CellBelow\(cell\)') {
    throw 'DoorDiagnostics must sample neighboring cells to diagnose cross-door leakage.'
}

foreach ($hook in @('DoorDiagnostics.LogPrefabInit', 'DoorDiagnostics.RegisterDoor', 'DoorDiagnostics.UnregisterDoor', 'DoorDiagnostics.LogSelfSealingApplication', 'DoorDiagnostics.LogCleanUp', 'DoorDiagnostics.SampleRegisteredDoors', 'DoorDiagnostics.LogOverrideAnimRepair', 'DoorDiagnostics.LogWorkableAnimInfo')) {
    if ($doorPatches -notmatch [regex]::Escape($hook)) {
        throw "Door patches must call $hook."
    }
}

if ($diagnostics -notmatch 'DoorAnim' -or $diagnostics -notmatch 'overrideAnims=' -or $diagnostics -notmatch 'resultOverrideAnims=') {
    throw 'DoorDiagnostics must log door animation override repair and Workable.GetAnim result state.'
}

if ($diagnostics -match 'isOpen failed') {
    throw 'DoorDiagnostics must not log expected pre-spawn Door.IsOpen nulls as failures.'
}

if ($doorPatches -notmatch 'GameLeakSamplerPatch' -or $doorPatches -notmatch 'HarmonyPatch\(typeof\(Game\), "UnsafeSim200ms"\)' -or $modLifecycle -notmatch 'PatchClass\(harmony,\s*typeof\(GameLeakSamplerPatch\)\)') {
    throw 'GameLeakSamplerPatch must sample registered airlocks from the global sim tick.'
}

if ($modLifecycle -notmatch 'FISSACLog\.Log\("Lifecycle"' -or $modLifecycle -notmatch 'FISSACLog\.Error\("PatchClass') {
    throw 'Mod lifecycle must log patch installation and patch errors to FISSACLog.'
}

if ($project -match 'ModMeta Include="[^"]*FISSAC\.log' -or $project -match 'FISSAC\.log') {
    throw 'FISSAC.log must be runtime-generated and must not be copied into the deploy package.'
}

Write-Host 'airlock debug logging regression checks passed'
