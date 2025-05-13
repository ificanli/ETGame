namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTBuffAddStackHandler: ABTHandler<BTBuffAddStack>
    {
        protected override int Run(BTBuffAddStack node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            int stack = buff.Stack + node.Value; 
            BuffHelper.UpdateStack(buff, stack);
            return 0;
        }
    }
}