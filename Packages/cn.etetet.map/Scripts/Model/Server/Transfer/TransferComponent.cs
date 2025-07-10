using Unity.Mathematics;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class TransferComponent: Entity, IAwake, IDestroy
    {
        public float3 FromPos;
    }
}