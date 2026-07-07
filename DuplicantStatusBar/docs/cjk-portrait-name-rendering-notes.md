# CJK Portrait Name Rendering Notes

## Summary

Issue: duplicant names containing Chinese characters rendered in DSB tooltips but rendered blank under the portrait card on the main status bar.

Confirmed fixed locally for public release v2.10.8.0 by increasing the portrait name label height from 16px to 20px and increasing the widget total height by the same 4px.

The root cause was not missing font discovery and not a mask hiding already-generated glyphs. Local field diagnostics showed TextMeshPro generated zero characters and zero vertices for the portrait label because the label rectangle was shorter than the CJK fallback font's preferred height.

## User-visible Reproduction

1. Enable DSB Debug Logging.
2. Use a duplicant name such as `小明`.
3. Observe the DSB portrait name label.
4. Hover the same duplicant and observe the tooltip name.

Before v2.10.8:

- Portrait bar name was blank.
- Tooltip name rendered correctly.
- English names beside it still rendered correctly.

After v2.10.8:

- Portrait bar name renders `小明`.
- Tooltip still renders `小明`.

## Important Evidence

The useful diagnostic comparison came from local field diagnostics added during the investigation.

Portrait name path:

```text
context=PortraitName script=CJK
tmpPreferred=24.0x17.8
rect=65.0x16.0
charDetails=0[]
mesh=2[0:verts=0, 1:verts=0]
subMeshes=1[...] meshBounds size=(0.0,0.0,0.0)
```

Tooltip path for the same CJK name:

```text
context=Tooltip script=CJK
charDetails=168[0:U+5C0F/visible=True/mat=1/font=NotoSansCJKjp-Regular, 1:U+660E/visible=True/...]
mesh=3[0:verts=508, 1:verts=8, 2:verts=4]
subMeshes=2[...NotoSansCJKjp-Regular...meshBounds size=(75.9,85.6,0.0)]
```

Conclusion:

- The portrait text object had the CJK fallback font available.
- The portrait text object had mask and stencil state, but it was not the primary failure.
- TMP did not generate CJK vertices for the portrait label at all.
- Tooltip did generate visible CJK characters and vertices with the same runtime fallback font chain.
- The failed portrait label had a 16px high rectangle while TMP reported 17.8px preferred height for the text.

## Failed Or Incomplete Hypotheses

These were investigated or attempted before the final fix:

- Missing CJK font discovery: false. Logs showed ONI runtime fallback fonts included Noto CJK fonts and sample support was true.
- Per-string primary font switching: rejected. It temporarily avoids fallback behavior but violates the design rule and can create mixed-script styling problems.
- Tooltip works because it has a different font only: incomplete. Tooltip and portrait both had access to the runtime fallback chain. The key difference was the layout box and vertex generation.
- Mask or stencil hiding generated glyphs: not supported by the field evidence. The failing portrait path had zero characters and zero text vertices before rendering.
- LocText versus raw TextMeshProUGUI: not the root cause by itself. LocText did not fix the issue while the label remained too short.

## Final Fix

File: `DuplicantStatusBar/UI/DupePortraitWidget.cs`

Changed:

- `nameLayout.preferredHeight = 20`
- `totalH = size + 26`

Previous values:

- `nameLayout.preferredHeight = 16`
- `totalH = size + 22`

Reasoning:

- 16px was smaller than the observed CJK preferred height of 17.8px.
- 20px gives a small margin for CJK fallback metrics.
- Widget total height grew by the same 4px so layout does not squeeze the label back down.
- No font switching, no shipped fonts, and no broad layout rewrite were needed.

## Runtime Font Design Rule

DSB should keep ONI's normal DSB/UI font as the primary font whenever available.

Selected-language and language-pack TMP fonts should be attached only through ONI/TMP fallback chains. DSB should not choose a new primary font based on whether the current string contains CJK, Cyrillic, accented, or other non-Latin glyphs.

## Diagnostics Kept

The investigation added shared text-render diagnostics for portrait names and tooltips:

- object path
- component type
- active state
- canvas
- world corners
- mask chain
- stencil depth
- material properties
- material slots
- per-character font and material assignment
- TMP submeshes
- fallback source material
- CanvasRenderer culling state

These are intentionally verbose but only log once per context and script class. They are useful if another language reports a similar issue.

## Validation

Commands run for v2.10.8:

```powershell
.\DuplicantStatusBar\tests\translation-path-fallback.ps1
.\DuplicantStatusBar\tests\skill-stamina-regression.ps1
.\DuplicantStatusBar\tests\aquatic-compat-regression.ps1
.\DuplicantStatusBar\tests\badge-scaling-regression.ps1
.\DuplicantStatusBar\tests\equipment-display-regression.ps1
.\DuplicantStatusBar\tests\filter-empty-regression.ps1
.\DuplicantStatusBar\tests\layout-bionic-regression.ps1
.\DuplicantStatusBar\tests\screenshot-mode-regression.ps1
.\DuplicantStatusBar\tests\temperature-units-regression.ps1
dotnet build DuplicantStatusBar\DuplicantStatusBar.csproj -c Release
git diff --check
```

Results:

- All listed regression scripts passed.
- Release build passed with 3 existing obsolete API warnings and 0 errors.
- `git diff --check` passed.
- Deployed dev DLL reported `FileVersion 2.10.8.0`.
- Visual local test confirmed `小明` rendered on the portrait bar and in the tooltip.

## GitHub Issue Response Points

Use this if replying to the original report:

- Chinese names are supported.
- The blank portrait-name issue was traced to the portrait label height, not missing CJK fonts.
- The tooltip worked because it had enough vertical layout space; the portrait label did not.
- v2.10.8 increases the portrait name label height enough for CJK fallback metrics while preserving ONI's runtime font fallback chain.
- Ask the reporter to test v2.10.8 and attach `dsb_debug.log` plus `Player.log` if any language still fails.
