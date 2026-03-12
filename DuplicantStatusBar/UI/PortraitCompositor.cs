using System.Collections.Generic;
using Database;
using UnityEngine;

namespace DuplicantStatusBar.UI
{
    /// <summary>
    /// Composites dupe accessory symbols from KAnim texture atlases into a
    /// static Texture2D/Sprite. Bypasses the KAnim batch rendering pipeline
    /// entirely — works in any Canvas render mode.
    ///
    /// </summary>
    public static class PortraitCompositor
    {
        // Cache readable copies of GPU textures to avoid repeated readbacks
        private static readonly Dictionary<Texture2D, Texture2D> readableCache
            = new Dictionary<Texture2D, Texture2D>();

        // Cache extracted sprite pixel regions by (symbol hash, frame index)
        private static readonly Dictionary<long, Texture2D> spriteCache
            = new Dictionary<long, Texture2D>();

        // Always composite at 125px native resolution — the UI Image
        // component scales the result down via bilinear filtering.
        private const int COMPOSE_SIZE = 125;

        /// <summary>
        /// Composites a dupe's accessories from KAnim atlas textures into a single Sprite.
        /// Layers: headshape -> eyes (flipped) -> mouth (frame 22) -> hair/hat. Always renders at 125px, UI Image scales down.
        /// </summary>
        public static Sprite ComposePortrait(MinionIdentity identity, int size)
        {
            if (identity == null) return null;

            var accessorizer = identity.GetComponent<Accessorizer>();
            if (accessorizer == null) return null;

            var slots = Db.Get().AccessorySlots;
            var resume = identity.GetComponent<MinionResume>();
            string hatId = resume?.CurrentHat;
            bool hasHat = !string.IsNullOrEmpty(hatId);

            // Create output texture (transparent)
            var output = new Texture2D(COMPOSE_SIZE, COMPOSE_SIZE, TextureFormat.RGBA32, false);
            output.filterMode = FilterMode.Bilinear;
            ClearTexture(output);

            // Composite layers back-to-front:
            // headshape → eyes → mouth → hair (or hathair+hat)
            WriteSymbol(output, accessorizer, slots.HeadShape);
            WriteSymbol(output, accessorizer, slots.Eyes, xOffset: 8, flipX: true);
            WriteSymbol(output, accessorizer, slots.Mouth, xOffset: 10, yOffset: -12, frameOverride: 22);

            if (hasHat)
            {
                // Hat replaces hair: render hathair underneath, then hat on top
                WriteSymbol(output, accessorizer, slots.HatHair, yOffset: 30, usePivot: true);
                var hatAcc = slots.Hat.Lookup(hatId);
                if (hatAcc != null)
                    WriteSymbolDirect(output, hatAcc.symbol, yOffset: 30, usePivot: true);
            }
            else
            {
                WriteSymbol(output, accessorizer, slots.Hair, xOffset: 8, yOffset: 30, usePivot: true);
            }

            output.Apply();
            var sprite = Sprite.Create(output,
                new Rect(0, 0, COMPOSE_SIZE, COMPOSE_SIZE),
                new Vector2(0.5f, 0.5f));
            return sprite;
        }

        private static void WriteSymbol(Texture2D output, Accessorizer accessorizer,
            AccessorySlot slot, int xOffset = 0, int yOffset = 0,
            bool usePivot = false, bool flipX = false, int frameOverride = -1)
        {
            var acc = accessorizer.GetAccessory(slot);
            if (acc == null) return;
            WriteSymbolDirect(output, acc.symbol, xOffset, yOffset, usePivot, flipX, frameOverride);
        }

        private static void WriteSymbolDirect(Texture2D output, KAnim.Build.Symbol symbol,
            int xOffset = 0, int yOffset = 0,
            bool usePivot = false, bool flipX = false, int frameOverride = -1)
        {
            if (symbol == null) return;

            int frameIdx = frameOverride >= 0 ? frameOverride : 0;
            long cacheKey = ((long)symbol.hash.HashValue << 16) ^ frameIdx;

            var sprite = GetSpriteFromSymbol(symbol, frameOverride);
            if (sprite == null) return;

            var pixels = GetSpritePixels(cacheKey, sprite);
            Object.Destroy(sprite); // temp sprite — texture data cached separately
            if (pixels == null) return;

            // Get pivot point from bounding box
            int pivotX = 0, pivotY = 0;
            if (usePivot)
            {
                var frame = symbol.GetFrame(frameOverride >= 0 ? frameOverride : 0);
                pivotX = Mathf.RoundToInt(frame.bboxMin.x + pixels.width);
                pivotY = Mathf.RoundToInt(frame.bboxMin.y + pixels.height);
            }

            int xStart = (output.width / 2) - (pixels.width / 2) + xOffset;
            int yStart = (output.height / 2) - (pixels.height / 2) + yOffset;
            if (usePivot)
            {
                xStart += pivotX / 2;
                yStart -= pivotY / 2;
            }

            for (int x = 0; x < pixels.width; x++)
            {
                for (int y = 0; y < pixels.height; y++)
                {
                    var px = pixels.GetPixel(x, y);
                    if (px.a <= 0.1f) continue;

                    int outX = flipX ? (pixels.width - 1 - x) + xStart : x + xStart;
                    int outY = y + yStart;

                    if (outX >= 0 && outX < output.width &&
                        outY >= 0 && outY < output.height)
                    {
                        // Alpha-blend over existing pixels
                        var existing = output.GetPixel(outX, outY);
                        output.SetPixel(outX, outY, AlphaBlend(existing, px));
                    }
                }
            }
        }

        private static Sprite GetSpriteFromSymbol(KAnim.Build.Symbol symbol, int frameOverride = -1)
        {
            if (symbol.frameLookup == null || symbol.frameLookup.Length == 0)
                return null;

            int frameIdx = frameOverride >= 0 ? frameOverride : 0;
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
                (int)(atlas.width * x),
                (int)(atlas.height * y),
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
        }
    }
}
