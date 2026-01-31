using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetReturnCheckHandler: ABTHandler<AI_PetReturnCheck>
    {
        protected override int Run(AI_PetReturnCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            Unit owner = PetHelper.GetOwner(unit);
            if (math.distance(unit.Position, owner.Position) < 30f)
            {
                return 1;
            }
            return 0;
        }
    }
}
