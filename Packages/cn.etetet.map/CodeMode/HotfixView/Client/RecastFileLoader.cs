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
            TextAsset textAsset = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>($"Packages/cn.etetet.map/Bundles/Recast/{args.Name}.bytes");
            return textAsset.bytes;
        }
    }
}