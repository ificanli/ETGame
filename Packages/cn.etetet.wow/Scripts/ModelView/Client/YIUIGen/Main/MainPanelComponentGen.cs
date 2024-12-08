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
    public partial class MainPanelComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "Main";
        public const string ResName = "MainPanel";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIPanelComponent> u_UIPanel;
        public YIUIPanelComponent UIPanel => u_UIPanel;
        public EntityRef<ET.Client.TargetInfoComponent> u_UITargetInfo;
        public ET.Client.TargetInfoComponent UITargetInfo => u_UITargetInfo;
        public EntityRef<ET.Client.PlayerInfoComponent> u_UIPlayerInfo;
        public ET.Client.PlayerInfoComponent UIPlayerInfo => u_UIPlayerInfo;
        public EntityRef<ET.Client.CastSliderComponent> u_UICastFrame;
        public ET.Client.CastSliderComponent UICastFrame => u_UICastFrame;

    }
}