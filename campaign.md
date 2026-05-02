# DSB Linux Workshop Issue Campaign

## Planned
- Fix repo-owned issues from GitHub issue 21.
- Keep `mod.yaml` description short and plain for the ONI mod list.
- Harden translation loading against Linux extraction of bad Workshop zip entry names.
- Research whether the backslash-packed Workshop `.bin` issue is known in the ONI Mod Uploader ecosystem.
- Investigate and fix Steam comment reports for empty active-world layout, `MaxDupesPerRow`, and bionic oxygen tank visibility.

## Read And Checked
- Issue 21 text and attached Linux `Player.log`.
- `DuplicantStatusBar/mod.yaml`.
- `DuplicantStatusBar/Patches/TranslationPatch.cs`.
- `DuplicantStatusBar/DuplicantStatusBar.csproj`.
- Klei bug tracker search results for ONI Mod Uploader issues.
- Klei 346893 patch notes showing `OniUploader` is a Klei-supported tool on Linux and OSX.
- PeterHan `DebugNotIncluded/ModEditor/ModEditor.cs`, which uploads Workshop data as `mod_publish_data_file.zip` through Steam Remote Storage.
- GitHub searches for public reports mentioning ONI uploader backslash zip paths, `mod_publish_data_file.zip`, and `translations\cs.po`.
- Local Steam Workshop cache under `D:\SteamLibrary\steamapps\workshop\content\457140` for cross-mod raw archive comparison.
- Steam published file details API for titles of mods whose raw archives contain `.po` paths.

