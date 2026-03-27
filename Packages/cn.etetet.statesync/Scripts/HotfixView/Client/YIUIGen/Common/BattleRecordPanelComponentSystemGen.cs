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
    [EntitySystemOf(typeof(BattleRecordPanelComponent))]
    public static partial class BattleRecordPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleRecordPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this BattleRecordPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this BattleRecordPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Panel;
            self.UIPanel.PanelOption = EPanelOption.None;
            self.UIPanel.StackOption = EPanelStackOption.VisibleTween;
            self.UIPanel.Priority = 0;

            self.u_ComMaskButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComMaskButton");
            self.u_ComWindowRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComWindowRoot");
            self.u_ComCloseButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComCloseButton");
            self.u_ComHintText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComHintText");
            self.u_ComListContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComListContent");
            self.u_ComListItemTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComListItemTemplate");
            self.u_ComListEmptyText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComListEmptyText");
            self.u_ComDetailSummaryText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComDetailSummaryText");
            self.u_ComTimelineContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComTimelineContent");
            self.u_ComTimelineItemTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComTimelineItemTemplate");
            self.u_ComDetailEmptyText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComDetailEmptyText");

        }
    }
}
