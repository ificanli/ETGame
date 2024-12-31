namespace ET
{
    [FriendOf(typeof(AIComponent))]
    public abstract class AIHandler<T>: ABTHandler<T> where T: AINode
    {
        protected override int Run(T node, BTEnv env)
        {
            AIComponent aiComponent = env.GetEntity<AIComponent>(node.AIComponent);

            int ret = this.Check(aiComponent, node, env);
            
            if (ret != 0)
            {
                return ret;
            }
            
            if (aiComponent.Current == node.Id)
            {
                return 0;
            }
            
            // 取消上一个协程
            aiComponent.Cancel();
            aiComponent.CancellationToken = new ETCancellationToken();
            
            aiComponent.Current = node.Id;
            this.Execute(aiComponent, node, env).WithContext(aiComponent.CancellationToken);
            return 0;
        }

        protected abstract int Check(AIComponent aiComponent, T node, BTEnv env);
        
        protected abstract ETTask Execute(AIComponent aiComponent, T node, BTEnv env);
    }
}