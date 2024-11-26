using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class SpellComponent: Entity, IAwake
    {
        [BsonIgnore]
        public EntityRef<Spell> Current { get; set; }

        public ETCancellationToken CancellationToken { get; set; }
        
        public long CDTime { get; set; }

        public Dictionary<int, long> SpellCD = new();
        
        [BsonIgnore]
        public MultiMapSet<int, EntityRef<Spell>> flagSpells = new();
    }
}