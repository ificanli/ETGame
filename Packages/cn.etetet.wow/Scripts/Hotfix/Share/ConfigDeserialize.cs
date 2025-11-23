using System;
using Luban;
using SimpleJSON;

namespace ET
{
    [Invoke(ConfigType.Luban)]
    public class ConfigDeserialize_Luban: AInvokeHandler<ConfigDeserialize, object>
    {
        public override object Handle(ConfigDeserialize args)
        {
            return Activator.CreateInstance(args.Type, new Luban.ByteBuf((byte[])args.ConfigBytes));
        }
    }
    
    [Invoke(ConfigType.Json)]
    public class ConfigDeserialize_Json: AInvokeHandler<ConfigDeserialize, object>
    {
        public override object Handle(ConfigDeserialize args)
        {
            JSONNode json = JSON.Parse((string)args.ConfigBytes);
            return Activator.CreateInstance(args.Type, (object)json);
        }
    }
    
    [Invoke(ConfigType.Bson)]
    public class ConfigDeserialize_Bson: AInvokeHandler<ConfigDeserialize, object>
    {
        public override object Handle(ConfigDeserialize args)
        {
            return MongoHelper.FromJson(args.Type, (string)args.ConfigBytes);
        }
    }
}