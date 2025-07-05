namespace ET.Client
{
    [Module(ModuleName.Spell)]
    public class BTAnimatorSetTriggerHandler: ABTHandler<BTAnimatorSetTrigger>
    {
        protected override int Run(BTAnimatorSetTrigger node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetTrigger(node.MotionType.ToString());
            return 0;
        }
    }
}