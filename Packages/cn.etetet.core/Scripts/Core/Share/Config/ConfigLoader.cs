using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if DOTNET || UNITY_STANDALONE
using System.Threading.Tasks;
#endif

namespace ET
{
    public struct ConfigDeserialize
    {
        public Type Type;
        public object ConfigBytes;
    }
    
    public class ConfigLoader : Singleton<ConfigLoader>, ISingletonAwake
    {
        public struct ConfigGetAllConfigBytes
        {
        }
        
        private readonly ConcurrentDictionary<Type, IConfig> allConfig = new();

        public void Awake()
        {
        }

        public async ETTask LoadAsync()
        {
            this.allConfig.Clear();

            // load config
            Dictionary<Type, object> configBytes = await EventSystem.Instance.Invoke<ConfigGetAllConfigBytes, ETTask<Dictionary<Type, object>>>(new ConfigGetAllConfigBytes());

#if DOTNET || UNITY_STANDALONE
            {
                using ListComponent<Task> listTasks = ListComponent<Task>.Create();

                foreach (Type type in configBytes.Keys)
                {
                    object oneConfigBytes = configBytes[type];
                    Task task = Task.Run(() => this.LoadOneConfig(type, oneConfigBytes));
                    listTasks.Add(task);
                }

                await Task.WhenAll(listTasks.ToArray());
            }
#else
            foreach (Type type in configBytes.Keys)
            {
                LoadOneConfig(type, configBytes[type]);
            }
#endif
            
            // 处理Ref
            foreach (IConfig config in this.allConfig.Values)
            {
                config.ResolveRef();
            }
        }
        
        private void LoadOneConfig(Type configType, object oneConfigBytes)
        {
            object category = null;
            
            ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
            
            category = EventSystem.Instance.Invoke<ConfigDeserialize, object>(configProcessAttribute.ConfigType, new ConfigDeserialize() {Type = configType, ConfigBytes = oneConfigBytes});
            
            IConfig iConfig = category as IConfig;
            this.allConfig[configType] = iConfig;
            World.Instance.AddSingleton(category as ASingleton);
        }
    }
}