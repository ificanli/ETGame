namespace ET
{
    public class BTGetBuffCasterHandler: ABTHandler<BTGetBuffCaster>
    {
        protected override int Run(BTGetBuffCaster node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            env.AddEntity(node.Unit, buff.Parent.GetParent<Unit>());
            return 0;
        }
    }
}