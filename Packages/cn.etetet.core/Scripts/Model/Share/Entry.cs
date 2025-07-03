using System;
using Unity.Mathematics;

namespace ET
{
    public struct EntryEvent1
    {
    }   
    
    public struct EntryEvent2
    {
    } 
    
    public struct EntryEvent3
    {
    }
    
    public static class Entry
    {
        public static void Init()
        {
            
        }
        
        public static void Start()
        {
            StartAsync().NoContext();
        }
        
        private static async ETTask StartAsync()
        {
            WinPeriod.Init();

            // 注册Mongo type
            MongoRegister.Init();
            
            MemoryPackRegister.Init();
            
            // 注册Entity序列化器
            EntitySerializeRegister.Init();
            
            MongoRegister.RegisterStruct<float2>();
            MongoRegister.RegisterStruct<float3>();
            MongoRegister.RegisterStruct<float4>();
            MongoRegister.RegisterStruct<quaternion>();

            World.Instance.AddSingleton<SceneTypeSingleton, Type>(typeof(SceneType));
            World.Instance.AddSingleton<ObjectPool>();
            World.Instance.AddSingleton<IdGenerater>();
            World.Instance.AddSingleton<OpcodeType>();
            
            World.Instance.AddSingleton<MessageQueue>();
            World.Instance.AddSingleton<NetServices>();
            
            LogMsg logMsg = World.Instance.AddSingleton<LogMsg>();
            logMsg.AddIgnore(typeof(C2G_Ping));
            logMsg.AddIgnore(typeof(G2C_Ping));
            logMsg.AddIgnore(typeof(C2M_PathfindingResult));
            logMsg.AddIgnore(typeof(M2C_PathfindingResult));
            logMsg.AddIgnore(typeof(M2C_Stop));
            logMsg.AddIgnore(typeof(MessageResponse));
            
            // 创建需要reload的code singleton
            CodeTypes.Instance.CodeProcess();
            
            await World.Instance.AddSingleton<ConfigLoader>().LoadAsync();
            World.Instance.AddSingleton<NavmeshComponent>();
            
            await FiberManager.CreateRoot(SceneType.Main);
        }
    }
}