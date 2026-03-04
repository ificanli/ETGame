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

            // 根据槽位索引获取武器ID
            int weaponId = node.SlotIndex == 1 ? weaponComp.Slot1WeaponId : weaponComp.Slot2WeaponId;
            if (weaponId == 0)
            {
                return 1; // 该槽位没有武器
            }

            // 获取武器配置
            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.Get(weaponId);
            if (weaponConfig == null)
            {
                return 1;
            }

            // 如果弹药满了，不需要换弹
            int currentAmmo = weaponComp.GetAmmo(node.SlotIndex);
            if (currentAmmo >= weaponConfig.MagazineSize)
            {
                return 1;
            }

            // 设置换弹状态，补充弹药
            weaponComp.SetReloading(node.SlotIndex, true);
            weaponComp.RefillAmmo(node.SlotIndex);
            weaponComp.SetReloading(node.SlotIndex, false);

            return 0;
        }
    }
}
