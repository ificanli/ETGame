using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Luban;
#if DOTNET || UNITY_STANDALONE
using System.Threading.Tasks;
#endif

namespace ET
{
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
            Dictionary<Type, byte[]> configBytes = await EventSystem.Instance.Invoke<ConfigGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>(new ConfigGetAllConfigBytes());

#if DOTNET || UNITY_STANDALONE
            {
                using ListComponent<Task> listTasks = ListComponent<Task>.Create();

                foreach (Type type in configBytes.Keys)
                {
                    byte[] oneConfigBytes = configBytes[type];
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
        }
        
        private void LoadOneConfig(Type configType, byte[] oneConfigBytes)
        {
            object category = null;
            
            ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
            switch(configProcessAttribute.ConfigType)
            {
                case ConfigType.Luban:
                    category = Activator.CreateInstance(configType, new ByteBuf(oneConfigBytes));
                    break;
                case ConfigType.Bson:
                    category = MongoHelper.Deserialize(configType, oneConfigBytes);
                    break;
            }
            
            IConfig iConfig = category as IConfig;
            iConfig.ResolveRef();
            this.allConfig[configType] = iConfig;
            World.Instance.AddSingleton(category as ASingleton);
        }
    }
}