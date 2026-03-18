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
    public partial class WeaponBarComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Main";
        public const string ResName = "WeaponBar";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public UnityEngine.RectTransform u_ComWeaponItemRectTransform;
        public EntityRef<ET.Client.WeaponItemComponent> u_UIWeaponItem1;
        public ET.Client.WeaponItemComponent UIWeaponItem1 => u_UIWeaponItem1;
        public EntityRef<ET.Client.WeaponItemComponent> u_UIWeaponItem;
        public ET.Client.WeaponItemComponent UIWeaponItem => u_UIWeaponItem;

    }
}