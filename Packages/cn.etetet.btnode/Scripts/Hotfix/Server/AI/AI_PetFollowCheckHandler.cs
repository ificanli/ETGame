using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetFollowCheckHandler: ABTHandler<AI_PetFollowCheck>
    {
        protected override int Run(AI_PetFollowCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            Unit target = targetComponent.Unit;
            if (target != null)
            {
                return 1;
            }

            long ownerId = unit.GetComponent<PetComponent>().OwnerId;
            Unit owner = unit.GetParent<UnitComponent>().Get(ownerId);
            if (math.distance(unit.Position, owner.Position) < 3f)
            {
                return 1;
            }

            return 0;
        }
    }
}
