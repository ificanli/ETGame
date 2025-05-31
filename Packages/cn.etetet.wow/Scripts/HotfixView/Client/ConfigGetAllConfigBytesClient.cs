using System;
using System.Collections.Generic;
using System.IO;
using Luban;
using UnityEngine;

namespace ET
{
    [Invoke]
    public class ConfigGetAllConfigBytesClient : AInvokeHandler<ConfigLoader.ConfigGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>
    {
        public override async ETTask<Dictionary<Type, byte[]>> Handle(ConfigLoader.ConfigGetAllConfigBytes args)
        {
            var output   = new Dictionary<Type, byte[]>();
            var allTypes = CodeTypes.Instance.GetTypes(typeof(ConfigProcessAttribute));

            if (Define.IsEditor)
            {
                var globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
                var codeMode     = globalConfig.CodeMode.ToString();
                foreach (Type configType in allTypes)
                {
                    string configFilePath = null;
                    ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
                    switch(configProcessAttribute.ConfigType)
                    {
                        case ConfigType.Luban:
                            if (LubanHelper.StartConfigs.Contains(configType.Name))
                            {
                                configFilePath = $"Packages/cn.etetet.startconfig/Assets/Config/Binary/{CodeMode.Server}/{Options.Instance.StartConfig}/{configType.Name}.bytes";
                            }
                            else
                            {
                                configFilePath = $"Packages/cn.etetet.wow/Assets/Config/Binary/{codeMode}/Config/{configType.Name}.bytes";
                            }
                            break;
                        case ConfigType.Bson:
                            configFilePath = Path.Combine($"Packages/cn.etetet.wow/Bundles/Config/{configType.Name}.bytes");
                            break;
                    }
                    output[configType] = File.ReadAllBytes(configFilePath);
                }
            }
            else
            {
                foreach (Type type in allTypes)
                {
                    TextAsset v = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>(type.Name);
                    output[type] = v.bytes;
                }
            }

            return output;
        }
    }
}