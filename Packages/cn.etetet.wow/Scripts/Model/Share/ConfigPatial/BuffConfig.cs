using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public partial class BuffConfig
    {
        [BsonIgnore]
        [MemoryPackIgnore]
        public MultiMap<int, int> EffectsMap = new();
        
        public override void EndInit()
        {
            base.EndInit();

            for (int i = 0; i < this.Effects.Length; i += 2)
            {
                this.EffectsMap.Add(this.Effects[i], this.Effects[i + 1]);
            }
        }
    }
}