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
                Log.Warning($"BTWeaponCanFire: unit {caster.Id} has no WeaponComponent");
                return 1;
            }

            bool canFire = weaponComp.CanFire(node.SlotIndex);
            if (!canFire)
            {
                int ammo = weaponComp.GetAmmo(node.SlotIndex);
                bool reloading = weaponComp.IsReloading(node.SlotIndex);
                Log.Warning($"BTWeaponCanFire: unit {caster.Id} slot={node.SlotIndex} ammo={ammo} reloading={reloading}");
            }
            return canFire ? 0 : 1;
        }
    }
}
