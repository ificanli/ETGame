using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorPositionHandler : ABTHandler<TargetSelectorPosition>
    {
        protected override int Run(TargetSelectorPosition node, BTEnv env)
        {
            Spell spell = env.GetEntity<Spell>(node.Spell);

            float3 pos = spell.GetCaster().GetComponent<TargetComponent>().Position;

            env.AddStruct(node.Pos, pos);
            return 0;
        }
    }
}