using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Buff))]
    [Module(ModuleName.Spell)]
    public class SpellTargetComponent: Entity, IAwake
    {
        public List<long> Units { get; } = new();
        public float3 Position { get; set; }
    }
}