using System;
using System.Collections.Generic;

namespace ET
{
    public class CostHandlerAttribute: BaseAttribute
    {
    }
    
    public interface ICostHandler
    {
        int Handle(CostNode node, Unit unit, SpellConfig spellConfig);
        Type GetNodeType();
    }
    
    [BTHandler]
    public abstract class CostHandler<Node>: HandlerObject, ICostHandler where Node : CostNode
    {
        protected abstract int Run(Node node, Unit unit, SpellConfig spellConfig);

        public int Handle(CostNode node, Unit unit, SpellConfig spellConfig)
        {
            if (node is not Node c)
            {
                throw new Exception($"类型转换错误: {node.GetType().FullName} to {typeof (Node).Name}");
            }

            return this.Run(c, unit, spellConfig);
        }

        public Type GetNodeType()
        {
            return typeof (Node);
        }
    }
    
    /// <summary>
    /// 分发组件
    /// </summary>
    [CodeProcess]
    public class CostDispatcher: Singleton<CostDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, ICostHandler> handlers = new();

        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (CostHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }
        }
        
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            ICostHandler costHandler = obj as ICostHandler;
            if (costHandler == null)
            {
                throw new Exception($"cost andler not inherit ICostHandler abstract class: {obj.GetType().FullName}");
            }

            Type nodeType = costHandler.GetNodeType();
            this.handlers.TryAdd(nodeType, costHandler);
        }
        
        
        public int Handle(Unit unit, SpellConfig spellConfig)
        {
            foreach (CostNode node in spellConfig.Cost)
            {
                if (!this.handlers.TryGetValue(node.GetType(), out ICostHandler costHandler))
                {
                    throw new Exception($"not found cost handler: {node.GetType().FullName}");
                }
                
                int ret = costHandler.Handle(node, unit, spellConfig);
                if (ret != 0)
                {
                    return ret;
                }
            }
            return 0;
        }
    }
}