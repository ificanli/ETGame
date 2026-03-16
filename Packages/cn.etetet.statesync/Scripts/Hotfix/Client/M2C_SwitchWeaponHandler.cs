namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_SwitchWeaponHandler : MessageHandler<Scene, M2C_SwitchWeapon>
    {
        protected override async ETTask Run(Scene root, M2C_SwitchWeapon message)
        {
            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            Unit unit = unitComponent.Get(message.UnitId);
            if (unit == null)
            {
                return;
            }

            // 更新武器组件的当前槽位
            WeaponComponent weaponComp = unit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                int slot1WeaponId = message.SlotIndex == 1 ? message.WeaponId : 0;
                int slot2WeaponId = message.SlotIndex == 2 ? message.WeaponId : 0;
                weaponComp = unit.AddComponent<WeaponComponent, int, int>(slot1WeaponId, slot2WeaponId);
                Log.Info($"[WeaponInitTrace][ClientSwitch] created client WeaponComponent, unitId={message.UnitId}, slot1={slot1WeaponId}, slot2={slot2WeaponId}");
            }
            else
            {
                if (message.SlotIndex == 1 && weaponComp.Slot1WeaponId == 0)
                {
                    weaponComp.Slot1WeaponId = message.WeaponId;
                }
                else if (message.SlotIndex == 2 && weaponComp.Slot2WeaponId == 0)
                {
                    weaponComp.Slot2WeaponId = message.WeaponId;
                }
            }

            if (weaponComp != null)
            {
                weaponComp.SwitchWeapon(message.SlotIndex);
            }

            // 发布武器切换事件，通知UI更新
            EventSystem.Instance.Publish(root, new EventWeaponSwitched
            {
                Scene = root,
                UnitId = message.UnitId,
                SlotIndex = message.SlotIndex,
                WeaponId = message.WeaponId,
            });

            Log.Info($"[WeaponInitTrace][ClientSwitch] unitId={message.UnitId}, slot={message.SlotIndex}, weaponId={message.WeaponId}, currentSlot={weaponComp?.CurrentSlot ?? 0}, slot1={weaponComp?.Slot1WeaponId ?? 0}, slot2={weaponComp?.Slot2WeaponId ?? 0}");

            await ETTask.CompletedTask;
        }
    }
}
