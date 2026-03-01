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
    [FriendOf(typeof(EquipSlotItemComponent))]
    public static partial class EquipSlotItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this EquipSlotItemComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this EquipSlotItemComponent self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
