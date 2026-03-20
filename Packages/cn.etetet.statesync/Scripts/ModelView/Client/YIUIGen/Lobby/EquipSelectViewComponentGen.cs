using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.View)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class EquipSelectViewComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Lobby";
        public const string ResName = "EquipSelectView";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIViewComponent> u_UIView;
        public YIUIViewComponent UIView => u_UIView;
        public UnityEngine.RectTransform u_ComEquipSelectLoopScroll;
        public YIUIFramework.UIDataValueString u_DataGunName;
        public UITaskEventP0 u_EventClickPrepared;
        public UITaskEventHandleP0 u_EventClickPreparedHandle;
        public const string OnEventClickPreparedInvoke = "EquipSelectViewComponent.OnEventClickPreparedInvoke";
        public UITaskEventP0 u_EventExitView;
        public UITaskEventHandleP0 u_EventExitViewHandle;
        public const string OnEventExitViewInvoke = "EquipSelectViewComponent.OnEventExitViewInvoke";
        public UITaskEventP0 u_EventClickWarhouse;
        public UITaskEventHandleP0 u_EventClickWarhouseHandle;
        public const string OnEventClickWarhouseInvoke = "EquipSelectViewComponent.OnEventClickWarhouseInvoke";
        public UITaskEventP0 u_EventClickShop;
        public UITaskEventHandleP0 u_EventClickShopHandle;
        public const string OnEventClickShopInvoke = "EquipSelectViewComponent.OnEventClickShopInvoke";

    }
}