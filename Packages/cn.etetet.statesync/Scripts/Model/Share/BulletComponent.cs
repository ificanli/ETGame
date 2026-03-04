using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 子弹组件，挂载在子弹 Unit 上。
    /// 支持三种锁定类型：ForcedTarget（追踪）、Direction（方向直线）、Position（位置直线）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class BulletComponent : Entity, IAwake<long, long, float>, IDestroy
    {
        /// <summary>子弹拥有者 ID</summary>
        public long OwnerId;

        /// <summary>目标 Unit ID（ForcedTarget 追踪用）</summary>
        public long TargetId;

        /// <summary>伤害值</summary>
        public float Damage;

        /// <summary>武器配置ID（用于命中表现）</summary>
        public int WeaponId;

        /// <summary>子弹速度（米/秒）</summary>
        public float Speed;

        /// <summary>子弹最大飞行距离（米）</summary>
        public float MaxDistance;

        /// <summary>已飞行距离（米）</summary>
        public float TraveledDistance;

        /// <summary>射击锁定类型</summary>
        public FireLockType LockType;

        /// <summary>
        /// 飞行方向（Direction/Position 类型使用）。
        /// ForcedTarget 类型每帧重算，此字段无效。
        /// </summary>
        public float3 FlyDirection;

        /// <summary>目标位置（Position 类型使用，锁定发射时的目标坐标）</summary>
        public float3 TargetPosition;
    }
}
