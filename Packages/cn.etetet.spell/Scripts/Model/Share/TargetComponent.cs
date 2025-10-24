using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class TargetComponent: Entity, IAwake
    {
        private EntityRef<Unit> unit;

        public Unit Unit
        {
            get
            {
                return this.unit; 
                
            }
            set 
            {
                this.unit = value;
            }
        }
        public float3          Position { get; set; }
    }
}