using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Common)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class TargetTargetInfoComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Main";
        public const string ResName = "TargetTargetInfo";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<ET.Client.UnitInfoComponent> u_UIUnitInfo;
        public ET.Client.UnitInfoComponent UIUnitInfo => u_UIUnitInfo;
        public UITaskEventP0 u_EventClickInfo;
        public UITaskEventHandleP0 u_EventClickInfoHandle;
        public const string OnEventClickInfoInvoke = "TargetTargetInfoComponent.OnEventClickInfoInvoke";

    }
}