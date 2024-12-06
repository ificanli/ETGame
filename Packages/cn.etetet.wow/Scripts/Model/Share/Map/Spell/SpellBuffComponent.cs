using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Spell))]
    public class SpellBuffComponent: Entity, IAwake, IDestroy
    {
        public List<long> Buffs { get; } = new();
    }
}