namespace ET
{
    public static partial class EffectHelper
    {
        public static void RunSpellEffects(Spell spell, EffectTimeType effectTimeType)
        {
            SpellConfig spellConfig = spell.Config;

            foreach (EffectConfig effectConfig in spellConfig.Effects)
            {
                if (effectConfig.EffectTimeType != effectTimeType)
                {
                    continue;
                }
                EffectDispatcher.Instance.Handle(new Effect(spell,  effectTimeType), effectConfig);
            }
        }
        
        public static void RunBuffEffects(Buff buff, EffectTimeType effectTimeType)
        {
            BuffConfig buffConfig = buff.Config;

            foreach (EffectConfig effectConfig in buffConfig.Effects)
            {
                if (effectConfig.EffectTimeType != effectTimeType)
                {
                    continue;
                }
                EffectDispatcher.Instance.Handle(new Effect(buff,  effectTimeType), effectConfig);
            }
        }
    }
}