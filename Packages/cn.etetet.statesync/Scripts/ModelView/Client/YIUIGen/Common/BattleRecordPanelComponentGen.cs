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
    public partial class BattleRecordPanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Common";
        public const string ResName = "BattleRecordPanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIPanelComponent> u_UIPanel;
        public YIUIPanelComponent UIPanel => u_UIPanel;
        public UnityEngine.UI.Button u_ComMaskButton;
        public UnityEngine.RectTransform u_ComWindowRoot;
        public UnityEngine.UI.Button u_ComCloseButton;
        public TMPro.TextMeshProUGUI u_ComHintText;
        public UnityEngine.RectTransform u_ComListContent;
        public UnityEngine.RectTransform u_ComListItemTemplate;
        public TMPro.TextMeshProUGUI u_ComListEmptyText;
        public TMPro.TextMeshProUGUI u_ComDetailSummaryText;
        public UnityEngine.RectTransform u_ComTimelineContent;
        public UnityEngine.RectTransform u_ComTimelineItemTemplate;
        public TMPro.TextMeshProUGUI u_ComDetailEmptyText;

    }
}