using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [EntitySystemOf(typeof(HPView3DComponent))]
    public static partial class HPView3DComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HPView3DComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this HPView3DComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this HPView3DComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_ComHPContent = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComHPContent");
            self.u_DataHPRatio = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueFloat>("u_DataHPRatio");
            self.u_DataShow = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueBool>("u_DataShow");

        }
    }
}
