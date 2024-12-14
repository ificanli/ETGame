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
    [FriendOf(typeof(ActionBarComponent))]
    public static partial class ActionBarComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this ActionBarComponent self)
        {
            //临时初始方式 正常肯定是动态拖进来的
            self.UISlot1.RefreshInfo("1", 100000);
            self.UISlot2.RefreshInfo("2", 100010);
            self.UISlot3.RefreshInfo("3", 100020);
            self.UISlot4.RefreshInfo("4", 100030);
            self.UISlot5.RefreshInfo("5", 0);
            self.UISlot6.RefreshInfo("6", 0);
            self.UISlot7.RefreshInfo("7", 0);
            self.UISlot8.RefreshInfo("8", 0);
            self.UISlot9.RefreshInfo("9", 0);
            self.UISlot10.RefreshInfo("0", 100100);
            self.UISlot11.RefreshInfo("-", 100110);
            self.UISlot12.RefreshInfo("=", 0);
        }

        [EntitySystem]
        private static void Destroy(this ActionBarComponent self)
        {
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}