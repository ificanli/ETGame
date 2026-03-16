using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorRectangleHandler: ABTHandler<TargetSelectorRectangle>
    {
        protected override int Run(TargetSelectorRectangle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (buff == null || buff.IsDisposed)
            {
                return 1;
            }

            SpellTargetComponent spellTargetComponent = buff.GetOrAddSpellTargetComponent();
            spellTargetComponent.Units.Clear();

            Unit caster = buff.GetCaster();
            if (caster == null || caster.IsDisposed)
            {
                return 0;
            }

            spellTargetComponent.Position = caster.Position;

            AOIEntity casterAoi = caster.GetComponent<AOIEntity>();
            if (casterAoi == null)
            {
                return 0;
            }

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = casterAoi.GetSeeUnits();
            if (seeUnits == null || seeUnits.Count == 0)
            {
                return 0;
            }

            float3 pos = caster.Position;

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                if (aoiEntity == null)
                {
                    continue;
                }

                Unit unit = aoiEntity.Unit;
                if (unit == null || unit.IsDisposed)
                {
                    continue;
                }

                if (!unit.UnitType.IsSame(node.UnitType))
                {
                    continue;
                }
                if (math.distance(pos, unit.Position) > 5f)
                {
                    continue;
                }
                spellTargetComponent.Units.Add(unit.Id);
            }
            return 0;
        }
    }
}
