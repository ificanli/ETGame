using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [EntitySystemOf(typeof(HUDPanelComponent))]
    public static partial class HUDPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HUDPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this HUDPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this HUDPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Scene;
            self.UIPanel.PanelOption = EPanelOption.None;
            self.UIPanel.StackOption = EPanelStackOption.None;
            self.UIPanel.Priority = 0;

            self.u_ComHPContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComHPContent");

        }
    }
}
