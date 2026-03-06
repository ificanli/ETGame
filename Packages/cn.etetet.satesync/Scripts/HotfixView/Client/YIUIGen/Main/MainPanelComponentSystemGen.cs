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
    [EntitySystemOf(typeof(MainPanelComponent))]
    public static partial class MainPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MainPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this MainPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this MainPanelComponent self)
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

            self.u_ComJoyStickRangeRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComJoyStickRangeRectTransform");
            self.u_ComJoyStickRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComJoyStickRectTransform");
            self.u_DataSearchingButton = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueBool>("u_DataSearchingButton");
            self.u_EventClickSearchingButton = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickSearchingButton");
            self.u_EventClickSearchingButtonHandle = self.u_EventClickSearchingButton.Add(self,MainPanelComponent.OnEventClickSearchingButtonInvoke);
            self.u_EventClickBagButton = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickBagButton");
            self.u_EventClickBagButtonHandle = self.u_EventClickBagButton.Add(self,MainPanelComponent.OnEventClickBagButtonInvoke);
            self.u_UIWeaponBar = self.UIBase.CDETable.FindUIOwner<ET.Client.WeaponBarComponent>("WeaponBar");
            self.u_UITargetTargetInfo = self.UIBase.CDETable.FindUIOwner<ET.Client.TargetTargetInfoComponent>("TargetTargetInfo");
            self.u_UITargetInfo = self.UIBase.CDETable.FindUIOwner<ET.Client.TargetInfoComponent>("TargetInfo");
            self.u_UIPlayerInfo = self.UIBase.CDETable.FindUIOwner<ET.Client.PlayerInfoComponent>("PlayerInfo");
            self.u_UICastFrame = self.UIBase.CDETable.FindUIOwner<ET.Client.CastSliderComponent>("CastFrame");
            self.u_UIActionBar = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarComponent>("ActionBar");

        }
    }
}
