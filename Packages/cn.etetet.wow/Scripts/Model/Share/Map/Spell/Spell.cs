using System.Collections.Generic;

namespace ET
{
    public struct CastTimeBuffTimeout: IWaitType
    {
        public int Error { get; set; }
    }
    
    [ChildOf(typeof(SpellComponent))]
    public class Spell: Entity, IAwake<int>
    {
        public int ConfigId { get; set; } //配置表id

        public long Caster { get; set; }
        
        public long Source { get; set; }
        
        public long CreateTime { get; set; }
        
        public long ExpireTime { get; set; }
    }
}