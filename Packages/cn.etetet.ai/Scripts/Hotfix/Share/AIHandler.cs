namespace ET
{
    public abstract class AIHandler<T>: ABTHandler<T> where T: AINode
    {
        protected override int Run(T node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);

            int ret = this.Check(unit, node, env);
            
            if (ret != 0)
            {
                return ret;
            }
            
            AIComponent aiComponent = unit.GetComponent<AIComponent>();
            
            if (aiComponent.Current == node.Id)
            {
                return 0;
            }
            
            // 取消上一个协程
            aiComponent.Cancel();
            aiComponent.CancellationToken = new ETCancellationToken();
            
            aiComponent.Current = node.Id;
            this.Execute(unit, node, env).Coroutine(aiComponent.CancellationToken);
            return 0;
        }

        protected abstract int Check(Unit unit, T node, BTEnv env);
        
        protected abstract ETTask Execute(Unit unit, T node, BTEnv env);
    }
}