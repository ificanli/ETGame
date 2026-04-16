using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Panel, EPanelLayer.Panel)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class HomePanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Home";
        public const string ResName = "HomePanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIPanelComponent> u_UIPanel;
        public YIUIPanelComponent UIPanel => u_UIPanel;
        public TMPro.TextMeshProUGUI u_ComTitleText;
        public TMPro.TextMeshProUGUI u_ComSelectionHintText;
        public TMPro.TextMeshProUGUI u_ComTotalWealthText;
        public TMPro.TextMeshProUGUI u_ComMainCityLevelText;
        public TMPro.TextMeshProUGUI u_ComWarehouseSummaryText;
        public TMPro.TextMeshProUGUI u_ComTodoHintText;
        public UnityEngine.UI.Button u_ComCloseButton;
        public TMPro.TextMeshProUGUI u_ComOverviewSummaryText;
        public UnityEngine.UI.Button u_ComOverviewButton;
        public UnityEngine.UI.Button u_ComMainCityTaskButton;
        public UnityEngine.UI.Button u_ComMuseumButton;
        public UnityEngine.UI.Button u_ComRecycleButton;
        public UnityEngine.UI.Button u_ComFarmButton;
        public UnityEngine.UI.Button u_ComWarehouseButton;
        public UnityEngine.RectTransform u_ComBuildingListContent;
        public UnityEngine.UI.Button u_ComBuildingItemTemplate;
        public TMPro.TextMeshProUGUI u_ComBuildingListEmptyText;
        public TMPro.TextMeshProUGUI u_ComPageTitleText;
        public TMPro.TextMeshProUGUI u_ComPageSubtitleText;
        public UnityEngine.RectTransform u_ComDetailCardRoot;
        public TMPro.TextMeshProUGUI u_ComDetailNameText;
        public TMPro.TextMeshProUGUI u_ComDetailStatusText;
        public TMPro.TextMeshProUGUI u_ComDetailHintText;
        public UnityEngine.UI.Button u_ComDetailPrimaryButton;
        public UnityEngine.UI.Button u_ComDetailUpgradeButton;
        public UnityEngine.UI.Button u_ComDetailDemolishButton;
        public UnityEngine.RectTransform u_ComOverviewPageRoot;
        public TMPro.TextMeshProUGUI u_ComBuildHintText;
        public UnityEngine.RectTransform u_ComBuildOptionContent;
        public UnityEngine.UI.Button u_ComBuildOptionTemplate;
        public TMPro.TextMeshProUGUI u_ComOverviewEmptyText;
        public UnityEngine.RectTransform u_ComTaskPageRoot;
        public TMPro.TextMeshProUGUI u_ComTaskProgressText;
        public UnityEngine.RectTransform u_ComTaskListContent;
        public UnityEngine.RectTransform u_ComTaskItemTemplate;
        public TMPro.TextMeshProUGUI u_ComTaskEmptyText;
        public UnityEngine.RectTransform u_ComMuseumPageRoot;
        public TMPro.TextMeshProUGUI u_ComMuseumCapacityText;
        public UnityEngine.RectTransform u_ComMuseumGridContent;
        public UnityEngine.UI.Button u_ComMuseumGridTemplate;
        public UnityEngine.RectTransform u_ComMuseumWarehouseContent;
        public UnityEngine.UI.Button u_ComMuseumWarehouseTemplate;
        public TMPro.TextMeshProUGUI u_ComMuseumHintText;
        public UnityEngine.RectTransform u_ComRecyclePageRoot;
        public TMPro.TextMeshProUGUI u_ComRecycleSummaryText;
        public UnityEngine.RectTransform u_ComRecycleSlotContent;
        public UnityEngine.UI.Button u_ComRecycleSlotTemplate;
        public UnityEngine.RectTransform u_ComRecycleSourceContent;
        public UnityEngine.UI.Button u_ComRecycleSourceTemplate;
        public TMPro.TextMeshProUGUI u_ComRecycleHintText;
        public UnityEngine.RectTransform u_ComFarmPageRoot;
        public TMPro.TextMeshProUGUI u_ComFarmSummaryText;
        public UnityEngine.UI.Button u_ComFarmCollectButton;
        public TMPro.TextMeshProUGUI u_ComFarmHintText;
        public UnityEngine.RectTransform u_ComWarehousePageRoot;
        public TMPro.TextMeshProUGUI u_ComWarehouseCapacityText;
        public UnityEngine.RectTransform u_ComWarehouseListContent;
        public UnityEngine.UI.Button u_ComWarehouseListTemplate;
        public TMPro.TextMeshProUGUI u_ComWarehouseDetailText;
        public TMPro.TextMeshProUGUI u_ComWarehouseEmptyText;

    }
}