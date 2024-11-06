namespace ET
{
    public partial class SpellConfig
    {
        public MultiMap<int, int> ServerEffectsMap { get; } = new();

        public override void EndInit()
        {
            base.EndInit();

            for (int i = 0; i < this.ServerEffects.Length; i += 2)
            {
                this.ServerEffectsMap.Add(this.ServerEffects[i], this.ServerEffects[i + 1]);
            }
            
            this.ServerEffects = null;
        }
    }
}

