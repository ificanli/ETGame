using System;
using System.Collections.Generic;

namespace ET
{
    public struct BTRunTreeEvent
    {
        public BTRoot Root;
        public BTEnv Env;
    }
    
    public class BTHandlerAttribute: BaseAttribute
    {
    }
    
    public interface IBTHandler
    {
        int Handle(BTNode node, BTEnv env);
        Type GetNodeType();
    }
    
    /// <summary>
    /// Actor消息分发组件
    /// </summary>
    [CodeProcess]
    public class BTDispatcher: Singleton<BTDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, IBTHandler> btHandlers = new();

        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (BTHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }
        }
        
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            IBTHandler ibtHandler = obj as IBTHandler;
            if (ibtHandler == null)
            {
                throw new Exception($"bt handler not inherit IMActorHandler abstract class: {obj.GetType().FullName}");
            }

            Type nodeType = ibtHandler.GetNodeType();
            this.btHandlers.Add(nodeType, ibtHandler);
        }
        
        
        public int Handle(BTNode node, BTEnv env)
        {
            if (node == null)
            {
                throw new Exception($"bt node is null");
            }
            if (!this.btHandlers.TryGetValue(node.GetType(), out IBTHandler ibtHandler))
            {
                throw new Exception($"not found bt handler: {node.GetType().FullName}");
            }
            int ret = ibtHandler.Handle(node, env);
            
#if UNITY_EDITOR
            env.AddSnapshot($"Node {node.GetType().Name} {node.Id} return {ret}");
#endif
            return ret;
        }
    }
}