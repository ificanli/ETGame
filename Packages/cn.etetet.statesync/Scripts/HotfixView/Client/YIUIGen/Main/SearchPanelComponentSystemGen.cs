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
    [EntitySystemOf(typeof(SearchPanelComponent))]
    public static partial class SearchPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SearchPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SearchPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SearchPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Panel;
            self.UIPanel.PanelOption = EPanelOption.None;
            self.UIPanel.StackOption = EPanelStackOption.VisibleTween;
            self.UIPanel.Priority = 0;

            self.u_ComContainerBoardRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComContainerBoardRoot");
            self.u_ComContainerItemsLayer = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComContainerItemsLayer");
            self.u_ComBagBoardRoot = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComBagBoardRoot");
            self.u_ComBagItemsLayer = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComBagItemsLayer");
            self.u_ComContainerItemTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComContainerItemTemplate");
            self.u_ComBagItemTemplate = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComBagItemTemplate");

        }
    }
}
