using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Editor
{
    public static class BattleRecordYiuiBuilder
    {
        private const string LobbyPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/LobbyPanel.prefab";
        private const string BattleRecordPanelPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Common/Prefabs/BattleRecordPanel.prefab";
        private const string BattleRecordComponentGenPath =
            "Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Common/BattleRecordPanelComponentGen.cs";
        private const string BattleRecordSystemGenPath =
            "Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUIGen/Common/BattleRecordPanelComponentSystemGen.cs";

        [InitializeOnLoadMethod]
        private static void ScheduleAutoBuild()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        [MenuItem("ET/YIUI/Build BattleRecord Resources")]
        public static void Build()
        {
            try
            {
                StyleContext style = LoadStyleContext();
                BuildLobbyPanel(style);
                BuildBattleRecordPanel(style);
                GenerateYiuiCode();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[BattleRecordYiuiBuilder] BattleRecord YIUI resources generated.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BattleRecordYiuiBuilder] Build failed: {exception}");
                throw;
            }
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoBuildIfNeeded;
                return;
            }

            if (!NeedBuild())
            {
                return;
            }

            Debug.Log("[BattleRecordYiuiBuilder] Auto build triggered.");
            Build();
        }

        private static bool NeedBuild()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), BattleRecordPanelPrefabPath)))
            {
                return true;
            }

            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), BattleRecordComponentGenPath)))
            {
                return true;
            }

            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), BattleRecordSystemGenPath)))
            {
                return true;
            }

            string lobbyPrefabFullPath = Path.Combine(Directory.GetCurrentDirectory(), LobbyPrefabPath);
            if (!File.Exists(lobbyPrefabFullPath))
            {
                return true;
            }

            string lobbyText = File.ReadAllText(lobbyPrefabFullPath);
            return !lobbyText.Contains("BattleRecordButton") || !lobbyText.Contains("BattleRecordOverlayRoot");
        }

        private static void BuildLobbyPanel(StyleContext style)
        {
            GameObject lobbyRoot = PrefabUtility.LoadPrefabContents(LobbyPrefabPath);
            try
            {
                RectTransform matchPanel = FindRequiredRect(lobbyRoot.transform, "MatchPanel");
                UIBindCDETable cdeTable = lobbyRoot.GetComponent<UIBindCDETable>();
                if (cdeTable == null)
                {
                    throw new InvalidOperationException("LobbyPanel missing UIBindCDETable.");
                }

                cdeTable.ComponentTable ??= lobbyRoot.GetOrAddComponent<UIBindComponentTable>();

                Button battleRecordButton = EnsureBattleRecordButton(matchPanel, style);
                RectTransform overlayRoot = EnsureBattleRecordOverlayRoot(matchPanel);

                EnsureBinding(cdeTable.ComponentTable, battleRecordButton, "BattleRecordButton");
                EnsureBinding(cdeTable.ComponentTable, overlayRoot, "BattleRecordOverlayRoot");

                cdeTable.ComponentTable.AutoCheck();
                EditorUtility.SetDirty(lobbyRoot);
                PrefabUtility.SaveAsPrefabAsset(lobbyRoot, LobbyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(lobbyRoot);
            }
        }

        private static void BuildBattleRecordPanel(StyleContext style)
        {
            string directory = Path.GetDirectoryName(BattleRecordPanelPrefabPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), directory));
            }

            GameObject root = new GameObject("BattleRecordPanel", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                SetLayerRecursively(root, LayerMask.NameToLayer("UI"));

                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);

                UIBindCDETable cdeTable = root.GetOrAddComponent<UIBindCDETable>();
                cdeTable.UICodeType = EUICodeType.Common;
                cdeTable.PkgName = "Common";
                cdeTable.ResName = "BattleRecordPanel";
                cdeTable.ComponentTable = root.GetOrAddComponent<UIBindComponentTable>();

                Button maskButton = CreateFullScreenMask(rootRect, style);
                EnsureBinding(cdeTable.ComponentTable, maskButton, "MaskButton");

                RectTransform windowRoot = CreateRect("WindowRoot", rootRect);
                windowRoot.anchorMin = new Vector2(0.5f, 0.5f);
                windowRoot.anchorMax = new Vector2(0.5f, 0.5f);
                windowRoot.pivot = new Vector2(0.5f, 0.5f);
                windowRoot.anchoredPosition = Vector2.zero;
                windowRoot.sizeDelta = new Vector2(1220f, 780f);
                Image windowImage = windowRoot.gameObject.AddComponent<Image>();
                windowImage.color = new Color32(22, 26, 34, 242);
                EnsureBinding(cdeTable.ComponentTable, windowRoot, "WindowRoot");

                RectTransform headerRoot = CreateRect("HeaderRoot", windowRoot);
                headerRoot.anchorMin = new Vector2(0f, 1f);
                headerRoot.anchorMax = new Vector2(1f, 1f);
                headerRoot.pivot = new Vector2(0.5f, 1f);
                headerRoot.offsetMin = new Vector2(24f, -80f);
                headerRoot.offsetMax = new Vector2(-24f, -24f);

                TextMeshProUGUI titleText = CreateText("TitleText", headerRoot, style, 34f, FontStyles.Bold);
                titleText.text = "战绩总览";
                titleText.alignment = TextAlignmentOptions.Left;
                Stretch(titleText.rectTransform, new Vector2(0f, 0f), new Vector2(-140f, 0f));

                Button closeButton = CreateButton("CloseButton", headerRoot, style);
                RectTransform closeButtonRect = closeButton.transform as RectTransform;
                closeButtonRect.anchorMin = new Vector2(1f, 0.5f);
                closeButtonRect.anchorMax = new Vector2(1f, 0.5f);
                closeButtonRect.pivot = new Vector2(0.5f, 0.5f);
                closeButtonRect.sizeDelta = new Vector2(120f, 56f);
                closeButtonRect.anchoredPosition = new Vector2(-60f, 0f);
                SetButtonVisual(closeButton, new Color32(186, 73, 73, 255), style.ButtonSprite, style.ButtonColors);
                TextMeshProUGUI closeText = CreateText("Text", closeButtonRect, style, 24f, FontStyles.Bold);
                closeText.text = "关闭";
                closeText.alignment = TextAlignmentOptions.Center;
                closeText.color = Color.white;
                Stretch(closeText.rectTransform);
                EnsureBinding(cdeTable.ComponentTable, closeButton, "CloseButton");

                RectTransform hintRoot = CreateRect("HintRoot", windowRoot);
                hintRoot.anchorMin = new Vector2(0f, 1f);
                hintRoot.anchorMax = new Vector2(1f, 1f);
                hintRoot.pivot = new Vector2(0.5f, 1f);
                hintRoot.offsetMin = new Vector2(24f, -152f);
                hintRoot.offsetMax = new Vector2(-24f, -96f);
                Image hintImage = hintRoot.gameObject.AddComponent<Image>();
                hintImage.color = new Color32(49, 56, 73, 255);

                TextMeshProUGUI hintText = CreateText("HintText", hintRoot, style, 22f, FontStyles.Normal);
                hintText.text = "当前档案为运行时内存，进程重启后会清空。";
                hintText.alignment = TextAlignmentOptions.MidlineLeft;
                Stretch(hintText.rectTransform, new Vector2(16f, 0f), new Vector2(-16f, 0f));
                EnsureBinding(cdeTable.ComponentTable, hintText, "HintText");

                RectTransform listPanel = CreatePanel("ListPanel", windowRoot, new Color32(36, 42, 56, 255));
                listPanel.anchorMin = new Vector2(0f, 0f);
                listPanel.anchorMax = new Vector2(0f, 1f);
                listPanel.pivot = new Vector2(0f, 0.5f);
                listPanel.offsetMin = new Vector2(24f, 24f);
                listPanel.offsetMax = new Vector2(346f, -168f);

                TextMeshProUGUI listTitle = CreateText("ListTitleText", listPanel, style, 28f, FontStyles.Bold);
                listTitle.text = "最近对局";
                listTitle.alignment = TextAlignmentOptions.Left;
                listTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
                listTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
                listTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
                listTitle.rectTransform.offsetMin = new Vector2(18f, -60f);
                listTitle.rectTransform.offsetMax = new Vector2(-18f, -18f);

                RectTransform listScrollRoot = CreatePanel("ListScroll", listPanel, new Color32(20, 24, 31, 255));
                Stretch(listScrollRoot, new Vector2(18f, 18f), new Vector2(-18f, -70f));
                ScrollRect listScrollRect = listScrollRoot.gameObject.AddComponent<ScrollRect>();
                listScrollRect.horizontal = false;
                listScrollRect.vertical = true;
                listScrollRect.movementType = ScrollRect.MovementType.Clamped;

                RectTransform listViewport = CreatePanel("Viewport", listScrollRoot, new Color(0f, 0f, 0f, 0f));
                Stretch(listViewport);
                listViewport.gameObject.AddComponent<RectMask2D>();

                RectTransform listContent = CreateRect("Content", listViewport);
                listContent.anchorMin = new Vector2(0f, 1f);
                listContent.anchorMax = new Vector2(1f, 1f);
                listContent.pivot = new Vector2(0.5f, 1f);
                listContent.offsetMin = Vector2.zero;
                listContent.offsetMax = Vector2.zero;
                VerticalLayoutGroup listLayout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
                listLayout.spacing = 12f;
                listLayout.padding = new RectOffset(0, 0, 0, 0);
                listLayout.childAlignment = TextAnchor.UpperCenter;
                listLayout.childControlWidth = true;
                listLayout.childControlHeight = false;
                listLayout.childForceExpandWidth = true;
                listLayout.childForceExpandHeight = false;
                ContentSizeFitter listSizeFitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
                listSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                listSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                listScrollRect.viewport = listViewport;
                listScrollRect.content = listContent;
                EnsureBinding(cdeTable.ComponentTable, listContent, "ListContent");

                Button listItemTemplate = CreateButton("ListItemTemplate", listContent, style);
                RectTransform listItemRect = listItemTemplate.transform as RectTransform;
                listItemRect.anchorMin = new Vector2(0f, 1f);
                listItemRect.anchorMax = new Vector2(1f, 1f);
                listItemRect.pivot = new Vector2(0.5f, 1f);
                listItemRect.sizeDelta = new Vector2(0f, 104f);
                LayoutElement listItemLayout = listItemTemplate.gameObject.AddComponent<LayoutElement>();
                listItemLayout.preferredHeight = 104f;
                SetButtonVisual(listItemTemplate, new Color32(62, 72, 92, 255), null, style.ButtonColors);
                TextMeshProUGUI listItemText = CreateText("Label", listItemRect, style, 24f, FontStyles.Normal);
                listItemText.alignment = TextAlignmentOptions.Left;
                listItemText.textWrappingMode = TextWrappingModes.Normal;
                listItemText.overflowMode = TextOverflowModes.Ellipsis;
                Stretch(listItemText.rectTransform, new Vector2(18f, -10f), new Vector2(-18f, 10f));
                listItemTemplate.gameObject.SetActive(false);
                EnsureBinding(cdeTable.ComponentTable, listItemRect, "ListItemTemplate");

                TextMeshProUGUI listEmptyText = CreateText("ListEmptyText", listPanel, style, 26f, FontStyles.Normal);
                listEmptyText.text = "暂无战绩";
                listEmptyText.alignment = TextAlignmentOptions.Center;
                Stretch(listEmptyText.rectTransform, new Vector2(18f, 18f), new Vector2(-18f, -70f));
                EnsureBinding(cdeTable.ComponentTable, listEmptyText, "ListEmptyText");

                RectTransform detailPanel = CreatePanel("DetailPanel", windowRoot, new Color32(36, 42, 56, 255));
                Stretch(detailPanel, new Vector2(370f, 24f), new Vector2(-24f, -168f));

                TextMeshProUGUI detailSummaryText = CreateText("DetailSummaryText", detailPanel, style, 24f, FontStyles.Normal);
                detailSummaryText.text = "选择左侧一场对局查看详情。";
                detailSummaryText.alignment = TextAlignmentOptions.TopLeft;
                detailSummaryText.textWrappingMode = TextWrappingModes.Normal;
                detailSummaryText.rectTransform.anchorMin = new Vector2(0f, 1f);
                detailSummaryText.rectTransform.anchorMax = new Vector2(1f, 1f);
                detailSummaryText.rectTransform.pivot = new Vector2(0.5f, 1f);
                detailSummaryText.rectTransform.offsetMin = new Vector2(20f, -190f);
                detailSummaryText.rectTransform.offsetMax = new Vector2(-20f, -20f);
                EnsureBinding(cdeTable.ComponentTable, detailSummaryText, "DetailSummaryText");

                TextMeshProUGUI timelineTitle = CreateText("TimelineTitleText", detailPanel, style, 26f, FontStyles.Bold);
                timelineTitle.text = "事件时间轴";
                timelineTitle.alignment = TextAlignmentOptions.Left;
                timelineTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
                timelineTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
                timelineTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
                timelineTitle.rectTransform.offsetMin = new Vector2(20f, -232f);
                timelineTitle.rectTransform.offsetMax = new Vector2(-20f, -190f);

                RectTransform timelineScrollRoot = CreatePanel("TimelineScroll", detailPanel, new Color32(20, 24, 31, 255));
                Stretch(timelineScrollRoot, new Vector2(20f, 20f), new Vector2(-20f, -244f));
                ScrollRect timelineScrollRect = timelineScrollRoot.gameObject.AddComponent<ScrollRect>();
                timelineScrollRect.horizontal = false;
                timelineScrollRect.vertical = true;
                timelineScrollRect.movementType = ScrollRect.MovementType.Clamped;

                RectTransform timelineViewport = CreatePanel("Viewport", timelineScrollRoot, new Color(0f, 0f, 0f, 0f));
                Stretch(timelineViewport);
                timelineViewport.gameObject.AddComponent<RectMask2D>();

                RectTransform timelineContent = CreateRect("Content", timelineViewport);
                timelineContent.anchorMin = new Vector2(0f, 1f);
                timelineContent.anchorMax = new Vector2(1f, 1f);
                timelineContent.pivot = new Vector2(0.5f, 1f);
                timelineContent.offsetMin = Vector2.zero;
                timelineContent.offsetMax = Vector2.zero;
                VerticalLayoutGroup timelineLayout = timelineContent.gameObject.AddComponent<VerticalLayoutGroup>();
                timelineLayout.spacing = 10f;
                timelineLayout.padding = new RectOffset(0, 0, 0, 0);
                timelineLayout.childAlignment = TextAnchor.UpperCenter;
                timelineLayout.childControlWidth = true;
                timelineLayout.childControlHeight = false;
                timelineLayout.childForceExpandWidth = true;
                timelineLayout.childForceExpandHeight = false;
                ContentSizeFitter timelineSizeFitter = timelineContent.gameObject.AddComponent<ContentSizeFitter>();
                timelineSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                timelineSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                timelineScrollRect.viewport = timelineViewport;
                timelineScrollRect.content = timelineContent;
                EnsureBinding(cdeTable.ComponentTable, timelineContent, "TimelineContent");

                RectTransform timelineItemTemplate = CreatePanel("TimelineItemTemplate", timelineContent, new Color32(59, 69, 88, 255));
                timelineItemTemplate.anchorMin = new Vector2(0f, 1f);
                timelineItemTemplate.anchorMax = new Vector2(1f, 1f);
                timelineItemTemplate.pivot = new Vector2(0.5f, 1f);
                timelineItemTemplate.sizeDelta = new Vector2(0f, 88f);
                LayoutElement timelineLayoutElement = timelineItemTemplate.gameObject.AddComponent<LayoutElement>();
                timelineLayoutElement.preferredHeight = 88f;

                TextMeshProUGUI timelineTimeText = CreateText("TimeText", timelineItemTemplate, style, 20f, FontStyles.Bold);
                timelineTimeText.alignment = TextAlignmentOptions.Left;
                timelineTimeText.rectTransform.anchorMin = new Vector2(0f, 1f);
                timelineTimeText.rectTransform.anchorMax = new Vector2(1f, 1f);
                timelineTimeText.rectTransform.pivot = new Vector2(0.5f, 1f);
                timelineTimeText.rectTransform.offsetMin = new Vector2(14f, -34f);
                timelineTimeText.rectTransform.offsetMax = new Vector2(-14f, -10f);

                TextMeshProUGUI timelineDescText = CreateText("DescText", timelineItemTemplate, style, 22f, FontStyles.Normal);
                timelineDescText.alignment = TextAlignmentOptions.TopLeft;
                timelineDescText.textWrappingMode = TextWrappingModes.Normal;
                Stretch(timelineDescText.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -38f));
                timelineItemTemplate.gameObject.SetActive(false);
                EnsureBinding(cdeTable.ComponentTable, timelineItemTemplate, "TimelineItemTemplate");

                TextMeshProUGUI detailEmptyText = CreateText("DetailEmptyText", detailPanel, style, 26f, FontStyles.Normal);
                detailEmptyText.text = "选择左侧一场对局查看详情。";
                detailEmptyText.alignment = TextAlignmentOptions.Center;
                Stretch(detailEmptyText.rectTransform, new Vector2(20f, 20f), new Vector2(-20f, -244f));
                EnsureBinding(cdeTable.ComponentTable, detailEmptyText, "DetailEmptyText");

                cdeTable.ComponentTable.AutoCheck();
                PrefabUtility.SaveAsPrefabAsset(root, BattleRecordPanelPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void GenerateYiuiCode()
        {
            GameObject lobbyAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath);
            GameObject battleRecordAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BattleRecordPanelPrefabPath);
            if (lobbyAsset == null || battleRecordAsset == null)
            {
                throw new InvalidOperationException("BattleRecord YIUI assets were not saved correctly.");
            }

            UIBindCDETable lobbyCde = lobbyAsset.GetComponent<UIBindCDETable>();
            UIBindCDETable battleRecordCde = battleRecordAsset.GetComponent<UIBindCDETable>();
            if (lobbyCde == null || battleRecordCde == null)
            {
                throw new InvalidOperationException("BattleRecord YIUI CDE missing.");
            }

            InvokeUICreatePackages(lobbyCde, "statesync");
            InvokeUICreatePackages(battleRecordCde, "statesync");
        }

        private static void InvokeUICreatePackages(UIBindCDETable cdeTable, string createPackageName)
        {
            Type createModuleType = Type.GetType("YIUIFramework.Editor.UICreateModule, ET.YIUIFramework.Editor");
            if (createModuleType == null)
            {
                throw new InvalidOperationException("Cannot find YIUIFramework.Editor.UICreateModule.");
            }

            var methodInfo = createModuleType.GetMethod(
                "CreatePackages",
                new[] { typeof(UIBindCDETable), typeof(bool), typeof(bool), typeof(string) });
            if (methodInfo == null)
            {
                throw new InvalidOperationException("Cannot find UICreateModule.CreatePackages.");
            }

            methodInfo.Invoke(null, new object[] { cdeTable, false, false, createPackageName });
        }

        private static Button EnsureBattleRecordButton(RectTransform matchPanel, StyleContext style)
        {
            RectTransform buttonRect = FindDirectChild(matchPanel, "BattleRecordButton");
            if (buttonRect == null)
            {
                Button sourceButton = FindRequiredRect(matchPanel, "StartButton").GetComponent<Button>();
                if (sourceButton == null)
                {
                    throw new InvalidOperationException("StartButton missing Button component.");
                }

                GameObject buttonObject = UnityEngine.Object.Instantiate(sourceButton.gameObject, matchPanel);
                buttonObject.name = "BattleRecordButton";
                buttonRect = buttonObject.transform as RectTransform;
            }

            RemoveDirectChild(buttonRect, "ClickTask");
            buttonRect.SetAsLastSibling();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-132f, -54f);
            buttonRect.sizeDelta = new Vector2(200f, 72f);

            Button button = buttonRect.GetOrAddComponent<Button>();
            Image image = buttonRect.GetOrAddComponent<Image>();
            SetButtonVisual(button, new Color32(232, 197, 103, 255), style.ButtonSprite, style.ButtonColors);
            button.targetGraphic = image;

            TextMeshProUGUI label = buttonRect.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                label = CreateText("Label", buttonRect, style, 28f, FontStyles.Bold);
            }

            label.gameObject.name = "Label";
            label.text = "战绩";
            label.fontSize = 28f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(34, 28, 18, 255);
            label.raycastTarget = false;
            Stretch(label.rectTransform);

            return button;
        }

        private static RectTransform EnsureBattleRecordOverlayRoot(RectTransform matchPanel)
        {
            RectTransform overlayRoot = FindDirectChild(matchPanel, "BattleRecordOverlayRoot");
            if (overlayRoot == null)
            {
                overlayRoot = CreateRect("BattleRecordOverlayRoot", matchPanel);
            }

            overlayRoot.SetAsLastSibling();
            Stretch(overlayRoot);
            Image image = overlayRoot.GetOrAddComponent<Image>();
            image.color = new Color32(7, 10, 16, 196);
            image.raycastTarget = true;
            overlayRoot.gameObject.SetActive(false);
            return overlayRoot;
        }

        private static Button CreateFullScreenMask(RectTransform parent, StyleContext style)
        {
            Button button = CreateButton("MaskButton", parent, style);
            RectTransform rect = button.transform as RectTransform;
            Stretch(rect);
            Image image = button.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            button.targetGraphic = image;
            return button;
        }

        private static Button CreateButton(string name, RectTransform parent, StyleContext style)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Button button = buttonObject.GetComponent<Button>();
            SetButtonVisual(button, Color.white, style.ButtonSprite, style.ButtonColors);
            return button;
        }

        private static RectTransform CreatePanel(string name, RectTransform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            StyleContext style,
            float fontSize,
            FontStyles fontStyles)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (style.FontAsset != null)
            {
                text.font = style.FontAsset;
            }

            if (style.FontMaterial != null)
            {
                text.fontSharedMaterial = style.FontMaterial;
            }

            text.fontSize = fontSize;
            text.fontStyle = fontStyles;
            text.color = new Color32(239, 240, 244, 255);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static void SetButtonVisual(Button button, Color color, Sprite sprite, ColorBlock colorBlock)
        {
            Image image = button.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
            image.color = color;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colorBlock;
            button.targetGraphic = image;
        }

        private static void EnsureBinding(UIBindComponentTable componentTable, Component component, string bindName)
        {
            string uiName = $"u_Com{bindName}";
            if (componentTable.AllBindDic.ContainsKey(uiName))
            {
                return;
            }

            componentTable.EditorAddComponent(component, bindName);
        }

        private static RectTransform FindRequiredRect(Transform root, string name)
        {
            RectTransform rect = root.FindChildByName(name) as RectTransform;
            if (rect == null)
            {
                throw new InvalidOperationException($"Missing required node: {name}");
            }

            return rect;
        }

        private static RectTransform FindDirectChild(RectTransform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; ++i)
            {
                RectTransform child = parent.GetChild(i) as RectTransform;
                if (child != null && child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static void RemoveDirectChild(RectTransform parent, string name)
        {
            RectTransform child = FindDirectChild(parent, name);
            if (child == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void Stretch(RectTransform rectTransform)
        {
            Stretch(rectTransform, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (layer < 0)
            {
                return;
            }

            gameObject.layer = layer;
            Transform transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; ++i)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }

        private static StyleContext LoadStyleContext()
        {
            GameObject lobbyAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath);
            if (lobbyAsset == null)
            {
                return CreateFallbackStyle();
            }

            TextMeshProUGUI sourceText = lobbyAsset.GetComponentInChildren<TextMeshProUGUI>(true);
            Button sourceButton = lobbyAsset.transform.FindChildByName("StartButton")?.GetComponent<Button>();
            Image sourceImage = sourceButton != null ? sourceButton.GetComponent<Image>() : null;

            StyleContext style = new();
            style.FontAsset = sourceText != null ? sourceText.font : TMP_Settings.defaultFontAsset;
            style.FontMaterial = sourceText != null ? sourceText.fontSharedMaterial : null;
            style.ButtonSprite = sourceImage != null ? sourceImage.sprite : null;
            style.ButtonColors = sourceButton != null ? sourceButton.colors : ColorBlock.defaultColorBlock;
            return style;
        }

        private static StyleContext CreateFallbackStyle()
        {
            StyleContext style = new();
            style.FontAsset = TMP_Settings.defaultFontAsset;
            style.FontMaterial = null;
            style.ButtonSprite = null;
            style.ButtonColors = ColorBlock.defaultColorBlock;
            return style;
        }

        private struct StyleContext
        {
            public TMP_FontAsset FontAsset;
            public Material FontMaterial;
            public Sprite ButtonSprite;
            public ColorBlock ButtonColors;
        }
    }
}
