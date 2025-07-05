namespace ET.Client
{
    [Module(ModuleName.Spell)]
    public class BTAnimatorSetIntHandler: ABTHandler<BTAnimatorSetInt>
    {
        protected override int Run(BTAnimatorSetInt node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetInt(node.MotionType.ToString(), node.Value);
            return 0;
        }
    }
}