using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DuplicantStatusBar.Config;
using DuplicantStatusBar.Data;
using DSB = DuplicantStatusBar.Localization.STRINGS.DUPLICANTSTATUSBAR;

namespace DuplicantStatusBar.UI
{
    /// <summary>
    /// Drop-down popup for sort mode selection and per-dupe visibility filtering.
    /// Opened from the header menu button, anchored below the bar panel.
    /// </summary>
    internal static class SortFilterPopup
    {
        private static GameObject popupGO;
        private static GameObject blockerGO;
        private static RectTransform popupRT;
        private static Transform filterContent;
        private static Transform roleContent;

        // Sort section state
        private static SortOrder pendingSort;
        private static readonly List<TextMeshProUGUI> sortLabels = new List<TextMeshProUGUI>();
        private static readonly SortOrder[] sortValues = (SortOrder[])Enum.GetValues(typeof(SortOrder));

        // Smart filter state (pending)
        private static bool pendingAlertsOnly;
        private static bool pendingStressedOnly;
        private static TextMeshProUGUI alertsOnlyLabel;
        private static TextMeshProUGUI stressedOnlyLabel;

        // Role filter state (pending, rebuilt on open)
        private static readonly List<string> roleKeys = new List<string>();
        private static readonly List<string> roleDisplayNames = new List<string>();
        private static readonly List<bool> roleVisible = new List<bool>();
        private static readonly List<TextMeshProUGUI> roleLabels = new List<TextMeshProUGUI>();

        // Per-dupe filter state
        private static readonly List<string> filterNames = new List<string>();
        private static readonly List<bool> filterVisible = new List<bool>();
        private static readonly List<TextMeshProUGUI> filterLabels = new List<TextMeshProUGUI>();

        /// <summary>Set of dupe names currently hidden. Checked by DupeStatusTracker.</summary>
        public static readonly HashSet<string> HiddenDupes = new HashSet<string>();

        // Active (committed) smart filter state — read by DupeStatusTracker
        public static bool AlertsOnly { get; private set; }
        public static bool StressedOnly { get; private set; }
        public static readonly HashSet<string> HiddenRoles = new HashSet<string>();

        private const string PREFS_KEY = "DSB_HiddenDupes";
        private const string PREFS_ALERTS = "DSB_AlertsOnly";
        private const string PREFS_STRESSED = "DSB_StressedOnly";
        private const string PREFS_ROLES = "DSB_HiddenRoles";

        public static void Init(Transform canvasRoot)
        {
            LoadHiddenDupes();
            LoadSmartFilters();

            // Fullscreen click-away blocker
            blockerGO = new GameObject("DSB_PopupBlocker");
            blockerGO.transform.SetParent(canvasRoot, false);
            var blockerRT = blockerGO.AddComponent<RectTransform>();
            blockerRT.anchorMin = Vector2.zero;
            blockerRT.anchorMax = Vector2.one;
            blockerRT.sizeDelta = Vector2.zero;
            var blockerImg = blockerGO.AddComponent<Image>();
            blockerImg.color = Color.clear;
            var blockerBtn = blockerGO.AddComponent<Button>();
            blockerBtn.onClick.AddListener(Close);
            blockerGO.SetActive(false);

            // Popup panel
            popupGO = new GameObject("DSB_SortFilterPopup");
            popupGO.transform.SetParent(canvasRoot, false);
            popupRT = popupGO.AddComponent<RectTransform>();
            popupRT.pivot = new Vector2(0.5f, 1f);

            var bg = popupGO.AddComponent<Image>();
            bg.sprite = DupePortraitWidget.RoundedRect;
            bg.type = Image.Type.Sliced;
            bg.color = ColorUtil.WithAlpha(ColorUtil.CardBg, 0.97f);

            var vlg = popupGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 6, 6);
            vlg.spacing = 2;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = popupGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildSortSection(popupGO.transform);
            BuildSeparator(popupGO.transform);
            BuildSmartFilters(popupGO.transform);
            BuildSeparator(popupGO.transform);
            BuildFilterSection(popupGO.transform);
            BuildSeparator(popupGO.transform);
            BuildButtons(popupGO.transform);

            popupGO.SetActive(false);
        }

