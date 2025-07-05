using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTTargetSelectorPositionHandler : ABTHandler<TargetSelectorPosition>
    {
        protected override int Run(TargetSelectorPosition node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetCaster();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();

            float3 pos = targetComponent.Position;
            
            if (math.distance(pos, unit.Position) > node.MaxDistance)
            {
                pos = unit.Position + math.normalize(pos - unit.Position) * node.MaxDistance;
            }
            targetComponent.Position = pos;
            
            buff.GetComponent<SpellTargetComponent>().Position = pos;

            env.AddStruct(node.Pos, pos);
            return 0;
        }
    }
}