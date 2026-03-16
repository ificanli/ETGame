using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [FriendOf(typeof(YIUIChild))]
    [FriendOf(typeof(YIUIWindowComponent))]
    [FriendOf(typeof(YIUIViewComponent))]
    [EntitySystemOf(typeof(MatchViewComponent))]
    public static partial class MatchViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MatchViewComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this MatchViewComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this MatchViewComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIView = self.UIBase.GetComponent<YIUIViewComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIView.ViewWindowType = EViewWindowType.Popup;
            self.UIView.StackOption = EViewStackOption.VisibleTween;


        }
    }
}
