using System;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;
using System.Collections.Generic;

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
            if (loopScroll == null)
            {
                Log.Error("EquipSelectView: 未找到LoopScrollRect组件，请检查预制体配置");
                return;
            }
            self.m_EquipLoop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type, string>(
                loopScroll,
                typeof(EquipSelectItemComponent),
                "u_EventSelect"
            );

            self.PendingItemConfigId = 0;
            self.u_DataGunName?.SetValue(string.Empty, true);
        }

        [EntitySystem]
        private static void Destroy(this EquipSelectViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this EquipSelectViewComponent self)
        {
            self.PendingItemConfigId = 0;
            self.u_DataGunName?.SetValue(string.Empty, true);
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
            item.SetSelected(select);
            item.SetItemIcon(data.Icon);
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
            if (!select)
            {
                return;
            }

            item.SetSelected(true);
            self.UpdatePreview(data, true);
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(EquipSelectViewComponent.OnEventClickPreparedInvoke)]
        private static async ETTask OnEventClickPreparedInvoke(this EquipSelectViewComponent self)
        {
            if (self.PendingItemConfigId <= 0)
            {
                Log.Warning("[EquipSelectView] Prepared ignored: no pending selection");
                await ETTask.CompletedTask;
                return;
            }

            LobbyPanelComponent lobbyPanel = self.LobbyPanel;
            if (lobbyPanel == null || lobbyPanel.IsDisposed)
            {
                Log.Warning("[EquipSelectView] Prepared failed: LobbyPanel missing");
                await ETTask.CompletedTask;
                return;
            }

            lobbyPanel.EquipItem(self.PendingItemConfigId, self.CurrentSlotType);
            self.UIView.Close();
            await ETTask.CompletedTask;
        }

        public static void UpdatePreview(this EquipSelectViewComponent self, ItemConfig itemConfig, bool updatePending)
        {
            if (itemConfig == null)
            {
                self.u_DataGunName?.SetValue(string.Empty);
                if (updatePending)
                {
                    self.PendingItemConfigId = 0;
                }
                return;
            }

            if (updatePending)
            {
                self.PendingItemConfigId = itemConfig.Id;
            }

            string desc = BuildPreviewText(self, itemConfig);
            self.u_DataGunName?.SetValue(desc);
        }

        private static string BuildPreviewText(EquipSelectViewComponent self, ItemConfig itemConfig)
        {
            if (itemConfig == null)
            {
                return string.Empty;
            }

            string desc = null;
            if (self.CurrentSlotType == EquipSlotType.Weapon || self.CurrentSlotType == EquipSlotType.Weapon2)
            {
                WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(itemConfig.Id);
                desc = weaponConfig?.Desc;
            }

            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = itemConfig.Desc;
            }

            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = itemConfig.Name;
            }

            return desc;
        }
        
        [YIUIInvoke(EquipSelectViewComponent.OnEventExitViewInvoke)]
        private static async ETTask OnEventExitViewInvoke(this EquipSelectViewComponent self)
        {
            await self.UIView.CloseAsync();
        }
        #endregion YIUIEvent结束
    }
}
