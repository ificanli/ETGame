using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorPositionHandler : ABTHandler<TargetSelectorPosition>
    {
        protected override int Run(TargetSelectorPosition node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            float3 pos = buff.GetCaster().GetComponent<TargetComponent>().Position;

            env.AddStruct(node.Pos, pos);
            return 0;
        }
    }
}