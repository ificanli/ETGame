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
    public partial class EquipSelectItemComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Lobby";
        public const string ResName = "EquipSelectItem";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public YIUIFramework.UIDataValueBool u_DataSelect;
        public YIUIFramework.UIDataValueString u_DataEquipName;
        public UITaskEventP0 u_EventSelect;
        public UITaskEventHandleP0 u_EventSelectHandle;
        public const string OnEventSelectInvoke = "EquipSelectItemComponent.OnEventSelectInvoke";

    }
}