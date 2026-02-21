using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.C2G_Match)]
    [ResponseType(nameof(G2C_Match))]
    public partial class C2G_Match : MessageObject, ISessionRequest
    {
        public static C2G_Match Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_Match>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_Match)]
    public partial class G2C_Match : MessageObject, ISessionResponse
    {
        public static G2C_Match Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Match>(isFromPool);
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

    /// <summary>
    /// 匹配成功，通知客户端切换场景
    /// </summary>
    [MemoryPackable]
    [Message(Opcode.Match2G_NotifyMatchSuccess)]
    public partial class Match2G_NotifyMatchSuccess : MessageObject, IMessage
    {
        public static Match2G_NotifyMatchSuccess Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Match2G_NotifyMatchSuccess>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        /// <summary>
        /// 房间的ActorId
        /// </summary>
        [MemoryPackOrder(1)]
        public ActorId ActorId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ActorId = default;

            ObjectPool.Recycle(this);
        }
    }

    /// <summary>
    /// 客户端通知房间切换场景完成
    /// </summary>
    [MemoryPackable]
    [Message(Opcode.C2Room_ChangeSceneFinish)]
    public partial class C2Room_ChangeSceneFinish : MessageObject, IRoomMessage
    {
        public static C2Room_ChangeSceneFinish Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2Room_ChangeSceneFinish>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.LockStepUnitInfo)]
    public partial class LockStepUnitInfo : MessageObject
    {
        public static LockStepUnitInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<LockStepUnitInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(1)]
        public TrueSync.TSVector Position { get; set; }
        [MemoryPackOrder(2)]
        public TrueSync.TSQuaternion Rotation { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.Position = default;
            this.Rotation = default;

            ObjectPool.Recycle(this);
        }
    }

    /// <summary>
    /// 房间通知客户端进入战斗
    /// </summary>
    [MemoryPackable]
    [Message(Opcode.Room2C_Start)]
    public partial class Room2C_Start : MessageObject, IMessage
    {
        public static Room2C_Start Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Room2C_Start>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long StartTime { get; set; }
        [MemoryPackOrder(1)]
        public List<LockStepUnitInfo> UnitInfo { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.StartTime = default;
            this.UnitInfo.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.FrameMessage)]
    public partial class FrameMessage : MessageObject, IMessage
    {
        public static FrameMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<FrameMessage>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int Frame { get; set; }
        [MemoryPackOrder(1)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(2)]
        public LSInput Input { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Frame = default;
            this.PlayerId = default;
            this.Input = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.OneFrameInputs)]
    public partial class OneFrameInputs : MessageObject, IMessage
    {
        public static OneFrameInputs Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<OneFrameInputs>(isFromPool);
        }

        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(1)]
        public Dictionary<long, LSInput> Inputs { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Inputs.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Room2C_AdjustUpdateTime)]
    public partial class Room2C_AdjustUpdateTime : MessageObject, IMessage
    {
        public static Room2C_AdjustUpdateTime Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Room2C_AdjustUpdateTime>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int DiffTime { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.DiffTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2Room_CheckHash)]
    public partial class C2Room_CheckHash : MessageObject, IRoomMessage
    {
        public static C2Room_CheckHash Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2Room_CheckHash>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(1)]
        public int Frame { get; set; }
        [MemoryPackOrder(2)]
        public long Hash { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.Frame = default;
            this.Hash = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Room2C_CheckHashFail)]
    public partial class Room2C_CheckHashFail : MessageObject, IMessage
    {
        public static Room2C_CheckHashFail Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Room2C_CheckHashFail>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int Frame { get; set; }
        [MemoryPackOrder(1)]
        public byte[] LSWorldBytes { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Frame = default;
            this.LSWorldBytes = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_Reconnect)]
    public partial class G2C_Reconnect : MessageObject, IMessage
    {
        public static G2C_Reconnect Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Reconnect>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long StartTime { get; set; }
        [MemoryPackOrder(1)]
        public List<LockStepUnitInfo> UnitInfos { get; set; } = new();

        [MemoryPackOrder(2)]
        public int Frame { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.StartTime = default;
            this.UnitInfos.Clear();
            this.Frame = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.RouterSync)]
    public partial class RouterSync : MessageObject
    {
        public static RouterSync Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RouterSync>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public uint ConnectId { get; set; }
        [MemoryPackOrder(1)]
        public string Address { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ConnectId = default;
            this.Address = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2G_EnterMap)]
    [ResponseType(nameof(G2C_EnterMap))]
    public partial class C2G_EnterMap : MessageObject, ISessionRequest
    {
        public static C2G_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_EnterMap>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_EnterMap)]
    public partial class G2C_EnterMap : MessageObject, ISessionResponse
    {
        public static G2C_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_EnterMap>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        /// <summary>
        /// 自己的UnitId
        /// </summary>
        [MemoryPackOrder(3)]
        public long MyId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MyId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_Test)]
    public partial class G2C_Test : MessageObject, ISessionMessage
    {
        public static G2C_Test Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Test>(isFromPool);
        }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_TransferMap)]
    [ResponseType(nameof(M2C_TransferMap))]
    public partial class C2M_TransferMap : MessageObject, ILocationRequest
    {
        public static C2M_TransferMap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TransferMap>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_TransferMap)]
    public partial class M2C_TransferMap : MessageObject, ILocationResponse
    {
        public static M2C_TransferMap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TransferMap>(isFromPool);
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

    public static partial class Opcode
    {
        public const ushort C2G_Match = 11002;
        public const ushort G2C_Match = 11003;
        public const ushort Match2G_NotifyMatchSuccess = 11004;
        public const ushort C2Room_ChangeSceneFinish = 11005;
        public const ushort LockStepUnitInfo = 11006;
        public const ushort Room2C_Start = 11007;
        public const ushort FrameMessage = 11008;
        public const ushort OneFrameInputs = 11009;
        public const ushort Room2C_AdjustUpdateTime = 11010;
        public const ushort C2Room_CheckHash = 11011;
        public const ushort Room2C_CheckHashFail = 11012;
        public const ushort G2C_Reconnect = 11013;
        public const ushort RouterSync = 11014;
        public const ushort C2G_EnterMap = 11015;
        public const ushort G2C_EnterMap = 11016;
        public const ushort G2C_Test = 11017;
        public const ushort C2M_TransferMap = 11018;
        public const ushort M2C_TransferMap = 11019;
    }
}