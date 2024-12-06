using System;
using System.Collections.Generic;
using System.IO;
using Luban;
using UnityEngine;

namespace ET
{
    [Invoke]
    public class LubanClientLoaderInvokerGetAll : AInvokeHandler<ConfigLoader.LubanGetAllConfigBytes, ETTask<Dictionary<Type, ByteBuf>>>
    {
        public override async ETTask<Dictionary<Type, ByteBuf>> Handle(ConfigLoader.LubanGetAllConfigBytes args)
        {
            var output   = new Dictionary<Type, ByteBuf>();
            var allTypes = CodeTypes.Instance.GetTypes(typeof(ConfigAttribute));

            if (Define.IsEditor)
            {
                var globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
                var codeMode     = globalConfig.CodeMode.ToString();
                foreach (Type configType in allTypes)
                {
                    string configFilePath;
                    if (LubanHelper.StartConfigs.Contains(configType.Name))
                    {
                        configFilePath = $"{LubanHelper.ConfigResPath}/{CodeMode.Server}/{Options.Instance.StartConfig}/{configType.Name}.bytes";
                    }
                    else
                    {
                        configFilePath = $"{LubanHelper.ConfigResPath}/{codeMode}/{configType.Name}.bytes";
                    }

                    output[configType] = new ByteBuf(File.ReadAllBytes(configFilePath));
                }
            }
            else
            {
                foreach (Type type in allTypes)
                {
                    var v = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>(type.Name);
                    output[type] = new ByteBuf(v.bytes);
                }
            }

            return output;
        }
    }

    [Invoke]
    public class LubanClientLoaderInvokerGetOne : AInvokeHandler<ConfigLoader.LubanGetOneConfigBytes, ETTask<ByteBuf>>
    {
        public override async ETTask<ByteBuf> Handle(ConfigLoader.LubanGetOneConfigBytes args)
        {
            var globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            var codeMode     = globalConfig.CodeMode.ToString();
            var configFilePath = $"{LubanHelper.ConfigResPath}/{codeMode}/{args.ConfigName}.bytes";
            await ETTask.CompletedTask;
            return new ByteBuf(File.ReadAllBytes(configFilePath));
        }
    }
}