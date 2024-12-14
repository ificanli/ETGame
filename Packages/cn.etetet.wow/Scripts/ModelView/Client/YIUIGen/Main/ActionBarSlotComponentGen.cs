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
    public partial class ActionBarSlotComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Main";
        public const string ResName = "ActionBarSlot";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public YIUIFramework.UIDataValueString u_DataHotKey;
        public YIUIFramework.UIDataValueInt u_DataCharge;
        public YIUIFramework.UIDataValueString u_DataIcon;
        public YIUIFramework.UIDataValueFloat u_DataCountDown;
        public YIUIFramework.UIDataValueFloat u_DataIconFill;
        public YIUIFramework.UIDataValueInt u_DataId;

    }
}