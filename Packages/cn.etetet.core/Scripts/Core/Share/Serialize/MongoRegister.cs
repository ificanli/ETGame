using System;
using System.Collections.Generic;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace ET
{
    public static class MongoRegister
    {
        public static void RegisterStruct<T>() where T : struct
        {
            BsonSerializer.RegisterSerializer(typeof (T), new StructBsonSerialize<T>());
        }
        
        public static void Init()
        {
            // 清理老的数据
            MethodInfo createSerializerRegistry = typeof (BsonSerializer).GetMethod("CreateSerializerRegistry", BindingFlags.Static | BindingFlags.NonPublic);
            createSerializerRegistry.Invoke(null, Array.Empty<object>());
            MethodInfo registerIdGenerators = typeof (BsonSerializer).GetMethod("RegisterIdGenerators", BindingFlags.Static | BindingFlags.NonPublic);
            registerIdGenerators.Invoke(null, Array.Empty<object>());
            
            ObjectSerializer objectSerializer = new(_ => true);
            BsonSerializer.RegisterSerializer(objectSerializer);
            
            BsonSerializer.RegisterSerializer(typeof(ComponentsCollection), new BsonComponentsCollectionSerializer());
            BsonSerializer.RegisterSerializer(typeof(ChildrenCollection), new BsonChildrenCollectionSerializer());
            
            // 自动注册IgnoreExtraElements
            ConventionPack conventionPack = new() { new IgnoreExtraElementsConvention(true) };

            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            //RegisterStruct<float2>();
            //RegisterStruct<float3>();
            //RegisterStruct<float4>();
            //RegisterStruct<quaternion>();
            //RegisterStruct<FP>();
            //RegisterStruct<TSVector>();
            //RegisterStruct<TSVector2>();
            //RegisterStruct<TSVector4>();
            //RegisterStruct<TSQuaternion>();
            //RegisterStruct<LSInput>();
#if UNITY_EDITOR
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    RegisterClass(type);
                }
            }
#else
            foreach (Type type in CodeTypes.Instance.GetTypes().Values)
            {
                RegisterClass(type);
            }
#endif
        }

        private static void RegisterClass(Type type)
        {
            if (!type.IsSubclassOf(typeof(Object)))
            {
                return;
            }

            if (type.IsGenericType)
            {
                return;
            }

            if (BsonClassMap.IsClassMapRegistered(type))
            {
                return;
            }

            BsonClassMap cm = new(type);
            cm.AutoMap();
            cm.SetDiscriminator(type.FullName);
            BsonClassMap.RegisterClassMap(cm);
        }
    }
}