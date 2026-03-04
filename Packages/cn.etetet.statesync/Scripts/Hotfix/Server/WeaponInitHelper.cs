namespace ET.Server
{
    /// <summary>
    /// 武器初始化辅助类：从 EquipmentComponent 读取武器并初始化 WeaponComponent
    /// </summary>
    public static class WeaponInitHelper
    {
        /// <summary>
        /// Unit 传送到战斗地图后初始化武器（从 Unit 自身的 EquipmentComponent 读取）
        /// </summary>
        public static void InitializeWeaponsFromUnit(Unit unit)
        {
            // 避免重复初始化
            if (unit.GetComponent<WeaponComponent>() != null)
            {
                Log.Info($"WeaponInitHelper: unit {unit.Id} already has WeaponComponent, skip");
                return;
            }

            InitializeWeapons(unit);
        }

        /// <summary>
        /// 从装备组件初始化武器组件
        /// </summary>
        public static void InitializeWeapons(Unit unit)
        {
            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>();
            if (equipComp == null)
            {
                Log.Warning($"WeaponInitHelper: unit {unit.Id} has no EquipmentComponent");
                return;
            }

            // 读取主手和副手武器
            Item mainWeapon = equipComp.GetEquippedItem(EquipmentSlotType.MainHand);
            Item offWeapon = equipComp.GetEquippedItem(EquipmentSlotType.OffHand);

            int slot1WeaponId = mainWeapon?.ConfigId ?? 0;
            int slot2WeaponId = offWeapon?.ConfigId ?? 0;

            Log.Info($"WeaponInitHelper: unit {unit.Id} equipment check - MainHand={slot1WeaponId}, OffHand={slot2WeaponId}");

            // 如果没有武器，不创建 WeaponComponent
            if (slot1WeaponId == 0 && slot2WeaponId == 0)
            {
                Log.Warning($"WeaponInitHelper: unit {unit.Id} has no weapon equipped, skip weapon init");
                return;
            }

            // 创建 WeaponComponent
            WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(slot1WeaponId, slot2WeaponId);

            // 添加索敌组件（BTWeaponHasTarget 需要）
            if (unit.GetComponent<TargetSelectorComponent>() == null)
            {
                TargetSelectorComponent selector = unit.AddComponent<TargetSelectorComponent>();
                int primaryWeaponId = slot1WeaponId > 0 ? slot1WeaponId : slot2WeaponId;
                WeaponConfig weaponCfg = WeaponConfigCategory.Instance.GetOrDefault(primaryWeaponId);
                if (weaponCfg != null)
                {
                    selector.MaxRange = weaponCfg.AttackRange;
                }
            }

            Log.Debug($"WeaponInitHelper: initialized weapons for unit {unit.Id}, Slot1={slot1WeaponId}, Slot2={slot2WeaponId}");

            // 挂上武器BT Buff，让BT持续驱动射击逻辑
            BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), 200200, null);
        }

        /// <summary>
        /// 初始化英雄技能
        /// </summary>
        public static void InitializeHeroSkill(Unit unit, int heroConfigId)
        {
            if (heroConfigId <= 0)
            {
                return;
            }

            // 创建英雄技能组件
            unit.AddComponent<HeroSkillComponent, int>(heroConfigId);

            Log.Debug($"WeaponInitHelper: initialized hero skill for unit {unit.Id}, heroConfigId={heroConfigId}");
        }
    }
}
