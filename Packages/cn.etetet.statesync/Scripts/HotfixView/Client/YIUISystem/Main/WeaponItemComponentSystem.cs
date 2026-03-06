using System;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 武器槽位组件系统
    /// </summary>
    [FriendOf(typeof(WeaponItemComponent))]
    public static partial class WeaponItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this WeaponItemComponent self)
        {
            // SlotIndex会在WeaponBar初始化时设置
            Log.Info($"[WeaponItem] Slot {self.SlotIndex} initialized");
        }

        [EntitySystem]
        private static void Destroy(this WeaponItemComponent self)
        {
            // 清理事件
            if (self.u_EventClick != null && self.u_EventClickHandle != null)
            {
                self.u_EventClick.Remove(self.u_EventClickHandle);
            }
            self.u_EventClickHandle = null;
            self.u_EventClick = null;

            Log.Info($"[WeaponItem] Slot {self.SlotIndex} destroyed");
        }

        #region YIUIEvent开始

        /// <summary>
        /// 武器槽位点击事件
        /// </summary>
        [YIUIInvoke(WeaponItemComponent.OnEventClickInvoke)]
        private static async ETTask OnEventClickInvoke(this WeaponItemComponent self)
        {
            Scene root = self.Root();
            Log.Info($"[WeaponItem] Slot {self.SlotIndex} clicked");

            // 发送切换武器请求
            WeaponSwitchHelper.SwitchWeapon(root, self.SlotIndex);

            await ETTask.CompletedTask;
        }

        #endregion YIUIEvent结束

        /// <summary>
        /// 刷新武器槽位显示
        /// </summary>
        public static void RefreshSlot(this WeaponItemComponent self, WeaponComponent weaponComp)
        {
            if (weaponComp == null) return;

            int weaponId = self.SlotIndex == 1 ? weaponComp.Slot1WeaponId : weaponComp.Slot2WeaponId;
            int ammo = self.SlotIndex == 1 ? weaponComp.Slot1Ammo : weaponComp.Slot2Ammo;
            bool isReloading = self.SlotIndex == 1 ? weaponComp.Slot1Reloading : weaponComp.Slot2Reloading;
            bool isCurrent = weaponComp.CurrentSlot == self.SlotIndex;

            // TODO: 更新UI显示
            // - 武器图标
            // - 弹药数量
            // - 是否正在换弹
            // - 是否是当前武器（高亮显示）

            Log.Info($"[WeaponItem] Slot {self.SlotIndex} refreshed: weaponId={weaponId}, ammo={ammo}, reloading={isReloading}, current={isCurrent}");
        }
    }
}
