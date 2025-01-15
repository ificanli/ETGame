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
        public struct BsonGetAllConfigBytes
        {
        }

        public struct BsonGetOneConfigBytes
        {
            public string ConfigName;
        }

        public struct LubanGetAllConfigBytes
        {
        }

        public struct LubanGetOneConfigBytes
        {
            public string ConfigName;
        }

        private readonly ConcurrentDictionary<Type, IConfig> m_AllConfig = new();

        public void Awake()
        {
        }

        public async ETTask LoadAsync()
        {
            m_AllConfig.Clear();
            
            // load bson config
            Dictionary<Type, byte[]> bsonConfigBytes = await EventSystem.Instance.Invoke<BsonGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>(new BsonGetAllConfigBytes());
            
#if DOTNET || UNITY_STANDALONE
            {
                using ListComponent<Task> listTasks = ListComponent<Task>.Create();
                foreach (Type type in bsonConfigBytes.Keys)
                {
                    byte[] oneConfigBytes = bsonConfigBytes[type];
                    Task task = Task.Run(() => LoadOneBsonConfig(type, oneConfigBytes));
                    listTasks.Add(task);
                }
                await Task.WhenAll(listTasks.ToArray());
            }
#else
            foreach (Type type in bsonConfigBytes.Keys)
            {
                LoadOneConfig(type, bsonConfigBytes[type]);
            }
#endif

            // load luban config
            Dictionary<Type, ByteBuf> lubanConfigBytes = await EventSystem.Instance.Invoke<LubanGetAllConfigBytes, ETTask<Dictionary<Type, ByteBuf>>>(new LubanGetAllConfigBytes());

#if DOTNET || UNITY_STANDALONE
            {
                using ListComponent<Task> listTasks = ListComponent<Task>.Create();

                foreach (Type type in lubanConfigBytes.Keys)
                {
                    ByteBuf oneConfigBytes = lubanConfigBytes[type];
                    Task task = Task.Run(() => this.LoadOneLubanConfig(type, oneConfigBytes));
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
        
        private void LoadOneBsonConfig(Type configType, byte[] oneConfigBytes)
        {
            object category = MongoHelper.Deserialize(configType, oneConfigBytes);
            IConfig iConfig = category as IConfig;
            iConfig.ResolveRef();
            m_AllConfig[configType] = iConfig;
            World.Instance.AddSingleton(category as ASingleton);
        }

        private void LoadOneLubanConfig(Type configType, ByteBuf oneConfigBytes)
        {
            object category = Activator.CreateInstance(configType, oneConfigBytes);
            IConfig iConfig = category as IConfig;
            iConfig.ResolveRef();
            m_AllConfig[configType] = iConfig;
            World.Instance.AddSingleton(category as ASingleton);
        }
    }
}