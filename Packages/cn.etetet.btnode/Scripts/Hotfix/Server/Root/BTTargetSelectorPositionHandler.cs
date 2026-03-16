using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorPositionHandler : ABTHandler<TargetSelectorPosition>
    {
        protected override int Run(TargetSelectorPosition node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (buff == null || buff.IsDisposed)
            {
                return 1;
            }

            Unit unit = buff.GetCaster();
            if (unit == null || unit.IsDisposed)
            {
                return 1;
            }

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            if (targetComponent == null)
            {
                return 1;
            }

            float3 pos = targetComponent.Position;
            
            if (math.distance(pos, unit.Position) > node.MaxDistance)
            {
                pos = unit.Position + math.normalize(pos - unit.Position) * node.MaxDistance;
            }
            targetComponent.Position = pos;
            
            buff.GetOrAddSpellTargetComponent().Position = pos;

            env.AddStruct(node.Pos, pos);
            return 0;
        }
    }
}
