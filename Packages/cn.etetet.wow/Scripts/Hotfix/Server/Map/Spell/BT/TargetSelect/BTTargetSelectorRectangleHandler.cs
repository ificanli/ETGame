namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTTargetSelectorRectangleHandler: ABTHandler<TargetSelectorRectangle>
    {
        protected override int Run(TargetSelectorRectangle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            return 0;
        }
    }
}