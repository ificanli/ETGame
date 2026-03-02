using Unity.Mathematics;

namespace ET.Server
{
    public class BTWeaponInRangeHandler : ABTHandler<BTWeaponInRange>
    {
        protected override int Run(BTWeaponInRange node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);

            float distance = math.distance(caster.Position, target.Position);
            return distance <= node.Range ? 0 : 1;
        }
    }
}
