using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Panel, EPanelLayer.Panel)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class RoguePanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Main";
        public const string ResName = "RoguePanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIPanelComponent> u_UIPanel;
        public YIUIPanelComponent UIPanel => u_UIPanel;
        public UnityEngine.RectTransform u_ComRogueOptionRectTransform;
        public UnityEngine.RectTransform u_ComRogueOption1RectTransform;
        public UnityEngine.RectTransform u_ComRogueOption2RectTransform;
        public EntityRef<ET.Client.RogueOptionComponent> u_UIRogueOption2;
        public ET.Client.RogueOptionComponent UIRogueOption2 => u_UIRogueOption2;
        public EntityRef<ET.Client.RogueOptionComponent> u_UIRogueOption1;
        public ET.Client.RogueOptionComponent UIRogueOption1 => u_UIRogueOption1;
        public EntityRef<ET.Client.RogueOptionComponent> u_UIRogueOption;
        public ET.Client.RogueOptionComponent UIRogueOption => u_UIRogueOption;

    }
}