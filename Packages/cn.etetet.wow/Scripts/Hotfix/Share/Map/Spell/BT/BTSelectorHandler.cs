namespace ET
{
    public class BTSelectorHandler: ABTHandler<BTSelector>
    {
        protected override bool Run(BTSelector node, BTEnv env)
        {
            foreach (BTNode subNode in node.Children) 
            {
                if (BTDispatcher.Instance.Handle(subNode, env))
                {
                    return true;
                }
            }
            return false;
        }
    }
}