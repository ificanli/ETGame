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
    [EntitySystemOf(typeof(DamageTipsPanelComponent))]
    public static partial class DamageTipsPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this DamageTipsPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this DamageTipsPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this DamageTipsPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.BanOpenTween|EWindowOption.BanCloseTween|EWindowOption.BanAwaitOpenTween|EWindowOption.BanAwaitCloseTween|EWindowOption.SkipOtherOpenTween|EWindowOption.SkipOtherCloseTween|EWindowOption.AllowOptionByTween;
            self.UIPanel.Layer = EPanelLayer.Scene;
            self.UIPanel.PanelOption = EPanelOption.ForeverCache|EPanelOption.IgnoreBack|EPanelOption.IgnoreClose;
            self.UIPanel.StackOption = EPanelStackOption.None;
            self.UIPanel.Priority = 0;

            self.u_ComUIPoolParent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComUIPoolParent");

        }
    }
}
