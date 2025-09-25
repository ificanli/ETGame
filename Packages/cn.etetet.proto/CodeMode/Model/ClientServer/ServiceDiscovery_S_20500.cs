using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.ServiceRegisterRequest)]
    [ResponseType(nameof(ServiceRegisterResponse))]
    public partial class ServiceRegisterRequest : MessageObject, IRequest
    {
        public static ServiceRegisterRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceRegisterRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string SceneName { get; set; }

        [MemoryPackOrder(2)]
        public ActorId ActorId { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(3)]
        public Dictionary<string, string> Metadata { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SceneName = default;
            this.ActorId = default;
            this.Metadata.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceRegisterResponse)]
    public partial class ServiceRegisterResponse : MessageObject, IResponse
    {
        public static ServiceRegisterResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceRegisterResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceUnregisterRequest)]
    [ResponseType(nameof(ServiceUnregisterResponse))]
    public partial class ServiceUnregisterRequest : MessageObject, IRequest
    {
        public static ServiceUnregisterRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceUnregisterRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string SceneName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SceneName = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceUnregisterResponse)]
    public partial class ServiceUnregisterResponse : MessageObject, IResponse
    {
        public static ServiceUnregisterResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceUnregisterResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceHeartbeatRequest)]
    [ResponseType(nameof(ServiceHeartbeatResponse))]
    public partial class ServiceHeartbeatRequest : MessageObject, IRequest
    {
        public static ServiceHeartbeatRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceHeartbeatRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string SceneName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SceneName = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceHeartbeatResponse)]
    public partial class ServiceHeartbeatResponse : MessageObject, IResponse
    {
        public static ServiceHeartbeatResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceHeartbeatResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceQueryRequest)]
    [ResponseType(nameof(ServiceQueryResponse))]
    public partial class ServiceQueryRequest : MessageObject, IRequest
    {
        public static ServiceQueryRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceQueryRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(1)]
        public Dictionary<string, string> Filter { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Filter.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceQueryResponse)]
    public partial class ServiceQueryResponse : MessageObject, IResponse
    {
        public static ServiceQueryResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceQueryResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public List<ServiceInfoProto> Services { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Services.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceQueryBySceneNameRequest)]
    [ResponseType(nameof(ServiceQueryBySceneNameResponse))]
    public partial class ServiceQueryBySceneNameRequest : MessageObject, IRequest
    {
        public static ServiceQueryBySceneNameRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceQueryBySceneNameRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string SceneName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SceneName = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceQueryBySceneNameResponse)]
    public partial class ServiceQueryBySceneNameResponse : MessageObject, IResponse
    {
        public static ServiceQueryBySceneNameResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceQueryBySceneNameResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public ServiceInfoProto Services { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Services = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceSubscribeRequest)]
    [ResponseType(nameof(ServiceSubscribeResponse))]
    public partial class ServiceSubscribeRequest : MessageObject, IRequest
    {
        public static ServiceSubscribeRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceSubscribeRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string SceneName { get; set; }

        [MemoryPackOrder(2)]
        public string FilterName { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(3)]
        public Dictionary<string, string> FilterMetadata { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SceneName = default;
            this.FilterName = default;
            this.FilterMetadata.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceSubscribeResponse)]
    public partial class ServiceSubscribeResponse : MessageObject, IResponse
    {
        public static ServiceSubscribeResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceSubscribeResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceUnsubscribeRequest)]
    [ResponseType(nameof(ServiceUnsubscribeResponse))]
    public partial class ServiceUnsubscribeRequest : MessageObject, IRequest
    {
        public static ServiceUnsubscribeRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceUnsubscribeRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string SceneName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SceneName = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ServiceUnsubscribeResponse)]
    public partial class ServiceUnsubscribeResponse : MessageObject, IResponse
    {
        public static ServiceUnsubscribeResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceUnsubscribeResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    // 服务变更通知消息
    [MemoryPackable]
    [Message(Opcode.ServiceChangeNotification)]
    public partial class ServiceChangeNotification : MessageObject, IMessage
    {
        public static ServiceChangeNotification Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceChangeNotification>(isFromPool);
        }

        /// <summary>
        /// 1=添加, 2=删除
        /// </summary>
        [MemoryPackOrder(0)]
        public int ChangeType { get; set; }

        [MemoryPackOrder(3)]
        public List<ServiceInfoProto> ServiceInfo { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ChangeType = default;
            this.ServiceInfo.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 服务信息Proto定义
    [MemoryPackable]
    [Message(Opcode.ServiceInfoProto)]
    public partial class ServiceInfoProto : MessageObject
    {
        public static ServiceInfoProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ServiceInfoProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public string SceneName { get; set; }

        [MemoryPackOrder(2)]
        public ActorId ActorId { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(4)]
        public Dictionary<string, string> Metadata { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SceneName = default;
            this.ActorId = default;
            this.Metadata.Clear();

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort ServiceRegisterRequest = 20501;
        public const ushort ServiceRegisterResponse = 20502;
        public const ushort ServiceUnregisterRequest = 20503;
        public const ushort ServiceUnregisterResponse = 20504;
        public const ushort ServiceHeartbeatRequest = 20505;
        public const ushort ServiceHeartbeatResponse = 20506;
        public const ushort ServiceQueryRequest = 20507;
        public const ushort ServiceQueryResponse = 20508;
        public const ushort ServiceQueryBySceneNameRequest = 20509;
        public const ushort ServiceQueryBySceneNameResponse = 20510;
        public const ushort ServiceSubscribeRequest = 20511;
        public const ushort ServiceSubscribeResponse = 20512;
        public const ushort ServiceUnsubscribeRequest = 20513;
        public const ushort ServiceUnsubscribeResponse = 20514;
        public const ushort ServiceChangeNotification = 20515;
        public const ushort ServiceInfoProto = 20516;
    }
}