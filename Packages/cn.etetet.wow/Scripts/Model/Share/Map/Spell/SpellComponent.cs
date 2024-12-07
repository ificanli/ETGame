using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class SpellComponent: Entity, IAwake
    {
        [BsonIgnore]
        public EntityRef<Buff> Current { get; set; }
        
        public long CDTime { get; set; }

        public Dictionary<int, long> SpellCD = new();
    }
}