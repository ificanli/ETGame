namespace ET.Server
{
    public class BTWeaponCanFireHandler : ABTHandler<BTWeaponCanFire>
    {
        protected override int Run(BTWeaponCanFire node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);

            WeaponComponent weaponComp = caster.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                Log.Warning($"[WeaponFireTrace][CanFire] missing weapon component, caster={caster.Id}, slot={node.SlotIndex}");
                Log.Warning($"BTWeaponCanFire: unit {caster.Id} has no WeaponComponent");
                return 1;
            }

            bool reloadStateChanged = WeaponReloadHelper.TryCompleteReload(caster, node.SlotIndex);
            if (weaponComp.GetAmmo(node.SlotIndex) <= 0 && !weaponComp.IsReloading(node.SlotIndex))
            {
                reloadStateChanged = WeaponReloadHelper.TryStartReload(caster, node.SlotIndex) || reloadStateChanged;
            }

            if (reloadStateChanged)
            {
                WeaponReloadSchedulerHelper.RefreshTimer(caster);
            }

            long checkServerNow = TimeInfo.Instance.ServerNow();
            bool canFire = weaponComp.CanFire(node.SlotIndex);
            int weaponId = node.SlotIndex == 1 ? weaponComp.Slot1WeaponId : weaponComp.Slot2WeaponId;
            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(weaponId);
            JoystickMoveComponent moveComp = caster.GetComponent<JoystickMoveComponent>();
            bool isMoving = moveComp != null && Unity.Mathematics.math.lengthsq(moveComp.Direction) > 0.01f;
            if (canFire && isMoving && !(weaponConfig?.CanMoveWhileFire ?? false))
            {
                canFire = false;
            }
            if (!canFire)
            {
                int ammo = weaponComp.GetAmmo(node.SlotIndex);
                bool reloading = weaponComp.IsReloading(node.SlotIndex);
                Log.Warning(
                    $"[WeaponFireTrace][ServerCanFireBlocked] serverNow={checkServerNow}, caster={caster.Id}, slot={node.SlotIndex}, weaponId={weaponId}, ammo={ammo}, reloading={reloading}, moving={isMoving}, canMoveWhileFire={weaponConfig?.CanMoveWhileFire ?? false}, lastFire={(node.SlotIndex == 1 ? weaponComp.Slot1LastFireTime : weaponComp.Slot2LastFireTime)}");
                Log.Warning($"[WeaponFireTrace][CanFire] blocked, caster={caster.Id}, slot={node.SlotIndex}, weaponId={weaponId}, ammo={ammo}, reloading={reloading}, moving={isMoving}, canMoveWhileFire={weaponConfig?.CanMoveWhileFire ?? false}, lastFire={(node.SlotIndex == 1 ? weaponComp.Slot1LastFireTime : weaponComp.Slot2LastFireTime)}");
                Log.Warning($"BTWeaponCanFire: unit {caster.Id} slot={node.SlotIndex} ammo={ammo} reloading={reloading}");
            }
            else
            {
                Log.Info(
                    $"[WeaponFireTrace][ServerCanFireReady] serverNow={checkServerNow}, caster={caster.Id}, slot={node.SlotIndex}, weaponId={weaponId}, ammo={weaponComp.GetAmmo(node.SlotIndex)}, moving={isMoving}, canMoveWhileFire={weaponConfig?.CanMoveWhileFire ?? false}");
                Log.Info($"[WeaponFireTrace][CanFire] ready, caster={caster.Id}, slot={node.SlotIndex}, weaponId={weaponId}, ammo={weaponComp.GetAmmo(node.SlotIndex)}, moving={isMoving}, canMoveWhileFire={weaponConfig?.CanMoveWhileFire ?? false}");
            }
            return canFire ? 0 : 1;
        }
    }
}
