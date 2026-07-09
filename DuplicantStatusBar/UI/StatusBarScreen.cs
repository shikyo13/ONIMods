using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using DuplicantStatusBar.Config;
using DuplicantStatusBar.Core;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.UI
{
    public sealed partial class StatusBarScreen : MonoBehaviour
    {
        internal static StatusBarScreen Instance { get; private set; }
        private RectTransform canvasRT;
        private RectTransform barPanel;
        private GameObject contentArea;
        private RectTransform contentRT;
        private TMPro.TextMeshProUGUI collapseLabel;
        private readonly List<DupePortraitWidget> widgets = new List<DupePortraitWidget>();

        private Image panelImage;
        private GridLayoutGroup grid;
        private RectTransform headerRT;
        private RectTransform resizeGripRT;
        private ScrollRect scrollRect;
        private LayoutElement scrollViewLayout;
        private GameObject scrollbarGO;
        private GameObject scrollViewGO;
        private float updateTimer;
        private bool isCollapsed;
        internal int lastDupeCount = -1;
        internal int lastComputedSize;
        internal bool forceRefresh;
        private int lastConfiguredSize;
        internal float barWidthPx = -1f;   // -1 = auto (use PortraitSize, fit canvas width)
        internal float barHeightPx = -1f;  // -1 = auto (show all rows, no scroll)
        private CanvasScaler canvasScaler;
        private KCanvasScaler gameCanvasScaler;
        private float lastUIScale = 1f;
        private GameObject filterBtnGO;
        private TMPro.TextMeshProUGUI filterTMP;
        private HorizontalLayoutGroup headerHLG;
        private bool firstUpdate = true;
        private bool needsPostLayoutClamp;
        private float filterFullWidth = 80f; // measured at build time, fallback 80px
        private LayoutElement gripLE;
        internal bool isDraggingResize;
        private ContentSizeFitter panelFitter;
        private readonly Dictionary<int, Vector2> worldBoxSizes = new Dictionary<int, Vector2>();
        private int lastActiveWorldId = int.MinValue;

        // Game font (ONI-native, with localized glyph fallback)
        private static TMPro.TMP_FontAsset _gameFont;
        private static TMPro.TMP_FontAsset[] _fontAssets;
        private static bool _fontSearched;
        private static bool _fontDiagnosticsLogged;
        private static readonly HashSet<string> _textRenderDiagnosticsLogged =
            new HashSet<string>();
        private static System.Reflection.MethodInfo _forceMeshUpdate;
        private static bool _forceMeshSearched;
        internal static TMPro.TMP_FontAsset GameFont
        {
            get
            {
                EnsureFontCache();
                return _gameFont;
            }
        }

        internal static TMPro.TMP_FontAsset FontForText(string text)
        {
            EnsureFontCache();

            var gameFont = _gameFont;
            if (gameFont != null)
                return gameFont;

            var all = _fontAssets;
            if (all == null || all.Length == 0)
                return null;

            return GetCurrentLocalizedFont(all)
                ?? TMPro.TMP_Settings.defaultFontAsset
                ?? GetFirstTmpSettingsFallbackFont()
                ?? all[0];
        }

        internal static void ApplyFontForText(TMPro.TMP_Text tmp, string text)
        {
            if (tmp == null)
                return;

            var font = FontForText(text);
            if (font != null && !ReferenceEquals(tmp.font, font))
                tmp.font = font;
        }

        private static TMPro.TMP_FontAsset FindDirectFallbackFontForText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            EnsureFontCache();
            var all = _fontAssets;
            var gameFont = _gameFont;

            var localizedFont = GetCurrentLocalizedFont(all);
            if (SupportsDirectFallback(localizedFont, gameFont, text))
                return localizedFont;
            if (SupportsDirectFallback(TMPro.TMP_Settings.defaultFontAsset, gameFont, text))
                return TMPro.TMP_Settings.defaultFontAsset;

            var tmpFallback = FindDirectFallbackInFonts(TMPro.TMP_Settings.fallbackFontAssets, gameFont, text);
            if (tmpFallback != null)
                return tmpFallback;

            var primaryFallback = FindDirectFallbackInFonts(
                gameFont?.fallbackFontAssetTable, gameFont, text);
            if (primaryFallback != null)
                return primaryFallback;

            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    var font = all[i];
                    if (IsNotoSans(font) && SupportsDirectFallback(font, gameFont, text))
                        return font;
                }

                for (int i = 0; i < all.Length; i++)
                {
                    var font = all[i];
                    if (SupportsDirectFallback(font, gameFont, text))
                        return font;
                }
            }

            return null;
        }

        private static TMPro.TMP_FontAsset FindDirectFallbackInFonts(
            IEnumerable<TMPro.TMP_FontAsset> fonts,
            TMPro.TMP_FontAsset primaryFont,
            string text)
        {
            if (fonts == null)
                return null;

            foreach (var font in fonts)
            {
                if (SupportsDirectFallback(font, primaryFont, text))
                    return font;
            }

            return null;
        }

        private static bool SupportsDirectFallback(
            TMPro.TMP_FontAsset candidate,
            TMPro.TMP_FontAsset primaryFont,
            string text)
        {
            return candidate != null &&
                !ReferenceEquals(candidate, primaryFont) &&
                SupportsText(candidate, text, false);
        }

        internal static string BuildTextRenderDiagnostic(TMPro.TMP_Text tmp, string text)
        {
            EnsureFontCache();

            var font = tmp != null ? tmp.font : FontForText(text);
            return $"primaryFont={FontAssetName(font)}" +
                $" supportsText={SupportsText(font, text)}" +
                $" supportsTextDirect={SupportsText(font, text, false)}" +
                $" directFallbackFont={FontAssetName(FindDirectFallbackFontForText(text))}" +
                $" fallbackFonts={BuildFallbackFontSummary(font)}" +
                $" tmpSettingsFallbackFonts={BuildTmpSettingsFallbackSummary()}";
        }

        internal static void LogTextRenderDiagnostics(
            string context,
            TMPro.TextMeshProUGUI label,
            string text)
        {
            if (!DSBLog.Active || label == null)
                return;

            string scriptClass = ClassifyDiagnosticScript(text);
            if (scriptClass == "Ascii")
                return;

            string diagnosticContext = string.IsNullOrEmpty(context) ? "Text" : context;
            string onceKey = diagnosticContext + ":" + scriptClass;
            if (_textRenderDiagnosticsLogged.Contains(onceKey))
                return;
            _textRenderDiagnosticsLogged.Add(onceKey);

            Vector2 textPreferred = Vector2.zero;
            try { textPreferred = label.GetPreferredValues(text ?? ""); } catch { }
            SafeForceMeshUpdate(label);

            Rect rect = label.rectTransform != null ? label.rectTransform.rect : default(Rect);
            DSBLog.Log("Font",
                $"textRenderDiagnostic context={diagnosticContext}" +
                $" script={scriptClass}" +
                $" {BuildTextRenderDiagnostic(label, text)}" +
                $" textLength={(text != null ? text.Length : 0)}" +
                $" component={label.GetType().FullName}" +
                $" textObjectPath={BuildTransformPath(label.transform)}" +
                $" activeSelf={label.gameObject.activeSelf}" +
                $" activeInHierarchy={label.gameObject.activeInHierarchy}" +
                $" enabled={label.enabled}" +
                $" maskable={label.maskable}" +
                $" canvasCull={(label.canvasRenderer != null && label.canvasRenderer.cull)}" +
                $" cullTransparentMesh={(label.canvasRenderer != null && label.canvasRenderer.cullTransparentMesh)}" +
                $" richText={label.richText}" +
                $" fontStyle={label.fontStyle}" +
                $" alignment={label.alignment}" +
                $" verticalAlignment={label.verticalAlignment}" +
                $" textWrapping={label.textWrappingMode}" +
                $" overflow={label.overflowMode}" +
                $" fontSize={label.fontSize:F1}" +
                $" tmpPreferred={textPreferred.x:F1}x{textPreferred.y:F1}" +
                $" rect={rect.width:F1}x{rect.height:F1}" +
                $" rectTransform={BuildRectTransformSummary(label.rectTransform)}" +
                $" worldCorners={BuildWorldCornerSummary(label.rectTransform)}" +
                $" canvas={BuildCanvasSummary(label)}" +
                $" stencilDepth={BuildStencilDepth(label.transform)}" +
                $" masks={BuildMaskChain(label.transform)}" +
                $" canvasRenderer={BuildCanvasRendererSummary(label.canvasRenderer)}" +
                $" fontSharedMaterial={MaterialSummary(label.fontSharedMaterial)}" +
                $" materialForRendering={MaterialSummary(label.materialForRendering)}" +
                $" materialProps={ShaderPropertySummary(label.materialForRendering)}" +
                $" materialSlots={BuildMaterialSlotSummary(label.textInfo)}" +
                $" charDetails={BuildCharacterDetails(label.textInfo)}" +
                $" mesh={BuildMeshSummary(label.textInfo)}" +
                $" subMeshes={BuildSubMeshSummary(label)}");
        }

        private static string ClassifyDiagnosticScript(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "Ascii";

            bool latinExtended = false;
            bool nonAsciiLetter = false;
            bool otherNonAscii = false;
            foreach (char c in text)
            {
                if (c <= 0x7F)
                    continue;

                int code = c;
                if ((code >= 0x4E00 && code <= 0x9FFF) ||
                    (code >= 0x3400 && code <= 0x4DBF) ||
                    (code >= 0x3040 && code <= 0x30FF) ||
                    (code >= 0xAC00 && code <= 0xD7AF))
                    return "CJK";
                if (code >= 0x0400 && code <= 0x052F)
                    return "Cyrillic";
                if ((code >= 0x00C0 && code <= 0x024F) ||
                    (code >= 0x1E00 && code <= 0x1EFF))
                    latinExtended = true;
                else if (char.IsLetter(c))
                    nonAsciiLetter = true;
                else
                    otherNonAscii = true;
            }

            if (latinExtended)
                return "LatinExtended";
            if (nonAsciiLetter)
                return "NonAsciiLetter";
            return otherNonAscii ? "OtherNonAscii" : "Ascii";
        }

        private static void SafeForceMeshUpdate(TMPro.TMP_Text label)
        {
            if (label == null)
                return;

            try
            {
                if (!_forceMeshSearched)
                {
                    _forceMeshSearched = true;
                    _forceMeshUpdate = typeof(TMPro.TMP_Text).GetMethod("ForceMeshUpdate",
                        new[] { typeof(bool), typeof(bool) })
                        ?? typeof(TMPro.TMP_Text).GetMethod("ForceMeshUpdate",
                            new[] { typeof(bool) })
                        ?? typeof(TMPro.TMP_Text).GetMethod("ForceMeshUpdate",
                            System.Type.EmptyTypes);
                }

                if (_forceMeshUpdate == null)
                    return;

                var parameters = _forceMeshUpdate.GetParameters();
                object[] args = parameters.Length == 2
                    ? new object[] { false, false }
                    : parameters.Length == 1
                        ? new object[] { false }
                        : null;
                _forceMeshUpdate.Invoke(label, args);
            }
            catch
            {
            }
        }

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
                return "none";

            var parts = new List<string>();
            var current = transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static string BuildRectTransformSummary(RectTransform rt)
        {
            if (rt == null)
                return "none";

            return $"anchorMin={Vector2Summary(rt.anchorMin)}" +
                $"/anchorMax={Vector2Summary(rt.anchorMax)}" +
                $"/pivot={Vector2Summary(rt.pivot)}" +
                $"/anchored={Vector2Summary(rt.anchoredPosition)}" +
                $"/sizeDelta={Vector2Summary(rt.sizeDelta)}" +
                $"/localScale={Vector3Summary(rt.localScale)}" +
                $"/lossyScale={Vector3Summary(rt.lossyScale)}";
        }

        private static string BuildWorldCornerSummary(RectTransform rt)
        {
            if (rt == null)
                return "none";

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return "bl=" + Vector3Summary(corners[0]) +
                "/tl=" + Vector3Summary(corners[1]) +
                "/tr=" + Vector3Summary(corners[2]) +
                "/br=" + Vector3Summary(corners[3]);
        }

        private static string BuildCanvasSummary(TMPro.TextMeshProUGUI label)
        {
            if (label == null)
                return "none";

            var canvas = label.GetComponentInParent<Canvas>();
            if (canvas == null)
                return "none";

            var root = canvas.rootCanvas;
            return $"name={canvas.name}" +
                $"/root={(root != null ? root.name : "none")}" +
                $"/renderMode={canvas.renderMode}" +
                $"/sortingLayer={canvas.sortingLayerID}" +
                $"/sortingOrder={canvas.sortingOrder}" +
                $"/overrideSorting={canvas.overrideSorting}" +
                $"/scaleFactor={canvas.scaleFactor:F3}" +
                $"/pixelPerfect={canvas.pixelPerfect}" +
                $"/worldCamera={(canvas.worldCamera != null ? canvas.worldCamera.name : "none")}";
        }

        private static int BuildStencilDepth(Transform transform)
        {
            if (transform == null)
                return -1;

            try
            {
                var stopAfter = MaskUtilities.FindRootSortOverrideCanvas(transform);
                return MaskUtilities.GetStencilDepth(transform, stopAfter);
            }
            catch
            {
                return -1;
            }
        }

        private static string BuildMaskChain(Transform transform)
        {
            if (transform == null)
                return "0[]";

            var sb = new System.Text.StringBuilder();
            int count = 0;
            var current = transform;
            while (current != null)
            {
                var mask = current.GetComponent<Mask>();
                var rectMask = current.GetComponent<RectMask2D>();
                if (mask != null || rectMask != null)
                {
                    if (count > 0)
                        sb.Append(", ");
                    sb.Append(current.name);
                    if (mask != null)
                        sb.Append(":Mask(show=").Append(mask.showMaskGraphic)
                            .Append("/enabled=").Append(mask.enabled)
                            .Append("/active=").Append(mask.IsActive()).Append(")");
                    if (rectMask != null)
                        sb.Append(":RectMask2D(enabled=").Append(rectMask.enabled)
                            .Append("/active=").Append(rectMask.IsActive()).Append(")");
                    count++;
                }
                current = current.parent;
            }

            return count + "[" + sb + "]";
        }

        private static string BuildCanvasRendererSummary(CanvasRenderer renderer)
        {
            if (renderer == null)
                return "none";

            string material0 = "none";
            try { material0 = MaterialName(renderer.GetMaterial()); } catch { }
            Color color = Color.white;
            try { color = renderer.GetColor(); } catch { }

            return $"cull={renderer.cull}" +
                $"/cullTransparentMesh={renderer.cullTransparentMesh}" +
                $"/hasRectClipping={renderer.hasRectClipping}" +
                $"/materialCount={renderer.materialCount}" +
                $"/material0={material0}" +
                $"/absoluteDepth={renderer.absoluteDepth}" +
                $"/relativeDepth={renderer.relativeDepth}" +
                $"/alpha={renderer.GetAlpha():F3}" +
                $"/inheritedAlpha={renderer.GetInheritedAlpha():F3}" +
                $"/color={ColorSummary(color)}";
        }

        private static string BuildMaterialSlotSummary(TMPro.TMP_TextInfo textInfo)
        {
            var meshes = textInfo?.meshInfo;
            if (meshes == null || meshes.Length == 0)
                return "0[]";

            var sb = new System.Text.StringBuilder();
            int count = System.Math.Min(meshes.Length, 8);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                var mesh = meshes[i].mesh;
                var bounds = mesh != null ? mesh.bounds : default(Bounds);
                sb.Append(i)
                    .Append(":material=").Append(MaterialName(meshes[i].material))
                    .Append("/vertexCount=").Append(meshes[i].vertexCount)
                    .Append("/meshVerts=").Append(mesh != null ? mesh.vertexCount : 0)
                    .Append("/bounds=").Append(BoundsSummary(bounds))
                    .Append("/firstAlpha=").Append(FirstMeshAlpha(meshes[i]));
            }
            if (meshes.Length > count)
                sb.Append(", ...");
            return meshes.Length + "[" + sb + "]";
        }

        private static string BuildCharacterDetails(TMPro.TMP_TextInfo textInfo)
        {
            if (textInfo == null || textInfo.characterCount == 0)
                return "0[]";

            var sb = new System.Text.StringBuilder();
            int count = System.Math.Min(textInfo.characterCount, 16);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                var info = textInfo.characterInfo[i];
                sb.Append(i)
                    .Append(":U+").Append(((int)info.character).ToString("X4"))
                    .Append("/visible=").Append(info.isVisible)
                    .Append("/mat=").Append(info.materialReferenceIndex)
                    .Append("/font=").Append(FontAssetName(info.fontAsset))
                    .Append("/material=").Append(MaterialName(info.material))
                    .Append("/vertex=").Append(info.vertexIndex)
                    .Append("/point=").Append(info.pointSize.ToString("F1"))
                    .Append("/scale=").Append(info.scale.ToString("F3"))
                    .Append("/origin=").Append(info.origin.ToString("F1"))
                    .Append("/xAdvance=").Append(info.xAdvance.ToString("F1"))
                    .Append("/asc=").Append(info.ascender.ToString("F1"))
                    .Append("/desc=").Append(info.descender.ToString("F1"))
                    .Append("/color=").Append(Color32Summary(info.color))
                    .Append("/bl=").Append(Vector3Summary(info.bottomLeft))
                    .Append("/tr=").Append(Vector3Summary(info.topRight));
            }
            if (textInfo.characterCount > count)
                sb.Append(", ...");
            return textInfo.characterCount + "[" + sb + "]";
        }

        private static string BuildMeshSummary(TMPro.TMP_TextInfo textInfo)
        {
            var meshes = textInfo?.meshInfo;
            if (meshes == null || meshes.Length == 0)
                return "0[]";

            var sb = new System.Text.StringBuilder();
            int count = System.Math.Min(meshes.Length, 8);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                var colors = meshes[i].colors32;
                int alpha = colors != null && colors.Length > 0 ? colors[0].a : -1;
                sb.Append(i)
                    .Append(":verts=").Append(meshes[i].vertexCount)
                    .Append("/colors=").Append(colors != null ? colors.Length : 0)
                    .Append("/a0=").Append(alpha);
            }
            if (meshes.Length > count)
                sb.Append(", ...");
            return meshes.Length + "[" + sb + "]";
        }

        private static string BuildSubMeshSummary(TMPro.TextMeshProUGUI label)
        {
            if (label == null)
                return "0[]";

            var subMeshes = label.GetComponentsInChildren<TMPro.TMP_SubMeshUI>(true);
            if (subMeshes == null || subMeshes.Length == 0)
                return "0[]";

            var sb = new System.Text.StringBuilder();
            int count = System.Math.Min(subMeshes.Length, 8);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                var sub = subMeshes[i];
                var mesh = sub != null ? sub.mesh : null;
                sb.Append(i)
                    .Append(":name=").Append(sub != null ? sub.name : "none")
                    .Append("/path=").Append(sub != null ? BuildTransformPath(sub.transform) : "none")
                    .Append("/active=").Append(sub != null && sub.gameObject.activeInHierarchy)
                    .Append("/enabled=").Append(sub != null && sub.enabled)
                    .Append("/maskable=").Append(sub != null && sub.maskable)
                    .Append("/font=").Append(sub != null ? FontAssetName(sub.fontAsset) : "none")
                    .Append("/sprite=").Append(sub != null && sub.spriteAsset != null ? sub.spriteAsset.name : "none")
                    .Append("/sharedMaterial=").Append(sub != null ? MaterialSummary(sub.sharedMaterial) : "none")
                    .Append("/fallbackMaterial=").Append(sub != null ? MaterialSummary(sub.fallbackMaterial) : "none")
                    .Append("/fallbackSourceMaterial=").Append(sub != null ? MaterialSummary(sub.fallbackSourceMaterial) : "none")
                    .Append("/materialForRendering=").Append(sub != null ? MaterialSummary(sub.materialForRendering) : "none")
                    .Append("/materialProps=").Append(sub != null ? ShaderPropertySummary(sub.materialForRendering) : "none")
                    .Append("/canvasRenderer=").Append(sub != null ? BuildCanvasRendererSummary(sub.canvasRenderer) : "none")
                    .Append("/meshVerts=").Append(mesh != null ? mesh.vertexCount : 0)
                    .Append("/meshBounds=").Append(mesh != null ? BoundsSummary(mesh.bounds) : "none")
                    .Append("/rect=").Append(sub != null ? BuildRectTransformSummary(sub.rectTransform) : "none")
                    .Append("/worldCorners=").Append(sub != null ? BuildWorldCornerSummary(sub.rectTransform) : "none")
                    .Append("/stencilDepth=").Append(sub != null ? BuildStencilDepth(sub.transform) : -1);
            }
            if (subMeshes.Length > count)
                sb.Append(", ...");
            return subMeshes.Length + "[" + sb + "]";
        }

        private static int FirstMeshAlpha(TMPro.TMP_MeshInfo meshInfo)
        {
            var colors = meshInfo.colors32;
            return colors != null && colors.Length > 0 ? colors[0].a : -1;
        }

        private static string MaterialSummary(Material material)
        {
            if (material == null)
                return "none";

            string shader = material.shader != null ? material.shader.name : "none";
            string texture = "none";
            try
            {
                var mainTexture = material.mainTexture;
                texture = mainTexture != null ? mainTexture.name : "none";
            }
            catch
            {
            }
            return MaterialName(material) +
                $"/shader={shader}" +
                $"/queue={material.renderQueue}" +
                $"/mainTex={texture}";
        }

        private static string MaterialName(Material material)
        {
            return material != null ? material.name : "none";
        }

        private static string ShaderPropertySummary(Material material)
        {
            if (material == null)
                return "none";

            var sb = new System.Text.StringBuilder();
            AppendMaterialFloat(sb, material, "_Stencil");
            AppendMaterialFloat(sb, material, "_StencilComp");
            AppendMaterialFloat(sb, material, "_StencilOp");
            AppendMaterialFloat(sb, material, "_StencilWriteMask");
            AppendMaterialFloat(sb, material, "_StencilReadMask");
            AppendMaterialFloat(sb, material, "_ColorMask");
            AppendMaterialFloat(sb, material, "_CullMode");
            AppendMaterialFloat(sb, material, "_GradientScale");
            AppendMaterialFloat(sb, material, "_ScaleX");
            AppendMaterialFloat(sb, material, "_ScaleY");
            AppendMaterialFloat(sb, material, "_TextureWidth");
            AppendMaterialFloat(sb, material, "_TextureHeight");
            AppendMaterialFloat(sb, material, "_FaceDilate");
            AppendMaterialFloat(sb, material, "_OutlineWidth");
            return sb.Length > 0 ? sb.ToString() : "0[]";
        }

        private static void AppendMaterialFloat(
            System.Text.StringBuilder sb,
            Material material,
            string property)
        {
            if (material == null || !material.HasProperty(property))
                return;

            if (sb.Length > 0)
                sb.Append("/");
            float value = 0f;
            try { value = material.GetFloat(property); } catch { }
            sb.Append(property).Append("=").Append(value.ToString("F3"));
        }

        private static string BoundsSummary(Bounds bounds)
        {
            return "center=" + Vector3Summary(bounds.center) +
                "/size=" + Vector3Summary(bounds.size);
        }

        private static string ColorSummary(Color color)
        {
            return $"({color.r:F3},{color.g:F3},{color.b:F3},{color.a:F3})";
        }

        private static string Color32Summary(Color32 color)
        {
            return $"({color.r},{color.g},{color.b},{color.a})";
        }

        private static string Vector2Summary(Vector2 value)
        {
            return $"({value.x:F1},{value.y:F1})";
        }

        private static string Vector3Summary(Vector3 value)
        {
            return $"({value.x:F1},{value.y:F1},{value.z:F1})";
        }

        private static void EnsureFontCache()
        {
            if (_fontSearched)
                return;

            _fontSearched = true;
            _fontAssets = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
            _gameFont = FindLocalizedGameFont(_fontAssets);
        }

        private static TMPro.TMP_FontAsset FindLocalizedGameFont(TMPro.TMP_FontAsset[] all)
        {
            if (all == null || all.Length == 0)
                return null;

            string localizedSample = BuildLocalizedFontSample();
            var oniFont = System.Array.Find(all, f => f.name == "GRAYSTROKE REGULAR SDF");
            if (oniFont != null)
            {
                bool localizedTextAlreadySupported =
                    !RequiresFallbackFont(localizedSample) &&
                    SupportsText(oniFont, localizedSample);
                EnsureGameFontFallbacks(oniFont, all);
                if (DSBLog.Active)
                    DSBLog.Log("Screen", $"Using TMP font '{oniFont.name}' with localized fallback fonts" +
                        $" localizedSupported={localizedTextAlreadySupported}");
                LogFontSetupSummary(oniFont, all);
                return oniFont;
            }

            string fallbackSample = BuildFallbackFontSample(localizedSample);
            var localizedFont = GetCurrentLocalizedFont(all);
            var best = (SupportsText(localizedFont, fallbackSample) ? localizedFont : null)
                ?? System.Array.Find(all, f => IsNotoSans(f) && SupportsText(f, fallbackSample))
                ?? System.Array.Find(all, f => SupportsText(f, fallbackSample))
                ?? (SupportsText(oniFont, localizedSample) ? oniFont : null)
                ?? localizedFont
                ?? System.Array.Find(all, f => IsNotoSans(f))
                ?? oniFont
                ?? all[0];

            if (DSBLog.Active && best != null)
                DSBLog.Log("Screen", $"Using TMP font '{best.name}' for localized UI text");
            LogFontSetupSummary(best, all);
            return best;
        }

        private static void EnsureGameFontFallbacks(
            TMPro.TMP_FontAsset oniFont, TMPro.TMP_FontAsset[] all)
        {
            if (oniFont == null || all == null || all.Length == 0)
                return;

            var fallbackTable = oniFont.fallbackFontAssetTable;
            if (fallbackTable == null)
            {
                fallbackTable = new List<TMPro.TMP_FontAsset>();
                oniFont.fallbackFontAssetTable = fallbackTable;
            }

            string fallbackSample = BuildDynamicTextFallbackSample();
            TryAddRuntimeFallbackFont(oniFont, TMPro.TMP_Settings.defaultFontAsset);
            TryAddRuntimeFallbackFont(oniFont, global::Localization.FontAsset);
            TryAddRuntimeFallbackFont(oniFont, GetCurrentLocalizedFont(all));
            AddTmpSettingsFallbackFonts(oniFont);

            for (int i = 0; i < all.Length; i++)
            {
                var font = all[i];
                if (IsNotoSans(font))
                    TryAddFallbackFont(oniFont, font, fallbackSample);
            }

            for (int i = 0; i < all.Length; i++)
                TryAddFallbackFont(oniFont, all[i], fallbackSample);
        }

        private static void AddTmpSettingsFallbackFonts(TMPro.TMP_FontAsset primaryFont)
        {
            var fallbacks = TMPro.TMP_Settings.fallbackFontAssets;
            if (fallbacks == null)
                return;

            for (int i = 0; i < fallbacks.Count; i++)
                TryAddRuntimeFallbackFont(primaryFont, fallbacks[i]);
        }

        private static void TryAddRuntimeFallbackFont(
            TMPro.TMP_FontAsset primaryFont,
            TMPro.TMP_FontAsset candidate)
        {
            TryAddFallbackFont(primaryFont, candidate, null, false);
        }

        private static void TryAddFallbackFont(
            TMPro.TMP_FontAsset primaryFont,
            TMPro.TMP_FontAsset candidate,
            string fallbackSample)
        {
            TryAddFallbackFont(primaryFont, candidate, fallbackSample, true);
        }

        private static void TryAddFallbackFont(
            TMPro.TMP_FontAsset primaryFont,
            TMPro.TMP_FontAsset candidate,
            string fallbackSample,
            bool requireProbeSupport)
        {
            if (primaryFont == null || candidate == null || ReferenceEquals(primaryFont, candidate))
                return;

            var fallbackTable = primaryFont.fallbackFontAssetTable;
            if (fallbackTable == null || fallbackTable.Contains(candidate))
                return;

            if (requireProbeSupport && !SupportsDynamicFallbackText(candidate, fallbackSample))
                return;

            fallbackTable.Add(candidate);
        }

        private static void LogFontSetupSummary(
            TMPro.TMP_FontAsset primaryFont, TMPro.TMP_FontAsset[] all)
        {
            if (!DSBLog.Active || _fontDiagnosticsLogged)
                return;

            _fontDiagnosticsLogged = true;
            var locale = global::Localization.GetLocale();
            string activeLocale = GetCurrentLocaleCode();
            string localeFontName = locale?.FontName ?? "none";
            string localizationFont = FontAssetName(global::Localization.FontAsset);
            string selectedLocaleFont = FontAssetName(GetCurrentLocalizedFont(all));
            int loadedFontAssets = all != null ? all.Length : 0;
            string fallbackFonts = BuildFallbackFontSummary(primaryFont);
            string tmpSettingsFallbackFonts = BuildTmpSettingsFallbackSummary();
            string sampleNames = BuildSampleNameSupportSummary(primaryFont);

            DSBLog.Log("Font", $"activeLocale={activeLocale}" +
                $" localeFontName={localeFontName}" +
                $" Localization.FontAsset={localizationFont}" +
                $" selectedLocaleFont={selectedLocaleFont}" +
                $" primaryFont={FontAssetName(primaryFont)}" +
                $" loadedFontAssets={loadedFontAssets}" +
                $" tmpSettingsFallbackFonts={tmpSettingsFallbackFonts}" +
                $" fallbackFonts={fallbackFonts}" +
                $" sampleNames={sampleNames}");
        }

        private static string BuildFallbackFontSummary(TMPro.TMP_FontAsset primaryFont)
        {
            var fallbacks = primaryFont?.fallbackFontAssetTable;
            if (fallbacks == null || fallbacks.Count == 0)
                return "0[]";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < fallbacks.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(FontAssetName(fallbacks[i]));
            }

            return fallbacks.Count + "[" + sb + "]";
        }

        private static string BuildTmpSettingsFallbackSummary()
        {
            var fallbacks = TMPro.TMP_Settings.fallbackFontAssets;
            if (fallbacks == null || fallbacks.Count == 0)
                return "0[]";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < fallbacks.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(FontAssetName(fallbacks[i]));
            }

            return fallbacks.Count + "[" + sb + "]";
        }

        private static TMPro.TMP_FontAsset GetFirstTmpSettingsFallbackFont()
        {
            var fallbacks = TMPro.TMP_Settings.fallbackFontAssets;
            return fallbacks != null && fallbacks.Count > 0 ? fallbacks[0] : null;
        }

        private static string BuildSampleNameSupportSummary(TMPro.TMP_FontAsset primaryFont)
        {
            return "EnglishName=" + SupportsText(primaryFont, "Ada") +
                ", CzechName=" + SupportsText(primaryFont, "Ada " + GetCzechNameFallbackProbe()) +
                ", GermanName=" + SupportsText(primaryFont, "Ada " + GetGermanNameFallbackProbe()) +
                ", SpanishName=" + SupportsText(primaryFont, "Ada " + GetSpanishNameFallbackProbe()) +
                ", FrenchName=" + SupportsText(primaryFont, "Ada " + GetFrenchNameFallbackProbe()) +
                ", PortugueseName=" + SupportsText(primaryFont, "Ada " + GetPortugueseNameFallbackProbe()) +
                ", RussianName=" + SupportsText(primaryFont, "Ada " + GetRussianNameFallbackProbe()) +
                ", ChineseName=" + SupportsText(primaryFont, "Ada " + GetChineseNameFallbackProbe()) +
                ", JapaneseName=" + SupportsText(primaryFont, "Ada " + GetJapaneseNameFallbackProbe()) +
                ", KoreanName=" + SupportsText(primaryFont, "Ada " + GetKoreanNameFallbackProbe());
        }

        private static string FontAssetName(TMPro.TMP_FontAsset font)
        {
            return font != null ? font.name : "none";
        }

        private static string BuildLocalizedFontSample()
        {
            return string.Concat(
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.HEADER,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.POPUP_SORTFILTER,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_TASK,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_STRESS,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_HEALTH,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_EQUIPMENT,
                " ",
                "Dupes Sort/Filter Task Stress Health Equipment 0123456789");
        }

        private static string BuildFallbackFontSample(string localizedSample)
        {
            string probe = GetLocaleFallbackProbe();
            return string.IsNullOrEmpty(probe)
                ? localizedSample
                : localizedSample + " " + probe;
        }

        private static string BuildDynamicTextFallbackSample()
        {
            return string.Concat(
                BuildFallbackFontSample(BuildLocalizedFontSample()),
                " ",
                BuildSupportedLocalizationFallbackSample(),
                " ",
                BuildSupportedNameFallbackSample());
        }

        private static string BuildSupportedLocalizationFallbackSample()
        {
            return string.Concat(
                GetLatinExtendedFallbackProbe(),
                " ",
                GetCyrillicFallbackProbe(),
                " ",
                GetChineseFallbackProbe(),
                " ",
                GetJapaneseFallbackProbe(),
                " ",
                GetKoreanFallbackProbe());
        }

        private static string BuildSupportedNameFallbackSample()
        {
            return string.Concat(
                "Ada ",
                GetCzechNameFallbackProbe(),
                " ",
                GetGermanNameFallbackProbe(),
                " ",
                GetSpanishNameFallbackProbe(),
                " ",
                GetFrenchNameFallbackProbe(),
                " ",
                GetPortugueseNameFallbackProbe(),
                " ",
                GetRussianNameFallbackProbe(),
                " ",
                GetChineseNameFallbackProbe(),
                " ",
                GetJapaneseNameFallbackProbe(),
                " ",
                GetKoreanNameFallbackProbe());
        }

        private static string GetLocaleFallbackProbe()
        {
            string code = GetCurrentLocaleCode();
            if (StartsWithLocale(code, "cs"))
                return GetCzechFallbackProbe();
            if (StartsWithLocale(code, "de"))
                return GetGermanFallbackProbe();
            if (StartsWithLocale(code, "es"))
                return GetSpanishFallbackProbe();
            if (StartsWithLocale(code, "fr"))
                return GetFrenchFallbackProbe();
            if (StartsWithLocale(code, "pt"))
                return GetPortugueseFallbackProbe();
            if (StartsWithLocale(code, "ru"))
                return GetCyrillicFallbackProbe();
            if (StartsWithLocale(code, "zh"))
                return GetChineseFallbackProbe();
            if (StartsWithLocale(code, "ja"))
                return GetJapaneseFallbackProbe();
            if (StartsWithLocale(code, "ko"))
                return GetKoreanFallbackProbe();
            return "";
        }

        private static string GetLatinExtendedFallbackProbe()
        {
            return string.Concat(
                GetCzechFallbackProbe(),
                " ",
                GetGermanFallbackProbe(),
                " ",
                GetSpanishFallbackProbe(),
                " ",
                GetFrenchFallbackProbe(),
                " ",
                GetPortugueseFallbackProbe());
        }

        private static string GetCzechFallbackProbe()
        {
            return "\u010c\u010d \u010e\u010f \u011a\u011b \u0147\u0148 " +
                "\u0158\u0159 \u0160\u0161 \u0164\u0165 \u016e\u016f \u017d\u017e";
        }

        private static string GetCzechNameFallbackProbe()
        {
            return "Ji\u0159\u00ed \u0160\u00e1rka Dvo\u0159\u00e1k \u0158\u00edha";
        }

        private static string GetGermanFallbackProbe()
        {
            return "\u00c4\u00d6\u00dc \u00e4\u00f6\u00fc \u00df";
        }

        private static string GetGermanNameFallbackProbe()
        {
            return "J\u00fcrgen M\u00fcller Gro\u00df";
        }

        private static string GetSpanishFallbackProbe()
        {
            return "\u00c1\u00c9\u00cd\u00d3\u00da \u00e1\u00e9\u00ed\u00f3\u00fa " +
                "\u00dc\u00fc \u00d1\u00f1";
        }

        private static string GetSpanishNameFallbackProbe()
        {
            return "Jos\u00e9 Mar\u00eda N\u00fa\u00f1ez";
        }

        private static string GetFrenchFallbackProbe()
        {
            return "\u00c0\u00c2\u00c7\u00c9\u00c8\u00ca\u00cb\u00ce\u00cf\u00d4\u00d9\u00db\u00dc " +
                "\u00e0\u00e2\u00e7\u00e9\u00e8\u00ea\u00eb\u00ee\u00ef\u00f4\u00f9\u00fb\u00fc " +
                "\u00c6\u00e6 \u0152\u0153";
        }

        private static string GetFrenchNameFallbackProbe()
        {
            return "\u00c9lodie Fran\u00e7ois C\u0153ur";
        }

        private static string GetPortugueseFallbackProbe()
        {
            return "\u00c1\u00c2\u00c3\u00c0\u00c7\u00c9\u00ca\u00cd\u00d3\u00d4\u00d5\u00da " +
                "\u00e1\u00e2\u00e3\u00e0\u00e7\u00e9\u00ea\u00ed\u00f3\u00f4\u00f5\u00fa";
        }

        private static string GetPortugueseNameFallbackProbe()
        {
            return "Jo\u00e3o Andr\u00e9 Gon\u00e7alves";
        }

        private static string GetCyrillicFallbackProbe()
        {
            return "\u0414\u0443\u043f\u043b\u0438\u043a\u0430\u043d\u0442\u044b " +
                "\u0421\u043e\u0440\u0442\u0438\u0440\u043e\u0432\u043a\u0430 " +
                "\u0424\u0438\u043b\u044c\u0442\u0440";
        }

        private static string GetRussianNameFallbackProbe()
        {
            return "\u0418\u0432\u0430\u043d \u041e\u043b\u044c\u0433\u0430 \u0410\u0434\u0430";
        }

        private static string GetChineseFallbackProbe()
        {
            return "\u590d\u5236\u4eba \u6392\u5e8f \u7b5b\u9009";
        }

        private static string GetChineseNameFallbackProbe()
        {
            return "\u738b \u5c0f\u660e \u674e\u5a1c \u5f20\u4f1f";
        }

        private static string GetJapaneseFallbackProbe()
        {
            return "\u8907\u88fd\u4eba \u4e26\u3079\u66ff\u3048 \u30d5\u30a3\u30eb\u30bf\u30fc";
        }

        private static string GetJapaneseNameFallbackProbe()
        {
            return "\u5c71\u7530\u592a\u90ce \u82b1\u5b50 \u3055\u304f\u3089 \u30ab\u30ca";
        }

        private static string GetKoreanFallbackProbe()
        {
            return "\ubcf5\uc81c\uccb4 \uc815\ub82c \ud544\ud130";
        }

        private static string GetKoreanNameFallbackProbe()
        {
            return "\uae40\ubbfc\uc900 \uc11c\uc5f0 \uc9c0\ud6c8";
        }

        private static bool RequiresFallbackFont(string localizedSample)
        {
            if (ContainsNonLatinLetters(localizedSample))
                return true;

            string code = GetCurrentLocaleCode();
            return StartsWithLocale(code, "cs")
                || StartsWithLocale(code, "de")
                || StartsWithLocale(code, "es")
                || StartsWithLocale(code, "fr")
                || StartsWithLocale(code, "pt")
                || StartsWithLocale(code, "ru")
                || StartsWithLocale(code, "zh")
                || StartsWithLocale(code, "ja")
                || StartsWithLocale(code, "ko")
                || StartsWithLocale(code, "th")
                || StartsWithLocale(code, "ar")
                || StartsWithLocale(code, "he");
        }

        private static bool ContainsNonLatinLetters(string localizedSample)
        {
            if (string.IsNullOrEmpty(localizedSample))
                return false;

            foreach (char c in localizedSample)
            {
                if (c > 0x7F && char.IsLetter(c))
                    return true;
            }
            return false;
        }

        private static TMPro.TMP_FontAsset GetCurrentLocalizedFont(TMPro.TMP_FontAsset[] all)
        {
            var font = FindLocaleFontByName(all) ?? global::Localization.FontAsset;
            return font;
        }

        private static TMPro.TMP_FontAsset FindLocaleFontByName(TMPro.TMP_FontAsset[] all)
        {
            string fontName = global::Localization.GetLocale()?.FontName;
            if (all == null || string.IsNullOrEmpty(fontName))
                return null;

            return System.Array.Find(all, f => FontNameMatches(f, fontName));
        }

        private static bool FontNameMatches(TMPro.TMP_FontAsset font, string fontName)
        {
            if (font == null || string.IsNullOrEmpty(fontName))
                return false;

            return string.Equals(font.name, fontName, System.StringComparison.OrdinalIgnoreCase)
                || font.name.StartsWith(fontName, System.StringComparison.OrdinalIgnoreCase)
                || font.name.IndexOf(fontName, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetCurrentLocaleCode()
        {
            string code = global::Localization.GetLocale()?.Code;
            if (string.IsNullOrEmpty(code))
                code = global::Localization.GetCurrentLanguageCode();
            return code ?? "";
        }

        private static bool StartsWithLocale(string code, string prefix)
        {
            return !string.IsNullOrEmpty(code)
                && code.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNotoSans(TMPro.TMP_FontAsset font)
        {
            return font != null && font.name.Contains("NotoSans");
        }

        private static bool SupportsText(TMPro.TMP_FontAsset font, string text)
        {
            return SupportsText(font, text, true);
        }

        private static bool SupportsText(TMPro.TMP_FontAsset font, string text, bool searchFallbacks)
        {
            if (font == null || string.IsNullOrEmpty(text))
                return false;

            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                    continue;
                if (!font.HasCharacter(c, searchFallbacks, false))
                    return false;
            }
            return true;
        }

        private static bool SupportsDynamicFallbackText(
            TMPro.TMP_FontAsset font, string fallbackSample)
        {
            return SupportsText(font, fallbackSample)
                || SupportsText(font, GetCzechFallbackProbe())
                || SupportsText(font, GetCzechNameFallbackProbe())
                || SupportsText(font, GetGermanFallbackProbe())
                || SupportsText(font, GetGermanNameFallbackProbe())
                || SupportsText(font, GetSpanishFallbackProbe())
                || SupportsText(font, GetSpanishNameFallbackProbe())
                || SupportsText(font, GetFrenchFallbackProbe())
                || SupportsText(font, GetFrenchNameFallbackProbe())
                || SupportsText(font, GetPortugueseFallbackProbe())
                || SupportsText(font, GetPortugueseNameFallbackProbe())
                || SupportsText(font, GetCyrillicFallbackProbe())
                || SupportsText(font, GetRussianNameFallbackProbe())
                || SupportsText(font, GetChineseFallbackProbe())
                || SupportsText(font, GetChineseNameFallbackProbe())
                || SupportsText(font, GetJapaneseFallbackProbe())
                || SupportsText(font, GetJapaneseNameFallbackProbe())
                || SupportsText(font, GetKoreanFallbackProbe())
                || SupportsText(font, GetKoreanNameFallbackProbe());
        }

        private const int MIN_CARD_SIZE = 16;
        private const float UPDATE_INTERVAL = 0.25f;
        private const string PX = "DSB_PosX";
        private const string PY = "DSB_PosY";
        private const string PC = "DSB_Collapsed";
        private const string PW = "DSB_BoxW";
        private const string PH = "DSB_BoxH";
        private const string PVER = "DSB_PosVer";
        private const string PCW = "DSB_CanW";
        private const string PCH = "DSB_CanH";

        private void Start()
        {
            Instance = this;
            DSBLog.Log("Screen", "Start() — building UI");
            BuildUI();
            LoadState();
            DSBLog.Log("Screen", $"Start() complete — canvas={canvasRT.rect.size}" +
                $" pos={barPanel.anchoredPosition} collapsed={isCollapsed}" +
                $" size={lastComputedSize} scale={canvasScaler.scaleFactor}");
        }

        private void Update()
        {
            if (Game.Instance == null) return;

            // During resize drag, refresh every frame for smooth card reflow
            if (isDraggingResize)
            {
                UpdateGridLayout(lastDupeCount > 0 ? lastDupeCount : DupeStatusTracker.Snapshots.Count);
                for (int i = 0; i < widgets.Count && i < DupeStatusTracker.Snapshots.Count; i++)
                    widgets[i].SetSnapshot(DupeStatusTracker.Snapshots[i], lastComputedSize);
            }

            updateTimer -= Time.unscaledDeltaTime;
            if (updateTimer <= 0f)
            {
                updateTimer = UPDATE_INTERVAL;
                ApplyGameUIScale();
                HandleActiveWorldChanged(CurrentActiveWorldId());
                DupeStatusTracker.Update();
                RefreshWidgets();

                if (needsPostLayoutClamp && barPanel.rect.size.x > 0)
                {
                    needsPostLayoutClamp = false;
                    var before = barPanel.anchoredPosition;
                    ClampPanelPosition();
                    var after = barPanel.anchoredPosition;
                    if (before != after)
                        DSBLog.Log("Load", $"Post-layout clamp: ({before.x:F1}, {before.y:F1}) -> ({after.x:F1}, {after.y:F1})");
                }

                if (firstUpdate)
                {
                    firstUpdate = false;
                    var snaps = DupeStatusTracker.Snapshots;
                    DSBLog.Log("Screen", $"First tick — dupes={snaps.Count}" +
                        $" widgets={widgets.Count} canvas={canvasRT.rect.size}" +
                        $" pos={barPanel.anchoredPosition} scale={canvasScaler.scaleFactor}" +
                        $" collapsed={isCollapsed}" +
                        $" alertsOnly={SortFilterPopup.AlertsOnly}" +
                        $" stressedOnly={SortFilterPopup.StressedOnly}" +
                        $" hiddenDupes={SortFilterPopup.HiddenDupes.Count}" +
                        $" hiddenRoles={SortFilterPopup.HiddenRoles.Count}");
                    if (DSBLog.Active)
                        LogVisibilityDiagnostic();
                }
            }
        }

        private void OnDestroy()
        {
            Instance = null;
            DupeTooltip.Cleanup();
            SortFilterPopup.Cleanup();
            PortraitCompositor.ClearCaches();
            SaveState();
            Core.DSBLog.Close();
        }

        // canvasRT.rect reports Screen.size in ConstantPixelSize mode;
        // the visible area in canvas coordinates is Screen.size / scaleFactor.
        private Vector2 EffectiveCanvasSize()
        {
            float s = canvasScaler != null ? canvasScaler.scaleFactor : 1f;
            return new Vector2(Screen.width / s, Screen.height / s);
        }

        private void ApplyGameUIScale()
        {
            if (gameCanvasScaler == null)
                gameCanvasScaler = FindSceneObject<KCanvasScaler>();
            float scale = gameCanvasScaler != null
                ? gameCanvasScaler.GetCanvasScale()
                : 1f;
            if (scale != lastUIScale)
            {
                canvasScaler.scaleFactor = scale;
                lastUIScale = scale;
            }
        }

        private static T FindSceneObject<T>() where T : UnityEngine.Object
        {
            var inactiveType = Type.GetType("UnityEngine.FindObjectsInactive, UnityEngine.CoreModule");
            var modernMethod = inactiveType != null
                ? typeof(UnityEngine.Object).GetMethod(
                    "FindFirstObjectByType",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { inactiveType },
                    null)
                : null;
            if (modernMethod != null)
            {
                var exclude = Enum.Parse(inactiveType, "Exclude");
                return modernMethod.MakeGenericMethod(typeof(T))
                    .Invoke(null, new object[] { exclude }) as T;
            }

            var legacyMethod = typeof(UnityEngine.Object).GetMethod(
                "FindObjectOfType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);
            return legacyMethod?.MakeGenericMethod(typeof(T)).Invoke(null, null) as T;
        }

        private static T[] FindSceneObjects<T>() where T : UnityEngine.Object
        {
            var inactiveType = Type.GetType("UnityEngine.FindObjectsInactive, UnityEngine.CoreModule");
            var sortModeType = Type.GetType("UnityEngine.FindObjectsSortMode, UnityEngine.CoreModule");
            var modernMethod = inactiveType != null && sortModeType != null
                ? typeof(UnityEngine.Object).GetMethod(
                    "FindObjectsByType",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { inactiveType, sortModeType },
                    null)
                : null;
            if (modernMethod != null)
            {
                var exclude = Enum.Parse(inactiveType, "Exclude");
                var none = Enum.Parse(sortModeType, "None");
                return modernMethod.MakeGenericMethod(typeof(T))
                    .Invoke(null, new object[] { exclude, none }) as T[] ?? new T[0];
            }

            var legacyMethod = typeof(UnityEngine.Object).GetMethod(
                "FindObjectsOfType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);
            return legacyMethod?.MakeGenericMethod(typeof(T)).Invoke(null, null) as T[] ?? new T[0];
        }

        // ── Widget Sync ─────────────────────────────────────────

        private void RefreshWidgets()
        {
            var opts = StatusBarOptions.Instance;

            // Live-update opacity when option changes
            if (panelImage != null)
            {
                float alpha = opts.BarOpacity / 100f;
                var c = panelImage.color;
                if (!Mathf.Approximately(c.a, alpha))
                    panelImage.color = new Color(c.r, c.g, c.b, alpha);
            }

            var snaps = DupeStatusTracker.Snapshots;

            // Detect option change for PortraitSize (auto mode only)
            int configured = opts.PortraitSize;
            if (configured != lastConfiguredSize)
            {
                lastConfiguredSize = configured;
                forceRefresh = true;
            }

            if (snaps.Count != lastDupeCount)
            {
                lastDupeCount = snaps.Count;
                forceRefresh = true;
            }
            if (forceRefresh)
                forceRefresh = false;

            UpdateGridLayout(snaps.Count);
            int size = lastComputedSize;

            // Periodic size diagnostic (piggybacks on tracker's 5s Tick interval)
            if (DSBLog.Active && snaps.Count > 0)
            {
                float now = Time.unscaledTime;
                if ((int)(now / 5f) != (int)((now - UPDATE_INTERVAL) / 5f))
                    DSBLog.Log("Screen", $"size={size} box={barWidthPx:F0}x{barHeightPx:F0}");
            }

            // Add widgets if needed
            while (widgets.Count < snaps.Count)
            {
                var w = DupePortraitWidget.Create(contentRT, size);
                widgets.Add(w);
            }

            // Remove excess widgets
            while (widgets.Count > snaps.Count)
            {
                int last = widgets.Count - 1;
                var w = widgets[last];
                if (last < snaps.Count)
                    API.Internal.AlertRegistry.FireWidgetDestroyed(
                        new API.Experimental.WidgetEvent(w, snaps[last]));
                Destroy(w.gameObject);
                widgets.RemoveAt(last);
            }

            // Update each
            int prevCount = lastDupeCount < 0 ? 0 : lastDupeCount;
            for (int i = 0; i < snaps.Count; i++)
            {
                widgets[i].SetSnapshot(snaps[i], size);
                if (i >= prevCount)
                    API.Internal.AlertRegistry.FireWidgetCreated(
                        new API.Experimental.WidgetEvent(widgets[i], snaps[i]));
            }

            // Evict cached portrait textures for dead/transferred dupes
            PortraitCompositor.EvictStale(
                System.Linq.Enumerable.Select(snaps,
                    s => s.Identity != null ? s.Identity.GetInstanceID() : 0));
        }

        internal void ResetToDefaults()
        {
            isCollapsed = false;
            bool hasActiveDupes = DupeStatusTracker.ActiveWorldDupeCount > 0;
            int visibleDupeCount = DupeStatusTracker.Snapshots.Count > 0
                ? DupeStatusTracker.Snapshots.Count
                : Mathf.Max(0, lastDupeCount);
            scrollViewGO.SetActive(visibleDupeCount > 0);
            if (hasActiveDupes)
                UpdateGridLayout(visibleDupeCount);
            else
                HideFilterButton();
            if (collapseLabel != null) collapseLabel.text = "\u2212";
            if (barPanel != null && canvasRT != null)
            {
                float cx = EffectiveCanvasSize().x * 0.5f;
                barPanel.anchoredPosition = new Vector2(cx, -5f);
            }
            barWidthPx = -1f;
            barHeightPx = -1f;
            forceRefresh = true;
        }

        // ── Persistence ─────────────────────────────────────────

        private void SaveState()
        {
            if (barPanel == null) return;
            PlayerPrefs.SetFloat(PX, barPanel.anchoredPosition.x);
            PlayerPrefs.SetFloat(PY, barPanel.anchoredPosition.y);
            PlayerPrefs.SetInt(PC, isCollapsed ? 1 : 0);
            var ecs = EffectiveCanvasSize();
            PlayerPrefs.SetFloat(PCW, ecs.x);
            PlayerPrefs.SetFloat(PCH, ecs.y);
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            if (barPanel == null) return;
            lastConfiguredSize = StatusBarOptions.Instance.PortraitSize;
            bool isLegacy = !PlayerPrefs.HasKey(PVER);
            var ecv = EffectiveCanvasSize();
            float cvW = ecv.x;
            float cvH = ecv.y;

            if (PlayerPrefs.HasKey(PX))
            {
                float x = PlayerPrefs.GetFloat(PX, 0);
                float y = PlayerPrefs.GetFloat(PY, -5);

                DSBLog.Log("Load", $"Raw saved pos=({x:F1}, {y:F1}) canvas=({cvW:F0}, {cvH:F0})" +
                    $" legacy={isLegacy} panelRect={barPanel.rect.size}");

                if (isLegacy)
                {
                    // Migrate: old x was offset from center, new x is from left edge
                    x = cvW * 0.5f + x;
                    DSBLog.Log("Load", $"Legacy migration: x adjusted to {x:F1}");
                    PlayerPrefs.SetInt(PVER, 2);
                    PlayerPrefs.Save();
                }

                // Proportional remap if canvas dimensions changed (resolution or UI scale)
                if (PlayerPrefs.HasKey(PCW))
                {
                    float savedW = PlayerPrefs.GetFloat(PCW);
                    float savedH = PlayerPrefs.GetFloat(PCH);
                    if (savedW > 0 && savedH > 0
                        && (Mathf.Abs(savedW - cvW) > 10f || Mathf.Abs(savedH - cvH) > 10f))
                    {
                        x = x * (cvW / savedW);
                        y = y * (cvH / savedH);
                        DSBLog.Log("Load", $"Canvas changed ({savedW:F0}x{savedH:F0} -> {cvW:F0}x{cvH:F0}), remapped to ({x:F1}, {y:F1})");
                    }
                }

                barPanel.anchoredPosition = new Vector2(x, y);
                ClampPanelPosition();
                needsPostLayoutClamp = true;

                var clamped = barPanel.anchoredPosition;
                if (clamped.x != x || clamped.y != y)
                    DSBLog.Log("Load", $"Clamped pos=({x:F1}, {y:F1}) -> ({clamped.x:F1}, {clamped.y:F1})");
            }
            else
            {
                // No saved position: center the panel
                barPanel.anchoredPosition = new Vector2(cvW * 0.5f, -5f);
                needsPostLayoutClamp = true;
                DSBLog.Log("Load", "No saved position, centering");
            }

            isCollapsed = PlayerPrefs.GetInt(PC, 0) == 1;
            scrollViewGO.SetActive(!isCollapsed);
            if (filterBtnGO != null) filterBtnGO.SetActive(!isCollapsed);
            if (isCollapsed && collapseLabel != null)
                collapseLabel.text = "+";

            // Load new box-mode keys
            if (PlayerPrefs.HasKey(PW))
                barWidthPx = PlayerPrefs.GetFloat(PW, -1f);
            if (PlayerPrefs.HasKey(PH))
                barHeightPx = PlayerPrefs.GetFloat(PH, -1f);

            DSBLog.Log("Load", $"Final state: pos={barPanel.anchoredPosition}" +
                $" collapsed={isCollapsed} box={barWidthPx:F0}x{barHeightPx:F0}");

            // Migrate legacy keys (one-time cleanup)
            if (PlayerPrefs.HasKey("DSB_BarWidth") || PlayerPrefs.HasKey("DSB_BarHeight")
                || PlayerPrefs.HasKey("DSB_PortSize"))
            {
                PlayerPrefs.DeleteKey("DSB_BarWidth");
                PlayerPrefs.DeleteKey("DSB_BarHeight");
                PlayerPrefs.DeleteKey("DSB_PortSize");
                PlayerPrefs.Save();
            }
        }
    }
}
