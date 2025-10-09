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
            if (Define.IsEditor)
            {
                return File.ReadAllBytes($"Packages/cn.etetet.wow/Bundles/Recast/{args.Name}.bytes");
            }
            
#pragma warning disable CS0162 // Unreachable code detected
            TextAsset textAsset = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>($"Packages/cn.etetet.wow/Bundles/Recast/{args.Name}.bytes");
			return textAsset.bytes;
#pragma warning restore CS0162 // Unreachable code detected
        }
    }
}