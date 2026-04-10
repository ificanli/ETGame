using Unity.Mathematics;

namespace ET.Server
{
    public class BTWeaponInRangeHandler : ABTHandler<BTWeaponInRange>
    {
        protected override int Run(BTWeaponInRange node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            float range = ResolveRange(caster, node.Range);

            float distance = math.distance(
                new float2(caster.Position.x, caster.Position.z),
                new float2(target.Position.x, target.Position.z));
            bool inRange = distance <= range;
            if (!inRange)
            {
                Log.Warning($"[WeaponFireTrace][InRange] out of range, caster={caster.Id}, target={target.Id}, distance={distance:F2}, range={range:F2}");
                Log.Debug($"BTWeaponInRange: unit {caster.Id} dist={distance:F1} > range={range:F2}");
            }
            else
            {
                Log.Info($"[WeaponFireTrace][InRange] in range, caster={caster.Id}, target={target.Id}, distance={distance:F2}, range={range:F2}");
            }
            return inRange ? 0 : 1;
        }

        private static float ResolveRange(Unit caster, float fallbackRange)
        {
            TargetSelectorComponent selector = caster.GetComponent<TargetSelectorComponent>();
            if (selector != null && selector.MaxRange > 0.01f)
            {
                return selector.MaxRange;
            }

            WeaponComponent weaponComponent = caster.GetComponent<WeaponComponent>();
            if (weaponComponent != null && weaponComponent.CurrentSlot > 0)
            {
                float weaponRange = weaponComponent.GetEffectiveAttackRange(weaponComponent.CurrentSlot);
                if (weaponRange > 0.01f)
                {
                    return weaponRange;
                }
            }

            return fallbackRange;
        }
    }
}
