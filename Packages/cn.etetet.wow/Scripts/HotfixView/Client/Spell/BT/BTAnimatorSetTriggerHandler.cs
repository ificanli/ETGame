namespace ET.Client
{
    public class BTAnimatorSetTriggerHandler: ABTHandler<BTAnimatorSetTrigger>
    {
        protected override bool Run(BTAnimatorSetTrigger node, BTEnv env)
        {
            Unit unit = env.Get<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetTrigger(node.MotionType.ToString());
            return true;
        }
    }
}