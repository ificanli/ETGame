using Unity.Mathematics;

namespace ET.Server
{
    public class AI_RobotReturnCheckHandler: ABTHandler<AI_RobotReturnCheck>
    {
        protected override int Run(AI_RobotReturnCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return 1;
            }

            float maxDistance = node.MaxDistance;
            if (maxDistance <= 0f)
            {
                return 1;
            }

            UnitSpawnPointComponent spawnPointComponent = unit.GetComponent<UnitSpawnPointComponent>();
            float3 birthPos = spawnPointComponent?.Position ?? unit.Position;
            return math.distance(unit.Position, birthPos) > maxDistance ? 0 : 1;
        }
    }
}
