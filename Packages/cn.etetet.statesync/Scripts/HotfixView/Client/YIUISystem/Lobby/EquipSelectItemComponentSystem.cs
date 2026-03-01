using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.1
    /// Desc
    /// </summary>
    [FriendOf(typeof(EquipSelectItemComponent))]
    public static partial class EquipSelectItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this EquipSelectItemComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this EquipSelectItemComponent self)
        {
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(EquipSelectItemComponent.OnEventSelectInvoke)]
        private static void OnEventSelectInvoke(this EquipSelectItemComponent self)
        {

        }
        #endregion YIUIEvent结束
    }
}
