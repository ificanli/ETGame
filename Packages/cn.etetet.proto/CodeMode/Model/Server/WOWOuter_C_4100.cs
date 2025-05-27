using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(WOWOuter.RouterSync)]
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
    [Message(WOWOuter.C2M_TestRequest)]
    [ResponseType(nameof(M2C_TestResponse))]
    public partial class C2M_TestRequest : MessageObject, ILocationRequest
    {
        public static C2M_TestRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TestRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string request { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.request = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_TestResponse)]
    public partial class M2C_TestResponse : MessageObject, IResponse
    {
        public static M2C_TestResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TestResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public string response { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.response = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2G_EnterMap)]
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
    [Message(WOWOuter.G2C_EnterMap)]
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
    [Message(WOWOuter.MoveInfo)]
    public partial class MoveInfo : MessageObject
    {
        public static MoveInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<MoveInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public List<Unity.Mathematics.float3> Points { get; set; } = new();

        [MemoryPackOrder(1)]
        public Unity.Mathematics.quaternion Rotation { get; set; }

        [MemoryPackOrder(2)]
        public int TurnSpeed { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Points.Clear();
            this.Rotation = default;
            this.TurnSpeed = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.PetInfo)]
    public partial class PetInfo : MessageObject
    {
        public static PetInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<PetInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long OwnerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.OwnerId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.UnitInfo)]
    public partial class UnitInfo : MessageObject
    {
        public static UnitInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<UnitInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }

        [MemoryPackOrder(2)]
        public int Type { get; set; }

        [MemoryPackOrder(3)]
        public Unity.Mathematics.float3 Position { get; set; }

        [MemoryPackOrder(4)]
        public Unity.Mathematics.float3 Forward { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(5)]
        public Dictionary<int, long> KV { get; set; } = new();
        [MemoryPackOrder(6)]
        public MoveInfo MoveInfo { get; set; }

        [MemoryPackOrder(7)]
        public PetInfo PetInfo { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.ConfigId = default;
            this.Type = default;
            this.Position = default;
            this.Forward = default;
            this.KV.Clear();
            this.MoveInfo = default;
            this.PetInfo = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_CreateUnits)]
    public partial class M2C_CreateUnits : MessageObject, IMessage
    {
        public static M2C_CreateUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_CreateUnits>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public List<UnitInfo> Units { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Units.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_CreateMyUnit)]
    public partial class M2C_CreateMyUnit : MessageObject, IMessage
    {
        public static M2C_CreateMyUnit Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_CreateMyUnit>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public UnitInfo Unit { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Unit = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_StartSceneChange)]
    public partial class M2C_StartSceneChange : MessageObject, IMessage
    {
        public static M2C_StartSceneChange Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_StartSceneChange>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long SceneInstanceId { get; set; }

        [MemoryPackOrder(1)]
        public string SceneName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SceneInstanceId = default;
            this.SceneName = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_RemoveUnits)]
    public partial class M2C_RemoveUnits : MessageObject, IMessage
    {
        public static M2C_RemoveUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_RemoveUnits>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public List<long> Units { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Units.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2M_PathfindingResult)]
    public partial class C2M_PathfindingResult : MessageObject, ILocationMessage
    {
        public static C2M_PathfindingResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_PathfindingResult>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public Unity.Mathematics.float3 Position { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Position = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2M_Stop)]
    public partial class C2M_Stop : MessageObject, ILocationMessage
    {
        public static C2M_Stop Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_Stop>(isFromPool);
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
    [Message(WOWOuter.M2C_PathfindingResult)]
    public partial class M2C_PathfindingResult : MessageObject, IMessage
    {
        public static M2C_PathfindingResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_PathfindingResult>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long Id { get; set; }

        [MemoryPackOrder(1)]
        public Unity.Mathematics.float3 Position { get; set; }

        [MemoryPackOrder(2)]
        public List<Unity.Mathematics.float3> Points { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.Position = default;
            this.Points.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_Stop)]
    public partial class M2C_Stop : MessageObject, IMessage
    {
        public static M2C_Stop Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_Stop>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int Error { get; set; }

        [MemoryPackOrder(1)]
        public long Id { get; set; }

        [MemoryPackOrder(2)]
        public Unity.Mathematics.float3 Position { get; set; }

        [MemoryPackOrder(3)]
        public Unity.Mathematics.quaternion Rotation { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Error = default;
            this.Id = default;
            this.Position = default;
            this.Rotation = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.G2C_Test)]
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
    [Message(WOWOuter.C2M_Reload)]
    [ResponseType(nameof(M2C_Reload))]
    public partial class C2M_Reload : MessageObject, ISessionRequest
    {
        public static C2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_Reload>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string Account { get; set; }

        [MemoryPackOrder(2)]
        public string Password { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.Password = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_Reload)]
    public partial class M2C_Reload : MessageObject, ISessionResponse
    {
        public static M2C_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_Reload>(isFromPool);
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
    [Message(WOWOuter.G2C_TestHotfixMessage)]
    public partial class G2C_TestHotfixMessage : MessageObject, ISessionMessage
    {
        public static G2C_TestHotfixMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_TestHotfixMessage>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public string Info { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Info = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2M_TestRobotCase)]
    [ResponseType(nameof(M2C_TestRobotCase))]
    public partial class C2M_TestRobotCase : MessageObject, ILocationRequest
    {
        public static C2M_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TestRobotCase>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.N = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_TestRobotCase)]
    public partial class M2C_TestRobotCase : MessageObject, ILocationResponse
    {
        public static M2C_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TestRobotCase>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.N = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2M_TestRobotCase2)]
    public partial class C2M_TestRobotCase2 : MessageObject, ILocationMessage
    {
        public static C2M_TestRobotCase2 Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TestRobotCase2>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.N = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_TestRobotCase2)]
    public partial class M2C_TestRobotCase2 : MessageObject, ILocationMessage
    {
        public static M2C_TestRobotCase2 Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TestRobotCase2>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.N = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2M_TransferMap)]
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
    [Message(WOWOuter.M2C_TransferMap)]
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

    [MemoryPackable]
    [Message(WOWOuter.C2G_Benchmark)]
    [ResponseType(nameof(G2C_Benchmark))]
    public partial class C2G_Benchmark : MessageObject, ISessionRequest
    {
        public static C2G_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_Benchmark>(isFromPool);
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
    [Message(WOWOuter.G2C_Benchmark)]
    public partial class G2C_Benchmark : MessageObject, ISessionResponse
    {
        public static G2C_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Benchmark>(isFromPool);
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
    [Message(WOWOuter.C2M_SpellCast)]
    public partial class C2M_SpellCast : MessageObject, ILocationMessage
    {
        public static C2M_SpellCast Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_SpellCast>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int SpellConfigId { get; set; }

        [MemoryPackOrder(2)]
        public Unity.Mathematics.float3 TargetPosition { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SpellConfigId = default;
            this.TargetPosition = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_SpellAdd)]
    public partial class M2C_SpellAdd : MessageObject, IMessage
    {
        public static M2C_SpellAdd Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_SpellAdd>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public long SpellId { get; set; }

        [MemoryPackOrder(2)]
        public int SpellConfigId { get; set; }

        [MemoryPackOrder(2)]
        public List<long> TargetUnitId { get; set; } = new();

        [MemoryPackOrder(3)]
        public Unity.Mathematics.float3 TargetPosition { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.SpellId = default;
            this.SpellConfigId = default;
            this.TargetUnitId.Clear();
            this.TargetPosition = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_SpellRemove)]
    public partial class M2C_SpellRemove : MessageObject, IMessage
    {
        public static M2C_SpellRemove Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_SpellRemove>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public long SpellId { get; set; }

        [MemoryPackOrder(2)]
        public int RemoveType { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.SpellId = default;
            this.RemoveType = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.SpellTarget)]
    public partial class SpellTarget : MessageObject
    {
        public static SpellTarget Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<SpellTarget>(isFromPool);
        }

        [MemoryPackOrder(2)]
        public List<long> TargetUnitId { get; set; } = new();

        [MemoryPackOrder(3)]
        public Unity.Mathematics.float3 TargetPosition { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.TargetUnitId.Clear();
            this.TargetPosition = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_BuffAdd)]
    public partial class M2C_BuffAdd : MessageObject, IMessage
    {
        public static M2C_BuffAdd Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_BuffAdd>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public long BuffId { get; set; }

        [MemoryPackOrder(2)]
        public int BuffConfigId { get; set; }

        [MemoryPackOrder(3)]
        public long CreateTime { get; set; }

        [MemoryPackOrder(4)]
        public int TickTime { get; set; }

        [MemoryPackOrder(5)]
        public long ExpireTime { get; set; }

        [MemoryPackOrder(6)]
        public long CasterId { get; set; }

        [MemoryPackOrder(7)]
        public int Stack { get; set; }

        [MemoryPackOrder(8)]
        public SpellTarget SpellTarget { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.BuffId = default;
            this.BuffConfigId = default;
            this.CreateTime = default;
            this.TickTime = default;
            this.ExpireTime = default;
            this.CasterId = default;
            this.Stack = default;
            this.SpellTarget = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_BuffUpdate)]
    public partial class M2C_BuffUpdate : MessageObject, IMessage
    {
        public static M2C_BuffUpdate Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_BuffUpdate>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public long BuffId { get; set; }

        [MemoryPackOrder(4)]
        public int TickTime { get; set; }

        [MemoryPackOrder(5)]
        public long ExpireTime { get; set; }

        [MemoryPackOrder(6)]
        public int Stack { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.BuffId = default;
            this.TickTime = default;
            this.ExpireTime = default;
            this.Stack = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_BuffRemove)]
    public partial class M2C_BuffRemove : MessageObject, IMessage
    {
        public static M2C_BuffRemove Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_BuffRemove>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public long BuffId { get; set; }

        [MemoryPackOrder(2)]
        public int BuffConfigId { get; set; }

        [MemoryPackOrder(3)]
        public int RemoveType { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.BuffId = default;
            this.BuffConfigId = default;
            this.RemoveType = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_Error)]
    public partial class M2C_Error : MessageObject, IMessage
    {
        public static M2C_Error Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_Error>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int Error { get; set; }

        [MemoryPackOrder(1)]
        public List<string> Values { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Error = default;
            this.Values.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_NumericChange)]
    public partial class M2C_NumericChange : MessageObject, IMessage
    {
        public static M2C_NumericChange Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_NumericChange>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public int NumericType { get; set; }

        [MemoryPackOrder(2)]
        public long Value { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.NumericType = default;
            this.Value = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2M_SelectTarget)]
    public partial class C2M_SelectTarget : MessageObject, ILocationMessage
    {
        public static C2M_SelectTarget Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_SelectTarget>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public long TargetUnitId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.TargetUnitId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_Turn)]
    public partial class M2C_Turn : MessageObject, IMessage
    {
        public static M2C_Turn Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_Turn>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public Unity.Mathematics.quaternion Rotation { get; set; }

        [MemoryPackOrder(2)]
        public int TurnTime { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.Rotation = default;
            this.TurnTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2M_PetAttack)]
    public partial class C2M_PetAttack : MessageObject, ILocationMessage
    {
        public static C2M_PetAttack Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_PetAttack>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public long UnitId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.UnitId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.M2C_UpdateCD)]
    public partial class M2C_UpdateCD : MessageObject, IMessage
    {
        public static M2C_UpdateCD Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_UpdateCD>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public long UnitId { get; set; }

        /// <summary>
        /// 0表示公共CD
        /// </summary>
        [MemoryPackOrder(2)]
        public int SpellConfigId { get; set; }

        [MemoryPackOrder(3)]
        public long Time { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.UnitId = default;
            this.SpellConfigId = default;
            this.Time = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(WOWOuter.C2G_Logout)]
    [ResponseType(nameof(G2C_Logout))]
    public partial class C2G_Logout : MessageObject, ISessionRequest
    {
        public static C2G_Logout Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_Logout>(isFromPool);
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
    [Message(WOWOuter.G2C_Logout)]
    public partial class G2C_Logout : MessageObject, ISessionResponse
    {
        public static G2C_Logout Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Logout>(isFromPool);
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

    public static class WOWOuter
    {
        public const ushort RouterSync = 4101;
        public const ushort C2M_TestRequest = 4102;
        public const ushort M2C_TestResponse = 4103;
        public const ushort C2G_EnterMap = 4104;
        public const ushort G2C_EnterMap = 4105;
        public const ushort MoveInfo = 4106;
        public const ushort PetInfo = 4107;
        public const ushort UnitInfo = 4108;
        public const ushort M2C_CreateUnits = 4109;
        public const ushort M2C_CreateMyUnit = 4110;
        public const ushort M2C_StartSceneChange = 4111;
        public const ushort M2C_RemoveUnits = 4112;
        public const ushort C2M_PathfindingResult = 4113;
        public const ushort C2M_Stop = 4114;
        public const ushort M2C_PathfindingResult = 4115;
        public const ushort M2C_Stop = 4116;
        public const ushort G2C_Test = 4117;
        public const ushort C2M_Reload = 4118;
        public const ushort M2C_Reload = 4119;
        public const ushort G2C_TestHotfixMessage = 4120;
        public const ushort C2M_TestRobotCase = 4121;
        public const ushort M2C_TestRobotCase = 4122;
        public const ushort C2M_TestRobotCase2 = 4123;
        public const ushort M2C_TestRobotCase2 = 4124;
        public const ushort C2M_TransferMap = 4125;
        public const ushort M2C_TransferMap = 4126;
        public const ushort C2G_Benchmark = 4127;
        public const ushort G2C_Benchmark = 4128;
        public const ushort C2M_SpellCast = 4129;
        public const ushort M2C_SpellAdd = 4130;
        public const ushort M2C_SpellRemove = 4131;
        public const ushort SpellTarget = 4132;
        public const ushort M2C_BuffAdd = 4133;
        public const ushort M2C_BuffUpdate = 4134;
        public const ushort M2C_BuffRemove = 4135;
        public const ushort M2C_Error = 4136;
        public const ushort M2C_NumericChange = 4137;
        public const ushort C2M_SelectTarget = 4138;
        public const ushort M2C_Turn = 4139;
        public const ushort C2M_PetAttack = 4140;
        public const ushort M2C_UpdateCD = 4141;
        public const ushort C2G_Logout = 4142;
        public const ushort G2C_Logout = 4143;
    }
}