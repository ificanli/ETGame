using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(BulletComponent))]
    public static partial class BulletComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BulletComponent self, long ownerId, long targetId, float damage)
        {
            self.OwnerId = ownerId;
            self.TargetId = targetId;
            self.Damage = damage;
            self.WeaponId = 0;
            self.LockType = FireLockType.ForcedTarget; // 默认追踪
            self.Speed = 20f;  // 默认20米/秒
            self.MaxDistance = 100f;  // 默认最大飞行100米
            self.TraveledDistance = 0f;
            self.FlyDirection = float3.zero;
            self.TargetPosition = float3.zero;
        }

        [EntitySystem]
        private static void Destroy(this BulletComponent self)
        {
            self.OwnerId = 0;
            self.TargetId = 0;
            self.Damage = 0;
            self.WeaponId = 0;
        }

        /// <summary>
        /// 更新子弹位置（每帧调用）
        /// </summary>
        public static void Update(this BulletComponent self)
        {
            Unit bullet = self.GetParent<Unit>();
            if (bullet == null || bullet.IsDisposed)
            {
                return;
            }

            float deltaTime = 0.033f;  // 假设30fps
            float moveDistance = self.Speed * deltaTime;

            // 根据锁定类型计算飞行方向
            float3 direction = self.CalculateFlyDirection(bullet);
            if (math.lengthsq(direction) < 0.001f)
            {
                // 方向无效，销毁子弹
                bullet.Dispose();
                return;
            }

            // 移动子弹
            bullet.Position += direction * moveDistance;
            self.TraveledDistance += moveDistance;

            // 检查是否超过最大距离
            if (self.TraveledDistance >= self.MaxDistance)
            {
                bullet.Dispose();
                return;
            }

            // 检查是否命中目标
            self.CheckHit(bullet);
        }

        /// <summary>
        /// 根据锁定类型计算飞行方向
        /// </summary>
        private static float3 CalculateFlyDirection(this BulletComponent self, Unit bullet)
        {
            switch (self.LockType)
            {
                case FireLockType.ForcedTarget:
                    // 强制目标锁定：每帧追踪目标
                    Unit target = bullet.Scene().GetComponent<UnitComponent>()?.Get(self.TargetId);
                    if (target == null || target.IsDisposed)
                    {
                        return float3.zero; // 目标不存在
                    }
                    return math.normalize(target.Position - bullet.Position);

                case FireLockType.Direction:
                    // 方向锁定：沿固定方向飞行
                    return self.FlyDirection;

                case FireLockType.Position:
                    // 位置锁定：朝固定位置飞行
                    return math.normalize(self.TargetPosition - bullet.Position);

                default:
                    return float3.zero;
            }
        }

        /// <summary>
        /// 检查是否命中目标
        /// </summary>
        private static void CheckHit(this BulletComponent self, Unit bullet)
        {
            UnitComponent unitComponent = bullet.Scene().GetComponent<UnitComponent>();
            if (unitComponent == null) return;

            // 遍历所有单位，检查距离
            foreach (Unit unit in unitComponent.Children.Values)
            {
                if (unit.Id == self.OwnerId) continue; // 跳过发射者
                if (unit.Id == bullet.Id) continue; // 跳过子弹自己

                float distance = math.distance(bullet.Position, unit.Position);
                if (distance < 0.5f) // 命中判定距离
                {
                    // 检查是否敌对
                    Unit owner = unitComponent.Get(self.OwnerId);
                    if (owner != null && CampHelper.IsEnemy(owner, unit))
                    {
                        self.OnHit(unit);
                        bullet.Dispose();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 命中目标，造成伤害。
        /// 服务端通过 BulletDamageRequest 事件走 DamageContextHelper 统一管线；
        /// 客户端保持本地预测扣血。
        /// </summary>
        private static void OnHit(this BulletComponent self, Unit target)
        {
            if (target == null || target.IsDisposed)
            {
                return;
            }

            NumericComponent targetNumeric = target.NumericComponent;
            Scene scene = target.Scene();
            long targetUnitId = target.Id;
            long casterUnitId = self.OwnerId;
            int weaponId = self.WeaponId;
            Unit owner = scene?.GetComponent<UnitComponent>()?.Get(casterUnitId);
            int targetUnitType = (int)target.UnitType;
            if (targetNumeric == null || scene == null || scene.IsDisposed)
            {
                return;
            }

            long baseDamage = (long)self.Damage;

            // 发布命中事件（服务端和客户端都能订阅）
            if (!scene.IsDisposed)
            {
                EventSystem.Instance.Publish(scene, new BulletHitEvent
                {
                    CasterUnitId = casterUnitId,
                    TargetUnitId = targetUnitId,
                    WeaponId = weaponId
                });
            }

            // 发布伤害请求事件，服务端走统一 DamageContext 管线
            if (!scene.IsDisposed)
            {
                EventSystem.Instance.Publish(scene, new BulletDamageRequest
                {
                    CasterUnitId = casterUnitId,
                    TargetUnitId = targetUnitId,
                    TargetUnitType = targetUnitType,
                    WeaponId = weaponId,
                    BaseDamage = baseDamage,
                });
            }

            Log.Debug($"子弹命中: 目标={targetUnitId}, 伤害={self.Damage}");
        }
    }
}
