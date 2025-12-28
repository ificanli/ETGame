using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    public class BTCreateEffectOnPosHandler: ABTHandler<BTCreateEffectOnPos>
    {
        protected override int Run(BTCreateEffectOnPos node, BTEnv env)
        {
            float3 pos = env.GetStruct<float3>(node.Pos);

            CreateEffectOnPosAsync(env.Scene, node.Effect.Name, pos, node.Duration).Coroutine();
            return 0;
        }
        
        private static async ETTask CreateEffectOnPosAsync(Scene scene, string effectName, float3 pos, int duration)
        {
            GameObject go = await scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(effectName);
            EffectUnitHelper.Create(go, pos, Quaternion.identity, duration);
        }
    }
}