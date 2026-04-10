namespace ET
{
    /// <summary>
    /// 武器组件，挂载在 Unit 上。
    /// 管理两个武器槽位（Slot1/Slot2）的配置ID、弹药、射击间隔、换弹状态。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class WeaponComponent : Entity, IAwake<int, int>, IDestroy
    {
        /// <summary>第1槽武器配置ID（0 表示无武器）</summary>
        public int Slot1WeaponId;

        /// <summary>第2槽武器配置ID（0 表示无武器）</summary>
        public int Slot2WeaponId;

        /// <summary>当前使用的槽位（1 或 2，0 表示无武器）</summary>
        public int CurrentSlot;

        /// <summary>第1槽当前弹药</summary>
        public int Slot1Ammo;

        /// <summary>第2槽当前弹药</summary>
        public int Slot2Ammo;

        /// <summary>第1槽是否正在换弹</summary>
        public bool Slot1Reloading;

        /// <summary>第2槽是否正在换弹</summary>
        public bool Slot2Reloading;

        /// <summary>第1槽上次射击时间（毫秒）</summary>
        public long Slot1LastFireTime;

        /// <summary>第2槽上次射击时间（毫秒）</summary>
        public long Slot2LastFireTime;

        /// <summary>第1槽换弹完成时间（毫秒时间戳）</summary>
        public long Slot1ReloadFinishTime;

        /// <summary>第2槽换弹完成时间（毫秒时间戳）</summary>
        public long Slot2ReloadFinishTime;

        /// <summary>服务端当前挂起的一次性换弹完成定时器 ID</summary>
        public long ReloadTimerId;

        /// <summary>第1槽生效射程（已叠加肉鸽修正）</summary>
        public float Slot1EffectiveAttackRange;

        /// <summary>第2槽生效射程（已叠加肉鸽修正）</summary>
        public float Slot2EffectiveAttackRange;

        /// <summary>第1槽生效攻击间隔（毫秒）</summary>
        public int Slot1EffectiveAttackIntervalMs;

        /// <summary>第2槽生效攻击间隔（毫秒）</summary>
        public int Slot2EffectiveAttackIntervalMs;

        /// <summary>第1槽生效弹匣容量</summary>
        public int Slot1EffectiveMagazineSize;

        /// <summary>第2槽生效弹匣容量</summary>
        public int Slot2EffectiveMagazineSize;

        /// <summary>第1槽生效换弹时间（毫秒）</summary>
        public int Slot1EffectiveReloadTimeMs;

        /// <summary>第2槽生效换弹时间（毫秒）</summary>
        public int Slot2EffectiveReloadTimeMs;

        /// <summary>第1槽生效基础伤害</summary>
        public float Slot1EffectiveDamage;

        /// <summary>第2槽生效基础伤害</summary>
        public float Slot2EffectiveDamage;

        /// <summary>第1槽生效穿透次数</summary>
        public int Slot1EffectivePenetrationCount;

        /// <summary>第2槽生效穿透次数</summary>
        public int Slot2EffectivePenetrationCount;

        // ---- 只读便捷属性 ----

        /// <summary>当前槽的武器ID（0=无武器）</summary>
        public int CurrentWeaponId => CurrentSlot == 1 ? Slot1WeaponId :
                                      CurrentSlot == 2 ? Slot2WeaponId : 0;

        /// <summary>当前槽的弹药数</summary>
        public int CurrentAmmo => CurrentSlot == 1 ? Slot1Ammo :
                                  CurrentSlot == 2 ? Slot2Ammo : 0;

        /// <summary>当前槽是否正在换弹</summary>
        public bool CurrentReloading => CurrentSlot == 1 ? Slot1Reloading :
                                        CurrentSlot == 2 ? Slot2Reloading : false;
    }
}
