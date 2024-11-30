using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    public class BTCreateEffectOnPosHandler: ABTHandler<BTCreateEffectOnPos>
    {
        protected override int Run(BTCreateEffectOnPos node, BTEnv env)
        {
            float3 pos = env.GetStruct<float3>(node.Pos);
            
            EffectUnitHelper.Create(node.Effect, pos, Quaternion.identity, node.Duration);

            return 0;
        }
    }
}