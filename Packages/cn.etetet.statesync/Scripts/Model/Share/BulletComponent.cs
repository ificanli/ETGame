using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 子弹组件，挂载在子弹Unit上。
    /// 负责子弹的移动、命中检测和伤害计算。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class BulletComponent : Entity, IAwake<long, long, float>, IDestroy
    {
        /// <summary>子弹拥有者ID</summary>
        public long OwnerId;

        /// <summary>目标Unit ID</summary>
        public long TargetId;

        /// <summary>伤害值</summary>
        public float Damage;

        /// <summary>子弹速度（米/秒）</summary>
        public float Speed;

        /// <summary>子弹最大飞行距离（米）</summary>
        public float MaxDistance;

        /// <summary>子弹已飞行距离（米）</summary>
        public float TraveledDistance;
    }
}
