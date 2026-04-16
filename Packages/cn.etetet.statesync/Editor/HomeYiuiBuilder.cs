using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Editor
{
    public static class HomeYiuiBuilder
    {
        private const string LobbyPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/LobbyPanel.prefab";
        private const string HomeFontSourcePath =
            "Packages/cn.etetet.statesync/Assets/GameRes/Fonts/锐字云字库粗圆体GBK_爱给网_aigei_com.ttf";
        private const string HomeFontAssetPath =
            "Packages/cn.etetet.statesync/Resources/Fonts/HomeChinese SDF.asset";
        private const string HomeFontAssetName = "HomeChinese SDF";

        private const string HOME_RUNTIME_ROOT_NAME = "HomeRuntimeRoot";
        private const string HOME_BUILDING_LIST_CONTENT_NAME = "HomeBuildingListContent";
        private const string HOME_SUBTITLE_TEXT_NAME = "HomeSubtitleText";
        private const string HOME_DETAIL_TITLE_NAME = "HomeDetailTitleText";
        private const string HOME_DETAIL_SUBTITLE_NAME = "HomeDetailSubtitleText";
        private const string HOME_DETAIL_STATUS_NAME = "HomeDetailStatusText";
        private const string HOME_DETAIL_HINT_NAME = "HomeDetailHintText";
        private const string HOME_QUICK_EQUIP_BUTTON_NAME = "HomeQuickEquipButton";
        private const string HOME_QUICK_MATCH_BUTTON_NAME = "HomeQuickMatchButton";
        private const string HOME_UPGRADE_BUTTON_NAME = "HomeUpgradeButton";
        private const string HOME_COLLECT_BUTTON_NAME = "HomeCollectButton";
        private const string HOME_DEMOLISH_BUTTON_NAME = "HomeDemolishButton";

        [MenuItem("ET/YIUI/Build Home Resources")]
        public static void Build()
        {
            try
            {
                EnsureHomeFontAsset();
                StyleContext style = LoadStyleContext();
                BuildLobbyPanel(style);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[HomeYiuiBuilder] Home resources generated.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[HomeYiuiBuilder] Build failed: {exception}");
                throw;
            }
        }

        private static void BuildLobbyPanel(StyleContext style)
        {
            GameObject lobbyRoot = PrefabUtility.LoadPrefabContents(LobbyPrefabPath);
            try
            {
                RectTransform buildPanel = FindRequiredRect(lobbyRoot.transform, "BuildPanel");
                RectTransform existingRoot = FindDirectChild(buildPanel, HOME_RUNTIME_ROOT_NAME);
                if (existingRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingRoot.gameObject);
                }

                RectTransform root = CreatePanel(HOME_RUNTIME_ROOT_NAME, buildPanel, new Color(0.06f, 0.07f, 0.09f, 0.92f));
                root.SetAsLastSibling();
                Stretch(root);

                RectTransform scrollRoot = CreateLayoutNode("HomeScrollRoot", root);
                Stretch(scrollRoot);

                ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 24f;

                RectTransform viewport = CreatePanel("Viewport", scrollRoot, new Color(0f, 0f, 0f, 0.001f));
                Stretch(viewport);
                viewport.gameObject.AddComponent<RectMask2D>();

                RectTransform content = CreateLayoutNode("Content", viewport);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.offsetMin = new Vector2(18f, 0f);
                content.offsetMax = new Vector2(-18f, 0f);

                VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                contentLayout.padding = new RectOffset(0, 0, 18, 18);
                contentLayout.spacing = 14f;
                contentLayout.childAlignment = TextAnchor.UpperCenter;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;

                ContentSizeFitter contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.viewport = viewport;
                scrollRect.content = content;

                BuildHomeHeaderSection(style, content);
                BuildHomeOverviewSection(style, content);
                BuildHomeBuildEntrySection(style, content);
                BuildHomeListSection(style, content);
                BuildHomeDetailSection(style, content);
                SetLayerRecursively(root.gameObject, buildPanel.gameObject.layer);

                EditorUtility.SetDirty(lobbyRoot);
                PrefabUtility.SaveAsPrefabAsset(lobbyRoot, LobbyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(lobbyRoot);
            }
        }

        private static void BuildHomeHeaderSection(StyleContext style, RectTransform parent)
        {
            RectTransform card = CreatePanel("HomeHeaderCard", parent, new Color(0.12f, 0.15f, 0.19f, 0.94f));
            LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 148f;

            VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 18, 18);
            cardLayout.spacing = 10f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateText(style, "HomeHeaderTitle", card, "基地首页", 34f, new Color(0.97f, 0.91f, 0.68f, 1f), FontStyles.Bold);
            TMP_Text subtitle = CreateText(style, HOME_SUBTITLE_TEXT_NAME, card, "进入基地后，这里会承接首期家园首页与基础操作。", 24f, Color.white, FontStyles.Normal);
            subtitle.textWrappingMode = TextWrappingModes.Normal;

            RectTransform buttonRow = CreateLayoutNode("HomeQuickActionRow", card);
            HorizontalLayoutGroup rowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            CreateButton(style, HOME_QUICK_EQUIP_BUTTON_NAME, buttonRow, "前往配装", new Color(0.68f, 0.53f, 0.21f, 0.96f));
            CreateButton(style, HOME_QUICK_MATCH_BUTTON_NAME, buttonRow, "前往匹配", new Color(0.17f, 0.47f, 0.64f, 0.96f));
        }

        private static void BuildHomeOverviewSection(StyleContext style, RectTransform parent)
        {
            RectTransform card = CreatePanel("HomeOverviewCard", parent, new Color(0.11f, 0.12f, 0.16f, 0.94f));
            VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 18, 18);
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateText(style, "HomeOverviewTitle", card, "基地总览", 30f, Color.white, FontStyles.Bold);

            RectTransform grid = CreateLayoutNode("HomeOverviewGrid", card);
            HorizontalLayoutGroup gridLayout = grid.gameObject.AddComponent<HorizontalLayoutGroup>();
            gridLayout.spacing = 10f;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.childControlWidth = true;
            gridLayout.childControlHeight = true;
            gridLayout.childForceExpandWidth = true;
            gridLayout.childForceExpandHeight = false;

            for (int i = 0; i < 4; ++i)
            {
                RectTransform statCard = CreatePanel($"HomeStatCard_{i}", grid, new Color(0.16f, 0.18f, 0.24f, 0.96f));
                LayoutElement layout = statCard.gameObject.AddComponent<LayoutElement>();
                layout.flexibleWidth = 1f;
                layout.preferredHeight = 86f;

                VerticalLayoutGroup statLayout = statCard.gameObject.AddComponent<VerticalLayoutGroup>();
                statLayout.padding = new RectOffset(16, 16, 12, 12);
                statLayout.spacing = 4f;
                statLayout.childAlignment = TextAnchor.UpperLeft;
                statLayout.childControlWidth = true;
                statLayout.childControlHeight = true;
                statLayout.childForceExpandWidth = true;
                statLayout.childForceExpandHeight = false;

                CreateText(style, $"HomeStatLabelText_{i}", statCard, GetHomeStatLabel(i), 22f, new Color(0.77f, 0.82f, 0.88f, 1f), FontStyles.Normal);
                CreateText(style, $"HomeStatValueText_{i}", statCard, "--", 30f, Color.white, FontStyles.Bold);
            }
        }

        private static void BuildHomeBuildEntrySection(StyleContext style, RectTransform parent)
        {
            RectTransform card = CreatePanel("HomeBuildEntryCard", parent, new Color(0.11f, 0.13f, 0.17f, 0.94f));
            VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 18, 18);
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateText(style, "HomeBuildEntryTitle", card, "建造入口", 30f, Color.white, FontStyles.Bold);
            TMP_Text hint = CreateText(style, "HomeBuildEntryHint", card, "请先在下方选择一个空槽位；可建建筑列表会按正式配置动态生成。", 22f, new Color(0.78f, 0.83f, 0.9f, 1f), FontStyles.Normal);
            hint.textWrappingMode = TextWrappingModes.Normal;

            RectTransform buttonRow = CreateLayoutNode("HomeBuildEntryRow", card);
            HorizontalLayoutGroup rowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
        }

        private static void BuildHomeListSection(StyleContext style, RectTransform parent)
        {
            RectTransform card = CreatePanel("HomeBuildingListCard", parent, new Color(0.1f, 0.12f, 0.16f, 0.94f));
            VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 18, 18);
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateText(style, "HomeBuildingListTitle", card, "建筑与槽位", 30f, Color.white, FontStyles.Bold);
            CreateVerticalScrollContent(card, HOME_BUILDING_LIST_CONTENT_NAME, 300f);
        }

        private static void BuildHomeDetailSection(StyleContext style, RectTransform parent)
        {
            RectTransform card = CreatePanel("HomeDetailCard", parent, new Color(0.1f, 0.12f, 0.16f, 0.94f));
            VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 18, 18);
            cardLayout.spacing = 10f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateText(style, HOME_DETAIL_TITLE_NAME, card, "建筑详情", 30f, Color.white, FontStyles.Bold);
            CreateText(style, HOME_DETAIL_SUBTITLE_NAME, card, "当前未选中建筑。", 24f, new Color(0.88f, 0.9f, 0.93f, 1f), FontStyles.Normal);
            CreateText(style, HOME_DETAIL_STATUS_NAME, card, "状态：--", 22f, new Color(0.75f, 0.8f, 0.86f, 1f), FontStyles.Normal);
            TMP_Text hint = CreateText(style, HOME_DETAIL_HINT_NAME, card, "空基地时请先通过上方建造入口创建首座建筑。", 22f, new Color(0.75f, 0.8f, 0.86f, 1f), FontStyles.Normal);
            hint.textWrappingMode = TextWrappingModes.Normal;

            RectTransform actionRow = CreateLayoutNode("HomeDetailActionRow", card);
            HorizontalLayoutGroup rowLayout = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            CreateButton(style, HOME_UPGRADE_BUTTON_NAME, actionRow, "升级", new Color(0.57f, 0.47f, 0.18f, 0.96f), true);
            CreateButton(style, HOME_COLLECT_BUTTON_NAME, actionRow, "收取", new Color(0.24f, 0.54f, 0.39f, 0.96f), true);
            CreateButton(style, HOME_DEMOLISH_BUTTON_NAME, actionRow, "拆除", new Color(0.63f, 0.23f, 0.21f, 0.96f), true);
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        private static RectTransform CreateLayoutNode(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateText(
            StyleContext style,
            string name,
            Transform parent,
            string text,
            float fontSize,
            Color color,
            FontStyles fontStyles)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (style.FontAsset != null)
            {
                tmp.font = style.FontAsset;
            }

            if (style.FontMaterial != null)
            {
                tmp.fontSharedMaterial = style.FontMaterial;
            }

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = fontStyles;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateButton(
            StyleContext style,
            string name,
            Transform parent,
            string text,
            Color color,
            bool expandWidth = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 44f;
            if (expandWidth)
            {
                layout.flexibleWidth = 1f;
            }
            else
            {
                layout.preferredWidth = 138f;
            }

            Image image = go.GetComponent<Image>();
            if (style.ButtonSprite != null)
            {
                image.sprite = style.ButtonSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = color;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.08f;
            colors.pressedColor = color * 0.92f;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.25f, 0.28f, 0.32f, 0.72f);
            button.colors = colors;
            button.targetGraphic = image;

            TMP_Text label = CreateText(style, "Label", go.transform, text, 22f, Color.white, FontStyles.Bold);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 6f);
            labelRect.offsetMax = new Vector2(-8f, -6f);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static RectTransform CreateVerticalScrollContent(Transform parent, string contentName, float preferredHeight)
        {
            RectTransform root = CreateLayoutNode($"{contentName}Root", parent);
            LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = preferredHeight;

            Image rootImage = root.gameObject.AddComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.12f);

            ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            RectTransform viewport = CreatePanel("Viewport", root, new Color(0f, 0f, 0f, 0.001f));
            Stretch(viewport, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateLayoutNode(contentName, viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        private static RectTransform FindRequiredRect(Transform root, string name)
        {
            RectTransform rect = FindDescendantRectTransform(root, name);
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

        private static RectTransform FindDescendantRectTransform(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root is RectTransform rectTransform && root.name == name)
            {
                return rectTransform;
            }

            for (int i = 0; i < root.childCount; ++i)
            {
                RectTransform child = FindDescendantRectTransform(root.GetChild(i), name);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
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
            Button sourceButton = FindDescendantRectTransform(lobbyAsset.transform, "StartButton")?.GetComponent<Button>();
            Image sourceImage = sourceButton != null ? sourceButton.GetComponent<Image>() : null;

            StyleContext style = new();
            TMP_FontAsset homeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HomeFontAssetPath);
            style.FontAsset = homeFont != null ? homeFont : (sourceText != null ? sourceText.font : TMP_Settings.defaultFontAsset);
            style.FontMaterial = homeFont != null ? homeFont.material : (sourceText != null ? sourceText.fontSharedMaterial : null);
            style.ButtonSprite = sourceImage != null ? sourceImage.sprite : null;
            style.ButtonColors = sourceButton != null ? sourceButton.colors : ColorBlock.defaultColorBlock;
            return style;
        }

        private static TMP_FontAsset EnsureHomeFontAsset()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HomeFontAssetPath);
            string requiredCharacters = GetHomeRequiredCharacters();
            if (fontAsset != null &&
                fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic &&
                fontAsset.HasCharacters(requiredCharacters))
            {
                return fontAsset;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(HomeFontSourcePath);
            if (sourceFont == null)
            {
                throw new InvalidOperationException($"Missing required chinese font source: {HomeFontSourcePath}");
            }

            if (fontAsset == null || fontAsset.sourceFontFile != sourceFont || fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                if (fontAsset != null)
                {
                    AssetDatabase.DeleteAsset(HomeFontAssetPath);
                }

                fontAsset = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true);
                if (fontAsset == null)
                {
                    throw new InvalidOperationException($"Create TMP font asset failed: {HomeFontSourcePath}");
                }

                fontAsset.name = HomeFontAssetName;
                AssetDatabase.CreateAsset(fontAsset, HomeFontAssetPath);
            }

            EnsureFontSubAssets(fontAsset);

            if (!fontAsset.TryAddCharacters(requiredCharacters, out string missingCharacters))
            {
                Debug.LogWarning($"[HomeYiuiBuilder] Home chinese font missing characters: {missingCharacters}");
            }

            EnsureFontSubAssets(fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(HomeFontAssetPath, ImportAssetOptions.ForceUpdate);
            return fontAsset;
        }

        private static void EnsureFontSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = $"{HomeFontAssetName} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            if (fontAsset.atlasTextures == null)
            {
                return;
            }

            for (int i = 0; i < fontAsset.atlasTextures.Length; ++i)
            {
                Texture2D atlasTexture = fontAsset.atlasTextures[i];
                if (atlasTexture == null || AssetDatabase.Contains(atlasTexture))
                {
                    continue;
                }

                atlasTexture.name = i == 0 ? $"{HomeFontAssetName} Atlas" : $"{HomeFontAssetName} Atlas {i}";
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }
        }

        private static string GetHomeRequiredCharacters()
        {
            string[] sourceTexts =
            {
                "基地首页",
                "进入基地后，这里会承接首期家园首页与基础操作。",
                "前往配装",
                "前往匹配",
                "基地总览",
                "已建建筑",
                "主城等级",
                "仓库占用",
                "金币储备",
                "建造入口",
                "请先在下方选择一个空槽位；可建建筑列表会按正式配置动态生成。",
                "建筑与槽位",
                "建筑详情",
                "当前未选中建筑。",
                "状态：--",
                "空基地时请先通过上方建造入口创建首座建筑。",
                "升级",
                "收取",
                "拆除",
                "当前已进入家园场景，首页承接基地总览与首期建筑操作。家园版本：",
                "当前不在家园场景，建造页仅保留家园首页承接结构。",
                "请先进入家园场景再执行建造。",
                "请先进入家园场景再执行升级。",
                "请先进入家园场景再执行收取。",
                "请先进入家园场景再执行拆除。",
                "当前没有可用的原型建筑配置。",
                "建造失败：未收到服务端响应。",
                "升级失败：未收到服务端响应。",
                "收取失败：未收到服务端响应。",
                "拆除失败：未收到服务端响应。",
                "建造完成：原型建筑已落到槽位。",
                "当前没有可升级的建筑。",
                "当前没有可收取的建筑。",
                "当前没有可拆除的建筑。",
                "当前基地为空，请先从上方建造入口创建首座建筑。",
                "等级状态最近收取",
                "当前没有可查看的建筑。",
                "状态：空基地",
                "请先从建造入口创建首座原型建筑，然后再执行升级、收取、拆除。",
                "当前不在家园场景，所有基地操作按钮均已禁用。",
                "槽位原型建筑当前等级上次收取最近生产建筑",
                "空槽位未解锁待建造主城保留槽位",
                "请先在下方列表中选择一个槽位或建筑。",
                "请先在下方选择一个空槽位，再执行建造。",
                "当前槽位尚未解锁，无法建造。",
                "当前槽位已有建筑，请先选择空槽位。",
                "当前槽位不能建造该建筑。",
                "当前不在家园场景，建造入口仅保留展示；进入Home后请先选择空槽位。",
                "当前选中的是主城保留槽位，不能新建其他建筑。",
                "当前没有可建建筑，请先提升主城或释放同类建筑数量上限。",
                "当前选中槽位，可建：",
                "这是一个可建空槽位。当前兼容：",
                "请从上方建造入口选择建筑。",
                "升级主城后可解锁该地块。",
                "该槽位保留给主城，不参与普通建造。",
                "任意已解锁建筑",
                "大红收藏馆物品回收间农场仓库主城保留位",
                "槽位已被占用。",
                "该建筑已达当前样机最高等级。",
                "当前没有可收取产出。",
                "目标建筑不存在。",
                "当前不在家园场景。",
                "前置条件未满足，请先完成主城任务或补足资源。",
                "空闲生产中可收取无",
                "0123456789",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "abcdefghijklmnopqrstuvwxyz",
                " -_:：/.,，。()（）[]【】·\n"
            };

            HashSet<char> uniqueCharacters = new();
            for (int i = 0; i < sourceTexts.Length; ++i)
            {
                string text = sourceTexts[i];
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                for (int j = 0; j < text.Length; ++j)
                {
                    uniqueCharacters.Add(text[j]);
                }
            }

            char[] buffer = new char[uniqueCharacters.Count];
            uniqueCharacters.CopyTo(buffer);
            Array.Sort(buffer);
            return new string(buffer);
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

        private static string GetHomeStatLabel(int index)
        {
            return index switch
            {
                0 => "已建建筑",
                1 => "主城等级",
                2 => "仓库占用",
                3 => "金币储备",
                _ => "统计",
            };
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
