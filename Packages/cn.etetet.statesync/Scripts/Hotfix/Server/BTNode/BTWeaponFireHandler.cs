namespace ET.Server
{
    public class BTWeaponFireHandler : ABTHandler<BTWeaponFire>
    {
        protected override int Run(BTWeaponFire node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);

            WeaponComponent weaponComp = caster.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                return 1;
            }

            int weaponId = node.WeaponType == WeaponType.Rifle ? weaponComp.RifleId : weaponComp.SMGId;

            // 最终检查是否可以射击
            if (!weaponComp.CanFire(weaponId))
            {
                return 1;
            }

            // 消耗弹药、记录射击时间
            weaponComp.ConsumeAmmo(weaponId, 1);
            weaponComp.RecordFireTime(weaponId);

            // 创建子弹（使用固定伤害值，后续可从配置读取）
            float damage = node.WeaponType == WeaponType.Rifle ? 30f : 15f;
            BulletHelper.CreateBullet(caster.Scene(), caster, target, damage);

            return 0;
        }
    }
}
