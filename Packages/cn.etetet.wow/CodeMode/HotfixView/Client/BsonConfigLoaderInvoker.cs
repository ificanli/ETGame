using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ET
{
    [Invoke]
    public class BsonGetAllConfigBytes: AInvokeHandler<ConfigLoader.BsonGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>
    {
        public override async ETTask<Dictionary<Type, byte[]>> Handle(ConfigLoader.BsonGetAllConfigBytes args)
        {
            await ETTask.CompletedTask;
            
            Dictionary<Type, byte[]> output = new Dictionary<Type, byte[]>();
            HashSet<Type> configTypes = CodeTypes.Instance.GetTypes(typeof (ConfigAttribute));
            if (!Define.IsEditor)
            {
                foreach (Type type in configTypes)
                {
                    TextAsset v = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>($"{type.Name}.bytes");
                    output[type] = v.bytes;
                }
            }
            else
            {
                const string BsonConfigPath = "Packages/cn.etetet.wow/Bundles/Bson";

                foreach (Type configType in configTypes)
                {
                    string configFilePath = $"{BsonConfigPath}/{configType.Name}.bytes";
                    if (!File.Exists(configFilePath))
                    {
                        continue;
                    }
                    output[configType] = File.ReadAllBytes(configFilePath);
                }
            }
            return output;
        }
    }
    
    [Invoke]
    public class GetOneConfigBytes: AInvokeHandler<ConfigLoader.BsonGetOneConfigBytes, ETTask<byte[]>>
    {
        public override async ETTask<byte[]> Handle(ConfigLoader.BsonGetOneConfigBytes args)
        {
            await ETTask.CompletedTask;
            throw new NotImplementedException();
        }
    }
}