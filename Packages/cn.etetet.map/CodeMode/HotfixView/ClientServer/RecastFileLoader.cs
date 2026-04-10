using System;
using System.IO;
using UnityEngine;

namespace ET
{
    [Invoke]
    public class RecastFileReader: AInvokeHandler<NavmeshComponent.RecastFileLoader, ETTask<byte[]>>
    {
        public override async ETTask<byte[]> Handle(NavmeshComponent.RecastFileLoader args)
        {
            string location = $"Packages/cn.etetet.map/Bundles/Recast/{args.Name}.bytes";
            if (Application.isEditor)
            {
                return File.ReadAllBytes(location);
            }

            TextAsset textAsset = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>(location);
            return textAsset.bytes;
        }
    }
}
