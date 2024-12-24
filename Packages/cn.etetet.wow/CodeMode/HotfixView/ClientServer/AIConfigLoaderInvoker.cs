using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ET.Client
{
    [Invoke]
    public class AIConfigLoaderInvoker: AInvokeHandler<AIConfigLoader, AIConfig>
    {
        public override AIConfig Handle(AIConfigLoader args)
        {
            AIScriptableObject aiScriptableObject = ResourcesComponent.Instance.LoadAssetSync<AIScriptableObject>(args.Id.ToString());
            if (aiScriptableObject == null)
            {
                throw new Exception($"not found ai config: {args.Id}");
            }
            return aiScriptableObject.AIConfig;
        }
    }
}