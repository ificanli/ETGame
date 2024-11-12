using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Spell))]
    public class SpellTargetComponent: Entity, IAwake
    {
        public List<EntityRef<Unit>> Units { get; } = new();
        public float3 Position { get; set; }
    }
}