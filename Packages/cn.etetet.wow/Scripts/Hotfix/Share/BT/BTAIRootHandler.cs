namespace ET
{
    public class BTAIRootHandler: ABTHandler<AIRoot>
    {
        protected override int Run(AIRoot node, BTEnv env)
        {
            foreach (BTNode subNode in node.Children)
            {
                int ret = BTDispatcher.Instance.Handle(subNode, env);
                if (ret == 0)
                {
                    return ret;
                }
            }
            return 0;
        }
    }
}