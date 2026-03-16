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
    [EntitySystemOf(typeof(MapWorldPanelComponent))]
    public static partial class MapWorldPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MapWorldPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this MapWorldPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this MapWorldPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Panel;
            self.UIPanel.PanelOption = EPanelOption.TimeCache;
            self.UIPanel.StackOption = EPanelStackOption.Visible;
            self.UIPanel.Priority = 0;
            self.UIPanel.CachePanelTime = 10;

            self.u_EventClickCloseBg = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickCloseBg");
            self.u_EventClickCloseBgHandle = self.u_EventClickCloseBg.Add(self,MapWorldPanelComponent.OnEventClickCloseBgInvoke);

        }
    }
}
