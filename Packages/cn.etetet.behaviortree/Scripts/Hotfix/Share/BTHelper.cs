using System.Collections.Generic;

namespace ET
{
    public static class BTHelper
    {
        public static int RunTree(BTRoot btRoot, BTEnv env)
        {
            int ret = BTDispatcher.Instance.Handle(btRoot, env);
        
#if UNITY_EDITOR
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = btRoot, Env = env});
#endif
            
            return ret;
        }
    }
}