        public static void Toggle(RectTransform anchor)
        {
            if (popupGO == null) return;

            if (popupGO.activeSelf)
            {
                Close();
                return;
            }

            // Initialize pending state from current options
            pendingSort = StatusBarOptions.Instance.SortOrder;
            pendingAlertsOnly = AlertsOnly;
            pendingStressedOnly = StressedOnly;
            RefreshSortVisuals();
            RefreshSmartFilterVisuals();
            RebuildRoleList();
            RebuildFilterList();

            // Position below the bar panel
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            float cx = (corners[0].x + corners[2].x) * 0.5f;
            float bot = corners[0].y;

            blockerGO.SetActive(true);
            blockerGO.transform.SetAsLastSibling();
            popupGO.SetActive(true);
            popupGO.transform.SetAsLastSibling();
            popupRT.position = new Vector3(cx, bot - 2f, 0f);

            // Clamp to screen after layout
            Canvas.ForceUpdateCanvases();
            ClampToScreen();
        }

        // ── Sort Section ───────────────────────────────────

        private static void BuildSortSection(Transform parent)
        {
            AddHeader(parent, () => DSB.UI.POPUP_SORTBY);

            sortLabels.Clear();
            for (int i = 0; i < sortValues.Length; i++)
            {
                int idx = i;
                var item = AddClickableItem(parent, SortDisplayName(sortValues[i]),
                    () => { pendingSort = sortValues[idx]; RefreshSortVisuals(); });
                sortLabels.Add(item);
            }
        }

        private static void RefreshSortVisuals()
        {
            for (int i = 0; i < sortLabels.Count; i++)
            {
                bool selected = sortValues[i] == pendingSort;
                var label = sortLabels[i];
                string prefix = selected ? "\u25CF " : "\u25CB "; // ● vs ○
                label.text = prefix + SortDisplayName(sortValues[i]);
                label.color = selected ? Color.white : ColorUtil.TextMuted;
            }
        }

        private static string SortDisplayName(SortOrder order)
        {
            switch (order)
            {
                case SortOrder.StressDescending: return DSB.OPTIONS.SORTMODE.STRESSDESCENDING;
                case SortOrder.Alphabetical:     return DSB.OPTIONS.SORTMODE.ALPHABETICAL;
                case SortOrder.Role:             return DSB.OPTIONS.SORTMODE.ROLE;
                case SortOrder.CaloriesAscending: return DSB.OPTIONS.SORTMODE.CALORIESASCENDING;
                default: return order.ToString();
            }
        }

        // ── Smart Filters ──────────────────────────────────

