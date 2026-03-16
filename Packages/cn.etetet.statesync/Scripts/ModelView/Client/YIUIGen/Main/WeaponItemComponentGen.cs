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
    public partial class WeaponItemComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Main";
        public const string ResName = "WeaponItem";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public YIUIFramework.UIDataValueString u_DataAmmoText;
        public UITaskEventP0 u_EventClick;
        public UITaskEventHandleP0 u_EventClickHandle;
        public const string OnEventClickInvoke = "WeaponItemComponent.OnEventClickInvoke";

    }
}