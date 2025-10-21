#if DOTNET
using System;
using System.Collections.Generic;
using System.IO;

namespace ET.Server
{
    [Invoke]
    public class ConfigGetAllConfigBytesServer : AInvokeHandler<ConfigLoader.ConfigGetAllConfigBytes, ETTask<Dictionary<Type, object>>>
    {
        public override async ETTask<Dictionary<Type, object>> Handle(ConfigLoader.ConfigGetAllConfigBytes args)
        {
            var output = new Dictionary<Type, object>();
            var configTypes = CodeTypes.Instance.GetTypes(typeof(ConfigProcessAttribute));
            
            foreach (Type configType in configTypes)
            {
                string configFilePath = null;
                ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
                switch(configProcessAttribute.ConfigType)
                {
                    case ConfigType.Luban:
                        configFilePath = Path.Combine($"Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Binary/{configType.Name}.bytes");
                        output[configType] = File.ReadAllBytes(configFilePath);
                        break;
                    case ConfigType.Json:
                        if (StartConfigHelper.StartConfigs.Contains(configType.Name))
                        {
                            configFilePath = Path.Combine($"Packages/cn.etetet.startconfig/Bundles/Luban/{Options.Instance.StartConfig}/Server/Json/{configType.Name}.json");
                        }
                        else
                        {
                            configFilePath = Path.Combine($"Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/{configType.Name}.json");
                        }
                        output[configType] = File.ReadAllText(configFilePath);
                        break;
                    case ConfigType.Bson:
                        configFilePath = Path.Combine($"Packages/cn.etetet.wow/Bundles/Bson/{configType.Name}.bytes");
                        output[configType] = File.ReadAllBytes(configFilePath);
                        break;
                }
            }

            await ETTask.CompletedTask;
            return output;
        }
    }
}
#endif