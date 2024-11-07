using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public partial class SpellConfig
    {
        [BsonIgnore]
        [MemoryPackIgnore]
        public MultiMap<int, int> ServerEffectsMap = new();
        
        [BsonIgnore]
        [MemoryPackIgnore]
        public MultiMap<int, int> ClientEffectsMap = new();
        
        public override void EndInit()
        {
            base.EndInit();

            for (int i = 0; i < this.ClientEffects.Length; i += 2)
            {
                this.ClientEffectsMap.Add(this.ClientEffects[i], this.ClientEffects[i + 1]);
            }
            
            for (int i = 0; i < this.ServerEffects.Length; i += 2)
            {
                this.ServerEffectsMap.Add(this.ServerEffects[i], this.ServerEffects[i + 1]);
            }
            
            this.ClientEffects = null;
            this.ServerEffects = null;
        }
    }
}