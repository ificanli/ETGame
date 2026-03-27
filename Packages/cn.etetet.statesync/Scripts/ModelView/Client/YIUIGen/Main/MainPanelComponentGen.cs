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
    public enum EMainPanelViewEnum
    {
        EvacuateTipsView = 1,
    }
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Panel, EPanelLayer.Panel)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class MainPanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Main";
        public const string ResName = "MainPanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIPanelComponent> u_UIPanel;
        public YIUIPanelComponent UIPanel => u_UIPanel;
        public UnityEngine.RectTransform u_ComJoyStickRangeRectTransform;
        public UnityEngine.RectTransform u_ComJoyStickRectTransform;
        public UnityEngine.RectTransform u_ComRogueEffectRectTransform;
        public UnityEngine.RectTransform u_ComRogueEffectTextRectTransform;
        public UnityEngine.RectTransform u_ComEffectButton1;
        public UnityEngine.RectTransform u_ComEffectButton2;
        public UnityEngine.RectTransform u_ComEffectButton3;
        public UnityEngine.RectTransform u_ComEffectButton4;
        public YIUIFramework.UIDataValueBool u_DataSearchingButton;
        public YIUIFramework.UIDataValueString u_DataTxtLevel;
        public YIUIFramework.UIDataValueFloat u_DataCurExp;
        public YIUIFramework.UIDataValueString u_DataOpenDoorText;
        public EntityRef<ET.Client.WeaponBarComponent> u_UIWeaponBar;
        public ET.Client.WeaponBarComponent UIWeaponBar => u_UIWeaponBar;
        public EntityRef<ET.Client.TargetTargetInfoComponent> u_UITargetTargetInfo;
        public ET.Client.TargetTargetInfoComponent UITargetTargetInfo => u_UITargetTargetInfo;
        public EntityRef<ET.Client.TargetInfoComponent> u_UITargetInfo;
        public ET.Client.TargetInfoComponent UITargetInfo => u_UITargetInfo;
        public EntityRef<ET.Client.PlayerInfoComponent> u_UIPlayerInfo;
        public ET.Client.PlayerInfoComponent UIPlayerInfo => u_UIPlayerInfo;
        public EntityRef<ET.Client.CastSliderComponent> u_UICastFrame;
        public ET.Client.CastSliderComponent UICastFrame => u_UICastFrame;
        public EntityRef<ET.Client.ActionBarComponent> u_UIActionBar;
        public ET.Client.ActionBarComponent UIActionBar => u_UIActionBar;
        public UITaskEventP0 u_EventClickSearchingButton;
        public UITaskEventHandleP0 u_EventClickSearchingButtonHandle;
        public const string OnEventClickSearchingButtonInvoke = "MainPanelComponent.OnEventClickSearchingButtonInvoke";
        public UITaskEventP0 u_EventClickBagButton;
        public UITaskEventHandleP0 u_EventClickBagButtonHandle;
        public const string OnEventClickBagButtonInvoke = "MainPanelComponent.OnEventClickBagButtonInvoke";
        public UITaskEventP0 u_EventClickOpenMap;
        public UITaskEventHandleP0 u_EventClickOpenMapHandle;
        public const string OnEventClickOpenMapInvoke = "MainPanelComponent.OnEventClickOpenMapInvoke";
        public UITaskEventP0 u_EventOpenedDoor;
        public UITaskEventHandleP0 u_EventOpenedDoorHandle;
        public const string OnEventOpenedDoorInvoke = "MainPanelComponent.OnEventOpenedDoorInvoke";

    }
}