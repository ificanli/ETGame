namespace ET
{
    public class BTSequenceHandler: ABTHandler<BTSequence>
    {
        protected override bool Run(BTSequence node, BTEnv env)
        {
            foreach (BTNode subNode in node.Children) 
            {
                if (!BTDispatcher.Instance.Handle(subNode, env))
                {
                    return false;
                }
            }
            return true;
        }
    }
}