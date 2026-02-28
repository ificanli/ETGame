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
    [EntitySystemOf(typeof(LobbyPanelComponent))]
    public static partial class LobbyPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this LobbyPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this LobbyPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this LobbyPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Panel;
            self.UIPanel.PanelOption = EPanelOption.ForeverCache;
            self.UIPanel.StackOption = EPanelStackOption.Visible;
            self.UIPanel.Priority = 0;

            self.u_ComRolePanelRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComRolePanelRectTransform");
            self.u_ComEquipPanelRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComEquipPanelRectTransform");
            self.u_ComMatchPanelRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComMatchPanelRectTransform");
            self.u_ComBuildPanelRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComBuildPanelRectTransform");
            self.u_ComExplorePanelRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComExplorePanelRectTransform");
            self.u_ComHeroList = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComHeroList");
            self.u_EventEnterMap = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventEnterMap");
            self.u_EventEnterMapHandle = self.u_EventEnterMap.Add(self,LobbyPanelComponent.OnEventEnterMapInvoke);
            self.u_EventRoleToggle = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventRoleToggle");
            self.u_EventRoleToggleHandle = self.u_EventRoleToggle.Add(self,LobbyPanelComponent.OnEventRoleToggleInvoke);
            self.u_EventEquipToggle = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventEquipToggle");
            self.u_EventEquipToggleHandle = self.u_EventEquipToggle.Add(self,LobbyPanelComponent.OnEventEquipToggleInvoke);
            self.u_EventMatchToggle = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventMatchToggle");
            self.u_EventMatchToggleHandle = self.u_EventMatchToggle.Add(self,LobbyPanelComponent.OnEventMatchToggleInvoke);
            self.u_EventBuildToggle = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventBuildToggle");
            self.u_EventBuildToggleHandle = self.u_EventBuildToggle.Add(self,LobbyPanelComponent.OnEventBuildToggleInvoke);
            self.u_EventExploreToggle = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventExploreToggle");
            self.u_EventExploreToggleHandle = self.u_EventExploreToggle.Add(self,LobbyPanelComponent.OnEventExploreToggleInvoke);
            self.u_EventThreeThreeMatchButton = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventThreeThreeMatchButton");
            self.u_EventThreeThreeMatchButtonHandle = self.u_EventThreeThreeMatchButton.Add(self,LobbyPanelComponent.OnEventThreeThreeMatchButtonInvoke);
            self.u_EventSouDaCeMatchButton = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventSouDaCeMatchButton");
            self.u_EventSouDaCeMatchButtonHandle = self.u_EventSouDaCeMatchButton.Add(self,LobbyPanelComponent.OnEventSouDaCeMatchButtonInvoke);
            self.u_EventOneOneMatchButton = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventOneOneMatchButton");
            self.u_EventOneOneMatchButtonHandle = self.u_EventOneOneMatchButton.Add(self,LobbyPanelComponent.OnEventOneOneMatchButtonInvoke);

        }
    }
}
