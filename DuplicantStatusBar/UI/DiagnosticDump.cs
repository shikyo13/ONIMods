using System;
using System.Collections.Generic;
using System.IO;
using Database;
using UnityEngine;

namespace DuplicantStatusBar.UI
{
    /// <summary>
    /// One-shot diagnostic dump that logs KAnim transform data, per-dupe symbol
    /// metrics, and exports layer PNGs. Runs once per session on the first
    /// ComposePortrait call. All output prefixed [DSB-Diag] for easy grep.
    /// </summary>
    internal static class DiagnosticDump
    {
        private static bool hasRun;
        private const int PORTRAIT_Y_SHIFT = -8; // matches PortraitCompositor

        public static void RunOnce(MinionIdentity identity)
        {
            if (hasRun) return;
            hasRun = true;
            try
            {
                Log("========== DSB DIAGNOSTIC DUMP ==========");
                DumpAll(identity);
                Log("========== END DIAGNOSTIC DUMP ==========");
            }
            catch (Exception ex) { Log($"FATAL: {ex}"); }
        }

        private static void Log(string msg) => Debug.Log($"[DSB-Diag] {msg}");

        private static void DumpAll(MinionIdentity dupe)
        {
            DumpBodyAnimElements();
            DumpHeadSwapTransforms();
            DumpPerDupeSymbols();
            DumpSpriteExtraction(dupe);
            DumpCanvasPositioning(dupe);
        }

        #region Section 1: Body Animation Frame Elements

        private static void DumpBodyAnimElements()
        {
            try
            {
                Log("========== SECTION 1: BODY ANIM FRAME ELEMENTS ==========");

                KAnimFile animFile = null;
                string fileName = "anim_idles_default_kanim";
                try { animFile = Assets.GetAnim(fileName); } catch { }

                if (animFile == null)
                {
                    fileName = "body_comp_default_kanim";
                    Log("anim_idles_default_kanim not found, trying " + fileName);
                    try { animFile = Assets.GetAnim(fileName); } catch { }
                }

                if (animFile == null) { Log("ERROR: No body anim file found!"); return; }

                Log($"Loaded: {fileName}");
                var data = animFile.GetData();
                if (data == null) { Log("ERROR: GetData() returned null"); return; }

                var batchData = KAnimBatchManager.Instance()?.GetBatchGroupData(data.animBatchTag);
                if (batchData == null) { Log("ERROR: batchData null"); return; }

                // List all anim names
                var names = new List<string>();
                int uiIdleIdx = -1;
                var uiIdleHash = new HashedString("ui_idle");

                for (int i = 0; i < data.animCount; i++)
                {
                    var anim = data.GetAnim(i);
                    names.Add(anim.name ?? $"anim#{anim.hash.HashValue:X8}");
                    if (anim.hash == uiIdleHash) uiIdleIdx = i;
                }
                Log($"Anims found ({data.animCount}): {string.Join(", ", names)}");

                // Dump frame elements for ui_idle (or first anim as fallback)
                int dumpIdx = uiIdleIdx >= 0 ? uiIdleIdx : 0;
                if (dumpIdx < data.animCount)
                {
                    var anim = data.GetAnim(dumpIdx);
                    if (anim.TryGetFrame(data.build.batchTag, 0, out var frame))
                    {
                        string animName = uiIdleIdx >= 0 ? "ui_idle" : names[0];
                        Log($"--- {animName} frame 0 ({frame.numElements} elements) ---");
                        DumpFrameElements(batchData, frame.firstElementIdx, frame.numElements);
                    }
                    else
                    {
                        Log($"TryGetFrame failed for {names[dumpIdx]}");
                    }
                }

                if (uiIdleIdx < 0)
                    Log("WARNING: ui_idle not found in this file!");
            }
            catch (Exception ex) { Log($"Section 1 error: {ex}"); }
        }

        #endregion

        #region Section 2: Head Swap Transforms

