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
    [EntitySystemOf(typeof(PickupHintPanelComponent))]
    public static partial class PickupHintPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PickupHintPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this PickupHintPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this PickupHintPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Panel;
            self.UIPanel.PanelOption = EPanelOption.None;
            self.UIPanel.StackOption = EPanelStackOption.VisibleTween;
            self.UIPanel.Priority = 0;

            self.u_ComBackground = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComBackground");
            self.u_ComAccent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComAccent");
            self.u_ComItemName = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComItemName");
            self.u_ComSubTitle = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComSubTitle");
            self.u_ComPickupButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComPickupButton");
            self.u_DataItemName = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataItemName");
            self.u_EventPickup = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventPickup");
            self.u_EventPickupHandle = self.u_EventPickup.Add(self,PickupHintPanelComponent.OnEventPickupInvoke);

        }
    }
}
