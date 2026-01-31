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

#if UNITY_EDITOR
            env.AddPath(node.Id);
#endif

            BuffTickComponent buffTickComponent = buff.GetComponent<BuffTickComponent>();
            if (buffTickComponent == null)
            {
                throw new Exception("是否EffectServerBuffTick节点没有设置IsAI");
            }

#if UNITY_EDITOR
                EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env });
#endif
            
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

            RunWithChildrenAsync(buffTickComponent, node, env).Coroutine(buffTickComponent.CancellationToken);
            return 0;
        }

        protected abstract ETTask RunAsync(Node node, BTEnv env);

        private async ETTask RunWithChildrenAsync(BuffTickComponent buffTickComponent, Node node, BTEnv env)
        {
            EntityRef<BuffTickComponent> buffTickComponentRef = buffTickComponent;
            ETCancellationToken cancellationToken = null;
            if (node.Children.Count > 0)
            {
                env.IsFromPool = false; // 有协程，不要回收env
                cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            }

            await this.RunAsync(node, env);
            
            if (cancellationToken.IsCancel() || node.Children.Count <= 0)
            {
                buffTickComponent = buffTickComponentRef;
                if (buffTickComponent != null)
                {
                    // 这里需要重置，否则无法重新执行这个协程
                    buffTickComponent.Current = 0;
                }
                return;
            }

            // 这里可以继续让env回收
            env.IsFromPool = true;
            using BTEnv _ = env;

            BTDispatcher.Instance.Handle(node.Children[0], env);

#if UNITY_EDITOR
            env.RunPath.Add(node.Id);
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env});
#endif
        }
    }
}
