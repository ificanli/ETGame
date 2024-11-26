using System.Collections.Generic;

namespace ET
{
    public static partial class EffectHelper
    {
        public static T GetEffect<T>(this SpellConfig config) where T: EffectNode
        {
            List<EffectNode> effectNodes = config.Effects;
            foreach (EffectNode effectNode in effectNodes)
            {
                if (effectNode is T t)
                {
                    return t;
                }
            }
            return null;
        }
        
        public static T GetEffect<T>(this BuffConfig config) where T: EffectNode
        {
            List<EffectNode> effectNodes = config.Effects;
            foreach (EffectNode effectNode in effectNodes)
            {
                if (effectNode is T t)
                {
                    return t;
                }
            }
            return null;
        }
        
        public static int RunBT<T>(Spell spell) where T: EffectNode
        {
            EffectNode node = GetEffect<T>(spell.GetConfig());
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
            EffectNode node = GetEffect<T>(buff.GetConfig());
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