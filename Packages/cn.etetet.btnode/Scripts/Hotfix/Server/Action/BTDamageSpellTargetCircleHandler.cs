using Unity.Mathematics;

namespace ET.Server
{
    public class BTDamageSpellTargetCircleHandler: ABTHandler<BTDamageSpellTargetCircle>
    {
        protected override int Run(BTDamageSpellTargetCircle node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (caster == null || caster.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 1;
            }

            SpellTargetComponent spellTargetComponent = buff.GetOrAddSpellTargetComponent();
            AOIEntity casterAoi = caster.GetComponent<AOIEntity>();
            if (casterAoi == null)
            {
                return 0;
            }

            float radius = math.max(0f, node.Radius);
            float3 center = spellTargetComponent.Position;
            foreach ((long _, AOIEntity aoiEntity) in casterAoi.GetSeeUnits())
            {
                Unit target = aoiEntity?.Unit;
                if (target == null || target.IsDisposed)
                {
                    continue;
                }

                if (!target.UnitType.IsSame(node.UnitType))
                {
                    continue;
                }

                float targetRadius = target.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
                if (math.distance(center, target.Position) > radius + targetRadius)
                {
                    continue;
                }

                DamageHelper.Damage(caster, target, buff, node.Value);
            }

            return 0;
        }
    }
}
