namespace ET.Client
{
    [Module(ModuleName.Spell)]
    public class BTAnimatorSetBoolHandler: ABTHandler<BTAnimatorSetBool>
    {
        protected override int Run(BTAnimatorSetBool node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetBool(node.MotionType.ToString(), node.Value);
            return 0;
        }
    }
}