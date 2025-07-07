using System;
using System.Collections.Generic;

namespace ET.Client
{
    public class TargetSelectHandlerAttribute: BaseAttribute
    {
    }
    
    public interface ITargetSelectHandler
    {
        ETTask<int> Handle(TargetSelector node, Unit unit, SpellConfig spellConfig);
        Type GetNodeType();
    }
    
    [TargetSelectHandler]
    public abstract class TargetSelectHandler<Node>: HandlerObject, ITargetSelectHandler where Node : TargetSelector
    {
        protected abstract ETTask<int> Run(Node node, Unit unit, SpellConfig spellConfig);

        public async ETTask<int> Handle(TargetSelector node, Unit unit, SpellConfig spellConfig)
        {
            if (node is not Node c)
            {
                throw new Exception($"类型转换错误: {node.GetType().FullName} to {typeof (Node).Name}");
            }

            return await this.Run(c, unit, spellConfig);
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
    public class TargetSelectDispatcher: Singleton<TargetSelectDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, ITargetSelectHandler> handlers = new();

        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (TargetSelectHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }
        }
        
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            ITargetSelectHandler targetSelectHandler = obj as ITargetSelectHandler;
            if (targetSelectHandler == null)
            {
                throw new Exception($"target select handler not inherit ITargetSelectHandler abstract class: {obj.GetType().FullName}");
            }

            Type nodeType = targetSelectHandler.GetNodeType();
            this.handlers.TryAdd(nodeType, targetSelectHandler);
        }
        
        
        public async ETTask<int> Handle(TargetSelector node, Unit unit, SpellConfig spellConfig)
        {
            if (!this.handlers.TryGetValue(node.GetType(), out ITargetSelectHandler targetSelectHandler))
            {
                throw new Exception($"not found target select handler: {node.GetType().FullName}");
            }
            return await targetSelectHandler.Handle(node, unit, spellConfig);
        }
    }
}