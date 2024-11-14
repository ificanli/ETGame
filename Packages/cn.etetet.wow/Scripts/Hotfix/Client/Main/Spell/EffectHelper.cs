using System.Collections.Generic;

namespace ET
{
    public static partial class EffectHelper
    {
        public static bool RunSpellEffects(Spell spell, EffectTimeType effectTimeType)
        {
            foreach (EffectConfig effectConfig in spell.Config.Effects)
            {
                if (effectConfig.EffectTimeType != effectTimeType)
                {
                    continue;
                }
                using Effect effect = spell.AddChild<Effect>();
                return BTDispatcher.Instance.Handle(effect, effectConfig.Node);
            }
            return true;
        }
        
        public static bool RunBuffEffects(Buff buff, EffectTimeType effectTimeType)
        {
            foreach (EffectConfig effectConfig in buff.Config.Effects)
            {
                if (effectConfig.EffectTimeType != effectTimeType)
                {
                    continue;
                }
                using Effect effect = buff.AddChild<Effect>();
                return BTDispatcher.Instance.Handle(effect, effectConfig.Node);
            }
            return true;
        }
    }
}