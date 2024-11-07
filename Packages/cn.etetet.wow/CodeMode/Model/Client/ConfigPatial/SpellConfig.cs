namespace ET
{
    public partial class SpellConfig
    {
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

            this.ClientEffects = null;
        }
    }
}