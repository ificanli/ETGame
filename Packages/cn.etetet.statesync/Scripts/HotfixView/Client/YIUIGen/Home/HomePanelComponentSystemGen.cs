using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [FriendOf(typeof(YIUIChild))]
    [FriendOf(typeof(YIUIWindowComponent))]
    [FriendOf(typeof(YIUIPanelComponent))]
    [EntitySystemOf(typeof(HomePanelComponent))]
    public static partial class HomePanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HomePanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this HomePanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this HomePanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Panel;
            self.UIPanel.PanelOption = EPanelOption.None;
            self.UIPanel.StackOption = EPanelStackOption.VisibleTween;
            self.UIPanel.Priority = 0;

            self.u_ComTitleText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComTitleText");
            self.u_ComSelectionHintText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComSelectionHintText");
            self.u_ComTotalWealthText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComTotalWealthText");
            self.u_ComMainCityLevelText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComMainCityLevelText");
            self.u_ComWarehouseSummaryText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComWarehouseSummaryText");
            self.u_ComTodoHintText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComTodoHintText");
            self.u_ComCloseButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComCloseButton");
            self.u_ComOverviewSummaryText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComOverviewSummaryText");
            self.u_ComOverviewButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComOverviewButton");
            self.u_ComMainCityTaskButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComMainCityTaskButton");
            self.u_ComMuseumButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComMuseumButton");
            self.u_ComRecycleButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComRecycleButton");
            self.u_ComFarmButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComFarmButton");
            self.u_ComWarehouseButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComWarehouseButton");
            self.u_ComBuildingListContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComBuildingListContent");
            self.u_ComBuildingItemTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComBuildingItemTemplate");
            self.u_ComBuildingListEmptyText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComBuildingListEmptyText");
            self.u_ComPageTitleText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComPageTitleText");
            self.u_ComPageSubtitleText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComPageSubtitleText");
            self.u_ComDetailCardRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComDetailCardRoot");
            self.u_ComDetailNameText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComDetailNameText");
            self.u_ComDetailStatusText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComDetailStatusText");
            self.u_ComDetailHintText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComDetailHintText");
            self.u_ComDetailPrimaryButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComDetailPrimaryButton");
            self.u_ComDetailUpgradeButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComDetailUpgradeButton");
            self.u_ComDetailDemolishButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComDetailDemolishButton");
            self.u_ComOverviewPageRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComOverviewPageRoot");
            self.u_ComBuildHintText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComBuildHintText");
            self.u_ComBuildOptionContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComBuildOptionContent");
            self.u_ComBuildOptionTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComBuildOptionTemplate");
            self.u_ComOverviewEmptyText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComOverviewEmptyText");
            self.u_ComTaskPageRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComTaskPageRoot");
            self.u_ComTaskProgressText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComTaskProgressText");
            self.u_ComTaskListContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComTaskListContent");
            self.u_ComTaskItemTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComTaskItemTemplate");
            self.u_ComTaskEmptyText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComTaskEmptyText");
            self.u_ComMuseumPageRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComMuseumPageRoot");
            self.u_ComMuseumCapacityText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComMuseumCapacityText");
            self.u_ComMuseumGridContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComMuseumGridContent");
            self.u_ComMuseumGridTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComMuseumGridTemplate");
            self.u_ComMuseumWarehouseContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComMuseumWarehouseContent");
            self.u_ComMuseumWarehouseTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComMuseumWarehouseTemplate");
            self.u_ComMuseumHintText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComMuseumHintText");
            self.u_ComRecyclePageRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComRecyclePageRoot");
            self.u_ComRecycleSummaryText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComRecycleSummaryText");
            self.u_ComRecycleSlotContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComRecycleSlotContent");
            self.u_ComRecycleSlotTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComRecycleSlotTemplate");
            self.u_ComRecycleSourceContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComRecycleSourceContent");
            self.u_ComRecycleSourceTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComRecycleSourceTemplate");
            self.u_ComRecycleHintText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComRecycleHintText");
            self.u_ComFarmPageRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComFarmPageRoot");
            self.u_ComFarmSummaryText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComFarmSummaryText");
            self.u_ComFarmCollectButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComFarmCollectButton");
            self.u_ComFarmHintText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComFarmHintText");
            self.u_ComWarehousePageRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComWarehousePageRoot");
            self.u_ComWarehouseCapacityText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComWarehouseCapacityText");
            self.u_ComWarehouseListContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComWarehouseListContent");
            self.u_ComWarehouseListTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComWarehouseListTemplate");
            self.u_ComWarehouseDetailText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComWarehouseDetailText");
            self.u_ComWarehouseEmptyText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComWarehouseEmptyText");

        }
    }
}
