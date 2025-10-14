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
            await ETTask.CompletedTask;
#if UNITY_EDITOR
            return File.ReadAllBytes($"Packages/cn.etetet.wow/Bundles/Recast/{args.Name}.bytes");
#else
            TextAsset textAsset = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>($"Packages/cn.etetet.wow/Bundles/Recast/{args.Name}.bytes");
			return textAsset.bytes;
#endif
        }
    }
}