        private static void DumpHeadSwapTransforms()
        {
            try
            {
                Log("========== SECTION 2: HEAD SWAP TRANSFORMS ==========");

                KAnimFile swapFile;
                try { swapFile = Assets.GetAnim("head_master_swap_kanim"); }
                catch { Log("ERROR: head_master_swap_kanim not found"); return; }
                if (swapFile == null) { Log("ERROR: head_master_swap_kanim null"); return; }

                var data = swapFile.GetData();
                if (data == null) { Log("ERROR: GetData() null"); return; }

                var batchData = KAnimBatchManager.Instance()?.GetBatchGroupData(data.animBatchTag);
                if (batchData == null) { Log("ERROR: batchData null"); return; }

                var faces = Db.Get()?.Faces;
                if (faces == null) { Log("ERROR: Faces null"); return; }

                var targetFaces = new[]
                {
                    ("Neutral", faces.Neutral),
                    ("Happy", faces.Happy),
                    ("Angry", faces.Angry),
                    ("Dead", faces.Dead)
                };

                for (int i = 0; i < data.animCount; i++)
                {
                    var anim = data.GetAnim(i);
                    string matchedFace = null;
                    foreach (var (name, face) in targetFaces)
                    {
                        if (face != null && anim.hash == face.hash)
                        {
                            matchedFace = name;
                            break;
                        }
                    }
                    if (matchedFace == null) continue;

                    if (!anim.TryGetFrame(data.build.batchTag, 0, out var frame)) continue;

                    Log($"--- {matchedFace} ({frame.numElements} elements) ---");
                    DumpFrameElements(batchData, frame.firstElementIdx, frame.numElements);
                }
            }
            catch (Exception ex) { Log($"Section 2 error: {ex}"); }
        }

        #endregion

        #region Section 3: Per-Dupe Symbol Data

        private static void DumpPerDupeSymbols()
        {
            try
            {
                Log("========== SECTION 3: PER-DUPE SYMBOL DATA ==========");

                var dupes = Components.LiveMinionIdentities.Items;
                if (dupes == null || dupes.Count == 0) { Log("No live dupes"); return; }

                var slots = Db.Get().AccessorySlots;
                int count = Mathf.Min(3, dupes.Count);

                for (int d = 0; d < count; d++)
                {
                    var identity = dupes[d];
                    string dupeName = identity.name ?? $"Dupe{d}";
                    Log($"========== DUPE {d + 1}: {dupeName} ==========");

                    var acc = identity.GetComponent<Accessorizer>();
                    if (acc == null) { Log("  No Accessorizer"); continue; }

                    DumpSlot(acc, slots.HeadShape, "HEAD");
                    DumpSlot(acc, slots.Eyes, "EYES");
                    DumpSlot(acc, slots.Mouth, "MOUTH");
                    DumpSlot(acc, slots.Hair, "HAIR");
                }
            }
            catch (Exception ex) { Log($"Section 3 error: {ex}"); }
        }

        private static void DumpSlot(Accessorizer acc, AccessorySlot slot, string label)
        {
            try
            {
                var accessory = acc.GetAccessory(slot);
                if (accessory == null) { Log($"  {label}: null"); return; }

                var sym = accessory.symbol;
                int frameCount = sym.frameLookup?.Length ?? 0;
                string buildName = sym.build?.name ?? "?";
                Log($"  {label}: build={buildName} hash={sym.hash.HashValue:X8} frames={frameCount}");

                if (frameCount > 0)
                {
                    var f = sym.GetFrame(0);
                    Log($"    f0 bbox=({f.bboxMin.x:F1},{f.bboxMin.y:F1})\u2192({f.bboxMax.x:F1},{f.bboxMax.y:F1})" +
                        $" uv=({f.uvMin.x:F3},{f.uvMin.y:F3})\u2192({f.uvMax.x:F3},{f.uvMax.y:F3})");

                    try
                    {
                        var atlas = sym.build.GetTexture(0);
                        if (atlas != null)
                        {
                            int pxW = (int)(atlas.width * Mathf.Abs(f.uvMax.x - f.uvMin.x));
                            int pxH = (int)(atlas.height * Mathf.Abs(f.uvMax.y - f.uvMin.y));
                            Log($"    atlas={atlas.name} ({atlas.width}x{atlas.height}) px={pxW}x{pxH}");
                        }
                    }
                    catch (Exception ex) { Log($"    atlas error: {ex.Message}"); }
                }
            }
            catch (Exception ex) { Log($"  {label} error: {ex.Message}"); }
        }

