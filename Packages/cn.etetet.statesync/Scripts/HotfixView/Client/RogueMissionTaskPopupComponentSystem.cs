using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    [EntitySystemOf(typeof(RogueMissionTaskPopupComponent))]
    [FriendOf(typeof(RogueMissionTaskPopupComponent))]
    public static partial class RogueMissionTaskPopupComponentSystem
    {
        private const string FontResourcePath = "Fonts/HomeChinese SDF";
        private const string DefaultTitle = "任务委托";
        private const string DefaultDesc = "清理附近刷出的怪物后即可完成委托。";
        private const string DefaultAcceptText = "接受任务";
        private const string DefaultRewardFormat = "完成奖励：{0} 金币";

        [EntitySystem]
        private static void Awake(this RogueMissionTaskPopupComponent self)
        {
            GameObject rootObject = new(
                "RogueMissionTaskPopup",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            self.RootGameObject = rootObject;
            self.Canvas = rootObject.GetComponent<Canvas>();
            self.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            self.Canvas.sortingOrder = 29000;

            CanvasScaler scaler = rootObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            self.CanvasRect = rootObject.transform as RectTransform;
            self.CanvasRect.anchorMin = Vector2.zero;
            self.CanvasRect.anchorMax = Vector2.one;
            self.CanvasRect.offsetMin = Vector2.zero;
            self.CanvasRect.offsetMax = Vector2.zero;
            self.BuildView();
            self.Close();
        }

        [EntitySystem]
        private static void Destroy(this RogueMissionTaskPopupComponent self)
        {
            self.MaskButton?.onClick.RemoveAllListeners();
            self.AcceptButton?.onClick.RemoveAllListeners();
            self.CancelButton?.onClick.RemoveAllListeners();

            if (self.RootGameObject != null)
            {
                UnityEngine.Object.Destroy(self.RootGameObject);
            }

            self.RootGameObject = null;
            self.Canvas = null;
            self.CanvasRect = null;
            self.MaskButton = null;
            self.WindowRect = null;
            self.TitleText = null;
            self.DescText = null;
            self.RewardText = null;
            self.AcceptButton = null;
            self.AcceptButtonText = null;
            self.CancelButton = null;
            self.CurrentPointId = string.Empty;
            self.Visible = false;
        }

        [EntitySystem]
        private static void Update(this RogueMissionTaskPopupComponent self)
        {
            if (!self.Visible)
            {
                return;
            }

            Scene root = self.Root();
            ECAInteractClientComponent runtime = root?.GetComponent<ECAInteractClientComponent>();
            if (runtime == null ||
                string.IsNullOrWhiteSpace(self.CurrentPointId) ||
                !runtime.InRangePointIds.Contains(self.CurrentPointId))
            {
                self.Close();
            }
        }

        public static bool TryOpenFocusPoint(Scene root)
        {
            ECAInteractClientComponent runtime = root?.GetComponent<ECAInteractClientComponent>();
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.FocusPointId))
            {
                return false;
            }

            return TryOpenForPoint(root, runtime.FocusPointId);
        }

        public static bool TryOpenForPoint(Scene root, string pointId)
        {
            if (root == null || root.IsDisposed || string.IsNullOrWhiteSpace(pointId))
            {
                return false;
            }

            if (!TryResolvePopupData(pointId, out string title, out string desc, out string acceptText, out int rewardGold))
            {
                return false;
            }

            RogueMissionTaskPopupComponent popup = root.GetComponent<RogueMissionTaskPopupComponent>() ?? root.AddComponent<RogueMissionTaskPopupComponent>();
            popup.Show(pointId, title, desc, acceptText, rewardGold);
            return true;
        }

        public static void Close(this RogueMissionTaskPopupComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Visible = false;
            self.CurrentPointId = string.Empty;
            if (self.RootGameObject != null)
            {
                self.RootGameObject.SetActive(false);
            }
        }

        private static void Show(this RogueMissionTaskPopupComponent self, string pointId, string title, string desc, string acceptText, int rewardGold)
        {
            if (self == null || self.IsDisposed || self.RootGameObject == null)
            {
                return;
            }

            self.CurrentPointId = pointId;
            self.Visible = true;
            self.TitleText.text = string.IsNullOrWhiteSpace(title) ? DefaultTitle : title;
            self.DescText.text = string.IsNullOrWhiteSpace(desc) ? DefaultDesc : desc;
            self.RewardText.text = string.Format(DefaultRewardFormat, rewardGold);
            self.AcceptButtonText.text = string.IsNullOrWhiteSpace(acceptText) ? DefaultAcceptText : acceptText;
            self.RootGameObject.SetActive(true);
            self.WindowRect?.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(self.WindowRect);
        }

        private static void BuildView(this RogueMissionTaskPopupComponent self)
        {
            EntityRef<RogueMissionTaskPopupComponent> selfRef = self;
            RectTransform mask = CreatePanel("Mask", self.CanvasRect, new Color(0.03f, 0.05f, 0.07f, 0.78f));
            mask.anchorMin = Vector2.zero;
            mask.anchorMax = Vector2.one;
            mask.offsetMin = Vector2.zero;
            mask.offsetMax = Vector2.zero;

            self.MaskButton = mask.gameObject.AddComponent<Button>();
            self.MaskButton.targetGraphic = mask.GetComponent<Image>();
            self.MaskButton.onClick.AddListener(() => CloseByRef(selfRef));

            RectTransform window = CreatePanel("Window", mask, new Color(0.12f, 0.16f, 0.2f, 0.98f));
            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(660f, 0f);
            self.WindowRect = window;

            VerticalLayoutGroup windowLayout = window.gameObject.AddComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(32, 32, 28, 28);
            windowLayout.spacing = 18f;
            windowLayout.childAlignment = TextAnchor.UpperLeft;
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = window.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            self.TitleText = CreateText(self.Root(), "TitleText", window, DefaultTitle, 38f, Color.white, FontStyles.Bold);
            self.TitleText.alignment = TextAlignmentOptions.Left;

            self.DescText = CreateText(self.Root(), "DescText", window, DefaultDesc, 24f, new Color(0.89f, 0.92f, 0.95f, 1f), FontStyles.Normal);
            self.DescText.alignment = TextAlignmentOptions.Left;
            self.DescText.textWrappingMode = TextWrappingModes.Normal;

            self.RewardText = CreateText(self.Root(), "RewardText", window, string.Format(DefaultRewardFormat, 0), 24f, new Color(0.98f, 0.85f, 0.39f, 1f), FontStyles.Bold);
            self.RewardText.alignment = TextAlignmentOptions.Left;

            RectTransform hintPanel = CreatePanel("HintPanel", window, new Color(0.16f, 0.21f, 0.26f, 0.95f));
            hintPanel.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup hintLayout = hintPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            hintLayout.padding = new RectOffset(18, 18, 16, 16);
            hintLayout.spacing = 8f;
            hintLayout.childAlignment = TextAnchor.UpperLeft;
            hintLayout.childControlWidth = true;
            hintLayout.childControlHeight = true;
            hintLayout.childForceExpandWidth = true;
            hintLayout.childForceExpandHeight = false;
            ContentSizeFitter hintFitter = hintPanel.gameObject.AddComponent<ContentSizeFitter>();
            hintFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            hintFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TMP_Text hintTitle = CreateText(self.Root(), "HintTitleText", hintPanel, "接受后立即刷出本波怪物。", 22f, new Color(0.95f, 0.97f, 1f, 1f), FontStyles.Bold);
            hintTitle.alignment = TextAlignmentOptions.Left;
            TMP_Text hintDesc = CreateText(self.Root(), "HintDescText", hintPanel, "任务点一局只能完成一次，清完怪物后自动发放奖励。", 20f, new Color(0.78f, 0.84f, 0.9f, 1f), FontStyles.Normal);
            hintDesc.alignment = TextAlignmentOptions.Left;
            hintDesc.textWrappingMode = TextWrappingModes.Normal;

            RectTransform buttonRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            buttonRow.SetParent(window, false);
            buttonRow.anchorMin = new Vector2(0f, 0.5f);
            buttonRow.anchorMax = new Vector2(1f, 0.5f);
            buttonRow.sizeDelta = new Vector2(0f, 70f);
            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 16f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;

            self.CancelButton = CreateActionButton(self.Root(), "CancelButton", buttonRow, "暂不接受", new Color(0.26f, 0.31f, 0.37f, 1f));
            self.CancelButton.onClick.AddListener(() => CloseByRef(selfRef));

            self.AcceptButton = CreateActionButton(self.Root(), "AcceptButton", buttonRow, DefaultAcceptText, new Color(0.76f, 0.56f, 0.18f, 1f));
            self.AcceptButtonText = self.AcceptButton.GetComponentInChildren<TMP_Text>(true);
            self.AcceptButton.onClick.AddListener(() => AcceptByRef(selfRef));
        }

        private static void CloseByRef(EntityRef<RogueMissionTaskPopupComponent> selfRef)
        {
            RogueMissionTaskPopupComponent self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Close();
        }

        private static void AcceptByRef(EntityRef<RogueMissionTaskPopupComponent> selfRef)
        {
            RogueMissionTaskPopupComponent self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.HandleAcceptClick();
        }

        private static void HandleAcceptClick(this RogueMissionTaskPopupComponent self)
        {
            Scene root = self.Root();
            string pointId = self.CurrentPointId;
            self.Close();
            if (root == null || root.IsDisposed || string.IsNullOrWhiteSpace(pointId))
            {
                return;
            }

            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime != null)
            {
                runtime.FocusPointId = pointId;
            }

            Log.Info($"[RogueMissionTask] accept popup confirm: point={pointId}");
            ECAInteractHelper.TryInteractFocus(root).Coroutine();
        }

        private static bool TryResolvePopupData(string pointId, out string title, out string desc, out string acceptText, out int rewardGold)
        {
            title = DefaultTitle;
            desc = DefaultDesc;
            acceptText = DefaultAcceptText;
            rewardGold = 0;

            ECAPointMarker marker = FindMarker(pointId);
            if (marker == null || !TryResolveTaskConfig(marker.Params, out rewardGold))
            {
                return false;
            }

            if (FlowParamHelper.TryGetStringParam(marker.Params, RogueMissionTaskPointParamKey.Title, out string configuredTitle))
            {
                title = configuredTitle;
            }

            if (FlowParamHelper.TryGetStringParam(marker.Params, RogueMissionTaskPointParamKey.Desc, out string configuredDesc))
            {
                desc = configuredDesc;
            }

            if (FlowParamHelper.TryGetStringParam(marker.Params, RogueMissionTaskPointParamKey.AcceptText, out string configuredAcceptText))
            {
                acceptText = configuredAcceptText;
            }

            return true;
        }

        private static bool TryResolveTaskConfig(List<FlowParam> pointParams, out int rewardGold)
        {
            rewardGold = 0;
            return FlowParamHelper.TryGetIntParam(pointParams, RogueMissionTaskPointParamKey.MonsterUnitConfigId, out int monsterUnitConfigId) &&
                   monsterUnitConfigId > 0 &&
                   FlowParamHelper.TryGetIntParam(pointParams, RogueMissionTaskPointParamKey.MonsterCount, out int monsterCount) &&
                   monsterCount > 0 &&
                   FlowParamHelper.TryGetIntParam(pointParams, RogueMissionTaskPointParamKey.RewardGold, out rewardGold) &&
                   rewardGold > 0;
        }

        private static ECAPointMarker FindMarker(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId))
            {
                return null;
            }

            ECAPointMarker[] markers = GameObject.FindObjectsByType<ECAPointMarker>(FindObjectsSortMode.None);
            foreach (ECAPointMarker marker in markers)
            {
                if (marker != null && string.Equals(marker.ConfigId, pointId))
                {
                    return marker;
                }
            }

            return null;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return go.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI CreateText(
            Scene root,
            string name,
            Transform parent,
            string text,
            float fontSize,
            Color color,
            FontStyles fontStyles)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();

            TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>(FontResourcePath);
            if (fontAsset != null)
            {
                tmp.font = fontAsset;
                tmp.fontSharedMaterial = fontAsset.material;
            }
            else
            {
                TMP_Text template = ResolveTextTemplate(root);
                if (template != null)
                {
                    tmp.font = template.font;
                    tmp.fontSharedMaterial = template.fontSharedMaterial;
                }
            }

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = fontStyles;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateActionButton(Scene root, string name, Transform parent, string text, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 58f;
            layout.flexibleWidth = 1f;

            Image image = go.GetComponent<Image>();
            image.color = color;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.08f;
            colors.pressedColor = color * 0.9f;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.24f, 0.27f, 0.31f, 0.75f);
            button.colors = colors;

            TMP_Text label = CreateText(root, "Label", go.transform, text, 22f, Color.white, FontStyles.Bold);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 8f);
            labelRect.offsetMax = new Vector2(-12f, -8f);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static TMP_Text ResolveTextTemplate(Scene root)
        {
            MainPanelComponent mainPanel = root?.YIUIMgr()?.GetPanel<MainPanelComponent>();
            TMP_Text template = mainPanel?.UIBase?.OwnerGameObject?.GetComponentInChildren<TMP_Text>(true);
            if (template != null)
            {
                return template;
            }

            TextMeshProUGUI[] texts = GameObject.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            return texts.Length > 0 ? texts[0] : null;
        }
    }
}
