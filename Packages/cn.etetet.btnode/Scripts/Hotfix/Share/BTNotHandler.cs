namespace ET
{
    public class BTNotHandler: ABTHandler<BTNot>
    {
        protected override int Run(BTNot node, BTEnv env)
        {
            if (node.Children == null || node.Children.Count == 0)
            {
                return 1;
            }

            int ret = BTDispatcher.Instance.Handle(node.Children[0], env);
            return ret == 0 ? 1 : 0;
        }
    }
}
