using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(WeaponComponent))]
    public static partial class WeaponComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WeaponComponent self, int slot1WeaponId, int slot2WeaponId)
        {
            self.Slot1WeaponId = slot1WeaponId;
            self.Slot2WeaponId = slot2WeaponId;
            self.CurrentSlot = slot1WeaponId > 0 ? 1 : (slot2WeaponId > 0 ? 2 : 0);

            // 初始化弹药（从配置读取）
            if (slot1WeaponId > 0)
            {
                WeaponConfig config1 = WeaponConfigCategory.Instance.Get(slot1WeaponId);
                self.Slot1Ammo = config1?.MagazineSize ?? 0;
                self.SetEffectiveStats(1, config1?.AttackRange ?? 0f, config1?.AttackIntervalMs ?? 0, config1?.MagazineSize ?? 0, config1?.ReloadTimeMs ?? 0, config1?.Damage ?? 0f);
            }
            else
            {
                self.SetEffectiveStats(1, 0f, 0, 0, 0, 0f);
            }

            if (slot2WeaponId > 0)
            {
                WeaponConfig config2 = WeaponConfigCategory.Instance.Get(slot2WeaponId);
                self.Slot2Ammo = config2?.MagazineSize ?? 0;
                self.SetEffectiveStats(2, config2?.AttackRange ?? 0f, config2?.AttackIntervalMs ?? 0, config2?.MagazineSize ?? 0, config2?.ReloadTimeMs ?? 0, config2?.Damage ?? 0f);
            }
            else
            {
                self.SetEffectiveStats(2, 0f, 0, 0, 0, 0f);
            }

            self.Slot1Reloading = false;
            self.Slot2Reloading = false;
            self.Slot1LastFireTime = 0;
            self.Slot2LastFireTime = 0;
            self.Slot1ReloadFinishTime = 0;
            self.Slot2ReloadFinishTime = 0;
            self.ReloadTimerId = 0;
        }

        [EntitySystem]
        private static void Destroy(this WeaponComponent self)
        {
            if (self.ReloadTimerId != 0)
            {
                self.Root()?.TimerComponent?.Remove(ref self.ReloadTimerId);
                self.ReloadTimerId = 0;
            }

            self.Slot1WeaponId = 0;
            self.Slot2WeaponId = 0;
            self.CurrentSlot = 0;
            self.Slot1ReloadFinishTime = 0;
            self.Slot2ReloadFinishTime = 0;
            self.SetEffectiveStats(1, 0f, 0, 0, 0, 0f);
            self.SetEffectiveStats(2, 0f, 0, 0, 0, 0f);
        }

        /// <summary>
        /// 消耗弹药
        /// </summary>
        public static void ConsumeAmmo(this WeaponComponent self, int slotIndex, int count)
        {
            if (slotIndex == 1)
            {
                self.Slot1Ammo = math.max(0, self.Slot1Ammo - count);
            }
            else if (slotIndex == 2)
            {
                self.Slot2Ammo = math.max(0, self.Slot2Ammo - count);
            }
        }

        /// <summary>
        /// 补充弹药（换弹）
        /// </summary>
        public static void RefillAmmo(this WeaponComponent self, int slotIndex)
        {
            int weaponId = slotIndex == 1 ? self.Slot1WeaponId : self.Slot2WeaponId;
            if (weaponId == 0) return;

            int effectiveMagazineSize = self.GetEffectiveMagazineSize(slotIndex);
            if (effectiveMagazineSize <= 0)
            {
                WeaponConfig config = WeaponConfigCategory.Instance.Get(weaponId);
                effectiveMagazineSize = config?.MagazineSize ?? 0;
            }

            if (effectiveMagazineSize <= 0) return;

            if (slotIndex == 1)
            {
                self.Slot1Ammo = effectiveMagazineSize;
            }
            else if (slotIndex == 2)
            {
                self.Slot2Ammo = effectiveMagazineSize;
            }
        }

        /// <summary>
        /// 获取当前弹药数
        /// </summary>
        public static int GetAmmo(this WeaponComponent self, int slotIndex)
        {
            if (slotIndex == 1)
            {
                return self.Slot1Ammo;
            }
            else if (slotIndex == 2)
            {
                return self.Slot2Ammo;
            }
            return 0;
        }

        /// <summary>
        /// 检查是否可以射击
        /// </summary>
        public static bool CanFire(this WeaponComponent self, int slotIndex)
        {
            int weaponId = slotIndex == 1 ? self.Slot1WeaponId : self.Slot2WeaponId;
            if (weaponId == 0) return false;

            // 检查弹药
            if (self.GetAmmo(slotIndex) <= 0)
            {
                return false;
            }

            // 检查是否正在换弹
            if (self.IsReloading(slotIndex))
            {
                return false;
            }

            // 从配置读取射击间隔
            // 检查射击间隔
            long now = TimeInfo.Instance.ServerNow();
            long lastFireTime = slotIndex == 1 ? self.Slot1LastFireTime : self.Slot2LastFireTime;
            int effectiveAttackIntervalMs = self.GetEffectiveAttackIntervalMs(slotIndex);

            if (effectiveAttackIntervalMs <= 0)
            {
                WeaponConfig config = WeaponConfigCategory.Instance.Get(weaponId);
                effectiveAttackIntervalMs = config?.AttackIntervalMs ?? 0;
            }

            if (now - lastFireTime < effectiveAttackIntervalMs)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 记录射击时间
        /// </summary>
        public static void RecordFireTime(this WeaponComponent self, int slotIndex)
        {
            long now = TimeInfo.Instance.ServerNow();
            if (slotIndex == 1)
            {
                self.Slot1LastFireTime = now;
            }
            else if (slotIndex == 2)
            {
                self.Slot2LastFireTime = now;
            }
        }

        /// <summary>
        /// 设置换弹状态
        /// </summary>
        public static void SetReloading(this WeaponComponent self, int slotIndex, bool reloading)
        {
            if (slotIndex == 1)
            {
                self.Slot1Reloading = reloading;
            }
            else if (slotIndex == 2)
            {
                self.Slot2Reloading = reloading;
            }
        }

        /// <summary>
        /// 检查是否正在换弹
        /// </summary>
        public static bool IsReloading(this WeaponComponent self, int slotIndex)
        {
            if (slotIndex == 1)
            {
                return self.Slot1Reloading;
            }
            else if (slotIndex == 2)
            {
                return self.Slot2Reloading;
            }
            return false;
        }

        public static long GetReloadFinishTime(this WeaponComponent self, int slotIndex)
        {
            if (slotIndex == 1)
            {
                return self.Slot1ReloadFinishTime;
            }

            if (slotIndex == 2)
            {
                return self.Slot2ReloadFinishTime;
            }

            return 0;
        }

        public static void SetReloadFinishTime(this WeaponComponent self, int slotIndex, long finishTime)
        {
            if (slotIndex == 1)
            {
                self.Slot1ReloadFinishTime = finishTime;
            }
            else if (slotIndex == 2)
            {
                self.Slot2ReloadFinishTime = finishTime;
            }
        }

        /// <summary>
        /// 切换武器槽位
        /// </summary>
        public static void SwitchWeapon(this WeaponComponent self, int slotIndex)
        {
            if (slotIndex == 1 && self.Slot1WeaponId > 0)
            {
                self.CurrentSlot = 1;
            }
            else if (slotIndex == 2 && self.Slot2WeaponId > 0)
            {
                self.CurrentSlot = 2;
            }
        }

        public static int GetWeaponId(this WeaponComponent self, int slotIndex)
        {
            return slotIndex == 1 ? self.Slot1WeaponId :
                   slotIndex == 2 ? self.Slot2WeaponId : 0;
        }

        public static float GetEffectiveAttackRange(this WeaponComponent self, int slotIndex)
        {
            return slotIndex == 1 ? self.Slot1EffectiveAttackRange :
                   slotIndex == 2 ? self.Slot2EffectiveAttackRange : 0f;
        }

        public static int GetEffectiveAttackIntervalMs(this WeaponComponent self, int slotIndex)
        {
            return slotIndex == 1 ? self.Slot1EffectiveAttackIntervalMs :
                   slotIndex == 2 ? self.Slot2EffectiveAttackIntervalMs : 0;
        }

        public static int GetEffectiveMagazineSize(this WeaponComponent self, int slotIndex)
        {
            return slotIndex == 1 ? self.Slot1EffectiveMagazineSize :
                   slotIndex == 2 ? self.Slot2EffectiveMagazineSize : 0;
        }

        public static int GetEffectiveReloadTimeMs(this WeaponComponent self, int slotIndex)
        {
            return slotIndex == 1 ? self.Slot1EffectiveReloadTimeMs :
                   slotIndex == 2 ? self.Slot2EffectiveReloadTimeMs : 0;
        }

        public static float GetEffectiveDamage(this WeaponComponent self, int slotIndex)
        {
            return slotIndex == 1 ? self.Slot1EffectiveDamage :
                   slotIndex == 2 ? self.Slot2EffectiveDamage : 0f;
        }

        public static void SetEffectiveStats(this WeaponComponent self, int slotIndex, float attackRange, int attackIntervalMs, int magazineSize, int reloadTimeMs, float damage)
        {
            if (slotIndex == 1)
            {
                self.Slot1EffectiveAttackRange = attackRange;
                self.Slot1EffectiveAttackIntervalMs = attackIntervalMs;
                self.Slot1EffectiveMagazineSize = magazineSize;
                self.Slot1EffectiveReloadTimeMs = reloadTimeMs;
                self.Slot1EffectiveDamage = damage;
                return;
            }

            if (slotIndex == 2)
            {
                self.Slot2EffectiveAttackRange = attackRange;
                self.Slot2EffectiveAttackIntervalMs = attackIntervalMs;
                self.Slot2EffectiveMagazineSize = magazineSize;
                self.Slot2EffectiveReloadTimeMs = reloadTimeMs;
                self.Slot2EffectiveDamage = damage;
            }
        }
    }
}
