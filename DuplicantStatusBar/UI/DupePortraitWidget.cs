using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DuplicantStatusBar.Config;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.UI
{
    public sealed class DupePortraitWidget : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Image borderImage;
        private Image bgFill;
        private Image healthFill;
        private TextMeshProUGUI initialText;
        private Image damageOverlay;
        private const int MAX_BADGES = 3;
        private Image[] badgeImages = new Image[MAX_BADGES];
        private TextMeshProUGUI[] badgeSymbols = new TextMeshProUGUI[MAX_BADGES];
        private TextMeshProUGUI nameLabel;
        private LayoutElement rootLayout;
        private LayoutElement cardLayout;
        private LayoutElement nameLayout;

        // Portrait: compositor-generated sprite displayed via Image
        private Image portraitImage;
        private int currentIdentityId;
        private string currentHat;
        private const int PORTRAIT_THRESHOLD = 36;

        // Expression state
        private ExpressionType currentExpression = ExpressionType.Neutral;
        private int currentEyeFrame;
        private int currentMouthFrame = 22;

        // Blink system
        private float blinkTimer;
        private bool isBlinking;

        private DupeSnapshot currentSnapshot;
        private float pulseTimer;
        private bool isPulsing;
        private bool isOverjoyed;
        private float rainbowHue;
        private Image glowImage;
        private Texture2D rainbowTex;
        private Sprite rainbowSprite;
        private Color32[] rainbowPixels;
        private int rainbowTexSize;
        private float rainbowPulse;
        private const int RAINBOW_CORNER = 8;

        private ushort heldMask;
        private ushort currentAlertMask;
        private float[] holdTimers = new float[15]; // indexed by (int)AlertType

        private Color targetBorderColor;
        private Color targetFillColor;

        private static Sprite _circle;

        public static DupePortraitWidget Create(RectTransform parent, int size)
        {
            var go = new GameObject("DupeCard");
            go.transform.SetParent(parent, false);
            var widget = go.AddComponent<DupePortraitWidget>();
            widget.Build(size);
            return widget;
        }

        private void Build(int size)
        {
            int cardSz = size + 4;
            int totalW = size + 10;
            int totalH = size + 22;

            // Root: vertical layout (card on top, name below)
            var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 1;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            rootLayout = gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredWidth = totalW;
            rootLayout.preferredHeight = totalH;

            // Invisible raycast target (covers whole widget)
            var raycast = gameObject.AddComponent<Image>();
            raycast.color = Color.clear;

            // ── Card (portrait area) ──────────────────────
            var cardGO = new GameObject("Card");
            cardGO.transform.SetParent(transform, false);
            cardLayout = cardGO.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = cardSz;
            cardLayout.preferredHeight = cardSz;

            // Glow layer (behind border — same rainbow sprite, larger rect, lower alpha)
            glowImage = AddImage(cardGO.transform, "Glow");
            glowImage.type = Image.Type.Simple;
            glowImage.raycastTarget = false;
            Stretch(glowImage.rectTransform, 6f);
            glowImage.gameObject.SetActive(false);

            // Border (stress-colored frame)
            borderImage = AddImage(cardGO.transform, "Border");
            borderImage.sprite = RoundedRect;
            borderImage.type = Image.Type.Sliced;
            borderImage.color = TierColor(StressTier.Calm);
            borderImage.raycastTarget = false;
            Stretch(borderImage.rectTransform);

            // Inner fill (dark, tinted with stress color)
            bgFill = AddImage(cardGO.transform, "Fill");
            bgFill.sprite = RoundedRect;
            bgFill.type = Image.Type.Sliced;
            bgFill.color = ColorUtil.CardBg;
            bgFill.raycastTarget = false;
            Stretch(bgFill.rectTransform, -4f);

            // Health fill (anchor-based vertical bar, hidden at 100%)
            healthFill = AddImage(cardGO.transform, "HealthFill");
            healthFill.type = Image.Type.Simple;
            healthFill.color = ColorUtil.HealthFill;
            healthFill.raycastTarget = false;
            var hrt = healthFill.rectTransform;
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.offsetMin = new Vector2(2f, 2f);
            hrt.offsetMax = new Vector2(-2f, -2f);
            healthFill.gameObject.SetActive(false);

            // Portrait: compositor-generated sprite via Image
            portraitImage = AddImage(cardGO.transform, "Portrait");
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            Stretch(portraitImage.rectTransform, -4f);
            portraitImage.gameObject.SetActive(false);

            // Large initial letter (fallback when portrait too small)
            initialText = AddText(cardGO.transform, "Initial");
            initialText.fontSize = size * 0.5f;
            initialText.color = ColorUtil.TextMuted;
            initialText.alignment = TextAlignmentOptions.Center;
            initialText.fontStyle = FontStyles.Bold;
            initialText.raycastTarget = false;
            if (StatusBarScreen.GameFont != null) initialText.font = StatusBarScreen.GameFont;
            Stretch(initialText.rectTransform);

            // Damage overlay (above portrait + initials — dark red tint over missing HP)
            damageOverlay = AddImage(cardGO.transform, "DamageOverlay");
            damageOverlay.type = Image.Type.Simple;
            damageOverlay.color = ColorUtil.WithAlpha(ColorUtil.DamageBase, 0.45f);
            damageOverlay.raycastTarget = false;
            var dort = damageOverlay.rectTransform;
            dort.anchorMin = new Vector2(0f, 1f);
            dort.anchorMax = new Vector2(1f, 1f);
            dort.offsetMin = new Vector2(2f, 0f);
            dort.offsetMax = new Vector2(-2f, -2f);
            damageOverlay.gameObject.SetActive(false);

            // ── Alert badges (circular, top-right, up to MAX_BADGES) ─────────
            for (int i = 0; i < MAX_BADGES; i++)
            {
                var badge = AddImage(cardGO.transform, $"Badge{i}");
                badge.sprite = Circle;
                badge.type = Image.Type.Simple;
                badge.raycastTarget = false;
                var brt = badge.rectTransform;
                brt.anchorMin = new Vector2(1f, 1f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                badge.gameObject.SetActive(false);
                badgeImages[i] = badge;

                var sym = AddText(badge.transform, "Sym");
                sym.fontSize = 7f;
                sym.color = Color.white;
                sym.alignment = TextAlignmentOptions.Center;
                sym.fontStyle = FontStyles.Bold;
                sym.raycastTarget = false;
                if (StatusBarScreen.GameFont != null) sym.font = StatusBarScreen.GameFont;
                Stretch(sym.rectTransform);
                badgeSymbols[i] = sym;
            }

            // ── Name label (below card) ───────────────────
            nameLabel = AddText(transform, "Name");
            nameLabel.fontSize = 10;
            nameLabel.color = ColorUtil.TextPrimary;
            if (StatusBarScreen.GameFont != null) nameLabel.font = StatusBarScreen.GameFont;
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.enableWordWrapping = false;
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            nameLabel.raycastTarget = false;
            nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredWidth = totalW;
            nameLayout.preferredHeight = 14;
        }

        public void SetSnapshot(DupeSnapshot snapshot, int size)
        {
            currentSnapshot = snapshot;

            int cardSz = size + 4;
            int totalW = size + 10;
            int totalH = size + 22;

            // Show portrait or initials based on card size
            bool usePortrait = StatusBarOptions.Instance.DisplayMode == DisplayMode.Portraits
                && size >= PORTRAIT_THRESHOLD && snapshot.Identity != null;

            if (usePortrait)
            {
                int id = snapshot.Identity.GetInstanceID();
                var resume = snapshot.Identity.GetComponent<MinionResume>();
                string hat = resume?.CurrentHat ?? "";

                // Resolve expression → eye/mouth frame indices + transforms
                ExpressionType expr = ExpressionType.Neutral;
                var frames = new ExpressionResolver.ExpressionFrames();
                if (StatusBarOptions.Instance.EnableExpressions)
                {
                    expr = ExpressionResolver.Resolve(snapshot.HighestAlert, snapshot.Tier);
                    frames = ExpressionResolver.GetFrames(expr);
                }

                bool identityChanged = id != currentIdentityId || hat != currentHat;
                bool expressionChanged = expr != currentExpression;

                if (identityChanged || expressionChanged)
                {
                    DestroyPortraitSprite();

                    portraitImage.sprite = PortraitCompositor.ComposePortrait(
                        snapshot.Identity, frames);
                    currentIdentityId = id;
                    currentHat = hat;
                    currentExpression = expr;
                    currentEyeFrame = frames.EyeFrame;
                    currentMouthFrame = frames.MouthFrame;
                    isBlinking = false;
                }

                // Initialize blink timer per-widget (staggered)
                if (identityChanged)
                    blinkTimer = UnityEngine.Random.Range(2f, 6f);

                portraitImage.gameObject.SetActive(true);
                initialText.gameObject.SetActive(false);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
                initialText.gameObject.SetActive(true);
                var initials = !string.IsNullOrEmpty(snapshot.Name)
                    ? snapshot.Name.Substring(0, Math.Min(2, snapshot.Name.Length))
                    : "?";
                if (initialText.text != initials)
                    initialText.text = initials;
                initialText.fontSize = size * 0.5f;
            }

            // Name below card
            var displayName = snapshot.Name ?? "???";
            if (nameLabel.text != displayName)
                nameLabel.text = displayName;

            // Overjoyed → rainbow border (set here, applied in Update; gated by option)
            isOverjoyed = snapshot.IsOverjoyed && StatusBarOptions.Instance.AlertOverjoyed;

            // Border = full stress tier color (smoothed; overridden by rainbow in Update)
            var tc = TierColor(snapshot.Tier);
            targetBorderColor = tc;

            // Background = dark base blended with stress color (smoothed)
            targetFillColor = Color.Lerp(ColorUtil.DarkBase, tc, 0.30f);

            // Health fill (below portrait)
            float hp = Mathf.Clamp01(snapshot.HealthPercent / 100f);
            var hfrt = healthFill.rectTransform;
            hfrt.anchorMax = new Vector2(1f, hp);
            hfrt.offsetMax = new Vector2(-2f, 0f);
            healthFill.color = HealthColor(hp);
            healthFill.gameObject.SetActive(hp < 0.995f);

            // Damage overlay (above portrait — covers missing HP from top down)
            bool showDamage = hp < 0.995f;
            damageOverlay.gameObject.SetActive(showDamage);
            if (showDamage)
            {
                var dort = damageOverlay.rectTransform;
                dort.anchorMin = new Vector2(0f, hp);
                dort.anchorMax = new Vector2(1f, 1f);
                dort.offsetMin = new Vector2(2f, 0f);
                dort.offsetMax = new Vector2(-2f, -2f);
                float damageAlpha = Mathf.Lerp(0.40f, 0.65f, 1f - hp);
                damageOverlay.color = ColorUtil.WithAlpha(ColorUtil.DamageBase, damageAlpha);
            }

            // Pulse on critical
            isPulsing = snapshot.Tier == StressTier.Critical
                     || snapshot.HasAlert(AlertType.LowHP)
                     || snapshot.HasAlert(AlertType.Overstressed)
                     || snapshot.HasAlert(AlertType.Incapacitated);

            // Multi-badge with per-alert hysteresis
            UpdateBadges(snapshot.AlertMask, snapshot.CustomAlerts);

            // Resize
            rootLayout.preferredWidth = totalW;
            rootLayout.preferredHeight = totalH;
            cardLayout.preferredWidth = cardSz;
            cardLayout.preferredHeight = cardSz;
            nameLayout.preferredWidth = totalW;

            // Scale & position badges
            int badgeCount = BitCount((ushort)(heldMask));
            float badgeFrac = badgeCount <= 1 ? 0.28f : badgeCount == 2 ? 0.24f : 0.21f;
            float badgeSize = Mathf.Max(9f, cardSz * badgeFrac);
            float gap = 1f;
            int slot = 0;
            for (int i = 0; i < MAX_BADGES; i++)
            {
                if (!badgeImages[i].gameObject.activeSelf) continue;
                var brt = badgeImages[i].rectTransform;
                brt.sizeDelta = new Vector2(badgeSize, badgeSize);
                float xOff = -(badgeSize * 0.15f) - slot * (badgeSize + gap);
                brt.anchoredPosition = new Vector2(xOff, -badgeSize * 0.15f);
                badgeSymbols[i].fontSize = Mathf.Max(7f, badgeSize * 0.6f);
                slot++;
            }
        }

        private void DestroyPortraitSprite()
        {
            if (portraitImage.sprite != null)
            {
                var oldTex = portraitImage.sprite.texture;
                Destroy(portraitImage.sprite);
                Destroy(oldTex);
                portraitImage.sprite = null;
            }
        }

        private void RecomposeWithEyes(int eyeFrame)
        {
            if (currentSnapshot.Identity == null) return;
            DestroyPortraitSprite();
            // Build frames struct with overridden eye frame, keeping current transforms
            var frames = ExpressionResolver.GetFrames(currentExpression);
            frames.EyeFrame = eyeFrame;
            portraitImage.sprite = PortraitCompositor.ComposePortrait(
                currentSnapshot.Identity, frames);
        }

        private void OnDestroy()
        {
            DestroyPortraitSprite();
            CleanupRainbow();
        }

        private void UpdateRainbowBorder(int size)
        {
            if (rainbowTex == null || size != rainbowTexSize)
            {
                CleanupRainbow();
                rainbowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                rainbowTex.filterMode = FilterMode.Bilinear;
                rainbowPixels = new Color32[size * size];
                rainbowSprite = Sprite.Create(rainbowTex,
                    new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100f,
                    0, SpriteMeshType.FullRect);
                rainbowTexSize = size;
            }

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            int sparkSeed = Time.frameCount / 4;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Corner alpha (reuse MakeRoundedRect logic)
                    float dx = 0f, dy = 0f;
                    if (x < RAINBOW_CORNER) dx = RAINBOW_CORNER - x - 0.5f;
                    else if (x >= size - RAINBOW_CORNER) dx = x - (size - RAINBOW_CORNER) + 0.5f;
                    if (y < RAINBOW_CORNER) dy = RAINBOW_CORNER - y - 0.5f;
                    else if (y >= size - RAINBOW_CORNER) dy = y - (size - RAINBOW_CORNER) + 0.5f;

                    byte alpha;
                    if (dx > 0f && dy > 0f)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        alpha = (byte)(Mathf.Clamp01(RAINBOW_CORNER - dist + 0.5f) * 255);
                    }
                    else
                    {
                        alpha = 255;
                    }

                    // Angular hue (clockwise from 12 o'clock)
                    float angle = Mathf.Atan2(x - cx, y - cy);
                    float normalizedAngle = angle / (2f * Mathf.PI) + 0.5f;
                    float hue = (normalizedAngle - rainbowHue) % 1f;
                    if (hue < 0f) hue += 1f;

                    Color c = Color.HSVToRGB(hue, 1f, 1f);

                    // Sparkle (~4% of pixels, re-randomized every 4 frames)
                    int hash = (x * 7919 + y * 4813 + sparkSeed * 3571) & 0x3FF;
                    if (hash < 40)
                        c = Color.Lerp(c, Color.white, 0.7f);

                    rainbowPixels[y * size + x] = new Color32(
                        (byte)(c.r * 255), (byte)(c.g * 255),
                        (byte)(c.b * 255), alpha);
                }
            }

            rainbowTex.SetPixels32(rainbowPixels);
            rainbowTex.Apply(false, false);

            borderImage.sprite = rainbowSprite;
            borderImage.type = Image.Type.Simple;
            glowImage.sprite = rainbowSprite;
            glowImage.gameObject.SetActive(true);
        }

        private void CleanupRainbow()
        {
            if (rainbowSprite != null) { Destroy(rainbowSprite); rainbowSprite = null; }
            if (rainbowTex != null) { Destroy(rainbowTex); rainbowTex = null; }
            rainbowPixels = null;
            rainbowTexSize = 0;
        }

        private void UpdateBadges(ushort activeMask,
            System.Collections.Generic.Dictionary<string, bool> customAlerts)
        {
            currentAlertMask = activeMask;

            // Set bits and reset hold timers for active alerts
            foreach (var a in DupeSnapshot.AlertPriority)
            {
                int bit = 1 << (int)a;
                if ((activeMask & bit) != 0)
                {
                    heldMask |= (ushort)bit;
                    holdTimers[(int)a] = HoldTime(a);
                }
            }

            // Walk heldMask in priority order, populate up to MAX_BADGES slots
            int slot = 0;
            foreach (var a in DupeSnapshot.AlertPriority)
            {
                if (slot >= MAX_BADGES) break;
                if ((heldMask & (1 << (int)a)) == 0) continue;
                if (a == AlertType.Overjoyed) continue; // rainbow border replaces badge

                badgeImages[slot].gameObject.SetActive(true);
                badgeImages[slot].color = AlertColor(a);
                var sym = a == AlertType.Idle ? "?" : "!";
                if (badgeSymbols[slot].text != sym)
                    badgeSymbols[slot].text = sym;
                slot++;
            }

            // Custom alert badges in remaining slots
            var activeCustom = API.Internal.AlertRegistry.GetActiveCustomAlerts(customAlerts);
            if (activeCustom != null)
            {
                foreach (var reg in activeCustom)
                {
                    if (slot >= MAX_BADGES) break;
                    badgeImages[slot].gameObject.SetActive(true);
                    badgeImages[slot].color = reg.BaseColor;
                    if (badgeSymbols[slot].text != reg.BadgeSymbol)
                        badgeSymbols[slot].text = reg.BadgeSymbol;
                    slot++;
                }
            }

            // Hide unused slots
            for (int i = slot; i < MAX_BADGES; i++)
                badgeImages[i].gameObject.SetActive(false);
        }

        private static float HoldTime(AlertType alert)
        {
            switch (alert)
            {
                case AlertType.Suffocating:
                case AlertType.LowHP:
                case AlertType.Incapacitated:
                    return 0f;
                case AlertType.Overjoyed:
                    return 1f;
                default:
                    return 3f;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = 1f - Mathf.Exp(-dt / 0.3f);

            if (isOverjoyed)
            {
                rainbowHue = (rainbowHue + dt * 0.5f) % 1f;
                rainbowPulse += dt * 5f;
                UpdateRainbowBorder((int)cardLayout.preferredWidth);

                float pulse = 0.85f + 0.15f * Mathf.Sin(rainbowPulse);
                borderImage.color = new Color(1f, 1f, 1f, pulse);

                float glowAlpha = 0.2f + 0.25f * Mathf.Sin(rainbowPulse);
                glowImage.color = new Color(1f, 1f, 1f, glowAlpha);
            }
            else
            {
                // Leaving overjoyed → restore sliced RoundedRect
                if (borderImage.sprite != RoundedRect)
                {
                    borderImage.sprite = RoundedRect;
                    borderImage.type = Image.Type.Sliced;
                    glowImage.gameObject.SetActive(false);
                }

                var lerpedBorder = Color.Lerp(borderImage.color, targetBorderColor, t);
                float borderAlpha = isPulsing
                    ? 0.6f + 0.4f * Mathf.Sin(pulseTimer)
                    : Mathf.Lerp(borderImage.color.a, targetBorderColor.a, t);
                borderImage.color = new Color(lerpedBorder.r, lerpedBorder.g, lerpedBorder.b, borderAlpha);
            }

            bgFill.color = Color.Lerp(bgFill.color, targetFillColor, t);

            if (isPulsing)
                pulseTimer = (pulseTimer + dt * 3f) % (2f * Mathf.PI);

            // Per-alert hold timer decay (only for held-but-no-longer-active alerts)
            ushort expiredBits = (ushort)(heldMask & ~currentAlertMask);
            if (expiredBits != 0)
            {
                foreach (var a in DupeSnapshot.AlertPriority)
                {
                    int bit = 1 << (int)a;
                    if ((expiredBits & bit) == 0) continue;
                    holdTimers[(int)a] -= dt;
                    if (holdTimers[(int)a] <= 0f)
                        heldMask &= (ushort)~bit;
                }
            }

            // Blink system: randomized per-dupe, only when portrait is visible
            if (StatusBarOptions.Instance.EnableExpressions
                && portraitImage.gameObject.activeSelf
                && currentSnapshot.Identity != null)
            {
                int bf = ExpressionResolver.GetBlinkFrame();
                if (bf >= 0 && bf != currentEyeFrame)
                {
                    blinkTimer -= dt;
                    if (blinkTimer <= 0f)
                    {
                        if (!isBlinking)
                        {
                            isBlinking = true;
                            blinkTimer = 0.15f;
                            RecomposeWithEyes(bf);
                        }
                        else
                        {
                            isBlinking = false;
                            blinkTimer = UnityEngine.Random.Range(3f, 8f);
                            RecomposeWithEyes(currentEyeFrame);
                        }
                    }
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (currentSnapshot.Selectable == null) return;
            if (currentSnapshot.Selectable.gameObject == null) return;

            SelectTool.Instance.Select(currentSnapshot.Selectable);
            var pos = currentSnapshot.Selectable.transform.position;
            CameraController.Instance?.SetTargetPos(pos, 8f, true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            DupeTooltip.Show(currentSnapshot, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DupeTooltip.Hide();
        }

        // ── Colors ──────────────────────────────────────

        public static Color TierColor(StressTier tier)
        {
            switch (tier)
            {
                case StressTier.Calm:     return ColorUtil.Hex(ColorUtil.Green);
                case StressTier.Mild:     return ColorUtil.Hex(ColorUtil.Lime);
                case StressTier.Stressed: return ColorUtil.Hex(ColorUtil.Amber);
                case StressTier.High:     return ColorUtil.Hex(ColorUtil.Orange);
                case StressTier.Critical: return ColorUtil.Hex(ColorUtil.Red);
                default: return Color.gray;
            }
        }

        public static Color AlertColor(AlertType alert) => AlertEffects.GetColor(alert);

        private static Color HealthColor(float hp)
        {
            // 3-segment gradient: green → yellow → orange → red
            // Alpha increases as health drops (0.75 → 0.90)
            if (hp > 0.6f)
                return Color.Lerp(ColorUtil.HealthYellow, ColorUtil.HealthGreen, (hp - 0.6f) / 0.4f);
            if (hp > 0.3f)
                return Color.Lerp(ColorUtil.HealthOrange, ColorUtil.HealthYellow, (hp - 0.3f) / 0.3f);
            return Color.Lerp(ColorUtil.HealthRed, ColorUtil.HealthOrange, hp / 0.3f);
        }

        // ── Sprites ─────────────────────────────────────

        private static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = MakeCircle(64);
                return _circle;
            }
        }

        private static Sprite _roundedRect;
        internal static Sprite RoundedRect
        {
            get
            {
                if (_roundedRect == null) _roundedRect = MakeRoundedRect(32, 8);
                return _roundedRect;
            }
        }

        internal static Sprite MakeRoundedRect(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f, dy = 0f;
                    if (x < radius) dx = radius - x - 0.5f;
                    else if (x >= size - radius) dx = x - (size - radius) + 0.5f;
                    if (y < radius) dy = radius - y - 0.5f;
                    else if (y >= size - radius) dy = y - (size - radius) + 0.5f;

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (dx > 0f && dy > 0f)
                        ? (byte)(Mathf.Clamp01(radius - dist + 0.5f) * 255)
                        : (byte)255;
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        private static Sprite MakeCircle(int sz)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var pixels = new Color32[sz * sz];
            float c = sz * 0.5f, r = c - 1f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255);
                    pixels[y * sz + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
        }

        // ── Helpers ─────────────────────────────────────

        private static int BitCount(ushort v)
        {
            int c = 0;
            while (v != 0) { c++; v &= (ushort)(v - 1); }
            return c;
        }

        private static Image AddImage(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<Image>();
        }

        private static TextMeshProUGUI AddText(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        /// <summary>Stretch to fill parent. Positive = grow beyond parent, negative = shrink inward.</summary>
        private static void Stretch(RectTransform rt, float expand = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            float half = expand * 0.5f;
            rt.offsetMin = new Vector2(-half, -half);
            rt.offsetMax = new Vector2(half, half);
        }
    }
}
