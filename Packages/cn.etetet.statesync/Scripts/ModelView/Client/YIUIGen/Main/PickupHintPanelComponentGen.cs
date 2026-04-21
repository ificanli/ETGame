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
    public partial class PickupHintPanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Main";
        public const string ResName = "PickupHintPanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public UnityEngine.RectTransform u_ComBackground;
        public UnityEngine.RectTransform u_ComAccent;
        public TMPro.TextMeshProUGUI u_ComItemName;
        public TMPro.TextMeshProUGUI u_ComSubTitle;
        public UnityEngine.UI.Button u_ComPickupButton;
        public YIUIFramework.UIDataValueString u_DataItemName;
        public UITaskEventP0 u_EventPickup;
        public UITaskEventHandleP0 u_EventPickupHandle;
        public const string OnEventPickupInvoke = "PickupHintPanelComponent.OnEventPickupInvoke";

    }
}
