using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Panel, EPanelLayer.Panel)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class SearchPanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Main";
        public const string ResName = "SearchPanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIPanelComponent> u_UIPanel;
        public YIUIPanelComponent UIPanel => u_UIPanel;
        public UnityEngine.RectTransform u_ComContainerBoardRoot;
        public UnityEngine.RectTransform u_ComContainerItemsLayer;
        public UnityEngine.RectTransform u_ComBagBoardRoot;
        public UnityEngine.RectTransform u_ComBagItemsLayer;
        public UnityEngine.RectTransform u_ComContainerItemTemplate;
        public UnityEngine.RectTransform u_ComBagItemTemplate;
        public UnityEngine.RectTransform u_ComEquipSlotItemWeaponRectTransform;
        public UnityEngine.RectTransform u_ComEquipSlotItemWeapon2RectTransform;
        public UnityEngine.RectTransform u_ComEquipSlotItemArmorRectTransform;
        public UnityEngine.RectTransform u_ComEquipSlotItemBagRectTransform;
        public UnityEngine.RectTransform u_ComSecureBagRootRectTransform;
        public EntityRef<ET.Client.EquipSlotItemComponent> u_UIEquipSlotItemBag;
        public ET.Client.EquipSlotItemComponent UIEquipSlotItemBag => u_UIEquipSlotItemBag;
        public EntityRef<ET.Client.EquipSlotItemComponent> u_UIEquipSlotItemArmor;
        public ET.Client.EquipSlotItemComponent UIEquipSlotItemArmor => u_UIEquipSlotItemArmor;
        public EntityRef<ET.Client.EquipSlotItemComponent> u_UIEquipSlotItemWeapon2;
        public ET.Client.EquipSlotItemComponent UIEquipSlotItemWeapon2 => u_UIEquipSlotItemWeapon2;
        public EntityRef<ET.Client.EquipSlotItemComponent> u_UIEquipSlotItemWeapon;
        public ET.Client.EquipSlotItemComponent UIEquipSlotItemWeapon => u_UIEquipSlotItemWeapon;
        public UIEventP1<int> u_EventQuickChoose;
        public UIEventHandleP1<int> u_EventQuickChooseHandle;
        public const string OnEventQuickChooseInvoke = "SearchPanelComponent.OnEventQuickChooseInvoke";
        public UITaskEventP0 u_EventExit;
        public UITaskEventHandleP0 u_EventExitHandle;
        public const string OnEventExitInvoke = "SearchPanelComponent.OnEventExitInvoke";
        public UITaskEventP0 u_EventClickQuickChoose;
        public UITaskEventHandleP0 u_EventClickQuickChooseHandle;
        public const string OnEventClickQuickChooseInvoke = "SearchPanelComponent.OnEventClickQuickChooseInvoke";

    }
}