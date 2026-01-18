using System;
using System.Collections.Generic;

namespace ET
{
    public abstract class AIHandler<T>: ABTHandler<T> where T: AINode
    {
        public override int Handle(BTNode btNode, BTEnv env)
        {
            T node = (T)btNode;
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);

            int ret = this.Check(unit, node, env);
            
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
            this.RunAsync(unit, node, env).Coroutine(buffTickComponent.CancellationToken);
            return 0;
        }

        protected virtual int Check(Unit unit, T node, BTEnv env)
        {
            return 0;
        }
        
        protected abstract ETTask RunAsync(Unit unit, T node, BTEnv env);
    }
}
