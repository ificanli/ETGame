using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.17
    /// Desc
    /// </summary>
    [FriendOf(typeof(HPView3DComponent))]
    public static partial class HPView3DComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this HPView3DComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this HPView3DComponent self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
