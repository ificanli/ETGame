using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTTargetSelectorRectangleHandler: ABTHandler<TargetSelectorRectangle>
    {
        protected override int Run(TargetSelectorRectangle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetComponent<SpellTargetComponent>();

            Unit caster = buff.GetCaster();
            spellTargetComponent.Position = caster.Position;

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = caster.GetComponent<AOIEntity>().GetSeeUnits();

            float3 pos = caster.Position;

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                Log.Debug($"1111111111111111111111a1");
                Unit unit = aoiEntity.Unit;
                if (!unit.UnitType.IsSame(node.UnitType))
                {
                    continue;
                }
                if (math.distance(pos, unit.Position) > 5f)
                {
                    continue;
                }
                Log.Debug($"1111111111111111111111a2 {aoiEntity.Unit.Id}");
                spellTargetComponent.Units.Add(aoiEntity.Unit.Id);
            }
            return 0;
        }
    }
}