using System.Collections.Generic;
using System.Linq;
using Database;
using UnityEngine;

namespace DuplicantStatusBar.UI
{
    /// <summary>
    /// Composites dupe accessory symbols from KAnim texture atlases into a
    /// static Texture2D/Sprite. Bypasses the KAnim batch rendering pipeline
    /// entirely — works in any Canvas render mode.
    ///
    /// Eyes and mouth use edge-anchored positioning: eye bottom edge and mouth
    /// top edge are fixed at absolute y-coordinates, so the gap is constant
    /// regardless of sprite dimensions. Hair and hats use center+offset with
    /// pivot from their KAnim bbox data for vertical alignment.
    /// </summary>
    public static class PortraitCompositor
    {
        /// <summary>Shift all layers down to give tall hats more headroom above center.</summary>
        private const int PORTRAIT_Y_SHIFT = -8;

        // Portrait feature offsets (pixels relative to canvas center + PORTRAIT_Y_SHIFT)
        private const int EYE_X = 4;
        private const int EYE_Y = -16;
        private const int MOUTH_X = 6;
        private const int MOUTH_Y = -12;
        private const int HAIR_X = 8;
        private const int HAIR_Y = 28;
        private const int HATHAIR_X = 8;
        private const int HATHAIR_Y = 30;
        private const int HAT_X = 6;
        private const int HAT_Y = 30;
        private const float EYE_ROTATION = 3f; // degrees CCW to level 3/4 perspective tilt

        // Cache readable copies of GPU textures to avoid repeated readbacks
        private static readonly Dictionary<Texture2D, Texture2D> readableCache
            = new Dictionary<Texture2D, Texture2D>();

        // Cache extracted sprite pixel regions by (symbol hash, frame index)
        private static readonly Dictionary<long, Texture2D> spriteCache
            = new Dictionary<long, Texture2D>();

        // Cache composited base layers (head+eyes+mouth) per dupe, validated by expression frames
        private struct BaseCacheEntry
        {
            public Texture2D Texture;
            public int EyeFrame;
            public int MouthFrame;
        }
        private static readonly Dictionary<int, BaseCacheEntry> baseCache
            = new Dictionary<int, BaseCacheEntry>();

        /// <summary>Alpha at or below this is treated as fully transparent (anti-aliasing fringe filter).</summary>
        private const float ALPHA_THRESHOLD = 0.1f;

        // Max pixel dimensions for expression sprites (proportional clamping).
        // Oversized frames (e.g., Sparkle mouth ~50×30) get scaled down uniformly
        // to prevent clipping into adjacent features. Head is ~81×80 px.
        private const int MAX_EYE_W   = 68; // ~84% of head width
        private const int MAX_EYE_H   = 40; // ~50% of head height
        private const int MAX_MOUTH_W = 45; // ~55% of head width
        private const int MAX_MOUTH_H = 22; // ~27% of head height

