namespace ET.Server
{
    public class BTGetBuffCasterHandler: ABTHandler<BTGetBuffCaster>
    {
        protected override int Run(BTGetBuffCaster node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            env.AddEntity(node.Unit, buff.GetOwner());
            return 0;
        }
    }
}