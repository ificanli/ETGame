using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.14
    /// Desc
    /// </summary>
    public static partial class ActionBarComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this ActionBarComponent self)
        {
            //临时初始方式 正常肯定是动态拖进来的
            self.UISlot12.RefreshInfo("1", 100000);
        }

        [EntitySystem]
        private static void Destroy(this ActionBarComponent self)
        {
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}