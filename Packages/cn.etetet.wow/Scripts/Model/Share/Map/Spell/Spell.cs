using System.Collections.Generic;

namespace ET
{
    public enum SpellStatus
    {
        Create = 0,
        Casting = 1,
        Channeling = 2,
        Finished = 3,
    }
    
    public struct WaitSpellChanneling: IWaitType
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
        
        public SpellStatus SpellStatus { get; set; }

        public ETCancellationToken CancellationToken { get; set; }
    }
}