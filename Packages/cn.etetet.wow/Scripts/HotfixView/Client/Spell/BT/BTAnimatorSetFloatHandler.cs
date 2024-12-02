namespace ET.Client
{
    public class BTAnimatorSetFloatHandler: ABTHandler<BTAnimatorSetFloat>
    {
        protected override int Run(BTAnimatorSetFloat node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetFloat(node.MotionType.ToString(), node.Value);
            return 0;
        }
    }
}