        #endregion

        #region Section 4: Sprite Extraction Verification

        private static void DumpSpriteExtraction(MinionIdentity dupe)
        {
            try
            {
                Log("========== SECTION 4: SPRITE EXTRACTION ==========");

                var acc = dupe.GetComponent<Accessorizer>();
                if (acc == null) { Log("No Accessorizer"); return; }

                var slots = Db.Get().AccessorySlots;

                var headAcc = acc.GetAccessory(slots.HeadShape);
                if (headAcc != null) DumpSpriteFor("head", headAcc.symbol, 0);

                var eyeAcc = acc.GetAccessory(slots.Eyes);
                if (eyeAcc != null) DumpSpriteFor("eyes", eyeAcc.symbol, 0);

                var mouthAcc = acc.GetAccessory(slots.Mouth);
                if (mouthAcc != null) DumpSpriteFor("mouth", mouthAcc.symbol, 22);
            }
            catch (Exception ex) { Log($"Section 4 error: {ex}"); }
        }

        private static void DumpSpriteFor(string label, KAnim.Build.Symbol symbol, int frame)
        {
            try
            {
                var sprite = PortraitCompositor.GetSpriteFromSymbol(symbol, frame);
                if (sprite == null) { Log($"  {label}: GetSpriteFromSymbol returned null"); return; }

                var r = sprite.textureRect;
                Log($"  {label}: textureRect=({r.x:F0},{r.y:F0},{r.width:F0},{r.height:F0})" +
                    $" ppu={sprite.pixelsPerUnit:F0}");
                UnityEngine.Object.Destroy(sprite);
            }
            catch (Exception ex) { Log($"  {label} sprite error: {ex.Message}"); }
        }

        #endregion

        #region Section 5: Canvas Positioning + PNG Export

        private static void DumpCanvasPositioning(MinionIdentity dupe)
        {
            try
            {
                Log("========== SECTION 5: CANVAS POSITIONING + PNG ==========");

                var acc = dupe.GetComponent<Accessorizer>();
                if (acc == null) return;

                var slots = Db.Get().AccessorySlots;
                const int SIZE = 125;

                // Log centering math for each layer
                LogLayerPosition("head", acc.GetAccessory(slots.HeadShape)?.symbol,
                    0, 0, PORTRAIT_Y_SHIFT, SIZE);
                LogLayerPosition("eyes", acc.GetAccessory(slots.Eyes)?.symbol,
                    0, 8, PORTRAIT_Y_SHIFT, SIZE);
                LogLayerPosition("mouth", acc.GetAccessory(slots.Mouth)?.symbol,
                    22, 10, -12 + PORTRAIT_Y_SHIFT, SIZE);

                // PNG export
                ExportPngs(dupe, acc, slots);
            }
            catch (Exception ex) { Log($"Section 5 error: {ex}"); }
        }

        private static void LogLayerPosition(string label, KAnim.Build.Symbol sym,
            int frame, int xOff, int yOff, int canvasSize)
        {
            if (sym == null) return;
            try
            {
                var sprite = PortraitCompositor.GetSpriteFromSymbol(sym, frame);
                if (sprite == null) return;

                int w = (int)sprite.textureRect.width;
                int h = (int)sprite.textureRect.height;
                int xStart = (canvasSize / 2) - (w / 2) + xOff;
                int yStart = (canvasSize / 2) - (h / 2) + yOff;
                Log($"  CANVAS: {label}@({xStart},{yStart}) sprite={w}x{h} offset=({xOff},{yOff})");
                UnityEngine.Object.Destroy(sprite);
            }
            catch (Exception ex) { Log($"  {label} position error: {ex.Message}"); }
        }

