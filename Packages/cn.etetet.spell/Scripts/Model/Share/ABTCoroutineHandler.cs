using System;

namespace ET
{
    [BTHandler]
    public abstract class ABTCoroutineHandler<Node>: HandlerObject, IBTHandler where Node: BTCoroutine
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
#if UNITY_EDITOR
                // 协程节点在持续运行时会 early-return，这里仍然需要把当前帧/当前tick的运行路径推送给编辑器
                EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env });
#endif
                return 0;
            }
            
            // 协程节点在持续运行时会 early-return，这里仍然需要把当前帧/当前tick的运行路径推送给编辑器
#if UNITY_EDITOR
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env });
#endif

            // 取消上一个协程
            buffTickComponent.CancellationToken?.Cancel();
            buffTickComponent.CancellationToken = null;
            buffTickComponent.CancellationToken = new ETCancellationToken();
            
            buffTickComponent.Current = node.Id;
            buffTickComponent.HashCode = this.GetHashCode();
            EntityRef<Buff> buffRef = buff;
            RunWithChildrenAsync(buffRef, node, env).Coroutine(buffTickComponent.CancellationToken);
            return 0;
        }

        protected virtual int Check(Buff buff, Node node, BTEnv env)
        {
            return 0;
        }
        
        protected abstract ETTask RunAsync(Buff buff, Node node, BTEnv env);

        private async ETTask RunWithChildrenAsync(EntityRef<Buff> buffRef, Node node, BTEnv env)
        {
            if (node.Children.Count > 1)
            {
                throw new Exception($"{node.GetType().Name} only supports 1 child, but got {node.Children.Count}, nodeId: {node.Id}");
            }

            ETCancellationToken cancellationToken = null;
            if (node.Children.Count > 0)
            {
                env.IsFromPool = false; // 有协程，不要回收env
                cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            }

            Buff buff = buffRef;
            if (buff == null)
            {
                return;
            }

            await this.RunAsync(buff, node, env);
            if (cancellationToken == null || cancellationToken.IsCancel())
            {
                return;
            }

            // 这里可以继续让env回收
            env.IsFromPool = true;
            using BTEnv _ = env;
            
            BTDispatcher.Instance.Handle(node.Children[0], env);
            
#if UNITY_EDITOR
            env.AddPath(node.Id);
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env});
#endif
        }
    }
}
