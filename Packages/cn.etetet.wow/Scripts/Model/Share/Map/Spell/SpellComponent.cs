using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class SpellComponent: Entity, IAwake
    {
        public EntityRef<Spell> Current { get; set; }

        public ETCancellationToken CancellationToken { get; set; }
        
        public long CDTime { get; set; }

        public Dictionary<int, long> SpellCD = new();
    }
}