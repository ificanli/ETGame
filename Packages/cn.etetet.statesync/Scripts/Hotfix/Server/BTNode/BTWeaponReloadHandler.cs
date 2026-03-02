namespace ET.Server
{
    public class BTWeaponReloadHandler : ABTHandler<BTWeaponReload>
    {
        protected override int Run(BTWeaponReload node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);

            WeaponComponent weaponComp = caster.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                return 1;
            }

            int weaponId = node.WeaponType == WeaponType.Rifle ? weaponComp.RifleId : weaponComp.SMGId;

            // 如果弹药满了，不需要换弹
            int currentAmmo = weaponComp.GetAmmo(weaponId);
            int maxAmmo = node.WeaponType == WeaponType.Rifle ? 30 : 25;
            if (currentAmmo >= maxAmmo)
            {
                return 1;
            }

            // 设置换弹状态，补充弹药
            weaponComp.SetReloading(weaponId, true);
            weaponComp.RefillAmmo(weaponId);
            weaponComp.SetReloading(weaponId, false);

            return 0;
        }
    }
}
