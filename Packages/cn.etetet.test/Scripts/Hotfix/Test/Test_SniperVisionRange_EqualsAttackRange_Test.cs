using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Test_SniperVisionRange_EqualsAttackRange_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_SniperVisionRange_EqualsAttackRange_Test));
            Scene scene = scope.TestFiber.Root;
            if (scene.TimerComponent == null)
            {
                scene.AddComponent<TimerComponent>();
            }

            Unit unit = TestHelper.CreateServerUnit(scene, UnitType.Player, addBuffComponent: true, campId: 1);
            if (unit == null)
            {
                Log.Console("server unit is null");
                return 1;
            }

            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>() ?? unit.AddComponent<EquipmentComponent>();
            if (equipComp == null)
            {
                Log.Console("equipment component is null");
                return 2;
            }

            long baselineAoi = unit.NumericComponent?.GetAsLong(NumericType.AOI) ?? 0L;
            if (baselineAoi <= 0L)
            {
                Log.Console($"baseline AOI invalid: {baselineAoi}");
                return 3;
            }

            int nonSniperWeaponId = 0;
            int sniperWeaponId = 0;
            float sniperAttackRange = float.MaxValue;
            foreach (WeaponConfig weaponConfig in WeaponConfigCategory.Instance.DataList)
            {
                if (weaponConfig == null)
                {
                    continue;
                }

                if (weaponConfig.WeaponTypeId == (int)WeaponType.SniperRifle)
                {
                    long rawAttackRange = (long)math.round(weaponConfig.AttackRange * 1000f);
                    if (rawAttackRange < baselineAoi && weaponConfig.AttackRange < sniperAttackRange)
                    {
                        sniperWeaponId = weaponConfig.Id;
                        sniperAttackRange = weaponConfig.AttackRange;
                    }

                    continue;
                }

                if (nonSniperWeaponId == 0)
                {
                    nonSniperWeaponId = weaponConfig.Id;
                }
            }

            if (nonSniperWeaponId <= 0 || sniperWeaponId <= 0)
            {
                Log.Console($"weapon config invalid: nonSniper={nonSniperWeaponId}, sniper={sniperWeaponId}");
                return 4;
            }

            LoadoutHelper.ApplyLoadout(unit, nonSniperWeaponId, sniperWeaponId, 0);
            WeaponInitHelper.InitializeWeaponsFromUnit(unit);

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>();
            if (weaponComponent == null || selector == null)
            {
                Log.Console("weapon or selector component missing after init");
                return 5;
            }

            if ((unit.NumericComponent?.GetAsLong(NumericType.AOI) ?? 0L) != baselineAoi)
            {
                Log.Console($"non-sniper AOI should keep baseline: baseline={baselineAoi}, actual={unit.NumericComponent?.GetAsLong(NumericType.AOI) ?? 0L}");
                return 6;
            }

            float expectedSlot1Range = weaponComponent.GetEffectiveAttackRange(1);
            if (math.abs(selector.MaxRange - expectedSlot1Range) > 0.001f)
            {
                Log.Console($"slot1 selector range mismatch: expected={expectedSlot1Range}, actual={selector.MaxRange}");
                return 7;
            }

            weaponComponent.SwitchWeapon(2);
            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit, false);

            long expectedSniperAoi = (long)math.round(weaponComponent.GetEffectiveAttackRange(2) * 1000f);
            long actualSniperAoi = unit.NumericComponent?.GetAsLong(NumericType.AOI) ?? 0L;
            if (actualSniperAoi != expectedSniperAoi)
            {
                Log.Console($"sniper AOI mismatch: expected={expectedSniperAoi}, actual={actualSniperAoi}");
                return 8;
            }

            if (math.abs(selector.MaxRange - weaponComponent.GetEffectiveAttackRange(2)) > 0.001f)
            {
                Log.Console($"slot2 selector range mismatch: expected={weaponComponent.GetEffectiveAttackRange(2)}, actual={selector.MaxRange}");
                return 9;
            }

            weaponComponent.SwitchWeapon(1);
            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit, false);

            long actualBaselineAoi = unit.NumericComponent?.GetAsLong(NumericType.AOI) ?? 0L;
            if (actualBaselineAoi != baselineAoi)
            {
                Log.Console($"AOI should restore after switch back: baseline={baselineAoi}, actual={actualBaselineAoi}");
                return 10;
            }

            LoadoutHelper.ApplyLoadout(unit, 0, 0, 0);
            WeaponInitHelper.InitializeWeaponsFromUnit(unit);

            if (unit.GetComponent<WeaponComponent>() != null)
            {
                Log.Console("weapon component should be removed after clear loadout");
                return 11;
            }

            long clearedAoi = unit.NumericComponent?.GetAsLong(NumericType.AOI) ?? 0L;
            if (clearedAoi != baselineAoi)
            {
                Log.Console($"AOI should restore after clear loadout: baseline={baselineAoi}, actual={clearedAoi}");
                return 12;
            }

            Log.Console("Test_SniperVisionRange_EqualsAttackRange_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
