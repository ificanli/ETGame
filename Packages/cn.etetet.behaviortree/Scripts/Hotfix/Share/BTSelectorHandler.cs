namespace ET
{
    [Module(ModuleName.BehaviorTree)]
    public class BTSelectorHandler: ABTHandler<BTSelector>
    {
        protected override int Run(BTSelector node, BTEnv env)
        {
            foreach (BTNode subNode in node.Children)
            {
                int ret = BTDispatcher.Instance.Handle(subNode, env);
                if (ret == 0)
                {
                    return ret;
                }
            }
            return -1;
        }
    }
}