        private static void BuildSmartFilters(Transform parent)
        {
            AddHeader(parent, () => DSB.UI.POPUP_QUICKFILTERS);

            alertsOnlyLabel = AddClickableItem(parent, "",
                () => { pendingAlertsOnly = !pendingAlertsOnly; RefreshSmartFilterVisuals(); });

            stressedOnlyLabel = AddClickableItem(parent, "",
                () => { pendingStressedOnly = !pendingStressedOnly; RefreshSmartFilterVisuals(); });

            RefreshSmartFilterVisuals();

            // Roles sub-header
            AddHeader(parent, () => DSB.UI.POPUP_ROLES);

            // Scroll area for role items
            var scrollGO = new GameObject("RoleScroll");
            scrollGO.transform.SetParent(parent, false);
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredWidth = 140;
            scrollLE.preferredHeight = 60;

            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.15f);
            scrollGO.AddComponent<Mask>().showMaskGraphic = true;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);

            var contentVLG = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVLG.padding = new RectOffset(4, 4, 2, 2);
            contentVLG.spacing = 1;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            roleContent = contentGO.transform;
        }

        private static void RefreshSmartFilterVisuals()
        {
            string aPrefix = pendingAlertsOnly ? "\u2713 " : "\u2717 ";
            alertsOnlyLabel.text = aPrefix + (string)DSB.UI.POPUP_ALERTSONLY;
            alertsOnlyLabel.color = pendingAlertsOnly ? Color.white : ColorUtil.TextMuted;

            string sPrefix = pendingStressedOnly ? "\u2713 " : "\u2717 ";
            stressedOnlyLabel.text = sPrefix + (string)DSB.UI.POPUP_STRESSEDONLY;
            stressedOnlyLabel.color = pendingStressedOnly ? Color.white : ColorUtil.TextMuted;
        }

        private static void RebuildRoleList()
        {
            for (int i = roleContent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(roleContent.GetChild(i).gameObject);

            roleKeys.Clear();
            roleDisplayNames.Clear();
            roleVisible.Clear();
            roleLabels.Clear();

            var roles = GetAllRoles();
            for (int i = 0; i < roles.Count; i++)
            {
                int idx = i;
                string key = roles[i].Key;
                string display = roles[i].Value;
                bool visible = !HiddenRoles.Contains(key);

                roleKeys.Add(key);
                roleDisplayNames.Add(display);
                roleVisible.Add(visible);

                var item = AddClickableItem(roleContent, "", () => ToggleRole(idx));
                item.fontSize = 10;
                roleLabels.Add(item);
                UpdateRoleItemVisual(idx);
            }
        }

        private static void ToggleRole(int idx)
        {
            if (idx < 0 || idx >= roleVisible.Count) return;
            roleVisible[idx] = !roleVisible[idx];
            UpdateRoleItemVisual(idx);
        }

        private static void UpdateRoleItemVisual(int idx)
        {
            bool vis = roleVisible[idx];
            string prefix = vis ? "\u2713 " : "\u2717 ";
            roleLabels[idx].text = prefix + roleDisplayNames[idx];
            roleLabels[idx].color = vis ? Color.white : new Color(0.6f, 0.4f, 0.4f);
        }

        private static List<KeyValuePair<string, string>> GetAllRoles()
        {
            var result = new List<KeyValuePair<string, string>>();
            var seen = new HashSet<string>();

            if (Components.LiveMinionIdentities.Count == 0) return result;
            int worldId = ClusterManager.Instance?.activeWorldId ?? -1;
            if (worldId < 0) return result;

            var dupes = Components.LiveMinionIdentities.GetWorldItems(worldId);
            if (dupes == null) return result;

            foreach (var identity in dupes)
            {
                if (identity?.gameObject == null) continue;
                var resume = identity.gameObject.GetComponent<MinionResume>();
                string hat = resume?.CurrentHat ?? "";
                if (!seen.Add(hat)) continue;

                string display;
                if (string.IsNullOrEmpty(hat))
                {
                    display = DSB.UI.POPUP_NOROLE;
                }
                else
                {
                    display = hat;
                    // Try to find a readable name from the skill database
                    var db = Db.Get();
                    if (db?.Skills != null)
                    {
                        foreach (var skill in db.Skills.resources)
                        {
                            if (skill.hat == hat)
                            {
                                display = skill.Name;
                                break;
                            }
                        }
                    }
                }
                result.Add(new KeyValuePair<string, string>(hat, display));
            }

            result.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.Ordinal));
            return result;
        }

        // ── Filter Section (Per-Dupe) ─────────────────────

        private static void BuildFilterSection(Transform parent)
        {
            // Header row with "Show All" button
            var headerRow = new GameObject("FilterHeader");
            headerRow.transform.SetParent(parent, false);
            var hlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            AddHeaderInto(headerRow.transform, () => DSB.UI.POPUP_DUPES);

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(headerRow.transform, false);
            var spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleWidth = 1;

            // Show All button
            var showAllGO = new GameObject("ShowAll");
            showAllGO.transform.SetParent(headerRow.transform, false);
            var showAllTMP = showAllGO.AddComponent<TextMeshProUGUI>();
            showAllTMP.text = DSB.UI.POPUP_SHOWALL;
            showAllTMP.fontSize = 9;
            showAllTMP.color = ColorUtil.Hex(ColorUtil.Blue);
            if (StatusBarScreen.GameFont != null) showAllTMP.font = StatusBarScreen.GameFont;
            showAllTMP.alignment = TextAlignmentOptions.MidlineRight;
            var showAllBtn = showAllGO.AddComponent<Button>();
            showAllBtn.onClick.AddListener(ShowAllDupes);
            var showAllLE = showAllGO.AddComponent<LayoutElement>();
            showAllLE.preferredHeight = 14;

            // Scroll area for filter items
            var scrollGO = new GameObject("FilterScroll");
            scrollGO.transform.SetParent(parent, false);
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredWidth = 140;
            scrollLE.preferredHeight = 120;

            var scrollRT = scrollGO.GetComponent<RectTransform>();
            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.15f);
            scrollGO.AddComponent<Mask>().showMaskGraphic = true;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);

            var contentVLG = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVLG.padding = new RectOffset(4, 4, 2, 2);
            contentVLG.spacing = 1;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            filterContent = contentGO.transform;
        }

        private static void RebuildFilterList()
        {
            // Clear old items
            for (int i = filterContent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(filterContent.GetChild(i).gameObject);

            filterNames.Clear();
            filterVisible.Clear();
            filterLabels.Clear();

            // Build from current dupe list (all dupes, including hidden ones)
            var dupes = GetAllDupeNames();
            for (int i = 0; i < dupes.Count; i++)
            {
                int idx = i;
                string name = dupes[i];
                bool visible = !HiddenDupes.Contains(name);

                filterNames.Add(name);
                filterVisible.Add(visible);

                var item = AddClickableItem(filterContent, "", () => ToggleFilter(idx));
                filterLabels.Add(item);
                item.fontSize = 10;
                UpdateFilterItemVisual(idx);
            }
        }

        private static void ToggleFilter(int idx)
        {
            if (idx < 0 || idx >= filterVisible.Count) return;
            filterVisible[idx] = !filterVisible[idx];
            UpdateFilterItemVisual(idx);
        }

        private static void UpdateFilterItemVisual(int idx)
        {
            bool vis = filterVisible[idx];
            string prefix = vis ? "\u2713 " : "\u2717 "; // ✓ vs ✗
            filterLabels[idx].text = prefix + filterNames[idx];
            filterLabels[idx].color = vis ? Color.white : new Color(0.6f, 0.4f, 0.4f);
        }

        private static void ShowAllDupes()
        {
            for (int i = 0; i < filterVisible.Count; i++)
            {
                filterVisible[i] = true;
                UpdateFilterItemVisual(i);
            }
        }

        private static List<string> GetAllDupeNames()
        {
            var names = new List<string>();
            if (Components.LiveMinionIdentities.Count == 0) return names;

            int worldId = ClusterManager.Instance?.activeWorldId ?? -1;
            if (worldId < 0) return names;

            var dupes = Components.LiveMinionIdentities.GetWorldItems(worldId);
            if (dupes == null) return names;

            foreach (var identity in dupes)
            {
                if (identity?.gameObject == null) continue;
                string name = identity.gameObject.GetProperName() ?? "???";
                names.Add(name);
            }

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        // ── Buttons ────────────────────────────────────────

        private static void BuildButtons(Transform parent)
        {
            var row = new GameObject("Buttons");
            row.transform.SetParent(parent, false);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.padding = new RectOffset(0, 0, 2, 0);

            AddButton(row.transform, () => DSB.UI.POPUP_APPLY, Apply);
            AddButton(row.transform, () => DSB.UI.POPUP_RESET, Reset);
        }

        private static void Apply()
        {
            // Commit sort
            StatusBarOptions.Instance.SortOrder = pendingSort;
            DupeStatusTracker.SortSnapshots();

            // Commit smart filters
            AlertsOnly = pendingAlertsOnly;
            StressedOnly = pendingStressedOnly;
            HiddenRoles.Clear();
            for (int i = 0; i < roleKeys.Count; i++)
            {
                if (!roleVisible[i])
                    HiddenRoles.Add(roleKeys[i]);
            }

            // Commit per-dupe filter
            HiddenDupes.Clear();
            for (int i = 0; i < filterNames.Count; i++)
            {
                if (!filterVisible[i])
                    HiddenDupes.Add(filterNames[i]);
            }

            SaveHiddenDupes();
            SaveSmartFilters();
            Close();
        }

        private static void Reset()
        {
            pendingSort = SortOrder.StressDescending;
            pendingAlertsOnly = false;
            pendingStressedOnly = false;
            RefreshSortVisuals();
            RefreshSmartFilterVisuals();
            for (int i = 0; i < roleVisible.Count; i++)
            {
                roleVisible[i] = true;
                UpdateRoleItemVisual(i);
            }
            ShowAllDupes();
        }

        private static void Close()
        {
            if (popupGO != null) popupGO.SetActive(false);
            if (blockerGO != null) blockerGO.SetActive(false);
        }

        // ── Persistence ────────────────────────────────────

        private static void SaveHiddenDupes()
        {
            if (HiddenDupes.Count == 0)
            {
                PlayerPrefs.DeleteKey(PREFS_KEY);
            }
            else
            {
                var arr = new string[HiddenDupes.Count];
                HiddenDupes.CopyTo(arr);
                PlayerPrefs.SetString(PREFS_KEY, string.Join(",", arr));
            }
            PlayerPrefs.Save();
        }

        private static void SaveSmartFilters()
        {
            PlayerPrefs.SetInt(PREFS_ALERTS, AlertsOnly ? 1 : 0);
            PlayerPrefs.SetInt(PREFS_STRESSED, StressedOnly ? 1 : 0);
            if (HiddenRoles.Count == 0)
            {
                PlayerPrefs.DeleteKey(PREFS_ROLES);
            }
            else
            {
                var arr = new string[HiddenRoles.Count];
                HiddenRoles.CopyTo(arr);
                PlayerPrefs.SetString(PREFS_ROLES, string.Join(",", arr));
            }
            PlayerPrefs.Save();
        }

        private static void LoadSmartFilters()
        {
            AlertsOnly = PlayerPrefs.GetInt(PREFS_ALERTS, 0) == 1;
            StressedOnly = PlayerPrefs.GetInt(PREFS_STRESSED, 0) == 1;
            HiddenRoles.Clear();
            string saved = PlayerPrefs.GetString(PREFS_ROLES, "");
            if (!string.IsNullOrEmpty(saved))
            {
                foreach (var role in saved.Split(','))
                    if (role != null) HiddenRoles.Add(role);
            }
        }

        private static void LoadHiddenDupes()
        {
            HiddenDupes.Clear();
            string saved = PlayerPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrEmpty(saved)) return;
            foreach (var name in saved.Split(','))
            {
                if (!string.IsNullOrEmpty(name))
                    HiddenDupes.Add(name);
            }
        }

        // ── UI Helpers ─────────────────────────────────────

        private static void ClampToScreen()
        {
            Vector3[] c = new Vector3[4];
            popupRT.GetWorldCorners(c);
            Vector3 pos = popupRT.position;
            if (c[0].x < 0f) pos.x -= c[0].x;
            if (c[2].x > Screen.width) pos.x -= (c[2].x - Screen.width);
            if (c[0].y < 0f) pos.y -= c[0].y;
            popupRT.position = pos;
        }

        private static void AddHeader(Transform parent, Func<string> getText)
        {
            var go = new GameObject("Header");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = getText();
            tmp.fontSize = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            if (StatusBarScreen.GameFont != null) tmp.font = StatusBarScreen.GameFont;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 16;
        }

        private static void AddHeaderInto(Transform parent, Func<string> getText)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = getText();
            tmp.fontSize = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            if (StatusBarScreen.GameFont != null) tmp.font = StatusBarScreen.GameFont;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 16;
        }

        private static TextMeshProUGUI AddClickableItem(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Item");
            go.transform.SetParent(parent, false);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            // Invisible background for click target
            var img = go.AddComponent<Image>();
            img.color = Color.clear;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 10;
            tmp.color = Color.white;
            if (StatusBarScreen.GameFont != null) tmp.font = StatusBarScreen.GameFont;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;

            var tRT = textGO.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(2f, 0f);
            tRT.offsetMax = Vector2.zero;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 14;

            return tmp;
        }

        private static void AddButton(Transform parent, Func<string> getText, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.sprite = DupePortraitWidget.RoundedRect;
            img.type = Image.Type.Sliced;
            img.color = ColorUtil.HeaderBg;

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            btn.targetGraphic = img;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = getText();
            tmp.fontSize = 10;
            tmp.color = Color.white;
            if (StatusBarScreen.GameFont != null) tmp.font = StatusBarScreen.GameFont;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            var tRT = textGO.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 18;
        }

        private static void BuildSeparator(Transform parent)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.1f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1;
        }

        public static void Cleanup()
        {
            if (popupGO != null) UnityEngine.Object.Destroy(popupGO);
            if (blockerGO != null) UnityEngine.Object.Destroy(blockerGO);
            popupGO = null;
            blockerGO = null;
            sortLabels.Clear();
            filterLabels.Clear();
            filterNames.Clear();
            filterVisible.Clear();
        }
    }
}
