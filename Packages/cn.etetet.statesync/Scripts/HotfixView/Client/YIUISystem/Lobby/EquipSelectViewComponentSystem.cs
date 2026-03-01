using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.1
    /// Desc
    /// </summary>
    [FriendOf(typeof(EquipSelectViewComponent))]
    [FriendOf(typeof(LobbyPanelComponent))]
    [FriendOf(typeof(EquipSelectItemComponent))]
    public static partial class EquipSelectViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this EquipSelectViewComponent self)
        {
            // 初始化装备列表 LoopScroll
            var loopScroll = self.u_ComEquipSelectLoopScroll.GetComponentInChildren<LoopScrollRect>();
            self.m_EquipLoop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type, string>(
                loopScroll,
                typeof(EquipSelectItemComponent),
                "u_EventSelect"
            );
        }

        [EntitySystem]
        private static void Destroy(this EquipSelectViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this EquipSelectViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        /// <summary>
        /// 装备选择项绑定回调
        /// </summary>
        [EntitySystem]
        private static void YIUILoopRenderer(
            this EquipSelectViewComponent self,
            EquipSelectItemComponent item,
            ItemConfig data,
            int index,
            bool select)
        {
            item.u_DataEquipName.SetValue(data.Name);
            item.u_DataSelect.SetValue(select);
        }

        /// <summary>
        /// 装备选择项点击回调
        /// </summary>
        [EntitySystem]
        private static void YIUILoopOnClick(
            this EquipSelectViewComponent self,
            EquipSelectItemComponent item,
            ItemConfig data,
            int index,
            bool select)
        {
            if (select)
            {
                item.u_DataSelect.SetValue(true);

                // 装备物品
                if (self.LobbyPanel != null)
                {
                    self.LobbyPanel.EquipItem(data.Id, self.CurrentSlotType);
                    self.UIView.Close();
                }
            }
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
