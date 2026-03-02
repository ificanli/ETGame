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
                return 1;
            }

            int weaponId = node.WeaponType == WeaponType.Rifle ? weaponComp.RifleId : weaponComp.SMGId;
            return weaponComp.CanFire(weaponId) ? 0 : 1;
        }
    }
}
