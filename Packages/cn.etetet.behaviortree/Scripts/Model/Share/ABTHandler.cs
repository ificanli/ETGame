using System;

namespace ET
{
    [BTHandler]
    public abstract class ABTHandler<Node>: HandlerObject, IBTHandler where Node : BTNode
    {
        protected abstract int Run(Node node, BTEnv env);

        public virtual int Handle(BTNode node, BTEnv env)
        {
            if (node is not Node c)
            {
                throw new Exception($"AEffectHandler EffectConfig类型转换错误: {node.GetType().FullName} to {typeof (Node).Name}");
            }
#if UNITY_EDITOR
            env.AddPath(node.Id);
#endif
            return this.Run(c, env);
        }

        public virtual Type GetNodeType()
        {
            return typeof (Node);
        }
    }
}