        /// <summary>
        /// Composites a dupe's accessories from KAnim atlas textures into a single Sprite.
        /// Layers: headshape -> eyes (transform-positioned) -> mouth (transform-positioned) -> hair/hat.
        /// </summary>
        public static Sprite ComposePortrait(MinionIdentity identity,
            ExpressionResolver.ExpressionFrames frames = default)
        {
            if (identity == null) return null;
            DiagnosticDump.RunOnce(identity);

            var accessorizer = identity.GetComponent<Accessorizer>();
            if (accessorizer == null) return null;

            var slots = Db.Get().AccessorySlots;
            var resume = identity.GetComponent<MinionResume>();
            string hatId = resume?.CurrentHat;
            bool hasHat = !string.IsNullOrEmpty(hatId);

            var headAcc = accessorizer.GetAccessory(slots.HeadShape);
            if (headAcc == null) return null;

            int instanceId = identity.GetInstanceID();
            int eyeFrame = frames.EyeFrame;
            int mouthFrame = frames.MouthFrame;

            // Base cache: head+eyes+mouth — rebuild when expression changes
            bool needsRebuild = !baseCache.TryGetValue(instanceId, out var entry)
                || entry.EyeFrame != eyeFrame || entry.MouthFrame != mouthFrame;

            if (needsRebuild)
            {
                if (entry.Texture != null) Object.Destroy(entry.Texture);

                var headSymbol = headAcc.symbol;

                var baseTex = new Texture2D(125, 125, TextureFormat.RGBA32, false);
                baseTex.filterMode = FilterMode.Bilinear;
                ClearTexture(baseTex);

                WriteSymbolDirect(baseTex, headSymbol, yOffset: PORTRAIT_Y_SHIFT);

                // Eyes — center-offset, NO flipX (sprite has both eyes)
                var eyeAcc = accessorizer.GetAccessory(slots.Eyes);
                if (eyeAcc != null)
                {
                    int ef = eyeFrame;
                    if (ef >= eyeAcc.symbol.frameLookup.Length) ef = 0;
                    WriteSymbolDirect(baseTex, eyeAcc.symbol,
                        xOffset: EYE_X, yOffset: EYE_Y + PORTRAIT_Y_SHIFT, frameOverride: ef,
                        maxWidth: MAX_EYE_W, maxHeight: MAX_EYE_H,
                        anchor: VerticalAnchor.Bottom, rotation: EYE_ROTATION);
                }

                // Mouth — center-offset, shifted down
                var mouthAcc = accessorizer.GetAccessory(slots.Mouth);
                if (mouthAcc != null)
                {
                    int mf = mouthFrame;
                    if (mf >= mouthAcc.symbol.frameLookup.Length) mf = 0;
                    WriteSymbolDirect(baseTex, mouthAcc.symbol,
                        xOffset: MOUTH_X, yOffset: MOUTH_Y + PORTRAIT_Y_SHIFT, frameOverride: mf,
                        maxWidth: MAX_MOUTH_W, maxHeight: MAX_MOUTH_H,
                        anchor: VerticalAnchor.Top);
                }
                baseTex.Apply();

                entry = new BaseCacheEntry
                {
                    Texture = baseTex,
                    EyeFrame = eyeFrame,
                    MouthFrame = mouthFrame
                };
                baseCache[instanceId] = entry;
            }

            // Clone cached base, then composite hair/hat on top
            var output = new Texture2D(125, 125, TextureFormat.RGBA32, false);
            output.filterMode = FilterMode.Bilinear;
            output.SetPixels(entry.Texture.GetPixels());

            if (hasHat)
            {
                WriteSymbol(output, accessorizer, slots.HatHair, xOffset: HATHAIR_X, yOffset: HATHAIR_Y + PORTRAIT_Y_SHIFT, usePivot: true);
                var hatAcc = slots.Hat.Lookup(hatId);
                if (hatAcc != null)
                    WriteSymbolDirect(output, hatAcc.symbol, xOffset: HAT_X, yOffset: HAT_Y + PORTRAIT_Y_SHIFT, usePivot: true);
            }
            else
            {
                WriteSymbol(output, accessorizer, slots.Hair, xOffset: HAIR_X, yOffset: HAIR_Y + PORTRAIT_Y_SHIFT, usePivot: true);
            }

            output.Apply();
            return Sprite.Create(output, new Rect(0, 0, 125, 125), new Vector2(0.5f, 0.5f));
        }

        private static void WriteSymbol(Texture2D output, Accessorizer accessorizer,
            AccessorySlot slot, int xOffset = 0, int yOffset = 0,
            bool usePivot = false, bool flipX = false, int frameOverride = -1)
        {
            var acc = accessorizer.GetAccessory(slot);
            if (acc == null) return;
            WriteSymbolDirect(output, acc.symbol, xOffset, yOffset, usePivot, flipX, frameOverride);
        }

        private enum VerticalAnchor { Center, Bottom, Top }

