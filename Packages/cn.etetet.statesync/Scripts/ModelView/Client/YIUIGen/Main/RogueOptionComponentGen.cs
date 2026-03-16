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
    public partial class RogueOptionComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Main";
        public const string ResName = "RogueOption";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public UnityEngine.RectTransform u_ComImageRectTransform;
        public UnityEngine.RectTransform u_ComRogueOptionRectTransform;
        public UnityEngine.RectTransform u_ComTagsDescViewRectTransform;
        public UnityEngine.RectTransform u_ComTagRectTransform;
        public UnityEngine.RectTransform u_ComTag2RectTransform;
        public YIUIFramework.UIDataValueString u_DataTextTitle;
        public YIUIFramework.UIDataValueString u_DataTextDes;
        public YIUIFramework.UIDataValueString u_DataTagDes;
        public UITaskEventP0 u_EventClickReroll;
        public UITaskEventHandleP0 u_EventClickRerollHandle;
        public const string OnEventClickRerollInvoke = "RogueOptionComponent.OnEventClickRerollInvoke";
        public UITaskEventP0 u_EventClickRogueOption;
        public UITaskEventHandleP0 u_EventClickRogueOptionHandle;
        public const string OnEventClickRogueOptionInvoke = "RogueOptionComponent.OnEventClickRogueOptionInvoke";
        public UITaskEventP0 u_EventClickTags;
        public UITaskEventHandleP0 u_EventClickTagsHandle;
        public const string OnEventClickTagsInvoke = "RogueOptionComponent.OnEventClickTagsInvoke";

    }
}