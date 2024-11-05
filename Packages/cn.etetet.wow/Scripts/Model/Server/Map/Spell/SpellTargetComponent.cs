using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Spell))]
    public class SpellTargetComponent: Entity, IAwake
    {
        public List<EntityRef<Unit>> Units { get; } = new();
    }
}