        private static void WriteSymbolDirect(Texture2D output, KAnim.Build.Symbol symbol,
            int xOffset = 0, int yOffset = 0,
            bool usePivot = false, bool flipX = false, int frameOverride = -1,
            int maxWidth = 0, int maxHeight = 0,
            VerticalAnchor anchor = VerticalAnchor.Center,
            float rotation = 0f)
        {
            if (symbol == null || symbol.frameLookup == null || symbol.frameLookup.Length == 0)
                return;

            int frameIdx = frameOverride >= 0 ? frameOverride : 0;
            if (frameIdx >= symbol.frameLookup.Length)
                frameIdx = 0;
            long cacheKey = ((long)symbol.hash.HashValue << 32) | (uint)frameIdx;

            var sprite = GetSpriteFromSymbol(symbol, frameIdx);
            if (sprite == null) return;

            var pixels = GetSpritePixels(cacheKey, sprite);
            Object.Destroy(sprite); // temp sprite — texture data cached separately
            if (pixels == null) return;

            // Proportional size clamping: scale down oversized expression sprites
            Texture2D source = pixels;
            bool scaled = false;
            if (maxWidth > 0 && maxHeight > 0
                && (pixels.width > maxWidth || pixels.height > maxHeight))
            {
                float scale = Mathf.Min((float)maxWidth / pixels.width,
                                        (float)maxHeight / pixels.height);
                int newW = Mathf.Max(1, Mathf.RoundToInt(pixels.width * scale));
                int newH = Mathf.Max(1, Mathf.RoundToInt(pixels.height * scale));
                source = ScaleTexture(pixels, newW, newH);
                scaled = true;
            }

            int pivotX = 0, pivotY = 0;
            if (usePivot)
            {
                var frame = symbol.GetFrame(frameIdx);
                pivotX = Mathf.RoundToInt(frame.bboxMin.x + source.width);
                pivotY = Mathf.RoundToInt(frame.bboxMin.y + source.height);
            }
            int xStart = (output.width / 2) - (source.width / 2) + xOffset;
            int yStart;
            switch (anchor)
            {
                case VerticalAnchor.Bottom:
                    yStart = (output.height / 2) + yOffset;
                    break;
                case VerticalAnchor.Top:
                    yStart = (output.height / 2) + yOffset - source.height;
                    break;
                default:
                    yStart = (output.height / 2) - (source.height / 2) + yOffset;
                    break;
            }
            if (usePivot)
            {
                xStart += pivotX / 2;
                yStart -= pivotY / 2;
            }

            // Batch-read source pixels to avoid per-pixel managed-to-native calls
            var srcPixels = source.GetPixels();
            int srcW = source.width;
            int outW = output.width;

            if (rotation != 0f)
            {
                // Inverse-mapped rotation: iterate output pixels, sample rotated source
                float rad = rotation * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                float cx = srcW * 0.5f;
                float cy = source.height * 0.5f;

                for (int oy = yStart; oy < yStart + source.height; oy++)
                {
                    if (oy < 0 || oy >= output.height) continue;
                    for (int ox = xStart; ox < xStart + source.width; ox++)
                    {
                        if (ox < 0 || ox >= outW) continue;
                        float lx = (ox - xStart) - cx;
                        float ly = (oy - yStart) - cy;
                        int sx = Mathf.RoundToInt(cos * lx + sin * ly + cx);
                        int sy = Mathf.RoundToInt(-sin * lx + cos * ly + cy);
                        if (sx < 0 || sx >= srcW || sy < 0 || sy >= source.height) continue;
                        var px = srcPixels[sy * srcW + sx];
                        if (px.a <= ALPHA_THRESHOLD) continue;
                        var existing = output.GetPixel(ox, oy);
                        output.SetPixel(ox, oy, AlphaBlend(existing, px));
                    }
                }
            }
            else
            {
                for (int x = 0; x < srcW; x++)
                {
                    for (int y = 0; y < source.height; y++)
                    {
                        var px = srcPixels[y * srcW + x];
                        if (px.a <= ALPHA_THRESHOLD) continue;

                        int outX = flipX ? (srcW - 1 - x) + xStart : x + xStart;
                        int outY = y + yStart;

                        if (outX >= 0 && outX < outW &&
                            outY >= 0 && outY < output.height)
                        {
                            var existing = output.GetPixel(outX, outY);
                            output.SetPixel(outX, outY, AlphaBlend(existing, px));
                        }
                    }
                }
            }

            if (scaled) Object.Destroy(source);
        }

