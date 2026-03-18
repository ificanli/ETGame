namespace ET.Server
{
    public static class WeaponInitHelper
    {
        private const int AutoFireBuffConfigId = 200200;

        public static void InitializeWeaponsFromUnit(Unit unit)
        {
            InitializeWeapons(unit);
        }

        public static void InitializeWeapons(Unit unit)
        {
            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>();
            if (equipComp == null)
            {
                Log.Warning($"[WeaponFireTrace][Init] unit {unit.Id} has no EquipmentComponent");
                return;
            }

            Item mainWeapon = equipComp.GetEquippedItem(EquipmentSlotType.MainHand);
            Item offWeapon = equipComp.GetEquippedItem(EquipmentSlotType.OffHand);

            int slot1WeaponId = mainWeapon?.ConfigId ?? 0;
            int slot2WeaponId = offWeapon?.ConfigId ?? 0;

            Log.Info($"[WeaponFireTrace][Init] unit {unit.Id} equipment check - MainHand={slot1WeaponId}, OffHand={slot2WeaponId}");

            ResetWeaponRuntime(unit);

            if (slot1WeaponId == 0 && slot2WeaponId == 0)
            {
                Log.Warning($"[WeaponFireTrace][Init] unit {unit.Id} has no weapon equipped, skip weapon init");
                return;
            }

            if (unit.GetComponent<BuffComponent>() == null)
            {
                unit.AddComponent<BuffComponent>();
            }

            unit.AddComponent<WeaponComponent, int, int>(slot1WeaponId, slot2WeaponId);
            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();

            if (unit.GetComponent<TargetSelectorComponent>() == null)
            {
                unit.AddComponent<TargetSelectorComponent>();
            }

            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit, false);
            Log.Info($"[WeaponFireTrace][Init] unit {unit.Id} selector range set to {unit.GetComponent<TargetSelectorComponent>()?.MaxRange ?? 0f} by current slot {weaponComponent?.CurrentSlot ?? 0}");

            Log.Info($"[WeaponFireTrace][Init] initialized weapons for unit {unit.Id}, Slot1={slot1WeaponId}, Slot2={slot2WeaponId}, CurrentSlot={unit.GetComponent<WeaponComponent>()?.CurrentSlot ?? 0}");
            BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), AutoFireBuffConfigId, null);
            Log.Info($"[WeaponFireTrace][Init] auto fire buff attached, unit={unit.Id}, buffConfigId={AutoFireBuffConfigId}");
            WeaponReloadHelper.SyncAmmoState(unit);
            if (unit.UnitType == UnitType.Player)
            {
                WeaponSyncHelper.SendCurrentWeaponStateToViewer(unit, unit);
            }
        }

        public static void InitializeHeroPassiveBuff(Unit unit, int heroConfigId, bool forceRecreate = false)
        {
            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(heroConfigId);
            int passiveBuffId = heroConfig?.PassiveBuffId ?? 0;

            RemoveOtherHeroPassiveBuffs(unit, passiveBuffId);

            if (passiveBuffId <= 0)
            {
                Log.Info($"[HeroPassive] init skipped: no passive buff, heroConfigId={heroConfigId}, unitId={unit.Id}");
                return;
            }

            if (!BuffConfigCategory.Instance.Contain(passiveBuffId))
            {
                Log.Warning($"[HeroPassive] init skipped: passive buff config not found, heroConfigId={heroConfigId}, passiveBuffId={passiveBuffId}");
                return;
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                Log.Warning($"[HeroPassive] init skipped: BuffComponent missing, unitId={unit.Id}, heroConfigId={heroConfigId}");
                return;
            }

            if (forceRecreate && buffComponent.HasBuff(passiveBuffId))
            {
                BuffHelper.RemoveBuffByConfigId(unit, passiveBuffId, BuffFlags.SameConfigIdReplaceRemove);
            }

            if (buffComponent.HasBuff(passiveBuffId))
            {
                Log.Debug($"[HeroPassive] already active, unitId={unit.Id}, heroConfigId={heroConfigId}, passiveBuffId={passiveBuffId}");
                return;
            }

            Buff buff = BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), passiveBuffId, null);
            if (buff != null)
            {
                BuffData buffData = buff.GetBuffData();
                if (buffData.GetComponent<SpellTargetComponent>() == null)
                {
                    buffData.AddComponent<SpellTargetComponent>();
                }
            }
            Log.Debug($"[HeroPassive] initialized from config, unitId={unit.Id}, heroConfigId={heroConfigId}, passiveBuffId={passiveBuffId}");
        }

        public static void InitializeHeroPassiveBuffFromUnitConfig(Unit unit, bool forceRecreate = false)
        {
            int heroConfigId = GetHeroConfigIdByUnitConfigId(unit.ConfigId);
            InitializeHeroPassiveBuff(unit, heroConfigId, forceRecreate);
        }

        private static void RemoveOtherHeroPassiveBuffs(Unit unit, int keepBuffId)
        {
            foreach (HeroConfig heroConfig in HeroConfigCategory.Instance.DataList)
            {
                int passiveBuffId = heroConfig.PassiveBuffId;
                if (passiveBuffId <= 0 || passiveBuffId == keepBuffId)
                {
                    continue;
                }

                BuffHelper.RemoveBuffByConfigId(unit, passiveBuffId, BuffFlags.SameConfigIdReplaceRemove);
            }
        }

        public static int GetHeroConfigIdByUnitConfigId(int unitConfigId)
        {
            foreach (HeroConfig heroConfig in HeroConfigCategory.Instance.DataList)
            {
                if (heroConfig.UnitConfigId == unitConfigId)
                {
                    return heroConfig.Id;
                }
            }

            return 0;
        }

        private static void ResetWeaponRuntime(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            if (unit.GetComponent<WeaponComponent>() != null)
            {
                unit.RemoveComponent<WeaponComponent>();
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent != null && buffComponent.HasBuff(AutoFireBuffConfigId))
            {
                BuffHelper.RemoveBuffByConfigId(unit, AutoFireBuffConfigId, BuffFlags.SameConfigIdReplaceRemove);
            }

            TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>();
            if (selector != null)
            {
                selector.MaxRange = 0f;
            }
        }
    }
}
