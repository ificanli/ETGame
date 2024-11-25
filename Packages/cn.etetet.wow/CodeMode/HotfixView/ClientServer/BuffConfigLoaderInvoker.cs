using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ET.Client
{
    [Invoke]
    public class BuffConfigLoaderInvoker: AInvokeHandler<BuffConfigLoader, BuffConfig>
    {
        public override BuffConfig Handle(BuffConfigLoader args)
        {
            BuffScriptableObject buffScriptableObject = ResourcesComponent.Instance.LoadAssetSync<BuffScriptableObject>(args.Id.ToString());
            if (buffScriptableObject is null)
            {
                throw new Exception($"not found buff config: {args.Id}");
            }
            return buffScriptableObject.BuffConfig;
        }
    }
}