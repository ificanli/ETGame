using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(SpellComponent))]
    public class Spell: Entity, IAwake<int>
    {
        public int ConfigId { get; set; } //配置表id

        public EntityRef<Unit> Caster { get; }

        public EntityRef<Spell> ParentSpell { get; set; }
    }
}