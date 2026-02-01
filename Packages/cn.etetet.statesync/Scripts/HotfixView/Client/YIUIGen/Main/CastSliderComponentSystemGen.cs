using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [EntitySystemOf(typeof(CastSliderComponent))]
    public static partial class CastSliderComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CastSliderComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this CastSliderComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this CastSliderComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_DataProgress = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueFloat>("u_DataProgress");
            self.u_DataIcon = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataIcon");
            self.u_DataName = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataName");

        }
    }
}
