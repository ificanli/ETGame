#if DOTNET
using System;
using System.Collections.Generic;
using System.IO;
using Luban;

namespace ET
{
    [Invoke]
    public class LubanServerLoaderInvokerGetAll : AInvokeHandler<ConfigLoader.LubanGetAllConfigBytes, ETTask<Dictionary<Type, ByteBuf>>>
    {
        public override async ETTask<Dictionary<Type, ByteBuf>> Handle(ConfigLoader.LubanGetAllConfigBytes args)
        {
            var output = new Dictionary<Type, ByteBuf>();
            var configTypes = CodeTypes.Instance.GetTypes(typeof(ConfigAttribute));
            foreach (Type configType in configTypes)
            {
                string configFilePath;
                if (LubanHelper.StartConfigs.Contains(configType.Name))
                {
                    Log.Error($"Options.Instance.StartConfig {Options.Instance.StartConfig}");
                    
                    configFilePath = Path.Combine($"{LubanHelper.ConfigResPath}/Server/{Options.Instance.StartConfig}/{configType.Name}.bytes");
                }
                else
                {
                    configFilePath = Path.Combine($"{LubanHelper.ConfigResPath}/Server/{configType.Name}.bytes");
                }

                output[configType] = new ByteBuf(File.ReadAllBytes(configFilePath));
            }

            await ETTask.CompletedTask;
            return output;
        }
    }

    [Invoke]
    public class LubanServerLoaderInvokerGetOne : AInvokeHandler<ConfigLoader.LubanGetOneConfigBytes, ByteBuf>
    {
        public override ByteBuf Handle(ConfigLoader.LubanGetOneConfigBytes args)
        {
            return new ByteBuf(File.ReadAllBytes($"{LubanHelper.ConfigResPath}/Server/{args.ConfigName}.bytes"));
        }
    }
}
#endif