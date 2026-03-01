using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// 当前Panel所有可用view枚举
    /// </summary>
    public enum ELobbyPanelViewEnum
    {
        EquipSelectView = 1,
        MatchView = 2,
    }
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Panel, EPanelLayer.Panel)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class LobbyPanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Lobby";
        public const string ResName = "LobbyPanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIPanelComponent> u_UIPanel;
        public YIUIPanelComponent UIPanel => u_UIPanel;
        public UnityEngine.RectTransform u_ComRolePanelRectTransform;
        public UnityEngine.RectTransform u_ComEquipPanelRectTransform;
        public UnityEngine.RectTransform u_ComMatchPanelRectTransform;
        public UnityEngine.RectTransform u_ComBuildPanelRectTransform;
        public UnityEngine.RectTransform u_ComExplorePanelRectTransform;
        public UnityEngine.RectTransform u_ComHeroList;
        public UnityEngine.RectTransform u_ComEquipBagScroll;
        public EntityRef<ET.Client.EquipSlotItemComponent> u_UIEquipSlotItemBag;
        public ET.Client.EquipSlotItemComponent UIEquipSlotItemBag => u_UIEquipSlotItemBag;
        public EntityRef<ET.Client.EquipSlotItemComponent> u_UIEquipSlotItemArmor;
        public ET.Client.EquipSlotItemComponent UIEquipSlotItemArmor => u_UIEquipSlotItemArmor;
        public EntityRef<ET.Client.EquipSlotItemComponent> u_UIEquipSlotItemWeapon2;
        public ET.Client.EquipSlotItemComponent UIEquipSlotItemWeapon2 => u_UIEquipSlotItemWeapon2;
        public EntityRef<ET.Client.EquipSlotItemComponent> u_UIEquipSlotItemWeapon;
        public ET.Client.EquipSlotItemComponent UIEquipSlotItemWeapon => u_UIEquipSlotItemWeapon;
        public UITaskEventP0 u_EventEnterMap;
        public UITaskEventHandleP0 u_EventEnterMapHandle;
        public const string OnEventEnterMapInvoke = "LobbyPanelComponent.OnEventEnterMapInvoke";
        public UITaskEventP0 u_EventRoleToggle;
        public UITaskEventHandleP0 u_EventRoleToggleHandle;
        public const string OnEventRoleToggleInvoke = "LobbyPanelComponent.OnEventRoleToggleInvoke";
        public UITaskEventP0 u_EventEquipToggle;
        public UITaskEventHandleP0 u_EventEquipToggleHandle;
        public const string OnEventEquipToggleInvoke = "LobbyPanelComponent.OnEventEquipToggleInvoke";
        public UITaskEventP0 u_EventMatchToggle;
        public UITaskEventHandleP0 u_EventMatchToggleHandle;
        public const string OnEventMatchToggleInvoke = "LobbyPanelComponent.OnEventMatchToggleInvoke";
        public UITaskEventP0 u_EventBuildToggle;
        public UITaskEventHandleP0 u_EventBuildToggleHandle;
        public const string OnEventBuildToggleInvoke = "LobbyPanelComponent.OnEventBuildToggleInvoke";
        public UITaskEventP0 u_EventExploreToggle;
        public UITaskEventHandleP0 u_EventExploreToggleHandle;
        public const string OnEventExploreToggleInvoke = "LobbyPanelComponent.OnEventExploreToggleInvoke";
        public UITaskEventP0 u_EventThreeThreeMatchButton;
        public UITaskEventHandleP0 u_EventThreeThreeMatchButtonHandle;
        public const string OnEventThreeThreeMatchButtonInvoke = "LobbyPanelComponent.OnEventThreeThreeMatchButtonInvoke";
        public UITaskEventP0 u_EventSouDaCeMatchButton;
        public UITaskEventHandleP0 u_EventSouDaCeMatchButtonHandle;
        public const string OnEventSouDaCeMatchButtonInvoke = "LobbyPanelComponent.OnEventSouDaCeMatchButtonInvoke";
        public UITaskEventP0 u_EventOneOneMatchButton;
        public UITaskEventHandleP0 u_EventOneOneMatchButtonHandle;
        public const string OnEventOneOneMatchButtonInvoke = "LobbyPanelComponent.OnEventOneOneMatchButtonInvoke";
        public UITaskEventP0 u_EventClickPutIntoBag;
        public UITaskEventHandleP0 u_EventClickPutIntoBagHandle;
        public const string OnEventClickPutIntoBagInvoke = "LobbyPanelComponent.OnEventClickPutIntoBagInvoke";

    }
}