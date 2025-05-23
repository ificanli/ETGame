using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [Module(ModuleName.Spell)]
    [ComponentOf(typeof(Unit))]
    public class BuffComponent: Entity, IAwake, ITransfer, IDeserialize
    {
        [BsonIgnore]
        public MultiMapSet<int, EntityRef<Buff>> flagBuffs = new();

        // key是EffectNode Type
        [BsonIgnore]
        public MultiMapSet<Type, EntityRef<Buff>> effectBuffs = new();
        
        // key是BuffConfig Id
        [BsonIgnore]
        public MultiMapSet<int, EntityRef<Buff>> configIdBuffs = new();
    }
}