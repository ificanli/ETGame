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
    public partial class EquipSlotItemComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Lobby";
        public const string ResName = "EquipSlotItem";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public YIUIFramework.UIDataValueString u_DataSlotName;
        public YIUIFramework.UIDataValueString u_DataEquipName;
        public YIUIFramework.UIDataValueBool u_DataIsEmpty;

    }
}