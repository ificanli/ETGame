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
            bool inRange = distance <= node.Range;
            if (!inRange)
            {
                Log.Debug($"BTWeaponInRange: unit {caster.Id} dist={distance:F1} > range={node.Range}");
            }
            return inRange ? 0 : 1;
        }
    }
}
