using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class TargetComponent: Entity, IAwake
    {
        public EntityRef<Unit> Unit     { get; set; }
        public Unit            Target   => Unit;
        public float3          Position { get; set; }
    }
}