## Tested
- Baseline `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed with existing warnings.
- Added `DuplicantStatusBar/tests/translation-path-fallback.ps1`.
- Confirmed the regression check failed before the resolver existed.
- Confirmed the regression check failed again until `TranslationPatch` used the resolver.
- Confirmed `translation-path-fallback.ps1` passes after the resolver and patch wiring.
- Confirmed `translation-path-fallback.ps1` covers normal folder resolution, fallback candidate shape, and patch wiring.
- Final verification: `translation-path-fallback.ps1`, `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release`, and `git diff --check` all passed.
- Final added-line em dash scan passed. A broad file scan still finds older pre-existing em dashes in historical changelog entries and an existing project-file comment.
- Follow-up raw archive scan: 94 `_legacy.bin` archives checked, 30 contain backslash member names, 11 contain `.po` files, and 10 of those 11 store `.po` entries with backslashes.
- Follow-up examples with backslash `.po` paths include DSB, Mod Updater, Blueprints Expanded, Fast Save, Mass Move Tool, Waste Not Want Not, Suppress Notifications, Liquid Bottler, Fast Insulated Self Sealing AirLock, and Auto Change Wardrobe.
- WSL/Python extraction of both DSB and Mod Updater preserves root-level filenames like `translations\de.po`, confirming the generic Linux extraction problem is not unique to DSB.
- Windows ONI cache still normalizes both DSB and Mod Updater into a real `translations` folder under `Documents\Klei\OxygenNotIncluded\mods\Steam\<id>`.
- Upload-folder readiness check found `PLib.dll` still present in the dev deploy folder, contradicting the v2.8.8 ILRepack decision to ship PLib embedded in `DuplicantStatusBar.dll`.
- Updated the deploy target to copy only `DuplicantStatusBar.dll` and delete stale `PLib.dll` from the deploy folder.
- Final deploy folder contains `DuplicantStatusBar.dll` version `2.8.9.0`, `mod.yaml`, `mod_info.yaml`, `preview.jpg`, and 10 translation files. `PLib.dll`, `config.json`, and `.pdb` artifacts are absent.
- Follow-up check after user challenge: `DuplicantStatusBar\PLib.dll` exists as an untracked local file, version `4.25.0.0`, byte-identical to `PeterHan-ONIMods\PLib\bin\Release\netstandard2.1\PLib.dll`.
- Current project build does not use that untracked local `PLib.dll`. `DuplicantStatusBar.csproj` references NuGet `PLib` `4.24.0`, and `bin\Release\PLib.dll` is version `4.24.0.0`.
- The previous deploy target copied `$(OutputPath)PLib.dll`, so the separate DLL that was removed from deploy was the NuGet/output `4.24.0.0` DLL, not the untracked local `4.25.0.0` manual DLL.
- `StatusBarOptions.MaxDupesPerRow`, `MaxBarWidth`, and `MaxBarRows` had localized strings and JSON storage but were not exposed as PLib options.
- `StatusBarScreen.UpdateGridLayout()` returned immediately for zero dupes, leaving the prior portrait viewport size visible when switching to an empty active world.
- The layout path ignored `MaxDupesPerRow`, `MaxBarWidth`, and `MaxBarRows` when computing columns, auto width, and visible rows.
- ONI's current game assembly contains `BionicOxygenTank`, `BionicOxygenTankMonitor`, and `STRINGS.DUPLICANTS.STATS.BIONICOXYGENTANK`, confirming the bionic oxygen tank amount name to read.
- Added `DuplicantStatusBar/tests/layout-bionic-regression.ps1`; it failed before implementation on the missing PLib option exposure.
- Implemented layout cap handling in `StatusBarScreen` and bionic oxygen tank percentage tracking in `DupeStatusTracker`.
- Added `TOOLTIP_OXYGEN_TANK` and moved the layout options into a dedicated PLib Layout category.
- Follow-up regression: vertical drag resizing still had `MaxBarRows` applied afterward, so a manual vertical layout could be forced back to the configured row cap.
- Follow-up regression: asteroid transitions could carry the previous active world's manual dimensions and scroll position into a smaller or empty world, producing a stale blank bar.
- Added asteroid transition checks to `DuplicantStatusBar/tests/layout-bionic-regression.ps1`; the check failed before implementation on missing active-world reload handling.
- Implemented active-world change handling in `StatusBarScreen` so it saves manual size by world, resets unknown worlds to auto sizing, forces a dupe-count refresh, and resets scroll.
- Hardened the empty-world path to explicitly mark the bar layout dirty after zeroing the portrait viewport.
- Tightened `layout-bionic-regression.ps1` to check transition operation order, previous-world save before current-world restore, scroll momentum reset, viewport reset, empty viewport zeroing, and manual resize cache persistence.

## Built
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after adding the campaign log.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after adding the regression script.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after adding `TranslationFileResolver`.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after wiring `TranslationPatch`.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after shortening `mod.yaml`.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after bumping v2.8.9.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after adding the changelog entry.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after tightening fallback logging.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after the final campaign update.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after deploy target cleanup.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after adding the layout and bionic regression script.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after implementing layout and bionic oxygen changes, with only pre-existing warnings plus the existing duplicate ILRepack import warning.
- `DuplicantStatusBar/tests/layout-bionic-regression.ps1` passed after implementation.
- `DuplicantStatusBar/tests/translation-path-fallback.ps1` still passed after implementation.
- Added a regression check that `MaxBarRows` is skipped when a vertical drag height is active.
- `DuplicantStatusBar/tests/layout-bionic-regression.ps1` passed after adding asteroid transition checks and active-world reload handling.
- `DuplicantStatusBar/tests/translation-path-fallback.ps1` still passed after active-world reload handling.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after active-world reload handling, with only existing warnings.
- `DuplicantStatusBar/tests/layout-bionic-regression.ps1` passed after tightening asteroid transition invariants.
- `DuplicantStatusBar/tests/translation-path-fallback.ps1` passed after tightening asteroid transition invariants.
- `dotnet build DuplicantStatusBar/DuplicantStatusBar.csproj -c Release` passed after empty-layout rebuild hardening, with only existing warnings.

## Decisions
- Treat the archive backslash paths as a packer or uploader problem, but still mitigate in DSB because Linux users can receive a broken extracted shape.
- Do not flatten translations as the default layout. Keep the standard `translations/` folder and add fallback resolution.
- Shorten only `mod.yaml` because it controls the in-game mod list tooltip. Keep Workshop description content outside this metadata file.
- Use patch version `2.8.9` because this is a bugfix and metadata cleanup.
- Public research found other ONI Mod Uploader issues, but no public Klei or GitHub report specifically for backslash zip member names.
- The fix does not change Klei's raw Workshop archive packing. It makes DSB tolerate the broken extraction shape and fixes repo-owned metadata.
- If the actual Linux ONI runtime normalizes the archive the same way Windows does, the backslash issue is mostly a raw archive portability defect. If Steam Deck/Linux leaves the malformed root filenames in the ONI mod cache, DSB now has a fallback for translations.
- DSB is not the only mod with this raw archive shape. The lack of public reports likely means ONI or Steam usually normalizes during real subscription install, many users do not inspect raw `_legacy.bin` files, or localization loss is not obvious.
- Upload from `C:\Users\Zero\Documents\Klei\OxygenNotIncluded\mods\dev\DuplicantStatusBar` after a Release build, not from `bin\Release`, so the folder includes metadata, preview, and translations.
- Do not claim upload readiness if the untracked local `PLib.dll` `4.25.0.0` was intended to be the patched PLib source. In that case, the build must be explicitly changed and revalidated to embed that file instead of NuGet `PLib` `4.24.0`.
- Use minor version `2.9.0` because bionic oxygen tank display is a user-visible feature in addition to bug fixes.
- Keep the empty active-world state header-only rather than hiding the whole bar, so the user can still drag, collapse, or use the reset flow.
- Treat option caps as automatic-layout defaults. Manual drag sizing on an axis takes priority over the cap for that axis.
- Treat asteroid switches as explicit layout reload events. The new active world should size from its actual active dupe count unless that world already has a saved manual resize.
- Reset scroll on active-world changes so a smaller or empty world cannot inherit an off-screen viewport from a larger world.

## Issues
- No dedicated DSB test project exists. Use focused script checks plus full project build.
- Windows cannot create a literal filename containing `\`, so the regression script validates the fallback candidate shape and normal resolution rather than creating the Linux-only malformed filename.

## Remaining
- Publish/update Workshop after merging if desired. The raw archive backslash behavior may remain if Klei's uploader continues to pack that way.
