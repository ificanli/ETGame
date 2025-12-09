using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ET.Client
{
    [Invoke]
    public class ConfigGetAllConfigBytesClient : AInvokeHandler<ConfigLoader.ConfigGetAllConfigBytes, ETTask<Dictionary<Type, object>>>
    {
        public override async ETTask<Dictionary<Type, object>> Handle(ConfigLoader.ConfigGetAllConfigBytes args)
        {
            var output   = new Dictionary<Type, object>();
            var allTypes = CodeTypes.Instance.GetTypes(typeof(ConfigProcessAttribute));

#if UNITY_EDITOR
            var globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            var codeMode     = globalConfig.CodeMode.ToString();
            foreach (Type configType in allTypes)
            {
                string configFilePath = null;
                ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
                switch(configProcessAttribute.ConfigType)
                {
                    case ConfigType.Luban:
                        configFilePath = Path.Combine($"Packages/cn.etetet.excel/Bundles/Luban/Config/{codeMode}/Binary/{configType.Name}.bytes");
                        output[configType] = File.ReadAllBytes(configFilePath);
                        break;
                    case ConfigType.Json:
                        if (StartConfigHelper.StartConfigs.Contains(configType.Name))
                        {
                            configFilePath = Path.Combine($"Packages/cn.etetet.startconfig/Bundles/Luban/{Options.Instance.StartConfig}/Server/Json/{configType.Name}.json");
                        }
                        else
                        {
                            configFilePath = Path.Combine($"Packages/cn.etetet.excel/Bundles/Luban/Config/{codeMode}/Json/{configType.Name}.json");
                        }
                        output[configType] = File.ReadAllText(configFilePath);
                        break;
                    case ConfigType.Bson:
                        configFilePath = Path.Combine($"Packages/cn.etetet.map/Bundles/Json/{configType.Name}.txt");
                        output[configType] = File.ReadAllText(configFilePath);
                        break;
                }
                
            }
            await ETTask.CompletedTask;
#else
            foreach (Type configType in allTypes)
            {
                ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
                TextAsset v = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>(configType.Name);
                switch (configProcessAttribute.ConfigType)
                {
                    case ConfigType.Luban:
                        output[configType] = v.bytes;
                        break;
                    case ConfigType.Json:
                    case ConfigType.Bson:
                        output[configType] = v.text;
                        break;
                }
            }
#endif
            return output;
        }
    }
}