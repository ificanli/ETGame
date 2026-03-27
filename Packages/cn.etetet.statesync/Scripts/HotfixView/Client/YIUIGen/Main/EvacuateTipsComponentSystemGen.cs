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
    [EntitySystemOf(typeof(EvacuateTipsComponent))]
    public static partial class EvacuateTipsComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EvacuateTipsComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this EvacuateTipsComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this EvacuateTipsComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIView = self.UIBase.GetComponent<YIUIViewComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIView.ViewWindowType = EViewWindowType.View;
            self.UIView.StackOption = EViewStackOption.VisibleTween;

            self.u_DataTime = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueInt>("u_DataTime");

        }
    }
}
