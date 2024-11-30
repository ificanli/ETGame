namespace ET.Server
{
    public class BTBuffRemoveTypeCaseHandler: ABTHandler<BTBuffRemoveTypeCase>
    {
        protected override int Run(BTBuffRemoveTypeCase node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (buff.GetComponent<BuffRemoveTypeComponent>().BuffRemoveType != node.BuffRemoveType)
            {
                return -1;
            }
            return 0;
        }
    }
}