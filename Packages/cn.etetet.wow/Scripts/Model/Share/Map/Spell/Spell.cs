using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(SpellComponent))]
    public class Spell: Entity, IAwake<int>
    {
        public int ConfigId { get; set; } //配置表id

        public long Caster { get; set; }
        
        public long Source { get; set; }

        public long ParentSpell { get; set; }
        
        public long CreateTime { get; set; }
        
        public long ExpireTime { get; set; }
    }
}