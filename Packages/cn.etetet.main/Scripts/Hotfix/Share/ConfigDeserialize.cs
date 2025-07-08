using System;
using Luban;

namespace ET
{
    [Invoke(ConfigType.Luban)]
    public class ConfigDeserialize_Luban: AInvokeHandler<ConfigDeserialize, object>
    {
        public override object Handle(ConfigDeserialize args)
        {
            return Activator.CreateInstance(args.Type, new Luban.ByteBuf(args.ConfigBytes));
        }
    }
    
    [Invoke(ConfigType.Bson)]
    public class ConfigDeserialize_Bson: AInvokeHandler<ConfigDeserialize, object>
    {
        public override object Handle(ConfigDeserialize args)
        {
            return MongoHelper.Deserialize(args.Type, args.ConfigBytes);
        }
    }
}