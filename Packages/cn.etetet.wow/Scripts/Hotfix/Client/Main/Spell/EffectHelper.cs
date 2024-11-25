using System.Collections.Generic;

namespace ET
{
    public static partial class EffectHelper
    {
        public static int RunBT<T>(Spell spell) where T: BTNode
        {
            foreach (BTNode node in spell.Config.Effects)
            {
                if (node is not T)
                {
                    continue;
                }

                using BTEnv env = BTEnv.Create();
                env.AddEntity(BTEvnKey.Spell, spell);
                return BTDispatcher.Instance.Handle(node, env);
            }
            return 0;
        }
        
        public static int RunBT<T>(Buff buff) where T: BTNode
        {
            foreach (BTNode node in buff.Config.Effects)
            {
                if (node is not T)
                {
                    continue;
                }
                using BTEnv env = BTEnv.Create();
                env.AddEntity(BTEvnKey.Buff, buff);
                return BTDispatcher.Instance.Handle(node, env);
            }
            return 0;
        }
    }
}