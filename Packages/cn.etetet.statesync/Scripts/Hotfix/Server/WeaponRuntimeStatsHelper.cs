using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 负责把肉鸽枪械修正折算为武器运行时属性，并同步索敌范围与弹药上限。
    /// </summary>
    public static class WeaponRuntimeStatsHelper
    {
        public static void RefreshUnitWeaponRuntimeStats(Unit unit, bool syncAmmoState = true)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null)
            {
                return;
            }

            RogueWeaponModifierComponent modifierComponent = unit.GetComponent<RogueWeaponModifierComponent>();

            RefreshSlotStats(unit, weaponComponent, modifierComponent, 1);
            RefreshSlotStats(unit, weaponComponent, modifierComponent, 2);
            ClampAmmoToMagazine(weaponComponent, 1);
            ClampAmmoToMagazine(weaponComponent, 2);
            RefreshTargetSelectorRange(unit, weaponComponent);
            RefreshWeaponDrivenVision(unit, weaponComponent);

            if (syncAmmoState)
            {
                WeaponReloadHelper.SyncAmmoState(unit);
            }
        }

        private static void RefreshSlotStats(Unit unit, WeaponComponent weaponComponent, RogueWeaponModifierComponent modifierComponent, int slotIndex)
        {
            int weaponId = weaponComponent.GetWeaponId(slotIndex);
            if (weaponId <= 0)
            {
                weaponComponent.SetEffectiveStats(slotIndex, 0f, 0, 0, 0, 0f, 0);
                return;
            }

            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(weaponId);
            if (weaponConfig == null)
            {
                weaponComponent.SetEffectiveStats(slotIndex, 0f, 0, 0, 0, 0f, 0);
                return;
            }

            int attackRangeModifier = modifierComponent?.GetModifier(slotIndex, WeaponModType.AttackRange) ?? 0;
            int attackSpeedModifier = modifierComponent?.GetModifier(slotIndex, WeaponModType.AttackSpeed) ?? 0;
            int magazineModifier = modifierComponent?.GetModifier(slotIndex, WeaponModType.MagazineCapacity) ?? 0;
            int reloadSpeedModifier = modifierComponent?.GetModifier(slotIndex, WeaponModType.ReloadSpeed) ?? 0;
            int damageModifier = modifierComponent?.GetModifier(slotIndex, WeaponModType.BulletDamage) ?? 0;
            int penetrationCount = modifierComponent?.GetModifier(slotIndex, WeaponModType.Penetration) ?? 0;

            float attackRange = math.max(0f, weaponConfig.AttackRange * (1000 + attackRangeModifier) / 1000f);
            int attackIntervalMs = ApplySpeedModifier(weaponConfig.AttackIntervalMs, attackSpeedModifier);
            int magazineSize = math.max(1, (int)math.round(weaponConfig.MagazineSize * (1000 + magazineModifier) / 1000f));
            int reloadTimeMs = ApplySpeedModifier(weaponConfig.ReloadTimeMs, reloadSpeedModifier);

            // 伤害 = 攻击力 × 伤害系数% × 肉鸽修正
            long attack = unit.NumericComponent?.GetAsLong(NumericType.Attack) ?? 0;
            float baseDamage = attack * weaponConfig.Damage / 100f;
            float damage = math.max(0f, baseDamage * (1000 + damageModifier) / 1000f);

            weaponComponent.SetEffectiveStats(slotIndex, attackRange, attackIntervalMs, magazineSize, reloadTimeMs, damage, penetrationCount);
        }

        private static int ApplySpeedModifier(int baseValue, int speedModifier)
        {
            if (baseValue <= 0)
            {
                return 0;
            }

            int denominator = math.max(1, 1000 + speedModifier);
            return math.max(1, (int)math.round(baseValue * 1000f / denominator));
        }

        private static void ClampAmmoToMagazine(WeaponComponent weaponComponent, int slotIndex)
        {
            int maxMagazineSize = weaponComponent.GetEffectiveMagazineSize(slotIndex);
            if (maxMagazineSize <= 0)
            {
                return;
            }

            if (slotIndex == 1)
            {
                weaponComponent.Slot1Ammo = math.min(weaponComponent.Slot1Ammo, maxMagazineSize);
                return;
            }

            if (slotIndex == 2)
            {
                weaponComponent.Slot2Ammo = math.min(weaponComponent.Slot2Ammo, maxMagazineSize);
            }
        }

        private static void RefreshTargetSelectorRange(Unit unit, WeaponComponent weaponComponent)
        {
            TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>();
            if (selector == null)
            {
                return;
            }

            int currentSlot = weaponComponent.CurrentSlot;
            selector.MaxRange = currentSlot > 0 ? weaponComponent.GetEffectiveAttackRange(currentSlot) : 0f;
        }

        private static void RefreshWeaponDrivenVision(Unit unit, WeaponComponent weaponComponent)
        {
            if (unit == null || unit.IsDisposed || weaponComponent == null || unit.UnitType != UnitType.Player)
            {
                return;
            }

            NumericComponent numeric = unit.NumericComponent;
            if (numeric == null)
            {
                return;
            }

            long applied = weaponComponent.WeaponVisionAoiFinalAdd;
            long desiredApplied = ResolveDesiredWeaponVisionAoiFinalAdd(unit, numeric, applied);
            if (desiredApplied == applied)
            {
                return;
            }

            long currentFinalAdd = numeric.GetAsLong(NumericType.AOIFinalAdd);
            long preservedFinalAdd = currentFinalAdd - applied;
            weaponComponent.WeaponVisionAoiFinalAdd = desiredApplied;
            numeric.Set(NumericType.AOIFinalAdd, preservedFinalAdd + desiredApplied);
        }

        public static void ResetWeaponDrivenVision(Unit unit, WeaponComponent weaponComponent)
        {
            if (unit == null || unit.IsDisposed || weaponComponent == null)
            {
                return;
            }

            long applied = weaponComponent.WeaponVisionAoiFinalAdd;
            if (applied == 0)
            {
                return;
            }

            NumericComponent numeric = unit.NumericComponent;
            if (numeric != null)
            {
                long currentFinalAdd = numeric.GetAsLong(NumericType.AOIFinalAdd);
                numeric.Set(NumericType.AOIFinalAdd, currentFinalAdd - applied);
            }

            weaponComponent.WeaponVisionAoiFinalAdd = 0;
        }

        private static long ResolveDesiredWeaponVisionAoiFinalAdd(Unit unit, NumericComponent numeric, long applied)
        {
            if (!WeaponVisionRangeHelper.TryResolveCurrentSniperVisionRange(unit, out float sniperRange))
            {
                return 0;
            }

            long desiredAoi = math.max(0L, (long)math.round(sniperRange * 1000f));
            long baseValue = numeric.GetAsLong(NumericType.AOIBase);
            long addValue = numeric.GetAsLong(NumericType.AOIAdd);
            int pctValue = math.max(-100, numeric.GetAsInt(NumericType.AOIPct));
            int finalPctValue = math.max(-100, numeric.GetAsInt(NumericType.AOIFinalPct));
            long currentFinalAdd = numeric.GetAsLong(NumericType.AOIFinalAdd);
            long preservedFinalAdd = currentFinalAdd - applied;
            float preFinalValue = (baseValue + addValue) * (100 + pctValue) / 100f + preservedFinalAdd;
            float finalMultiplier = (100 + finalPctValue) / 100f;
            if (finalMultiplier <= 0f)
            {
                return 0;
            }

            return (long)math.round(desiredAoi / finalMultiplier - preFinalValue);
        }
    }
}
