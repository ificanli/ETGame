using System.Collections.Generic;

namespace ET
{
    [Module(ModuleName.Spell)]
    public static partial class EffectHelper
    {
        public static int RunBT<T>(Buff buff) where T: EffectNode
        {
            EffectNode node = buff.GetConfig().GetEffect<T>();
            if (node == null)
            {
                return 0;
            }
            using BTEnv env = BTEnv.Create(buff.Scene());
            env.AddEntity(BTEvnKey.Buff, buff);
            env.AddEntity(BTEvnKey.Unit, buff.Parent.GetParent<Unit>());
            env.AddEntity(BTEvnKey.Caster, buff.GetCaster());
            return BTDispatcher.Instance.Handle(node, env);
        }
    }
}