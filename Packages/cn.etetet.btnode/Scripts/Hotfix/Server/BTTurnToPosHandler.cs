using Unity.Mathematics;

namespace ET.Server
{
    public class BTTurnToPosHandler: ABTHandler<BTTurnToPos>
    {
        protected override int Run(BTTurnToPos node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            if (caster == null)
            {
                return 1;
            }

            float3 pos = env.GetStruct<float3>(node.Pos);

            float3 v = pos - caster.Position;
            v.y = 0;
            if (math.lengthsq(v) < 0.0001f)
            {
                return 0;
            }

            quaternion to = quaternion.LookRotation(v, math.up());
            caster.Turn(to, 100);
            
            return 0;
        }
    }
}
