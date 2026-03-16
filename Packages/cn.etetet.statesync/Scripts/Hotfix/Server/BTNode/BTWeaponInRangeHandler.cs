using Unity.Mathematics;

namespace ET.Server
{
    public class BTWeaponInRangeHandler : ABTHandler<BTWeaponInRange>
    {
        protected override int Run(BTWeaponInRange node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);

            float distance = math.distance(
                new float2(caster.Position.x, caster.Position.z),
                new float2(target.Position.x, target.Position.z));
            bool inRange = distance <= node.Range;
            if (!inRange)
            {
                Log.Warning($"[WeaponFireTrace][InRange] out of range, caster={caster.Id}, target={target.Id}, distance={distance:F2}, range={node.Range}");
                Log.Debug($"BTWeaponInRange: unit {caster.Id} dist={distance:F1} > range={node.Range}");
            }
            else
            {
                Log.Info($"[WeaponFireTrace][InRange] in range, caster={caster.Id}, target={target.Id}, distance={distance:F2}, range={node.Range}");
            }
            return inRange ? 0 : 1;
        }
    }
}
