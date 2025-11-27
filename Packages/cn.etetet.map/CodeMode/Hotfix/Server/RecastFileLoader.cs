using System;
using System.IO;

namespace ET
{
    [Invoke]
    public class RecastFileReader: AInvokeHandler<NavmeshComponent.RecastFileLoader, ETTask<byte[]>>
    {
        public override async ETTask<byte[]> Handle(NavmeshComponent.RecastFileLoader args)
        {
            await ETTask.CompletedTask;
            return File.ReadAllBytes($"Packages/cn.etetet.map/Bundles/Recast/{args.Name}.bytes");
        }
    }
}