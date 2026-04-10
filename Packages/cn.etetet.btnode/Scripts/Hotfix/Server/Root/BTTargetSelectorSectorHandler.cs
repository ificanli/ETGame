using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorSectorHandler: ABTHandler<TargetSelectorSector>
    {
        protected override int Run(TargetSelectorSector node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit caster = env.GetEntity<Unit>(node.Caster);
            if (buff == null || buff.IsDisposed || caster == null || caster.IsDisposed)
            {
                return 1;
            }

            SpellTargetComponent spellTargetComponent = buff.GetOrAddSpellTargetComponent();
            spellTargetComponent.Units.Clear();
            spellTargetComponent.Position = caster.Position;

            AOIEntity casterAoi = caster.GetComponent<AOIEntity>();
            if (casterAoi == null)
            {
                env.AddCollection(node.Units, spellTargetComponent.Units);
                return 0;
            }

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = casterAoi.GetSeeUnits();
            if (seeUnits == null || seeUnits.Count == 0)
            {
                env.AddCollection(node.Units, spellTargetComponent.Units);
                return 0;
            }

            float radius = math.max(0f, node.Radius / 1000f);
            float halfAngleCos = math.cos(math.radians(math.clamp(node.Angle, 1, 360) * 0.5f));
            float3 center = caster.Position;
            float3 forward = math.normalizesafe(new float3(caster.Forward.x, 0f, caster.Forward.z), new float3(0f, 0f, 1f));

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                Unit unit = aoiEntity?.Unit;
                if (unit == null || unit.IsDisposed)
                {
                    continue;
                }

                if (!unit.UnitType.IsSame(node.UnitType))
                {
                    continue;
                }

                float3 delta = unit.Position - center;
                delta.y = 0f;
                float distance = math.length(delta);
                float targetRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
                if (distance > radius + targetRadius)
                {
                    continue;
                }

                float3 direction = math.normalizesafe(delta, forward);
                if (math.dot(forward, direction) < halfAngleCos)
                {
                    continue;
                }

                if (node.Children.Count > 0)
                {
                    env.AddEntity(node.Unit, unit);
                    bool filter = false;
                    foreach (BTNode child in node.Children)
                    {
                        int ret = BTDispatcher.Instance.Handle(child, env);
                        if (ret == 0)
                        {
                            continue;
                        }

                        filter = true;
                        break;
                    }

                    if (filter)
                    {
                        continue;
                    }
                }

                spellTargetComponent.Units.Add(unit.Id);
            }

            env.AddCollection(node.Units, spellTargetComponent.Units);
            return 0;
        }
    }
}
