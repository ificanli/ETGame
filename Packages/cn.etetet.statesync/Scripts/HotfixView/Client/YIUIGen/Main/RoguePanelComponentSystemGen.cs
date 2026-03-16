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
    [EntitySystemOf(typeof(RoguePanelComponent))]
    public static partial class RoguePanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RoguePanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this RoguePanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this RoguePanelComponent self)
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

            self.u_ComRogueOptionRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComRogueOptionRectTransform");
            self.u_ComRogueOption1RectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComRogueOption1RectTransform");
            self.u_ComRogueOption2RectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComRogueOption2RectTransform");
            self.u_UIRogueOption2 = self.UIBase.CDETable.FindUIOwner<ET.Client.RogueOptionComponent>("RogueOption2");
            self.u_UIRogueOption1 = self.UIBase.CDETable.FindUIOwner<ET.Client.RogueOptionComponent>("RogueOption1");
            self.u_UIRogueOption = self.UIBase.CDETable.FindUIOwner<ET.Client.RogueOptionComponent>("RogueOption");

        }
    }
}