        private static void ExportPngs(MinionIdentity dupe, Accessorizer acc, AccessorySlots slots)
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "DSB_Diag");
                Directory.CreateDirectory(dir);
                Log($"PNGs saving to: {dir}");

                // Individual layer sprites
                SaveSymbolPng(acc.GetAccessory(slots.HeadShape)?.symbol, 0, dir, "head.png");
                SaveSymbolPng(acc.GetAccessory(slots.Eyes)?.symbol, 0, dir, "eyes.png");
                SaveSymbolPng(acc.GetAccessory(slots.Mouth)?.symbol, 22, dir, "mouth.png");

                // Composited portrait
                var portrait = PortraitCompositor.ComposePortrait(dupe);
                if (portrait != null && portrait.texture != null)
                {
                    try
                    {
                        var bytes = portrait.texture.EncodeToPNG();
                        File.WriteAllBytes(Path.Combine(dir, "composited.png"), bytes);
                        Log("  Saved composited.png");
                    }
                    catch
                    {
                        // Texture may not be readable directly; use RenderTexture readback
                        var readable = ReadbackTexture(portrait.texture);
                        if (readable != null)
                        {
                            File.WriteAllBytes(Path.Combine(dir, "composited.png"), readable.EncodeToPNG());
                            UnityEngine.Object.Destroy(readable);
                            Log("  Saved composited.png (via readback)");
                        }
                    }
                    UnityEngine.Object.Destroy(portrait.texture);
                    UnityEngine.Object.Destroy(portrait);
                }
            }
            catch (Exception ex) { Log($"  PNG export error: {ex}"); }
        }

        private static void SaveSymbolPng(KAnim.Build.Symbol symbol, int frame,
            string dir, string filename)
        {
            if (symbol == null) return;
            try
            {
                var sprite = PortraitCompositor.GetSpriteFromSymbol(symbol, frame);
                if (sprite == null) return;

                var r = sprite.textureRect;
                var readable = ReadbackTexture(sprite.texture);
                if (readable != null)
                {
                    var cropped = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
                    var pixels = readable.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                    cropped.SetPixels(pixels);
                    cropped.Apply();
                    File.WriteAllBytes(Path.Combine(dir, filename), cropped.EncodeToPNG());
                    Log($"  Saved {filename} ({(int)r.width}x{(int)r.height})");
                    UnityEngine.Object.Destroy(cropped);
                    UnityEngine.Object.Destroy(readable);
                }
                UnityEngine.Object.Destroy(sprite);
            }
            catch (Exception ex) { Log($"  {filename} error: {ex.Message}"); }
        }

        #endregion

        #region Helpers

        private static void DumpFrameElements(KBatchGroupData batchData,
            int firstIdx, int count)
        {
            for (int j = 0; j < count; j++)
            {
                try
                {
                    var elem = batchData.GetFrameElement(firstIdx + j);
                    string symName = HashCache.Get().Get(elem.symbol);
                    if (string.IsNullOrEmpty(symName))
                        symName = $"#{elem.symbol.HashValue:X8}";

                    var t = elem.transform;
                    Log($"  [{j}] {symName,-20} frame={elem.frame} alpha={elem.multAlpha:F1}" +
                        $"  tx=[{t.m00:F2} {t.m01:F2} {t.m02:F2} | {t.m10:F2} {t.m11:F2} {t.m12:F2}]");
                }
                catch (Exception ex) { Log($"  [{j}] ERROR: {ex.Message}"); }
            }
        }

        private static Texture2D ReadbackTexture(Texture2D source)
        {
            if (source == null) return null;
            try
            {
                var rt = RenderTexture.GetTemporary(
                    source.width, source.height, 0,
                    RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(source, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                readable.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                return readable;
            }
            catch { return null; }
        }

        #endregion
    }
}
