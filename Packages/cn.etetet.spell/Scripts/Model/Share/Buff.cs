using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [ChildOf(typeof(BuffComponent))]
    public class Buff: Entity, IAwake<int>, IDestroy
    {
        public int ConfigId { get; set; }
        public long Caster { get; set; }
        public long CreateTime { get; set; }
        public int TickTime { get; set; }
        public long ExpireTime { get; set; }
        public int Stack { get; set; }
        public long TimeoutTimer;

        [BsonIgnore]
        public EntityRef<BuffData> BuffData;
    }
}