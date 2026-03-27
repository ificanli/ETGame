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
    public partial class ItemClickedComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Lobby";
        public const string ResName = "ItemClicked";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public UnityEngine.RectTransform u_ComImage;
        public UnityEngine.RectTransform u_ComEquipRectTransform;
        public YIUIFramework.UIDataValueString u_DataValue;
        public YIUIFramework.UIDataValueString u_DataTitle;
        public YIUIFramework.UIDataValueString u_DataDesc;
        public UITaskEventP0 u_EventEquip;
        public UITaskEventHandleP0 u_EventEquipHandle;
        public const string OnEventEquipInvoke = "ItemClickedComponent.OnEventEquipInvoke";
        public UITaskEventP0 u_EventExit;
        public UITaskEventHandleP0 u_EventExitHandle;
        public const string OnEventExitInvoke = "ItemClickedComponent.OnEventExitInvoke";

    }
}