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
        private Image alertBadge;
        private TextMeshProUGUI badgeSymbol;
        private TextMeshProUGUI nameLabel;
        private LayoutElement rootLayout;
        private LayoutElement cardLayout;
        private LayoutElement nameLayout;

        // Portrait: compositor-generated sprite displayed via Image
        private Image portraitImage;
        private int currentIdentityId;
        private string currentHat;
        private const int PORTRAIT_THRESHOLD = 36;

        private DupeSnapshot currentSnapshot;
        private float pulseTimer;
        private bool isPulsing;

        private float badgeHoldTimer;
        private AlertType heldAlert = AlertType.None;

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
            bgFill.color = new Color(0.118f, 0.165f, 0.220f); // #1E2A38
            bgFill.raycastTarget = false;
            Stretch(bgFill.rectTransform, -4f);

            // Health fill (anchor-based vertical bar, hidden at 100%)
            healthFill = AddImage(cardGO.transform, "HealthFill");
            healthFill.type = Image.Type.Simple;
            healthFill.color = new Color(0.298f, 0.686f, 0.314f, 0.55f);
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
            initialText.color = new Color(0.9f, 0.9f, 0.9f);
            initialText.alignment = TextAlignmentOptions.Center;
            initialText.fontStyle = FontStyles.Bold;
            initialText.raycastTarget = false;
            if (StatusBarScreen.GameFont != null) initialText.font = StatusBarScreen.GameFont;
            Stretch(initialText.rectTransform);

            // ── Alert badge (circular, top-right) ─────────
            alertBadge = AddImage(cardGO.transform, "Badge");
            alertBadge.sprite = Circle;
            alertBadge.type = Image.Type.Simple;
            alertBadge.raycastTarget = false;
            var brt = alertBadge.rectTransform;
            brt.anchorMin = new Vector2(1f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            float badgeSz = Mathf.Max(10f, cardSz * 0.30f);
            brt.sizeDelta = new Vector2(badgeSz, badgeSz);
            brt.anchoredPosition = new Vector2(-badgeSz * 0.15f, -badgeSz * 0.15f);

            badgeSymbol = AddText(alertBadge.transform, "Sym");
            badgeSymbol.fontSize = Mathf.Max(7f, badgeSz * 0.6f);
            badgeSymbol.color = Color.white;
            badgeSymbol.alignment = TextAlignmentOptions.Center;
            badgeSymbol.fontStyle = FontStyles.Bold;
            badgeSymbol.raycastTarget = false;
            if (StatusBarScreen.GameFont != null) badgeSymbol.font = StatusBarScreen.GameFont;
            Stretch(badgeSymbol.rectTransform);

            alertBadge.gameObject.SetActive(false);

            // ── Name label (below card) ───────────────────
            nameLabel = AddText(transform, "Name");
            nameLabel.fontSize = 10;
            nameLabel.color = new Color(0.910f, 0.929f, 0.949f); // #E8EDF2
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

                if (id != currentIdentityId || hat != currentHat)
                {
                    // Destroy old texture to prevent leak
                    DestroyPortraitSprite();

                    portraitImage.sprite = PortraitCompositor.ComposePortrait(
                        snapshot.Identity, cardSz);
                    currentIdentityId = id;
                    currentHat = hat;
                }

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

            // Border = full stress tier color (smoothed)
            var tc = TierColor(snapshot.Tier);
            targetBorderColor = tc;

            // Background = dark base blended with stress color (smoothed)
            targetFillColor = Color.Lerp(
                new Color(0.08f, 0.10f, 0.14f), tc, 0.30f);

            // Health fill
            float hp = Mathf.Clamp01(snapshot.HealthPercent / 100f);
            var hfrt = healthFill.rectTransform;
            hfrt.anchorMax = new Vector2(1f, hp);
            hfrt.offsetMax = new Vector2(-2f, 0f);
            healthFill.color = HealthColor(hp);
            healthFill.gameObject.SetActive(hp < 0.995f);

            // Pulse on critical
            isPulsing = snapshot.Tier == StressTier.Critical
                     || snapshot.HighestAlert == AlertType.LowHP
                     || snapshot.HighestAlert == AlertType.Overstressed;

            // Alert badge with hysteresis
            UpdateBadge(snapshot.HighestAlert);

            // Resize
            rootLayout.preferredWidth = totalW;
            rootLayout.preferredHeight = totalH;
            cardLayout.preferredWidth = cardSz;
            cardLayout.preferredHeight = cardSz;
            nameLayout.preferredWidth = totalW;

            // Scale badge with card size
            float badgeSize = Mathf.Max(10f, cardSz * 0.30f);
            alertBadge.rectTransform.sizeDelta = new Vector2(badgeSize, badgeSize);
            alertBadge.rectTransform.anchoredPosition = new Vector2(-badgeSize * 0.15f, -badgeSize * 0.15f);
            badgeSymbol.fontSize = Mathf.Max(7f, badgeSize * 0.6f);
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

        private void OnDestroy()
        {
            DestroyPortraitSprite();
        }

        private void UpdateBadge(AlertType newAlert)
        {
            if (newAlert != AlertType.None)
            {
                // New alert or different alert: show immediately
                heldAlert = newAlert;
                badgeHoldTimer = HoldTime(newAlert);
                alertBadge.gameObject.SetActive(true);
                alertBadge.color = AlertColor(newAlert);
                var sym = newAlert == AlertType.Overjoyed ? "*" : "!";
                if (badgeSymbol.text != sym)
                    badgeSymbol.text = sym;
            }
            else if (heldAlert != AlertType.None)
            {
                // Alert cleared — hold badge for minimum duration
                // (timer decremented in Update)
                if (badgeHoldTimer <= 0f)
                {
                    heldAlert = AlertType.None;
                    alertBadge.gameObject.SetActive(false);
                }
            }
        }

        private static float HoldTime(AlertType alert)
        {
            switch (alert)
            {
                case AlertType.Suffocating:
                case AlertType.LowHP:
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

            // Smooth color transitions (RGB only — alpha handled by pulse)
            float t = 1f - Mathf.Exp(-dt / 0.3f);
            var lerpedBorder = Color.Lerp(borderImage.color, targetBorderColor, t);
            float borderAlpha = isPulsing
                ? 0.6f + 0.4f * Mathf.Sin(pulseTimer)
                : Mathf.Lerp(borderImage.color.a, targetBorderColor.a, t);
            borderImage.color = new Color(lerpedBorder.r, lerpedBorder.g, lerpedBorder.b, borderAlpha);
            bgFill.color = Color.Lerp(bgFill.color, targetFillColor, t);

            // Pulse on critical
            if (isPulsing)
                pulseTimer = (pulseTimer + dt * 3f) % (2f * Mathf.PI);

            // Badge hold timer countdown
            if (badgeHoldTimer > 0f)
                badgeHoldTimer -= dt;
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
                case StressTier.Calm:     return Hex(0x4ade80);
                case StressTier.Mild:     return Hex(0x84cc16);
                case StressTier.Stressed: return Hex(0xfbbf24);
                case StressTier.High:     return Hex(0xf97316);
                case StressTier.Critical: return Hex(0xef4444);
                default: return Color.gray;
            }
        }

        public static Color AlertColor(AlertType alert)
        {
            switch (alert)
            {
                case AlertType.Suffocating:  return Hex(0x60a5fa);
                case AlertType.LowHP:        return Hex(0xef4444);
                case AlertType.Scalding:     return Hex(0xfb923c);
                case AlertType.Hypothermia:  return Hex(0x22d3ee);
                case AlertType.Overstressed: return Hex(0xe879f9);
                case AlertType.Diseased:     return Hex(0xa855f7);
                case AlertType.Overjoyed:    return Hex(0xfbbf24);
                case AlertType.Irradiated:    return Hex(0x86efac);
                case AlertType.Starving:      return Hex(0xea580c);
                case AlertType.BladderUrgent: return Hex(0xeab308);
                default: return Color.clear;
            }
        }

        private static Color HealthColor(float hp)
        {
            // 3-segment gradient: green → yellow → orange → red
            // Alpha increases as health drops (0.55 → 0.70)
            var green  = new Color(0.298f, 0.686f, 0.314f, 0.55f);
            var yellow = new Color(0.937f, 0.792f, 0.373f, 0.60f);
            var orange = new Color(0.902f, 0.486f, 0.255f, 0.65f);
            var red    = new Color(0.890f, 0.247f, 0.278f, 0.70f);

            if (hp > 0.6f)
                return Color.Lerp(yellow, green, (hp - 0.6f) / 0.4f);
            if (hp > 0.3f)
                return Color.Lerp(orange, yellow, (hp - 0.3f) / 0.3f);
            return Color.Lerp(red, orange, hp / 0.3f);
        }

        private static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
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
            float c = sz * 0.5f, r = c - 1f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f,
                        Mathf.Clamp01(r - d + 0.5f)));
                }
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
        }

        // ── Helpers ─────────────────────────────────────

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

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(inset, inset);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
