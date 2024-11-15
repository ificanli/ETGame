using System.Collections.Generic;

namespace ET
{
    public static partial class EffectHelper
    {
        public static bool RunSpellEffects(Spell spell, BTTimeType btTimeType)
        {
            foreach (EffectConfig effectConfig in spell.Config.Effects)
            {
                if (effectConfig.btTimeType != btTimeType)
                {
                    continue;
                }

                using BTEnv env = BTEnv.Create(btTimeType);
                env.Add(BTEvnKey.Spell, spell);
                return BTDispatcher.Instance.Handle(effectConfig.Node, env);
            }
            return true;
        }
        
        public static bool RunBuffEffects(Buff buff, BTTimeType btTimeType)
        {
            foreach (EffectConfig effectConfig in buff.Config.Effects)
            {
                if (effectConfig.btTimeType != btTimeType)
                {
                    continue;
                }
                using BTEnv env = BTEnv.Create(btTimeType);
                env.Add(BTEvnKey.Buff, buff);
                return BTDispatcher.Instance.Handle(effectConfig.Node, env);
            }
            return true;
        }
    }
}