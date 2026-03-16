namespace ET.Server
{
    /// <summary>
    /// 武器同步辅助：向指定玩家同步目标单位当前武器状态。
    /// </summary>
    public static class WeaponSyncHelper
    {
        public static void SendCurrentWeaponStateToViewer(Unit viewer, Unit target)
        {
            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
            {
                return;
            }

            if (target == null || target.IsDisposed)
            {
                return;
            }

            UnitGateInfoComponent gateInfo = viewer.GetComponent<UnitGateInfoComponent>();
            if (gateInfo == null)
            {
                return;
            }

            WeaponComponent weaponComponent = target.GetComponent<WeaponComponent>();
            if (weaponComponent == null || weaponComponent.CurrentWeaponId <= 0 || weaponComponent.CurrentSlot <= 0)
            {
                return;
            }

            M2C_SwitchWeapon message = M2C_SwitchWeapon.Create();
            message.UnitId = target.Id;
            message.SlotIndex = weaponComponent.CurrentSlot;
            message.WeaponId = weaponComponent.CurrentWeaponId;

            MessageSender messageSender = viewer.Root().GetComponent<MessageSender>();
            messageSender.Send(gateInfo.ActorId, message);

            M2C_WeaponAmmoState ammoMessage = M2C_WeaponAmmoState.Create();
            ammoMessage.UnitId = target.Id;
            ammoMessage.Slot1Ammo = weaponComponent.Slot1Ammo;
            ammoMessage.Slot2Ammo = weaponComponent.Slot2Ammo;
            ammoMessage.Slot1Reloading = weaponComponent.Slot1Reloading;
            ammoMessage.Slot2Reloading = weaponComponent.Slot2Reloading;
            ammoMessage.Slot1MagazineSize = weaponComponent.GetEffectiveMagazineSize(1);
            ammoMessage.Slot2MagazineSize = weaponComponent.GetEffectiveMagazineSize(2);
            ammoMessage.Slot1AttackRange = weaponComponent.GetEffectiveAttackRange(1);
            ammoMessage.Slot2AttackRange = weaponComponent.GetEffectiveAttackRange(2);
            messageSender.Send(gateInfo.ActorId, ammoMessage);
            Log.Info($"[WeaponInitTrace][ServerSync] viewerId={viewer.Id}, targetId={target.Id}, slot={message.SlotIndex}, weaponId={message.WeaponId}");
        }
    }
}
