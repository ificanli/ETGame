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
    [EntitySystemOf(typeof(HPViewComponent))]
    public static partial class HPViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HPViewComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this HPViewComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this HPViewComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIView = self.UIBase.GetComponent<YIUIViewComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIView.ViewWindowType = EViewWindowType.View;
            self.UIView.StackOption = EViewStackOption.VisibleTween;

            self.u_ComHPContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComHPContent");
            self.u_DataHPRatio = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueFloat>("u_DataHPRatio");

        }
    }
}
