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

            if (Application.isEditor)
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
                            configFilePath = GetLubanConfigPath(codeMode, "Binary", configType.Name, "bytes");
                            output[configType] = File.ReadAllBytes(configFilePath);
                            break;
                        case ConfigType.Json:
                            if (StartConfigHelper.StartConfigs.Contains(configType.Name))
                            {
                                configFilePath = Path.Combine($"Packages/cn.etetet.startconfig/Bundles/Luban/{Options.Instance.StartConfig}/Server/Json/{configType.Name}.json");
                            }
                            else
                            {
                                configFilePath = GetLubanConfigPath(codeMode, "Json", configType.Name, "json");
                            }
                            output[configType] = File.ReadAllText(configFilePath);
                            break;
                        case ConfigType.Bson:
                            configFilePath = Path.Combine($"Packages/cn.etetet.map/Bundles/Json/{configType.Name}.txt");
                            output[configType] = File.ReadAllText(configFilePath);
                            break;
                    }
                }

                return output;
            }

            foreach (Type configType in allTypes)
            {
                ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
                TextAsset v = await LoadConfigAsset(configType.Name);
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
            return output;
        }

        private static string GetLubanConfigPath(string codeMode, string folder, string configName, string extension)
        {
            string normalPath = Path.Combine($"Packages/cn.etetet.excel/Bundles/Luban/Config/{codeMode}/{folder}/{configName}.{extension}");
            if (File.Exists(normalPath))
            {
                return normalPath;
            }

            string etLowerPath = Path.Combine($"Packages/cn.etetet.excel/Bundles/Luban/Config/{codeMode}/{folder}/et_{configName.ToLowerInvariant()}.{extension}");
            return etLowerPath;
        }

        private static async ETTask<TextAsset> LoadConfigAsset(string configName)
        {
            Exception lastException = null;

            foreach (string location in GetConfigLocations(configName))
            {
                try
                {
                    TextAsset asset = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>(location);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
                catch (Exception e)
                {
                    lastException = e;
                }
            }

            string locations = string.Join(", ", GetConfigLocations(configName));
            throw new Exception($"客户端配置资源加载失败: {configName}. 尝试位置: {locations}", lastException);
        }

        private static IEnumerable<string> GetConfigLocations(string configName)
        {
            yield return configName;

            string etLowerName = $"et_{configName.ToLowerInvariant()}";
            if (!string.Equals(configName, etLowerName, StringComparison.Ordinal))
            {
                yield return etLowerName;
            }
        }
    }
}
