namespace ET.Client
{
    public class BTAnimatorSetIntHandler: ABTHandler<BTAnimatorSetInt>
    {
        protected override int Run(BTAnimatorSetInt node, BTEnv env)
        {
            Unit unit = env.Get<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetInt(node.MotionType.ToString(), node.Value);
            return 0;
        }
    }
}