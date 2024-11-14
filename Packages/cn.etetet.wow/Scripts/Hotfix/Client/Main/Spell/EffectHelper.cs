using System.Collections.Generic;

namespace ET
{
    public static partial class EffectHelper
    {
        public static void RunSpellEffects(Spell spell, EffectTimeType effectTimeType)
        {
            foreach (EffectConfig effectConfig in spell.Config.Effects)
            {
                if (effectConfig.EffectTimeType != effectTimeType)
                {
                    continue;
                }
                using Effect effect = spell.AddChild<Effect>();
                EffectDispatcher.Instance.Handle(effect, effectConfig);
            }
        }
        
        public static void RunBuffEffects(Buff buff, EffectTimeType effectTimeType)
        {
            foreach (EffectConfig effectConfig in buff.Config.Effects)
            {
                if (effectConfig.EffectTimeType != effectTimeType)
                {
                    continue;
                }
                using Effect effect = buff.AddChild<Effect>();
                EffectDispatcher.Instance.Handle(effect, effectConfig);
            }
        }
    }
}