using Unity.Mathematics;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class TacticalCastClientComponent : Entity, IAwake, IDestroy
    {
        public bool IsCasting;
        public long ItemId;
        public int ItemConfigId;
        public int TacticalConfigId;
        public TacticalCastMode CastMode;
        public float3 PendingTargetPosition;
    }
}
