using System;

namespace ET
{
    [BTHandler]
    public abstract class ABTAsyncHandler<Node>: HandlerObject, IBTHandler where Node: BTAsyncNode
    {
        public virtual Type GetNodeType()
        {
            return typeof (Node);
        }
        
        public virtual int Handle(BTNode btNode, BTEnv env)
        {
            Node node = (Node)btNode;
            Buff buff = env.GetEntity<Buff>(node.Buff);

            int ret = this.Check(buff, node, env);
            
            if (ret != 0)
            {
                return ret;
            }
            
#if UNITY_EDITOR
            env.AddPath(node.Id);
#endif

            BuffTickComponent buffTickComponent = buff.GetComponent<BuffTickComponent>();
            if (buffTickComponent == null)
            {
                throw new Exception("是否EffectServerBuffTick节点没有设置IsAI");
            }
            
            // 如果热重载了，那么hashcode会变化，不等于的话就会往下走取消当前协程
            if (buffTickComponent.Current == node.Id && buffTickComponent.HashCode == this.GetHashCode())
            {
                return 0;
            }

            // 取消上一个协程
            buffTickComponent.CancellationToken?.Cancel();
            buffTickComponent.CancellationToken = null;
            buffTickComponent.CancellationToken = new ETCancellationToken();
            
            buffTickComponent.Current = node.Id;
            buffTickComponent.HashCode = this.GetHashCode();
            this.RunAsync(buff, node, env).Coroutine(buffTickComponent.CancellationToken);
            return 0;
        }

        protected virtual int Check(Buff buff, Node node, BTEnv env)
        {
            return 0;
        }
        
        protected abstract ETTask RunAsync(Buff buff, Node node, BTEnv env);
    }
}
