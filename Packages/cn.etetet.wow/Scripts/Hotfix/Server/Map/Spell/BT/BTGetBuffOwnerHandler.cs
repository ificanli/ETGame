namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTGetBuffOwnerHandler: ABTHandler<BTGetBuffOwner>
    {
        protected override int Run(BTGetBuffOwner node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            env.AddEntity(node.Unit, buff.GetOwner());
            return 0;
        }
    }
}