#if DOTNET
using System;
using System.Collections.Generic;
using System.IO;
using Luban;

namespace ET
{
    [Invoke]
    public class ConfigGetAllConfigBytesServer : AInvokeHandler<ConfigLoader.ConfigGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>
    {
        public override async ETTask<Dictionary<Type, byte[]>> Handle(ConfigLoader.ConfigGetAllConfigBytes args)
        {
            var output = new Dictionary<Type, byte[]>();
            var configTypes = CodeTypes.Instance.GetTypes(typeof(ConfigProcessAttribute));
            
            foreach (Type configType in configTypes)
            {
                string configFilePath = null;
                ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
                switch(configProcessAttribute.ConfigType)
                {
                    case ConfigType.Luban:
                        if (LubanHelper.StartConfigs.Contains(configType.Name))
                        {
                            configFilePath = Path.Combine($"Packages/cn.etetet.startconfig/Assets/Config/Binary/Server/{Options.Instance.StartConfig}/{configType.Name}.bytes");
                        }
                        else
                        {
                            configFilePath = Path.Combine($"Packages/cn.etetet.startconfig/Assets/Config/Binary/Server/{configType.Name}.bytes");
                        }
                        break;
                    case ConfigType.Bson:
                        configFilePath = Path.Combine($"Packages/cn.etetet.wow/Bundles/Config/{configType.Name}.bytes");
                        break;
                }

                output[configType] = File.ReadAllBytes(configFilePath);
            }

            await ETTask.CompletedTask;
            return output;
        }
    }
}
#endif