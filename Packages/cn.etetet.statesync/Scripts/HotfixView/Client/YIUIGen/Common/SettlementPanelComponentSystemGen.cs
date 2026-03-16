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
    [EntitySystemOf(typeof(SettlementPanelComponent))]
    public static partial class SettlementPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SettlementPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SettlementPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SettlementPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Panel;
            self.UIPanel.PanelOption = EPanelOption.TimeCache;
            self.UIPanel.StackOption = EPanelStackOption.VisibleTween;
            self.UIPanel.Priority = 0;
            self.UIPanel.CachePanelTime = 10;

            self.u_DataGoldSettletment = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataGoldSettletment");
            self.u_DataKillNum = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataKillNum");
            self.u_DataExtractResult = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueBool>("u_DataExtractResult");
            self.u_EventClickExitPanel = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickExitPanel");
            self.u_EventClickExitPanelHandle = self.u_EventClickExitPanel.Add(self,SettlementPanelComponent.OnEventClickExitPanelInvoke);

        }
    }
}
