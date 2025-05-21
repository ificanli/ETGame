using System;
using System.Collections.Generic;

namespace ET
{
    public class BTHandlerAttribute: BaseAttribute
    {
    }
    
    public interface IBTHandler
    {
        int Handle(BTNode node, BTEnv env);
        Type GetNodeType();
    }
    
    [BTHandler]
    public abstract class ABTHandler<Node>: HandlerObject, IBTHandler where Node : BTNode
    {
        protected abstract int Run(Node node, BTEnv env);

        public int Handle(BTNode node, BTEnv env)
        {
            if (node is not Node c)
            {
                throw new Exception($"AEffectHandler EffectConfig类型转换错误: {node.GetType().FullName} to {typeof (Node).Name}");
            }

            return this.Run(c, env);
        }

        public virtual Type GetNodeType()
        {
            return typeof (Node);
        }
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
            this.btHandlers.TryAdd(nodeType, ibtHandler);
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
            return ibtHandler.Handle(node, env);
        }
    }
}