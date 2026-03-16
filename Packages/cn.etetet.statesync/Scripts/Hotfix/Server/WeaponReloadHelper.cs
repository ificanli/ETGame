namespace ET.Server
{
    /// <summary>
    /// 武器换弹辅助逻辑。
    /// </summary>
    public static class WeaponReloadHelper
    {
        public static bool TryStartReload(Unit unit, int slotIndex)
        {
            if (unit == null || unit.IsDisposed)
            {
                return false;
            }

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null)
            {
                return false;
            }

            int weaponId = weaponComponent.GetWeaponId(slotIndex);
            if (weaponId <= 0 || weaponComponent.IsReloading(slotIndex))
            {
                return false;
            }

            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(weaponId);
            int magazineSize = weaponComponent.GetEffectiveMagazineSize(slotIndex);
            int reloadTimeMs = weaponComponent.GetEffectiveReloadTimeMs(slotIndex);
            if (weaponConfig == null || magazineSize <= 0 || reloadTimeMs <= 0)
            {
                return false;
            }

            if (weaponComponent.GetAmmo(slotIndex) >= magazineSize)
            {
                return false;
            }

            long finishTime = TimeInfo.Instance.ServerNow() + reloadTimeMs;
            weaponComponent.SetReloading(slotIndex, true);
            weaponComponent.SetReloadFinishTime(slotIndex, finishTime);
            SyncAmmoState(unit);

            Log.Info($"[WeaponReload] start reload, unit={unit.Id}, slot={slotIndex}, weaponId={weaponId}, reloadMs={reloadTimeMs}, finishTime={finishTime}");
            return true;
        }

        public static bool TryCompleteReload(Unit unit, int slotIndex)
        {
            if (unit == null || unit.IsDisposed)
            {
                return false;
            }

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null || !weaponComponent.IsReloading(slotIndex))
            {
                return false;
            }

            long now = TimeInfo.Instance.ServerNow();
            long finishTime = weaponComponent.GetReloadFinishTime(slotIndex);
            if (finishTime > now)
            {
                return false;
            }

            int weaponId = slotIndex == 1 ? weaponComponent.Slot1WeaponId : weaponComponent.Slot2WeaponId;
            weaponComponent.RefillAmmo(slotIndex);
            weaponComponent.SetReloading(slotIndex, false);
            weaponComponent.SetReloadFinishTime(slotIndex, 0);
            SyncAmmoState(unit);

            Log.Info($"[WeaponReload] finish reload, unit={unit.Id}, slot={slotIndex}, weaponId={weaponId}, ammo={weaponComponent.GetAmmo(slotIndex)}");
            return true;
        }

        public static void SyncAmmoState(Unit unit)
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

            M2C_WeaponAmmoState message = M2C_WeaponAmmoState.Create();
            message.UnitId = unit.Id;
            message.Slot1Ammo = weaponComponent.Slot1Ammo;
            message.Slot2Ammo = weaponComponent.Slot2Ammo;
            message.Slot1Reloading = weaponComponent.Slot1Reloading;
            message.Slot2Reloading = weaponComponent.Slot2Reloading;
            message.Slot1MagazineSize = weaponComponent.GetEffectiveMagazineSize(1);
            message.Slot2MagazineSize = weaponComponent.GetEffectiveMagazineSize(2);
            message.Slot1AttackRange = weaponComponent.GetEffectiveAttackRange(1);
            message.Slot2AttackRange = weaponComponent.GetEffectiveAttackRange(2);

            if (unit.UnitType == UnitType.Player)
            {
                MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
                MapMessageHelper.NoticeClient(unit, message, NoticeType.BroadcastWithoutSelf);
                return;
            }

            MapMessageHelper.NoticeClient(unit, message, NoticeType.Broadcast);
        }
    }
}
