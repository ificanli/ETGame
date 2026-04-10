using System.IO;
using UnityEngine;
using YooAsset;

namespace ET.Client
{
    [Invoke]
    public class ECAConcealmentConfigLoaderHandler : AInvokeHandler<ECAConcealmentConfigLoader, string>
    {
        public override string Handle(ECAConcealmentConfigLoader args)
        {
            if (Application.isEditor)
            {
                if (!File.Exists(args.Location))
                {
                    return null;
                }

                return File.ReadAllText(args.Location);
            }

            if (!YooAssets.CheckLocationValid(args.Location))
            {
                return null;
            }

            RawFileHandle handle = YooAssets.LoadRawFileSync(args.Location);
            try
            {
                return handle.GetRawFileText();
            }
            finally
            {
                handle.Release();
            }
        }
    }
}
