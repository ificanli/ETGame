using System.Collections.Generic;

namespace ET
{
    public static partial class EffectHelper
    {
        public static int RunBT<T>(Spell spell) where T: EffectNode
        {
            EffectNode node = spell.GetConfig().GetEffect<T>();
            if (node == null)
            {
                return 0;
            }
            using BTEnv env = BTEnv.Create();
            env.AddEntity(BTEvnKey.Spell, spell);
            return BTDispatcher.Instance.Handle(node, env);
        }
        
        public static int RunBT<T>(Buff buff) where T: EffectNode
        {
            EffectNode node = buff.GetConfig().GetEffect<T>();
            if (node == null)
            {
                return 0;
            }
            using BTEnv env = BTEnv.Create();
            env.AddEntity(BTEvnKey.Buff, buff);
            return BTDispatcher.Instance.Handle(node, env);
        }
    }
}