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
            self.Speed = 20f;  // 默认20米/秒
            self.MaxDistance = 100f;  // 默认最大飞行100米
            self.TraveledDistance = 0f;
        }

        [EntitySystem]
        private static void Destroy(this BulletComponent self)
        {
            self.OwnerId = 0;
            self.TargetId = 0;
            self.Damage = 0;
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

            // 获取目标
            Unit target = bullet.Scene().GetComponent<UnitComponent>()?.Get(self.TargetId);
            if (target == null || target.IsDisposed)
            {
                // 目标不存在，销毁子弹
                bullet.Dispose();
                return;
            }

            // 计算移动方向
            float3 direction = math.normalize(target.Position - bullet.Position);
            float deltaTime = 0.033f;  // 假设30fps
            float moveDistance = self.Speed * deltaTime;

            // 移动子弹
            bullet.Position += direction * moveDistance;
            self.TraveledDistance += moveDistance;

            // 检查是否超过最大距离
            if (self.TraveledDistance >= self.MaxDistance)
            {
                bullet.Dispose();
                return;
            }

            // 检查是否命中目标（距离小于0.5米）
            float distanceToTarget = math.distance(bullet.Position, target.Position);
            if (distanceToTarget < 0.5f)
            {
                self.OnHit(target);
                bullet.Dispose();
            }
        }

        /// <summary>
        /// 命中目标，造成伤害
        /// </summary>
        private static void OnHit(this BulletComponent self, Unit target)
        {
            NumericComponent targetNumeric = target.NumericComponent;
            if (targetNumeric == null)
            {
                return;
            }

            // 扣除HP
            float currentHp = targetNumeric.GetAsFloat(NumericType.HP);
            float newHp = math.max(0, currentHp - self.Damage);
            targetNumeric.Set(NumericType.HP, (long)newHp);
        }
    }
}
