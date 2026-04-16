using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Editor
{
    public static class HomePanelYiuiBuilder
    {
        private const string LobbyPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/LobbyPanel.prefab";
        private const string HomePanelPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Home/Prefabs/HomePanel.prefab";
        private const string HomeDetailPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Home/Prefabs/HomeBuildingDetailCard.prefab";
        private const string HomeComponentGenPath =
            "Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Home/HomePanelComponentGen.cs";
        private const string HomeSystemGenPath =
            "Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUIGen/Home/HomePanelComponentSystemGen.cs";
        private const string HomeFontSourcePath =
            "Packages/cn.etetet.statesync/Assets/GameRes/Fonts/锐字云字库粗圆体GBK_爱给网_aigei_com.ttf";
        private const string HomeFontAssetPath =
            "Packages/cn.etetet.statesync/Resources/Fonts/HomeChinese SDF.asset";
        private const string HomeFontAssetName = "HomeChinese SDF";
        private const float HomePanelEdge = 24f;
        private const float HomePanelContentTop = 112f;
        private const float HomeLeftPanelWidth = 264f;
        private const float HomeRightPanelWidth = 460f;
        private const float HomeDetailButtonBottom = 18f;
        private const float HomeDetailButtonHeight = 50f;
        private const float HomeDetailButtonOuterPadding = 18f;
        private const float HomeDetailButtonInnerPadding = 10f;

        [MenuItem("ET/YIUI/Build Home Formal Resources")]
        public static void Build()
        {
            EnsureHomeFontAsset();
            StyleContext style = LoadStyleContext();
            BuildHomeDetailPrefab(style);
            BuildHomePanel(style);
            GenerateYiuiCode();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HomePanelYiuiBuilder] Build finished.");
        }

        private static void BuildHomePanel(StyleContext style)
        {
            EnsureDirectoryForAsset(HomePanelPrefabPath);

            GameObject root = new("HomePanel", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                SetLayerRecursively(root, LayerMask.NameToLayer("UI"));

                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);

                UIBindCDETable cdeTable = root.GetOrAddComponent<UIBindCDETable>();
                cdeTable.UICodeType = EUICodeType.Panel;
                cdeTable.PkgName = "Home";
                cdeTable.ResName = "HomePanel";
                cdeTable.ComponentTable = root.GetOrAddComponent<UIBindComponentTable>();

                RectTransform background = CreatePanel("Background", rootRect, new Color32(8, 12, 18, 32));
                Stretch(background);
                SetGraphicRaycastTarget(background, false);

                RectTransform topBar = CreatePanel("TopBarRoot", rootRect, new Color32(22, 28, 40, 245));
                topBar.anchorMin = new Vector2(0f, 1f);
                topBar.anchorMax = new Vector2(1f, 1f);
                topBar.pivot = new Vector2(0.5f, 1f);
                topBar.offsetMin = new Vector2(HomePanelEdge, -98f);
                topBar.offsetMax = new Vector2(-HomePanelEdge, -HomePanelEdge);

                TextMeshProUGUI titleText = CreateText("TitleText", topBar, style, 34f, FontStyles.Bold);
                titleText.text = "家园";
                titleText.alignment = TextAlignmentOptions.Left;
                titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
                titleText.rectTransform.anchorMax = new Vector2(0f, 1f);
                titleText.rectTransform.pivot = new Vector2(0f, 0.5f);
                titleText.rectTransform.sizeDelta = new Vector2(160f, 0f);
                titleText.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                EnsureBinding(cdeTable.ComponentTable, titleText, "TitleText");

                TextMeshProUGUI selectionHintText = CreateText("SelectionHintText", topBar, style, 22f, FontStyles.Normal);
                selectionHintText.text = "点击中间场景中的主城、建筑或空地，右侧会打开对应管理页。";
                selectionHintText.alignment = TextAlignmentOptions.Left;
                selectionHintText.textWrappingMode = TextWrappingModes.Normal;
                selectionHintText.rectTransform.anchorMin = new Vector2(0f, 0f);
                selectionHintText.rectTransform.anchorMax = new Vector2(0f, 1f);
                selectionHintText.rectTransform.pivot = new Vector2(0f, 0.5f);
                selectionHintText.rectTransform.sizeDelta = new Vector2(520f, 0f);
                selectionHintText.rectTransform.anchoredPosition = new Vector2(196f, 0f);
                EnsureBinding(cdeTable.ComponentTable, selectionHintText, "SelectionHintText");

                CreateTopValueText("TotalWealthText", topBar, style, "金币 --", new Vector2(-528f, 0f), cdeTable);
                CreateTopValueText("MainCityLevelText", topBar, style, "主城 --", new Vector2(-360f, 0f), cdeTable);
                CreateTopValueText("WarehouseSummaryText", topBar, style, "仓库 --", new Vector2(-192f, 0f), cdeTable);
                CreateTopValueText("TodoHintText", topBar, style, "待办 --", new Vector2(-24f, 0f), cdeTable);

                Button closeButton = CreateButton("CloseButton", topBar, style);
                RectTransform closeRect = closeButton.transform as RectTransform;
                closeRect.anchorMin = new Vector2(1f, 0.5f);
                closeRect.anchorMax = new Vector2(1f, 0.5f);
                closeRect.pivot = new Vector2(1f, 0.5f);
                closeRect.sizeDelta = new Vector2(116f, 52f);
                closeRect.anchoredPosition = new Vector2(-16f, 0f);
                SetButtonVisual(closeButton, new Color32(159, 82, 82, 255), style.ButtonSprite, style.ButtonColors);
                CreateButtonLabel(closeRect, style, "关闭");
                EnsureBinding(cdeTable.ComponentTable, closeButton, "CloseButton");

                RectTransform leftPanel = CreatePanel("LeftPanel", rootRect, new Color32(18, 24, 34, 242));
                leftPanel.anchorMin = new Vector2(0f, 0f);
                leftPanel.anchorMax = new Vector2(0f, 1f);
                leftPanel.pivot = new Vector2(0f, 1f);
                leftPanel.offsetMin = new Vector2(HomePanelEdge, HomePanelEdge);
                leftPanel.offsetMax = new Vector2(HomePanelEdge + HomeLeftPanelWidth, -HomePanelContentTop);

                TextMeshProUGUI overviewSummaryText = CreateText("OverviewSummaryText", leftPanel, style, 22f, FontStyles.Normal);
                overviewSummaryText.text = "家园总览";
                overviewSummaryText.alignment = TextAlignmentOptions.TopLeft;
                overviewSummaryText.textWrappingMode = TextWrappingModes.Normal;
                overviewSummaryText.rectTransform.anchorMin = new Vector2(0f, 1f);
                overviewSummaryText.rectTransform.anchorMax = new Vector2(1f, 1f);
                overviewSummaryText.rectTransform.pivot = new Vector2(0.5f, 1f);
                overviewSummaryText.rectTransform.offsetMin = new Vector2(16f, -132f);
                overviewSummaryText.rectTransform.offsetMax = new Vector2(-16f, -16f);
                EnsureBinding(cdeTable.ComponentTable, overviewSummaryText, "OverviewSummaryText");

                CreateNavButton("OverviewButton", leftPanel, style, "总览", new Vector2(16f, -190f), cdeTable);
                CreateNavButton("MainCityTaskButton", leftPanel, style, "主城任务", new Vector2(16f, -250f), cdeTable);
                CreateNavButton("MuseumButton", leftPanel, style, "收藏馆", new Vector2(16f, -310f), cdeTable);
                CreateNavButton("RecycleButton", leftPanel, style, "回收间", new Vector2(16f, -370f), cdeTable);
                CreateNavButton("FarmButton", leftPanel, style, "农场", new Vector2(16f, -430f), cdeTable);
                CreateNavButton("WarehouseButton", leftPanel, style, "仓库", new Vector2(16f, -490f), cdeTable);

                BuildBuildingListPanel(style, cdeTable, leftPanel);
                BuildRightPanel(style, cdeTable, rootRect);

                cdeTable.ComponentTable.AutoCheck();
                PrefabUtility.SaveAsPrefabAsset(root, HomePanelPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildHomeDetailPrefab(StyleContext style)
        {
            EnsureDirectoryForAsset(HomeDetailPrefabPath);

            GameObject root = new("HomeBuildingDetailCard", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(920f, 220f);
                CreateHomeDetailCard("DetailCardRoot", rootRect, style);
                PrefabUtility.SaveAsPrefabAsset(root, HomeDetailPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildBuildingListPanel(StyleContext style, UIBindCDETable cdeTable, RectTransform leftPanel)
        {
            RectTransform buildingListPanel = CreatePanel("BuildingListPanel", leftPanel, new Color32(10, 14, 20, 214));
            buildingListPanel.anchorMin = new Vector2(0f, 0f);
            buildingListPanel.anchorMax = new Vector2(1f, 0f);
            buildingListPanel.pivot = new Vector2(0.5f, 0f);
            buildingListPanel.offsetMin = new Vector2(16f, 16f);
            buildingListPanel.offsetMax = new Vector2(-16f, 350f);

            TextMeshProUGUI buildingListTitle = CreateText("BuildingListTitle", buildingListPanel, style, 24f, FontStyles.Bold);
            buildingListTitle.text = "建筑与槽位";
            buildingListTitle.alignment = TextAlignmentOptions.Left;
            buildingListTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            buildingListTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            buildingListTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            buildingListTitle.rectTransform.offsetMin = new Vector2(14f, -48f);
            buildingListTitle.rectTransform.offsetMax = new Vector2(-14f, -10f);

            RectTransform buildingListScroll = CreateScrollListRoot("BuildingListScroll", buildingListPanel, out RectTransform buildingListContentRoot);
            Stretch(buildingListScroll, new Vector2(14f, 12f), new Vector2(-14f, -54f));
            EnsureBinding(cdeTable.ComponentTable, buildingListContentRoot, "BuildingListContent");

            Button buildingItemTemplate = CreateCardButtonTemplate(
                "BuildingItemTemplate",
                buildingListContentRoot,
                style,
                new Color32(53, 64, 84, 255),
                96f);
            buildingItemTemplate.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, buildingItemTemplate, "BuildingItemTemplate");

            TextMeshProUGUI buildingListEmptyText = CreateText("BuildingListEmptyText", buildingListPanel, style, 22f, FontStyles.Normal);
            buildingListEmptyText.text = "暂无家园槽位数据";
            buildingListEmptyText.alignment = TextAlignmentOptions.Center;
            Stretch(buildingListEmptyText.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -54f));
            buildingListEmptyText.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, buildingListEmptyText, "BuildingListEmptyText");
        }

        private static void BuildRightPanel(StyleContext style, UIBindCDETable cdeTable, RectTransform rootRect)
        {
            RectTransform rightPanel = CreatePanel("RightPanel", rootRect, new Color32(18, 24, 34, 242));
            rightPanel.anchorMin = new Vector2(1f, 0f);
            rightPanel.anchorMax = new Vector2(1f, 1f);
            rightPanel.pivot = new Vector2(1f, 1f);
            rightPanel.offsetMin = new Vector2(-(HomePanelEdge + HomeRightPanelWidth), HomePanelEdge);
            rightPanel.offsetMax = new Vector2(-HomePanelEdge, -HomePanelContentTop);

            TextMeshProUGUI pageTitleText = CreateText("PageTitleText", rightPanel, style, 32f, FontStyles.Bold);
            pageTitleText.text = "家园总览";
            pageTitleText.alignment = TextAlignmentOptions.Left;
            pageTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            pageTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pageTitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            pageTitleText.rectTransform.offsetMin = new Vector2(22f, -56f);
            pageTitleText.rectTransform.offsetMax = new Vector2(-22f, -10f);
            EnsureBinding(cdeTable.ComponentTable, pageTitleText, "PageTitleText");

            TextMeshProUGUI pageSubtitleText = CreateText("PageSubtitleText", rightPanel, style, 22f, FontStyles.Normal);
            pageSubtitleText.text = "总览、建造、管理操作都在这里承接。";
            pageSubtitleText.alignment = TextAlignmentOptions.Left;
            pageSubtitleText.textWrappingMode = TextWrappingModes.Normal;
            pageSubtitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            pageSubtitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pageSubtitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            pageSubtitleText.rectTransform.offsetMin = new Vector2(22f, -104f);
            pageSubtitleText.rectTransform.offsetMax = new Vector2(-22f, -58f);
            EnsureBinding(cdeTable.ComponentTable, pageSubtitleText, "PageSubtitleText");

            RectTransform detailHost = CreateRect("DetailHost", rightPanel);
            detailHost.anchorMin = new Vector2(0f, 1f);
            detailHost.anchorMax = new Vector2(1f, 1f);
            detailHost.pivot = new Vector2(0.5f, 1f);
            detailHost.offsetMin = new Vector2(22f, -340f);
            detailHost.offsetMax = new Vector2(-22f, -120f);

            RectTransform detailCardRoot = CreateHomeDetailCard("DetailCardRoot", detailHost, style);
            Stretch(detailCardRoot);
            EnsureBinding(cdeTable.ComponentTable, detailCardRoot, "DetailCardRoot");
            EnsureBinding(cdeTable.ComponentTable, FindRequiredText(detailCardRoot, "DetailNameText"), "DetailNameText");
            EnsureBinding(cdeTable.ComponentTable, FindRequiredText(detailCardRoot, "DetailStatusText"), "DetailStatusText");
            EnsureBinding(cdeTable.ComponentTable, FindRequiredText(detailCardRoot, "DetailHintText"), "DetailHintText");
            EnsureBinding(cdeTable.ComponentTable, FindRequiredButton(detailCardRoot, "DetailPrimaryButton"), "DetailPrimaryButton");
            EnsureBinding(cdeTable.ComponentTable, FindRequiredButton(detailCardRoot, "DetailUpgradeButton"), "DetailUpgradeButton");
            EnsureBinding(cdeTable.ComponentTable, FindRequiredButton(detailCardRoot, "DetailDemolishButton"), "DetailDemolishButton");

            RectTransform pageHost = CreateRect("PageHost", rightPanel);
            pageHost.anchorMin = new Vector2(0f, 0f);
            pageHost.anchorMax = new Vector2(1f, 1f);
            pageHost.pivot = new Vector2(0.5f, 0f);
            pageHost.offsetMin = new Vector2(22f, 18f);
            pageHost.offsetMax = new Vector2(-22f, -356f);

            BuildOverviewPage(pageHost, style, cdeTable);
            BuildTaskPage(pageHost, style, cdeTable);
            BuildMuseumPage(pageHost, style, cdeTable);
            BuildRecyclePage(pageHost, style, cdeTable);
            BuildFarmPage(pageHost, style, cdeTable);
            BuildWarehousePage(pageHost, style, cdeTable);
        }

        private static void BuildOverviewPage(RectTransform parent, StyleContext style, UIBindCDETable cdeTable)
        {
            RectTransform pageRoot = CreatePageRoot("OverviewPageRoot", parent, new Color32(12, 17, 24, 220));
            EnsureBinding(cdeTable.ComponentTable, pageRoot, "OverviewPageRoot");

            TextMeshProUGUI buildHintText = CreateText("BuildHintText", pageRoot, style, 22f, FontStyles.Normal);
            buildHintText.text = "选中空地后，会在这里显示当前可建建筑。";
            buildHintText.alignment = TextAlignmentOptions.Left;
            buildHintText.textWrappingMode = TextWrappingModes.Normal;
            buildHintText.rectTransform.anchorMin = new Vector2(0f, 1f);
            buildHintText.rectTransform.anchorMax = new Vector2(1f, 1f);
            buildHintText.rectTransform.pivot = new Vector2(0.5f, 1f);
            buildHintText.rectTransform.offsetMin = new Vector2(16f, -68f);
            buildHintText.rectTransform.offsetMax = new Vector2(-16f, -14f);
            EnsureBinding(cdeTable.ComponentTable, buildHintText, "BuildHintText");

            RectTransform buildOptionScroll = CreateScrollListRoot("BuildOptionScroll", pageRoot, out RectTransform buildOptionContent);
            Stretch(buildOptionScroll, new Vector2(16f, 16f), new Vector2(-16f, -74f));
            EnsureBinding(cdeTable.ComponentTable, buildOptionContent, "BuildOptionContent");

            Button buildOptionTemplate = CreateCardButtonTemplate(
                "BuildOptionTemplate",
                buildOptionContent,
                style,
                new Color32(50, 61, 79, 255),
                108f);
            buildOptionTemplate.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, buildOptionTemplate, "BuildOptionTemplate");

            TextMeshProUGUI overviewEmptyText = CreateText("OverviewEmptyText", pageRoot, style, 22f, FontStyles.Normal);
            overviewEmptyText.text = "当前没有需要显示的建造项。";
            overviewEmptyText.alignment = TextAlignmentOptions.Center;
            Stretch(overviewEmptyText.rectTransform, new Vector2(16f, 16f), new Vector2(-16f, -74f));
            overviewEmptyText.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, overviewEmptyText, "OverviewEmptyText");
        }

        private static void BuildTaskPage(RectTransform parent, StyleContext style, UIBindCDETable cdeTable)
        {
            RectTransform pageRoot = CreatePageRoot("TaskPageRoot", parent, new Color32(12, 17, 24, 220));
            EnsureBinding(cdeTable.ComponentTable, pageRoot, "TaskPageRoot");

            TextMeshProUGUI taskProgressText = CreateText("TaskProgressText", pageRoot, style, 22f, FontStyles.Bold);
            taskProgressText.text = "任务进度";
            taskProgressText.alignment = TextAlignmentOptions.Left;
            taskProgressText.rectTransform.anchorMin = new Vector2(0f, 1f);
            taskProgressText.rectTransform.anchorMax = new Vector2(1f, 1f);
            taskProgressText.rectTransform.pivot = new Vector2(0.5f, 1f);
            taskProgressText.rectTransform.offsetMin = new Vector2(16f, -60f);
            taskProgressText.rectTransform.offsetMax = new Vector2(-16f, -16f);
            EnsureBinding(cdeTable.ComponentTable, taskProgressText, "TaskProgressText");

            RectTransform taskScroll = CreateScrollListRoot("TaskScroll", pageRoot, out RectTransform taskContent);
            Stretch(taskScroll, new Vector2(16f, 16f), new Vector2(-16f, -66f));
            EnsureBinding(cdeTable.ComponentTable, taskContent, "TaskListContent");

            RectTransform taskItemTemplate = CreateTaskTemplate(taskContent, style);
            taskItemTemplate.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, taskItemTemplate, "TaskItemTemplate");

            TextMeshProUGUI taskEmptyText = CreateText("TaskEmptyText", pageRoot, style, 22f, FontStyles.Normal);
            taskEmptyText.text = "当前没有主城任务。";
            taskEmptyText.alignment = TextAlignmentOptions.Center;
            Stretch(taskEmptyText.rectTransform, new Vector2(16f, 16f), new Vector2(-16f, -66f));
            taskEmptyText.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, taskEmptyText, "TaskEmptyText");
        }

        private static void BuildMuseumPage(RectTransform parent, StyleContext style, UIBindCDETable cdeTable)
        {
            RectTransform pageRoot = CreatePageRoot("MuseumPageRoot", parent, new Color32(12, 17, 24, 220));
            EnsureBinding(cdeTable.ComponentTable, pageRoot, "MuseumPageRoot");

            TextMeshProUGUI capacityText = CreateText("MuseumCapacityText", pageRoot, style, 22f, FontStyles.Bold);
            capacityText.text = "展示容量";
            capacityText.alignment = TextAlignmentOptions.Left;
            capacityText.rectTransform.anchorMin = new Vector2(0f, 1f);
            capacityText.rectTransform.anchorMax = new Vector2(1f, 1f);
            capacityText.rectTransform.pivot = new Vector2(0.5f, 1f);
            capacityText.rectTransform.offsetMin = new Vector2(16f, -52f);
            capacityText.rectTransform.offsetMax = new Vector2(-16f, -12f);
            EnsureBinding(cdeTable.ComponentTable, capacityText, "MuseumCapacityText");

            RectTransform gridScroll = CreateScrollListRoot("MuseumGridScroll", pageRoot, out RectTransform gridContent);
            gridScroll.anchorMin = new Vector2(0f, 0.44f);
            gridScroll.anchorMax = new Vector2(1f, 1f);
            gridScroll.pivot = new Vector2(0.5f, 1f);
            gridScroll.offsetMin = new Vector2(16f, 0f);
            gridScroll.offsetMax = new Vector2(-16f, -60f);
            EnsureBinding(cdeTable.ComponentTable, gridContent, "MuseumGridContent");

            Button gridTemplate = CreateCardButtonTemplate(
                "MuseumGridTemplate",
                gridContent,
                style,
                new Color32(78, 56, 64, 255),
                104f);
            gridTemplate.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, gridTemplate, "MuseumGridTemplate");

            RectTransform warehouseScroll = CreateScrollListRoot("MuseumWarehouseScroll", pageRoot, out RectTransform warehouseContent);
            warehouseScroll.anchorMin = new Vector2(0f, 0f);
            warehouseScroll.anchorMax = new Vector2(1f, 0.4f);
            warehouseScroll.pivot = new Vector2(0.5f, 0f);
            warehouseScroll.offsetMin = new Vector2(16f, 16f);
            warehouseScroll.offsetMax = new Vector2(-16f, -12f);
            EnsureBinding(cdeTable.ComponentTable, warehouseContent, "MuseumWarehouseContent");

            Button warehouseTemplate = CreateCardButtonTemplate(
                "MuseumWarehouseTemplate",
                warehouseContent,
                style,
                new Color32(64, 48, 42, 255),
                96f);
            warehouseTemplate.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, warehouseTemplate, "MuseumWarehouseTemplate");

            TextMeshProUGUI hintText = CreateText("MuseumHintText", pageRoot, style, 20f, FontStyles.Normal);
            hintText.text = "点击下方仓库物品进行选择，再点击上方空展示位摆放；点击已摆放单位可取下。";
            hintText.alignment = TextAlignmentOptions.TopLeft;
            hintText.textWrappingMode = TextWrappingModes.Normal;
            hintText.rectTransform.anchorMin = new Vector2(0f, 0.4f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 0.44f);
            hintText.rectTransform.offsetMin = new Vector2(16f, 0f);
            hintText.rectTransform.offsetMax = new Vector2(-16f, -4f);
            EnsureBinding(cdeTable.ComponentTable, hintText, "MuseumHintText");
        }

        private static void BuildRecyclePage(RectTransform parent, StyleContext style, UIBindCDETable cdeTable)
        {
            RectTransform pageRoot = CreatePageRoot("RecyclePageRoot", parent, new Color32(12, 17, 24, 220));
            EnsureBinding(cdeTable.ComponentTable, pageRoot, "RecyclePageRoot");

            TextMeshProUGUI summaryText = CreateText("RecycleSummaryText", pageRoot, style, 22f, FontStyles.Bold);
            summaryText.text = "回收位概览";
            summaryText.alignment = TextAlignmentOptions.Left;
            summaryText.rectTransform.anchorMin = new Vector2(0f, 1f);
            summaryText.rectTransform.anchorMax = new Vector2(1f, 1f);
            summaryText.rectTransform.pivot = new Vector2(0.5f, 1f);
            summaryText.rectTransform.offsetMin = new Vector2(16f, -52f);
            summaryText.rectTransform.offsetMax = new Vector2(-16f, -12f);
            EnsureBinding(cdeTable.ComponentTable, summaryText, "RecycleSummaryText");

            RectTransform slotScroll = CreateScrollListRoot("RecycleSlotScroll", pageRoot, out RectTransform slotContent);
            slotScroll.anchorMin = new Vector2(0f, 0.44f);
            slotScroll.anchorMax = new Vector2(1f, 1f);
            slotScroll.pivot = new Vector2(0.5f, 1f);
            slotScroll.offsetMin = new Vector2(16f, 0f);
            slotScroll.offsetMax = new Vector2(-16f, -60f);
            EnsureBinding(cdeTable.ComponentTable, slotContent, "RecycleSlotContent");

            Button slotTemplate = CreateCardButtonTemplate(
                "RecycleSlotTemplate",
                slotContent,
                style,
                new Color32(42, 72, 76, 255),
                104f);
            slotTemplate.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, slotTemplate, "RecycleSlotTemplate");

            RectTransform sourceScroll = CreateScrollListRoot("RecycleSourceScroll", pageRoot, out RectTransform sourceContent);
            sourceScroll.anchorMin = new Vector2(0f, 0f);
            sourceScroll.anchorMax = new Vector2(1f, 0.4f);
            sourceScroll.pivot = new Vector2(0.5f, 0f);
            sourceScroll.offsetMin = new Vector2(16f, 16f);
            sourceScroll.offsetMax = new Vector2(-16f, -12f);
            EnsureBinding(cdeTable.ComponentTable, sourceContent, "RecycleSourceContent");

            Button sourceTemplate = CreateCardButtonTemplate(
                "RecycleSourceTemplate",
                sourceContent,
                style,
                new Color32(60, 55, 36, 255),
                96f);
            sourceTemplate.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, sourceTemplate, "RecycleSourceTemplate");

            TextMeshProUGUI hintText = CreateText("RecycleHintText", pageRoot, style, 20f, FontStyles.Normal);
            hintText.text = "先点下方仓库物品，再点上方空回收位开工；已完成回收位可直接点卡片收取。";
            hintText.alignment = TextAlignmentOptions.TopLeft;
            hintText.textWrappingMode = TextWrappingModes.Normal;
            hintText.rectTransform.anchorMin = new Vector2(0f, 0.4f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 0.44f);
            hintText.rectTransform.offsetMin = new Vector2(16f, 0f);
            hintText.rectTransform.offsetMax = new Vector2(-16f, -4f);
            EnsureBinding(cdeTable.ComponentTable, hintText, "RecycleHintText");
        }

        private static void BuildFarmPage(RectTransform parent, StyleContext style, UIBindCDETable cdeTable)
        {
            RectTransform pageRoot = CreatePageRoot("FarmPageRoot", parent, new Color32(12, 17, 24, 220));
            EnsureBinding(cdeTable.ComponentTable, pageRoot, "FarmPageRoot");

            TextMeshProUGUI summaryText = CreateText("FarmSummaryText", pageRoot, style, 24f, FontStyles.Bold);
            summaryText.text = "农场概览";
            summaryText.alignment = TextAlignmentOptions.TopLeft;
            summaryText.textWrappingMode = TextWrappingModes.Normal;
            Stretch(summaryText.rectTransform, new Vector2(18f, 96f), new Vector2(-18f, -18f));
            EnsureBinding(cdeTable.ComponentTable, summaryText, "FarmSummaryText");

            Button collectButton = CreateButton("FarmCollectButton", pageRoot, style);
            RectTransform collectRect = collectButton.transform as RectTransform;
            collectRect.anchorMin = new Vector2(0f, 1f);
            collectRect.anchorMax = new Vector2(0f, 1f);
            collectRect.pivot = new Vector2(0f, 1f);
            collectRect.sizeDelta = new Vector2(180f, 56f);
            collectRect.anchoredPosition = new Vector2(18f, -18f);
            SetButtonVisual(collectButton, new Color32(87, 143, 86, 255), style.ButtonSprite, style.ButtonColors);
            CreateButtonLabel(collectRect, style, "收取金币");
            EnsureBinding(cdeTable.ComponentTable, collectButton, "FarmCollectButton");

            TextMeshProUGUI hintText = CreateText("FarmHintText", pageRoot, style, 20f, FontStyles.Normal);
            hintText.text = "农场按时间累积金币，收取后会同步刷新顶部财富。";
            hintText.alignment = TextAlignmentOptions.TopLeft;
            hintText.textWrappingMode = TextWrappingModes.Normal;
            hintText.rectTransform.anchorMin = new Vector2(0f, 1f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 1f);
            hintText.rectTransform.pivot = new Vector2(0.5f, 1f);
            hintText.rectTransform.offsetMin = new Vector2(214f, -64f);
            hintText.rectTransform.offsetMax = new Vector2(-18f, -18f);
            EnsureBinding(cdeTable.ComponentTable, hintText, "FarmHintText");
        }

        private static void BuildWarehousePage(RectTransform parent, StyleContext style, UIBindCDETable cdeTable)
        {
            RectTransform pageRoot = CreatePageRoot("WarehousePageRoot", parent, new Color32(12, 17, 24, 220));
            EnsureBinding(cdeTable.ComponentTable, pageRoot, "WarehousePageRoot");

            TextMeshProUGUI capacityText = CreateText("WarehouseCapacityText", pageRoot, style, 22f, FontStyles.Bold);
            capacityText.text = "仓库容量";
            capacityText.alignment = TextAlignmentOptions.Left;
            capacityText.rectTransform.anchorMin = new Vector2(0f, 1f);
            capacityText.rectTransform.anchorMax = new Vector2(1f, 1f);
            capacityText.rectTransform.pivot = new Vector2(0.5f, 1f);
            capacityText.rectTransform.offsetMin = new Vector2(16f, -52f);
            capacityText.rectTransform.offsetMax = new Vector2(-16f, -12f);
            EnsureBinding(cdeTable.ComponentTable, capacityText, "WarehouseCapacityText");

            RectTransform listScroll = CreateScrollListRoot("WarehouseListScroll", pageRoot, out RectTransform listContent);
            listScroll.anchorMin = new Vector2(0f, 0.28f);
            listScroll.anchorMax = new Vector2(1f, 1f);
            listScroll.pivot = new Vector2(0.5f, 1f);
            listScroll.offsetMin = new Vector2(16f, 0f);
            listScroll.offsetMax = new Vector2(-16f, -60f);
            EnsureBinding(cdeTable.ComponentTable, listContent, "WarehouseListContent");

            Button listTemplate = CreateCardButtonTemplate(
                "WarehouseListTemplate",
                listContent,
                style,
                new Color32(71, 64, 42, 255),
                96f);
            listTemplate.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, listTemplate, "WarehouseListTemplate");

            TextMeshProUGUI detailText = CreateText("WarehouseDetailText", pageRoot, style, 20f, FontStyles.Normal);
            detailText.text = "选择下方物品可查看详情。";
            detailText.alignment = TextAlignmentOptions.TopLeft;
            detailText.textWrappingMode = TextWrappingModes.Normal;
            detailText.rectTransform.anchorMin = new Vector2(0f, 0f);
            detailText.rectTransform.anchorMax = new Vector2(1f, 0.24f);
            detailText.rectTransform.offsetMin = new Vector2(16f, 52f);
            detailText.rectTransform.offsetMax = new Vector2(-16f, -12f);
            EnsureBinding(cdeTable.ComponentTable, detailText, "WarehouseDetailText");

            TextMeshProUGUI emptyText = CreateText("WarehouseEmptyText", pageRoot, style, 22f, FontStyles.Normal);
            emptyText.text = "仓库当前为空。";
            emptyText.alignment = TextAlignmentOptions.Center;
            Stretch(emptyText.rectTransform, new Vector2(16f, 52f), new Vector2(-16f, -60f));
            emptyText.gameObject.SetActive(false);
            EnsureBinding(cdeTable.ComponentTable, emptyText, "WarehouseEmptyText");
        }

        private static void GenerateYiuiCode()
        {
            GameObject homeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HomePanelPrefabPath);
            if (homeAsset == null)
            {
                throw new InvalidOperationException("HomePanel prefab save failed.");
            }

            UIBindCDETable homeCde = homeAsset.GetComponent<UIBindCDETable>();
            if (homeCde == null)
            {
                throw new InvalidOperationException("HomePanel missing UIBindCDETable.");
            }

            InvokeUICreatePackages(homeCde, "statesync");
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

        private static void CreateTopValueText(
            string name,
            RectTransform parent,
            StyleContext style,
            string text,
            Vector2 anchoredPosition,
            UIBindCDETable cdeTable)
        {
            TextMeshProUGUI valueText = CreateText(name, parent, style, 20f, FontStyles.Bold);
            valueText.text = text;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            valueText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            valueText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            valueText.rectTransform.sizeDelta = new Vector2(150f, 42f);
            valueText.rectTransform.anchoredPosition = anchoredPosition;
            EnsureBinding(cdeTable.ComponentTable, valueText, name);
        }

        private static void CreateNavButton(
            string name,
            RectTransform parent,
            StyleContext style,
            string label,
            Vector2 anchoredPosition,
            UIBindCDETable cdeTable)
        {
            Button button = CreateButton(name, parent, style);
            RectTransform rect = button.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(156f, 48f);
            rect.anchoredPosition = anchoredPosition;
            SetButtonVisual(button, new Color32(72, 86, 110, 255), style.ButtonSprite, style.ButtonColors);
            CreateButtonLabel(rect, style, label);
            EnsureBinding(cdeTable.ComponentTable, button, name);
        }

        private static RectTransform CreatePageRoot(string name, RectTransform parent, Color color)
        {
            RectTransform pageRoot = CreatePanel(name, parent, color);
            Stretch(pageRoot);
            return pageRoot;
        }

        private static RectTransform CreateHomeDetailCard(string name, RectTransform parent, StyleContext style)
        {
            RectTransform card = CreatePanel(name, parent, new Color32(25, 32, 43, 245));

            TextMeshProUGUI nameText = CreateText("DetailNameText", card, style, 28f, FontStyles.Bold);
            nameText.text = "未选中建筑";
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameText.rectTransform.anchorMax = new Vector2(1f, 1f);
            nameText.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameText.rectTransform.offsetMin = new Vector2(18f, -54f);
            nameText.rectTransform.offsetMax = new Vector2(-18f, -14f);

            TextMeshProUGUI statusText = CreateText("DetailStatusText", card, style, 20f, FontStyles.Bold);
            statusText.text = "状态：--";
            statusText.alignment = TextAlignmentOptions.Left;
            statusText.rectTransform.anchorMin = new Vector2(0f, 1f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 1f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            statusText.rectTransform.offsetMin = new Vector2(18f, -92f);
            statusText.rectTransform.offsetMax = new Vector2(-18f, -58f);

            TextMeshProUGUI hintText = CreateText("DetailHintText", card, style, 20f, FontStyles.Normal);
            hintText.text = "点击场景中的建筑或空地后，这里会显示对应规则、产出和操作说明。";
            hintText.alignment = TextAlignmentOptions.TopLeft;
            hintText.textWrappingMode = TextWrappingModes.Normal;
            hintText.rectTransform.anchorMin = new Vector2(0f, 0f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 1f);
            hintText.rectTransform.offsetMin = new Vector2(18f, 76f);
            hintText.rectTransform.offsetMax = new Vector2(-18f, -102f);

            BuildDetailActionButton(
                card,
                style,
                "DetailPrimaryButton",
                "查看",
                new Color32(92, 119, 169, 255),
                new Vector2(0f, 0f),
                new Vector2(1f / 3f, 0f),
                new Vector2(HomeDetailButtonOuterPadding, HomeDetailButtonBottom),
                new Vector2(-HomeDetailButtonInnerPadding, HomeDetailButtonBottom + HomeDetailButtonHeight));
            BuildDetailActionButton(
                card,
                style,
                "DetailUpgradeButton",
                "升级",
                new Color32(140, 108, 52, 255),
                new Vector2(1f / 3f, 0f),
                new Vector2(2f / 3f, 0f),
                new Vector2(HomeDetailButtonInnerPadding, HomeDetailButtonBottom),
                new Vector2(-HomeDetailButtonInnerPadding, HomeDetailButtonBottom + HomeDetailButtonHeight));
            BuildDetailActionButton(
                card,
                style,
                "DetailDemolishButton",
                "拆除",
                new Color32(142, 74, 74, 255),
                new Vector2(2f / 3f, 0f),
                new Vector2(1f, 0f),
                new Vector2(HomeDetailButtonInnerPadding, HomeDetailButtonBottom),
                new Vector2(-HomeDetailButtonOuterPadding, HomeDetailButtonBottom + HomeDetailButtonHeight));
            return card;
        }

        private static void BuildDetailActionButton(
            RectTransform card,
            StyleContext style,
            string name,
            string label,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            Button button = CreateButton(name, card, style);
            RectTransform rect = button.transform as RectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            SetButtonVisual(button, color, style.ButtonSprite, style.ButtonColors);
            CreateButtonLabel(rect, style, label);
        }

        private static RectTransform CreateScrollListRoot(string name, RectTransform parent, out RectTransform content)
        {
            RectTransform root = CreatePanel(name, parent, new Color32(7, 10, 16, 196));
            ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return root;
        }

        private static Button CreateCardButtonTemplate(
            string name,
            RectTransform parent,
            StyleContext style,
            Color color,
            float preferredHeight)
        {
            Button button = CreateButton(name, parent, style);
            RectTransform rect = button.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, preferredHeight);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            SetButtonVisual(button, color, style.ButtonSprite, style.ButtonColors);

            TextMeshProUGUI title = CreateText("TitleText", rect, style, 22f, FontStyles.Bold);
            title.text = "标题";
            title.alignment = TextAlignmentOptions.Left;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.offsetMin = new Vector2(16f, -40f);
            title.rectTransform.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI subTitle = CreateText("SubTitleText", rect, style, 18f, FontStyles.Normal);
            subTitle.text = "描述";
            subTitle.alignment = TextAlignmentOptions.TopLeft;
            subTitle.textWrappingMode = TextWrappingModes.Normal;
            subTitle.rectTransform.anchorMin = new Vector2(0f, 0f);
            subTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            subTitle.rectTransform.offsetMin = new Vector2(16f, 12f);
            subTitle.rectTransform.offsetMax = new Vector2(-16f, -44f);
            return button;
        }

        private static RectTransform CreateTaskTemplate(RectTransform parent, StyleContext style)
        {
            RectTransform rect = CreatePanel("TaskItemTemplate", parent, new Color32(52, 62, 78, 255));
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 112f);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 112f;

            TextMeshProUGUI title = CreateText("TitleText", rect, style, 22f, FontStyles.Bold);
            title.text = "任务标题";
            title.alignment = TextAlignmentOptions.Left;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.offsetMin = new Vector2(16f, -40f);
            title.rectTransform.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI progress = CreateText("ProgressText", rect, style, 18f, FontStyles.Bold);
            progress.text = "0/1";
            progress.alignment = TextAlignmentOptions.Right;
            progress.rectTransform.anchorMin = new Vector2(1f, 1f);
            progress.rectTransform.anchorMax = new Vector2(1f, 1f);
            progress.rectTransform.pivot = new Vector2(1f, 1f);
            progress.rectTransform.sizeDelta = new Vector2(140f, 28f);
            progress.rectTransform.anchoredPosition = new Vector2(-16f, -12f);

            TextMeshProUGUI desc = CreateText("DescText", rect, style, 18f, FontStyles.Normal);
            desc.text = "任务说明";
            desc.alignment = TextAlignmentOptions.TopLeft;
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.rectTransform.anchorMin = new Vector2(0f, 0f);
            desc.rectTransform.anchorMax = new Vector2(1f, 1f);
            desc.rectTransform.offsetMin = new Vector2(16f, 12f);
            desc.rectTransform.offsetMax = new Vector2(-16f, -44f);
            return rect;
        }

        private static Button CreateButton(string name, RectTransform parent, StyleContext style)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
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
            GameObject rectObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
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
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
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

        private static void CreateButtonLabel(RectTransform parent, StyleContext style, string label)
        {
            TextMeshProUGUI text = CreateText("Label", parent, style, 20f, FontStyles.Bold);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);
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

        private static void SetGraphicRaycastTarget(Component component, bool raycastTarget)
        {
            Graphic graphic = component != null ? component.GetComponent<Graphic>() : null;
            if (graphic != null)
            {
                graphic.raycastTarget = raycastTarget;
            }
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

        private static TextMeshProUGUI FindRequiredText(RectTransform root, string name)
        {
            Transform found = root.FindChildByName(name);
            TextMeshProUGUI text = found != null ? found.GetComponent<TextMeshProUGUI>() : null;
            if (text == null)
            {
                throw new InvalidOperationException($"Missing required text node: {name}");
            }

            return text;
        }

        private static Button FindRequiredButton(RectTransform root, string name)
        {
            Transform found = root.FindChildByName(name);
            Button button = found != null ? found.GetComponent<Button>() : null;
            if (button == null)
            {
                throw new InvalidOperationException($"Missing required button node: {name}");
            }

            return button;
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

        private static void EnsureDirectoryForAsset(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), directory));
            }
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
                Debug.LogWarning($"[HomePanelYiuiBuilder] Home chinese font missing characters: {missingCharacters}");
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
                "家园",
                "点击场景中的主城、建筑或空地，右侧会打开对应管理页。",
                "金币 --",
                "主城 --",
                "仓库 --",
                "待办 --",
                "关闭",
                "家园总览",
                "总览",
                "主城任务",
                "收藏馆",
                "回收间",
                "农场",
                "建筑与槽位",
                "暂无家园槽位数据",
                "总览、建造、管理操作都在这里承接。",
                "未选中建筑",
                "状态：--",
                "点击场景中的建筑或空地后，这里会显示对应规则、产出和操作说明。",
                "查看",
                "升级",
                "拆除",
                "选中空地后，会在这里显示当前可建建筑。",
                "当前没有需要显示的建造项。",
                "任务进度",
                "当前没有主城任务。",
                "任务标题",
                "任务说明",
                "展示容量",
                "点击下方仓库物品进行选择，再点击上方空展示位摆放；点击已摆放单位可取下。",
                "回收位概览",
                "先点下方仓库物品，再点上方空回收位开工；已完成回收位可直接点卡片收取。",
                "农场概览",
                "收取金币",
                "农场按时间累积金币，收取后会同步刷新顶部财富。",
                "仓库容量",
                "选择下方物品可查看详情。",
                "仓库当前为空。",
                "标题",
                "描述",
                "0123456789",
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "abcdefghijklmnopqrstuvwxyz",
                " -_:：/.,，。()（）[]【】·\n"
            };

            System.Collections.Generic.HashSet<char> uniqueCharacters = new();
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

        private struct StyleContext
        {
            public TMP_FontAsset FontAsset;
            public Material FontMaterial;
            public Sprite ButtonSprite;
            public ColorBlock ButtonColors;
        }
    }
}
