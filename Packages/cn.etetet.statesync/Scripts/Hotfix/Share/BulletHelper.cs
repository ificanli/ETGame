using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 子弹工厂辅助类，用于创建子弹实体
    /// </summary>
    public static class BulletHelper
    {
        /// <summary>
        /// 创建单发子弹
        /// </summary>
        public static Unit CreateBullet(Scene scene, Unit owner, Unit target, float damage, FireLockType lockType, int weaponId, int penetrationCount = 0)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return null;
            }

            // 创建子弹Unit
            Unit bullet = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 1);
            bullet.Position = owner.Position;
            bullet.Rotation = quaternion.identity;

            // 添加子弹组件（只传3个参数，LockType 后面手动设置）
            BulletComponent bulletComp = bullet.AddComponent<BulletComponent, long, long, float>(
                owner.Id,
                target.Id,
                damage
            );

            // 设置锁定类型
            bulletComp.LockType = lockType;
            bulletComp.WeaponId = weaponId;
            bulletComp.RemainingPenetrationCount = penetrationCount > 0 ? penetrationCount : 0;

            // 根据锁定类型初始化方向和目标位置
            InitializeBulletDirection(bulletComp, owner, target, lockType);

            HighFrequencySchedulerComponent scheduler = scene.GetComponent<HighFrequencySchedulerComponent>();
            if (scheduler == null)
            {
                Log.Warning($"[HighFreq][Bullet] scheduler missing when create bullet. scene={scene.Name}, bulletId={bullet.Id}");
                return bullet;
            }

            scheduler.AddEntity(HighFrequencyChannelId.Bullet33ms, bulletComp);

            return bullet;
        }

        /// <summary>
        /// 创建散射子弹（霰弹枪）
        /// </summary>
        public static void CreateScatterBullets(
            Scene scene,
            Unit owner,
            Unit target,
            float damage,
            FireLockType lockType,
            int bulletCount,
            float spreadAngle,
            int weaponId,
            int penetrationCount = 0)
        {
            if (bulletCount <= 0) return;

            // 计算基准方向（朝向目标）
            float3 baseDirection = math.normalize(target.Position - owner.Position);

            // 计算散射角度范围
            float halfSpread = spreadAngle * 0.5f * math.PI / 180f; // 转换为弧度

            for (int i = 0; i < bulletCount; i++)
            {
                // 计算每个子弹的偏移角度
                float angleOffset = 0f;
                if (bulletCount > 1)
                {
                    // 均匀分布在散射角度范围内
                    float t = i / (float)(bulletCount - 1); // 0 到 1
                    angleOffset = math.lerp(-halfSpread, halfSpread, t);
                }

                // 创建子弹
                Unit bullet = CreateBullet(scene, owner, target, damage, lockType, weaponId, penetrationCount);
                if (bullet == null) continue;

                // 应用散射角度偏移
                BulletComponent bulletComp = bullet.GetComponent<BulletComponent>();
                if (bulletComp != null)
                {
                    // 绕Y轴旋转基准方向
                    quaternion rotation = quaternion.AxisAngle(math.up(), angleOffset);
                    float3 scatteredDirection = math.mul(rotation, baseDirection);
                    bulletComp.FlyDirection = scatteredDirection;
                }
            }
        }

        /// <summary>
        /// 初始化子弹方向和目标位置
        /// </summary>
        private static void InitializeBulletDirection(BulletComponent bulletComp, Unit owner, Unit target, FireLockType lockType)
        {
            switch (lockType)
            {
                case FireLockType.ForcedTarget:
                    // 强制目标锁定：每帧追踪目标，不需要初始化方向
                    break;

                case FireLockType.Direction:
                    // 方向锁定：锁定发射时的方向
                    bulletComp.FlyDirection = math.normalize(target.Position - owner.Position);
                    break;

                case FireLockType.Position:
                    // 位置锁定：锁定目标当前位置
                    bulletComp.TargetPosition = target.Position;
                    bulletComp.FlyDirection = math.normalize(target.Position - owner.Position);
                    break;
            }
        }
    }
}