        internal static Sprite GetSpriteFromSymbol(KAnim.Build.Symbol symbol, int frameOverride = -1)
        {
            if (symbol.frameLookup == null || symbol.frameLookup.Length == 0)
                return null;

            int frameIdx = frameOverride >= 0 ? frameOverride : 0;
            if (frameIdx >= symbol.frameLookup.Length)
                frameIdx = 0;
            var frame = symbol.GetFrame(frameIdx);
            if (frame.Equals(default(KAnim.Build.SymbolFrameInstance)))
                return null;

            Texture2D atlas;
            try
            {
                atlas = symbol.build.GetTexture(0);
            }
            catch
            {
                return null;
            }
            if (atlas == null) return null;

            float x = frame.uvMin.x;
            float x2 = frame.uvMax.x;
            float y = frame.uvMax.y;
            float y2 = frame.uvMin.y;
            int w = (int)(atlas.width * Mathf.Abs(x2 - x));
            int h = (int)(atlas.height * Mathf.Abs(y2 - y));
            if (w <= 0 || h <= 0) return null;

            float bboxW = Mathf.Abs(frame.bboxMax.x - frame.bboxMin.x);
            float ppu = 100f;
            if (w != 0 && bboxW > 0)
                ppu = 100f / (bboxW / w);

            var rect = new Rect(
                (int)(atlas.width * Mathf.Min(x, x2)),
                (int)(atlas.height * Mathf.Min(y, y2)),
                w, h);

            return Sprite.Create(atlas, rect, Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
        }

        private static Texture2D GetSpritePixels(long cacheKey, Sprite sprite)
        {
            if (sprite == null) return null;

            if (spriteCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var r = sprite.textureRect;
            if (r.width <= 0 || r.height <= 0) return null;

            var readable = GetReadableCopy(sprite.texture);
            if (readable == null) return null;

            var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
            var pixels = readable.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
            tex.SetPixels(pixels);
            tex.Apply();

            spriteCache[cacheKey] = tex;
            return tex;
        }

        private static Texture2D GetReadableCopy(Texture2D source)
        {
            if (source == null || source.width == 0 || source.height == 0)
                return null;

            if (readableCache.TryGetValue(source, out var cached))
                return cached;

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

            readableCache[source] = readable;
            return readable;
        }

        private static Color AlphaBlend(Color dst, Color src)
        {
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0f) return Color.clear;
            return new Color(
                (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA,
                (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA,
                (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA,
                outA);
        }

        private static void ClearTexture(Texture2D tex)
        {
            var clear = new Color[tex.width * tex.height];
            tex.SetPixels(clear);
        }

        private static Texture2D ScaleTexture(Texture2D source, int targetW, int targetH)
        {
            var rt = RenderTexture.GetTemporary(targetW, targetH);
            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
            result.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        /// <summary>
        /// Evicts baseCache entries for dupes no longer in the active snapshot set.
        /// Call periodically (e.g., on widget refresh) to prevent texture leaks from dead/transferred dupes.
        /// </summary>
        public static void EvictStale(IEnumerable<int> liveInstanceIds)
        {
            var liveSet = new HashSet<int>(liveInstanceIds);
            var stale = new List<int>();
            foreach (var kv in baseCache)
            {
                if (!liveSet.Contains(kv.Key))
                    stale.Add(kv.Key);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                if (baseCache.TryGetValue(stale[i], out var entry) && entry.Texture != null)
                    Object.Destroy(entry.Texture);
                baseCache.Remove(stale[i]);
            }
        }

        /// <summary>
        /// Call on scene unload or when caches should be freed.
        /// </summary>
        public static void ClearCaches()
        {
            foreach (var kv in readableCache)
                if (kv.Value != null) Object.Destroy(kv.Value);
            readableCache.Clear();

            foreach (var kv in spriteCache)
                if (kv.Value != null) Object.Destroy(kv.Value);
            spriteCache.Clear();

            foreach (var kv in baseCache)
                if (kv.Value.Texture != null) Object.Destroy(kv.Value.Texture);
            baseCache.Clear();

            ExpressionResolver.ClearCache();
        }
    }
}
