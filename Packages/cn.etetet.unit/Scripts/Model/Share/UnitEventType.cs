using Unity.Mathematics;

namespace ET
{
    public struct ChangePosition
    {
        public EntityRef<Unit> Unit;
        public float3 OldPos;
    }

    public struct ChangeRotation
    {
        public EntityRef<Unit> Unit;
    }
}