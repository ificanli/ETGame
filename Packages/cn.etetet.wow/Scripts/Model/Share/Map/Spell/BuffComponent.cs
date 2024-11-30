using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public enum BuffRemoveType
    {
        None = 0,
        Timeout = 1,
        Stack = 2,
    }
    
    [ComponentOf(typeof(Unit))]
    public class BuffComponent: Entity, IAwake, ITransfer, IDeserialize
    {
        [BsonIgnore]
        public MultiMapSet<int, EntityRef<Buff>> flagBuffs = new();

        // key是EffectNode Type
        public MultiMapSet<Type, EntityRef<Buff>> effectBuffs = new();
        
        // key是BuffConfig Id
        public MultiMapSet<int, EntityRef<Buff>> configIdBuffs = new();
    }
}