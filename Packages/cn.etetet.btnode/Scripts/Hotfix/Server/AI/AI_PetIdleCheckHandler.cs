using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetIdleCheckHandler: ABTHandler<AI_PetIdleCheck>
    {
        protected override int Run(AI_PetIdleCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            Unit target = targetComponent.Unit;
            if (target != null)
            {
                return 1;
            }

            Unit owner = PetHelper.GetOwner(unit);
            if (math.distance(unit.Position, owner.Position) > 3f)
            {
                return 1;
            }
            return 0;
        }
    }
}
