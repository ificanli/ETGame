using System.Collections.Generic;

namespace ET
{
    public static class BTHelper
    {
        public static int RunTree(BTRoot btRoot, BTEnv env)
        {
            env.SetTreeId(btRoot.TreeId);
            int ret = BTDispatcher.Instance.Handle(btRoot, env);
        
#if UNITY_EDITOR
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = btRoot, Env = env});
#endif
            
            return ret;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void PublishRunPath(BTEnv env)
        {
#if UNITY_EDITOR
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env});
#endif
        }
    }
}
