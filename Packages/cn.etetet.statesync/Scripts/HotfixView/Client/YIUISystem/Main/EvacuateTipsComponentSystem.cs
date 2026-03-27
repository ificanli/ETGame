using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.23
    /// Desc
    /// </summary>
    [FriendOf(typeof(EvacuateTipsComponent))]
    public static partial class EvacuateTipsComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this EvacuateTipsComponent self)
        {
            self.u_DataTime?.SetValue(0, true);
        }

        [EntitySystem]
        private static void Destroy(this EvacuateTipsComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this EvacuateTipsComponent self)
        {
            self.u_DataTime?.SetValue(0, true);
            await ETTask.CompletedTask;
            return true;
        }

        public static void SetRemainSeconds(this EvacuateTipsComponent self, int remainSeconds)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.u_DataTime?.SetValue(Math.Max(remainSeconds, 0));
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
