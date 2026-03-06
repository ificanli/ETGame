using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ET.Client
{
    /// <summary>
    /// 武器栏组件系统
    /// </summary>
    [FriendOf(typeof(WeaponBarComponent))]
    public static partial class WeaponBarComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this WeaponBarComponent self)
        {
            // 查找所有WeaponItem子组件并设置SlotIndex
            WeaponItemComponent[] weaponItems = self.UIBase.OwnerGameObject.GetComponentsInChildren<WeaponItemComponent>();

            for (int i = 0; i < weaponItems.Length; i++)
            {
                // 根据顺序设置槽位索引（第一个是槽位1，第二个是槽位2）
                weaponItems[i].SlotIndex = i + 1;
                Log.Info($"[WeaponBar] Set weapon slot {i + 1} index");
            }

            Log.Info($"[WeaponBar] Initialized with {weaponItems.Length} weapon slots");
        }

        [EntitySystem]
        private static void Destroy(this WeaponBarComponent self)
        {
            // Button的onClick会在GameObject销毁时自动清理
            Log.Info("[WeaponBar] Destroyed");
        }

        /// <summary>
        /// 刷新武器栏显示
        /// </summary>
        public static void RefreshWeaponBar(this WeaponBarComponent self, WeaponComponent weaponComp)
        {
            if (weaponComp == null)
            {
                Log.Warning("[WeaponBar] WeaponComponent is null");
                return;
            }

            // 刷新所有武器槽位
            WeaponItemComponent[] weaponItems = self.UIBase.OwnerGameObject.GetComponentsInChildren<WeaponItemComponent>();
            foreach (WeaponItemComponent item in weaponItems)
            {
                item.RefreshSlot(weaponComp);
            }

            Log.Info($"[WeaponBar] Refreshed: currentSlot={weaponComp.CurrentSlot}, slot1={weaponComp.Slot1WeaponId}, slot2={weaponComp.Slot2WeaponId}");
        }
    }
}
