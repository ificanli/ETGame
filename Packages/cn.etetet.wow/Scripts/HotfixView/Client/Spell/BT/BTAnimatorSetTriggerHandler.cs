namespace ET.Client
{
    public class BTAnimatorSetTriggerHandler: ABTHandler<BTAnimatorSetTrigger>
    {
        protected override int Run(BTAnimatorSetTrigger node, BTEnv env)
        {
            Unit unit = env.Get<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetTrigger(node.MotionType.ToString());
            return 0;
        }
    }
}