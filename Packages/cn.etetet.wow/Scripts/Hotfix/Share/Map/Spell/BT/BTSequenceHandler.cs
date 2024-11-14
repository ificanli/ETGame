namespace ET
{
    public class BTSequenceHandler: ABTHandler<BTSequence>
    {
        protected override bool Run(Effect effect, BTSequence node)
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