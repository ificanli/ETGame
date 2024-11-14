namespace ET
{
    public class BTSelectorHandlerHandler: ABTHandler<BTSelector>
    {
        protected override bool Run(Effect effect, BTSelector node)
        {
            foreach (BTNode subNode in node.Children) 
            {
                if (BTDispatcher.Instance.Handle(effect, subNode))
                {
                    return true;
                }
            }
            return false;
        }
    }
}