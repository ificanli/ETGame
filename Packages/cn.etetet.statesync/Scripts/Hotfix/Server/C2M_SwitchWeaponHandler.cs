namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_SwitchWeaponHandler : MessageLocationHandler<Unit, C2M_SwitchWeapon>
    {
        protected override async ETTask Run(Unit unit, C2M_SwitchWeapon message)
        {
            WeaponComponent weaponComp = unit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                return;
            }

            // 切换武器槽位
            int slotIndex = message.SlotIndex;
            if (slotIndex != 1 && slotIndex != 2)
            {
                Log.Warning($"Invalid slot index: {slotIndex}");
                return;
            }

            // 检查该槽位是否有武器
            int weaponId = slotIndex == 1 ? weaponComp.Slot1WeaponId : weaponComp.Slot2WeaponId;
            if (weaponId == 0)
            {
                Log.Warning($"No weapon in slot {slotIndex}");
                return;
            }

            // 切换武器
            weaponComp.SwitchWeapon(slotIndex);
            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit);

            // 广播武器切换消息
            M2C_SwitchWeapon switchMsg = M2C_SwitchWeapon.Create();
            switchMsg.UnitId = unit.Id;
            switchMsg.SlotIndex = slotIndex;
            switchMsg.WeaponId = weaponId;

            MapMessageHelper.NoticeClient(unit, switchMsg, NoticeType.Broadcast);

            await ETTask.CompletedTask;
        }
    }
}
