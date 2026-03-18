using ET.Server;

namespace ET.Test
{
    public class Test_Weapon_LoadoutRuntimeReset_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Weapon_LoadoutRuntimeReset_Test));
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

            int firstWeaponId = 0;
            int secondWeaponId = 0;
            foreach (WeaponConfig weaponConfig in WeaponConfigCategory.Instance.DataList)
            {
                if (weaponConfig == null)
                {
                    continue;
                }

                if (firstWeaponId == 0)
                {
                    firstWeaponId = weaponConfig.Id;
                    continue;
                }

                if (weaponConfig.Id != firstWeaponId)
                {
                    secondWeaponId = weaponConfig.Id;
                    break;
                }
            }

            if (firstWeaponId <= 0 || secondWeaponId <= 0)
            {
                Log.Console($"weapon configs invalid: first={firstWeaponId}, second={secondWeaponId}");
                return 3;
            }

            LoadoutHelper.ApplyLoadout(unit, firstWeaponId, 0, 0);
            if (equipComp.GetEquippedItem(EquipmentSlotType.MainHand)?.ConfigId != firstWeaponId)
            {
                Log.Console($"main hand config mismatch after first apply: {equipComp.GetEquippedItem(EquipmentSlotType.MainHand)?.ConfigId ?? 0}");
                return 4;
            }

            WeaponInitHelper.InitializeWeaponsFromUnit(unit);
            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null || weaponComponent.Slot1WeaponId != firstWeaponId || weaponComponent.CurrentWeaponId != firstWeaponId)
            {
                Log.Console($"weapon runtime mismatch after first init: slot1={weaponComponent?.Slot1WeaponId ?? 0}, current={weaponComponent?.CurrentWeaponId ?? 0}");
                return 5;
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null || !buffComponent.HasBuff(200200))
            {
                Log.Console("auto fire buff missing after first init");
                return 6;
            }

            LoadoutHelper.ApplyLoadout(unit, secondWeaponId, 0, 0);
            if (equipComp.GetEquippedItem(EquipmentSlotType.MainHand)?.ConfigId != secondWeaponId)
            {
                Log.Console($"main hand config mismatch after replace apply: {equipComp.GetEquippedItem(EquipmentSlotType.MainHand)?.ConfigId ?? 0}");
                return 7;
            }

            WeaponInitHelper.InitializeWeaponsFromUnit(unit);
            weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null || weaponComponent.Slot1WeaponId != secondWeaponId || weaponComponent.CurrentWeaponId != secondWeaponId)
            {
                Log.Console($"weapon runtime mismatch after replace init: slot1={weaponComponent?.Slot1WeaponId ?? 0}, current={weaponComponent?.CurrentWeaponId ?? 0}");
                return 8;
            }

            LoadoutHelper.ApplyLoadout(unit, 0, 0, 0);
            if (equipComp.HasEquippedItem(EquipmentSlotType.MainHand))
            {
                Log.Console("main hand should be empty after clear apply");
                return 9;
            }

            WeaponInitHelper.InitializeWeaponsFromUnit(unit);
            if (unit.GetComponent<WeaponComponent>() != null)
            {
                Log.Console("weapon component should be removed when no weapon is equipped");
                return 10;
            }

            if (buffComponent.HasBuff(200200))
            {
                Log.Console("auto fire buff should be removed when no weapon is equipped");
                return 11;
            }

            TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>();
            if (selector != null && selector.MaxRange > 0.001f)
            {
                Log.Console($"target selector range should reset to 0, actual={selector.MaxRange}");
                return 12;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
