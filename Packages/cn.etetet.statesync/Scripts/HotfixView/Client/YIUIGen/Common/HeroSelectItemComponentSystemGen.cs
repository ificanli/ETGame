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
    [EntitySystemOf(typeof(HeroSelectItemComponent))]
    public static partial class HeroSelectItemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HeroSelectItemComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this HeroSelectItemComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this HeroSelectItemComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_DataSelect = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueBool>("u_DataSelect");
            self.u_DataHeroName = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataHeroName");
            self.u_EventSelect = self.UIBase.EventTable.FindEvent<UIEventP0>("u_EventSelect");
            self.u_EventSelectHandle = self.u_EventSelect.Add(self,HeroSelectItemComponent.OnEventSelectInvoke);

        }
    }
}
