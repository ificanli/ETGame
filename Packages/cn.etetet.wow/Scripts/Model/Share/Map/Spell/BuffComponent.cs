using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class BuffComponent: Entity, IAwake, ITransfer, IDeserialize
    {
        [BsonIgnore]
        public MultiMap<int, EntityRef<Buff>> flagBuffs = new();
    }
}