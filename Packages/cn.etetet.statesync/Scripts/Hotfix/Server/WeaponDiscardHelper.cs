namespace ET.Server
{
    public static class WeaponDiscardHelper
    {
        public static async ETTask<int> TryDiscardWeaponAsync(Unit unit, int slotIndex)
        {
            if (unit == null || unit.IsDisposed)
                return ErrorCode.ERR_Cancel;

            if (slotIndex != 1 && slotIndex != 2)
                return ErrorCode.ERR_Cancel;

            WeaponComponent weaponComp = unit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
                return ErrorCode.ERR_Cancel;

            int weaponId = weaponComp.GetWeaponId(slotIndex);
            if (weaponId <= 0)
                return ErrorCode.ERR_Cancel;

            // 换弹中不允许丢弃
            if (weaponComp.IsReloading(slotIndex))
                return ErrorCode.ERR_Cancel;

            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>();
            if (equipComp == null)
                return ErrorCode.ERR_Cancel;

            EquipmentSlotType slotType = slotIndex == 1
                ? EquipmentSlotType.MainHand
                : EquipmentSlotType.OffHand;

            Item weaponItem = equipComp.GetEquippedItem(slotType);
            int configId = weaponItem?.ConfigId ?? weaponId;

            // 从装备槽移除
            if (weaponItem != null && !weaponItem.IsDisposed)
            {
                equipComp.UnEquipItem(slotType);
                weaponItem.Dispose();
            }

            // 生成地面掉落
            bool dropped = PlayerCorpseLootHelper.TryCreateGroundDrop(
                unit,
                configId,
                1,
                out string pointId,
                out long pointUnitId);

            // 清空 WeaponComponent 槽位
            if (slotIndex == 1)
            {
                weaponComp.Slot1WeaponId = 0;
                weaponComp.Slot1Ammo = 0;
                weaponComp.Slot1Reloading = false;
                weaponComp.SetEffectiveStats(1, 0f, 0, 0, 0, 0f, 0);
            }
            else
            {
                weaponComp.Slot2WeaponId = 0;
                weaponComp.Slot2Ammo = 0;
                weaponComp.Slot2Reloading = false;
                weaponComp.SetEffectiveStats(2, 0f, 0, 0, 0, 0f, 0);
            }

            // 如果丢弃的是当前槽，自动切换
            if (weaponComp.CurrentSlot == slotIndex)
            {
                int otherSlot = slotIndex == 1 ? 2 : 1;
                int otherWeaponId = weaponComp.GetWeaponId(otherSlot);
                weaponComp.CurrentSlot = otherWeaponId > 0 ? otherSlot : 0;
            }

            // 重算武器运行时参数
            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit);

            // 广播武器状态变化
            M2C_WeaponDiscarded discardMsg = M2C_WeaponDiscarded.Create();
            discardMsg.UnitId = unit.Id;
            discardMsg.Slot1WeaponId = weaponComp.Slot1WeaponId;
            discardMsg.Slot2WeaponId = weaponComp.Slot2WeaponId;
            discardMsg.CurrentSlot = weaponComp.CurrentSlot;
            MapMessageHelper.NoticeClient(unit, discardMsg, NoticeType.Broadcast);

            Log.Info($"[WeaponDiscard] unit={unit.Id}, slot={slotIndex}, configId={configId}, dropped={dropped}, pointId={pointId ?? "none"}");

            await ETTask.CompletedTask;
            return ErrorCode.ERR_Success;
        }
    }
}
