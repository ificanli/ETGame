namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_SwitchWeaponHandler : MessageLocationHandler<Unit, C2M_SwitchWeapon>
    {
        protected override async ETTask Run(Unit unit, C2M_SwitchWeapon message)
        {
            long recvServerNow = TimeInfo.Instance.ServerNow();
            int slotIndex = message.SlotIndex;
            WeaponComponent weaponComp = unit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                Log.Warning(
                    $"[WeaponSwitchTrace][ServerRecv] serverNow={recvServerNow}, unitId={unit.Id}, requestedSlot={slotIndex}, result=weapon_component_missing");
                return;
            }

            Log.Info(
                $"[WeaponSwitchTrace][ServerRecv] serverNow={recvServerNow}, unitId={unit.Id}, requestedSlot={slotIndex}, currentSlot={weaponComp.CurrentSlot}, slot1WeaponId={weaponComp.Slot1WeaponId}, slot2WeaponId={weaponComp.Slot2WeaponId}, slot1Ammo={weaponComp.Slot1Ammo}, slot2Ammo={weaponComp.Slot2Ammo}");

            // 切换武器槽位
            if (slotIndex != 1 && slotIndex != 2)
            {
                Log.Warning(
                    $"[WeaponSwitchTrace][ServerRecv] serverNow={TimeInfo.Instance.ServerNow()}, unitId={unit.Id}, requestedSlot={slotIndex}, result=invalid_slot");
                Log.Warning($"Invalid slot index: {slotIndex}");
                return;
            }

            // 检查该槽位是否有武器
            int weaponId = slotIndex == 1 ? weaponComp.Slot1WeaponId : weaponComp.Slot2WeaponId;
            if (weaponId == 0)
            {
                Log.Warning(
                    $"[WeaponSwitchTrace][ServerRecv] serverNow={TimeInfo.Instance.ServerNow()}, unitId={unit.Id}, requestedSlot={slotIndex}, result=slot_empty");
                Log.Warning($"No weapon in slot {slotIndex}");
                return;
            }

            // 切换武器
            weaponComp.SwitchWeapon(slotIndex);
            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit);
            long afterSwitchServerNow = TimeInfo.Instance.ServerNow();

            // 广播武器切换消息
            M2C_SwitchWeapon switchMsg = M2C_SwitchWeapon.Create();
            switchMsg.UnitId = unit.Id;
            switchMsg.SlotIndex = slotIndex;
            switchMsg.WeaponId = weaponId;

            MapMessageHelper.NoticeClient(unit, switchMsg, NoticeType.Broadcast);
            Log.Info(
                $"[WeaponSwitchTrace][ServerBroadcast] serverNow={afterSwitchServerNow}, unitId={unit.Id}, slot={slotIndex}, weaponId={weaponId}, currentSlot={weaponComp.CurrentSlot}, slot1Ammo={weaponComp.Slot1Ammo}, slot2Ammo={weaponComp.Slot2Ammo}, handlerCostMs={afterSwitchServerNow - recvServerNow}");

            await ETTask.CompletedTask;
        }
    }
}
