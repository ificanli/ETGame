using Unity.Mathematics;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class MoveValidationComponent : Entity, IAwake, IDestroy
    {
        public float3 LastValidPosition;
        public long LastValidateTimeMs;
        public uint LastClientSequence;
        public int ViolationCount;
        public float MaxSpeedTolerance = 1.5f;
        public float NavMeshSampleRadius = 1.0f;
    }
}
