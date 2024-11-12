using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(SpellComponent))]
    public class Spell: Entity, IAwake<SpellConfig>
    {
        public SpellConfig Config { get; set; } //配置表id

        public EntityRef<Unit> Caster { get; set; }

        public EntityRef<Spell> ParentSpell { get; set; }
    }
}