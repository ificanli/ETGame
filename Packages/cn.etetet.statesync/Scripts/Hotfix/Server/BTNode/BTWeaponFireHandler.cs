using Unity.Mathematics;

namespace ET.Server
{
    public class BTWeaponFireHandler : ABTHandler<BTWeaponFire>
    {
        protected override int Run(BTWeaponFire node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            if (caster == null || target == null)
            {
                return 1;
            }

            WeaponComponent weaponComp = caster.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                return 1;
            }

            // 根据槽位索引获取武器ID
            int weaponId = weaponComp.GetWeaponId(node.SlotIndex);
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

            // 最终检查是否可以射击
            if (!weaponComp.CanFire(node.SlotIndex))
            {
                return 1;
            }

            // 消耗弹药、记录射击时间
            weaponComp.ConsumeAmmo(node.SlotIndex, 1);
            weaponComp.RecordFireTime(node.SlotIndex);
            if (weaponComp.GetAmmo(node.SlotIndex) <= 0)
            {
                WeaponReloadHelper.TryStartReload(caster, node.SlotIndex);
            }
            else
            {
                WeaponReloadHelper.SyncAmmoState(caster);
            }
            Log.Info($"BTWeaponFire: unit {caster.Id} fired slot={node.SlotIndex} weaponId={weaponId} at target={target.Id}");
            Log.Info($"[WeaponFireTrace][Server] fired, caster={caster.Id}, target={target.Id}, slot={node.SlotIndex}, weaponId={weaponId}, bulletCount={weaponConfig.BulletCount}, effect={weaponConfig.ProjectileEffect}");

            Scene scene = caster.Scene();
            if (scene.GetComponent<BulletTickComponent>() == null)
            {
                scene.AddComponent<BulletTickComponent>();
            }

            M2C_WeaponFire fireMsg = M2C_WeaponFire.Create();
            fireMsg.CasterUnitId = caster.Id;
            fireMsg.TargetUnitId = target.Id;
            fireMsg.WeaponId = weaponId;
            fireMsg.SlotIndex = node.SlotIndex;
            fireMsg.BulletCount = weaponConfig.BulletCount;
            fireMsg.FireLockTypeId = weaponConfig.FireLockTypeId;
            MapMessageHelper.NoticeClient(caster, fireMsg, NoticeType.Broadcast);

            // 根据武器配置创建子弹
            FireLockType lockType = (FireLockType)weaponConfig.FireLockTypeId;

            float damage = weaponComp.GetEffectiveDamage(node.SlotIndex);
            int reloadDamageBonusPermille = 0;
            int reloadPenetrationCount = 0;
            RogueReloadFirstShotsStateComponent reloadFirstShotsState = caster.GetComponent<RogueReloadFirstShotsStateComponent>();
            if (reloadFirstShotsState != null)
            {
                reloadFirstShotsState.ConsumeShot(out reloadDamageBonusPermille, out reloadPenetrationCount);
            }

            if (reloadDamageBonusPermille != 0)
            {
                damage = math.max(0f, damage * (1000 + reloadDamageBonusPermille) / 1000f);
            }

            // 如果是散射武器（BulletCount > 1），创建多个子弹
            if (weaponConfig.BulletCount > 1)
            {
                BulletHelper.CreateScatterBullets(
                    scene,
                    caster,
                    target,
                    damage,
                    lockType,
                    weaponConfig.BulletCount,
                    weaponConfig.SpreadAngle,
                    weaponId,
                    reloadPenetrationCount
                );
            }
            else
            {
                // 单发子弹
                BulletHelper.CreateBullet(
                    scene,
                    caster,
                    target,
                    damage,
                    lockType,
                    weaponId,
                    reloadPenetrationCount
                );
            }

            EventSystem.Instance.Publish(scene, new UnitWeaponFired
            {
                Caster = caster,
                Target = target,
                SlotIndex = node.SlotIndex,
                WeaponId = weaponId,
                Damage = damage,
            });

            return 0;
        }
    }
}
