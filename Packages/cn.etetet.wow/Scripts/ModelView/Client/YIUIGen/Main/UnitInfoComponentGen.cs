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
    public partial class UnitInfoComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Main";
        public const string ResName = "UnitInfo";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public YIUIFramework.UIDataValueString u_DataName;
        public YIUIFramework.UIDataValueInt u_DataHP;
        public YIUIFramework.UIDataValueInt u_DataMaxHP;
        public YIUIFramework.UIDataValueInt u_DataMP;
        public YIUIFramework.UIDataValueInt u_DataMaxMP;
        public YIUIFramework.UIDataValueString u_DataIcon;
        public YIUIFramework.UIDataValueFloat u_DataHPRatio;
        public YIUIFramework.UIDataValueFloat u_DataMPRatio;
        public YIUIFramework.UIDataValueString u_DataClass;
        public YIUIFramework.UIDataValueBool u_DataShowClass;
        public YIUIFramework.UIDataValueString u_DataLayoutAnim;
        public UITaskEventP0 u_EventClickInfo;
        public UITaskEventHandleP0 u_EventClickInfoHandle;
        public const string OnEventClickInfoInvoke = "UnitInfoComponent.OnEventClickInfoInvoke";

    }
}