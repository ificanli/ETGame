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
    public partial class SettlementPanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Common";
        public const string ResName = "SettlementPanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIPanelComponent> u_UIPanel;
        public YIUIPanelComponent UIPanel => u_UIPanel;
        public YIUIFramework.UIDataValueString u_DataGoldSettletment;
        public YIUIFramework.UIDataValueString u_DataKillNum;
        public YIUIFramework.UIDataValueBool u_DataExtractResult;
        public UITaskEventP0 u_EventClickExitPanel;
        public UITaskEventHandleP0 u_EventClickExitPanelHandle;
        public const string OnEventClickExitPanelInvoke = "SettlementPanelComponent.OnEventClickExitPanelInvoke";

    }
}