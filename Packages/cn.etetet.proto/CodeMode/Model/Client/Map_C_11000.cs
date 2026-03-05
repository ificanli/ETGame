using MemoryPack;
using System.Collections.Generic;

namespace ET
{
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
    [Message(Opcode.MoveInfo)]
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
    [Message(Opcode.PetInfo)]
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
    [Message(Opcode.UnitInfo)]
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
    [Message(Opcode.M2C_CreateUnits)]
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
    [Message(Opcode.M2C_CreateMyUnit)]
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
    [Message(Opcode.M2C_StartSceneChange)]
    public partial class M2C_StartSceneChange : MessageObject, IMessage
    {
        public static M2C_StartSceneChange Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_StartSceneChange>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long SceneId { get; set; }
        [MemoryPackOrder(1)]
        public string SceneName { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SceneId = default;
            this.SceneName = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_RemoveUnits)]
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
    [Message(Opcode.C2M_PathfindingResult)]
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
    [Message(Opcode.C2M_Stop)]
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
    [Message(Opcode.M2C_PathfindingResult)]
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
    [Message(Opcode.M2C_Stop)]
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
    [Message(Opcode.M2C_Error)]
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
    [Message(Opcode.M2C_NumericChange)]
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
    [Message(Opcode.M2C_Turn)]
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
    [Message(Opcode.C2M_ClickUnitRequest)]
    [ResponseType(nameof(M2C_ClickUnitResponse))]
    public partial class C2M_ClickUnitRequest : MessageObject, ILocationRequest
    {
        public static C2M_ClickUnitRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_ClickUnitRequest>(isFromPool);
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
    [Message(Opcode.M2C_ClickUnitResponse)]
    public partial class M2C_ClickUnitResponse : MessageObject, ILocationResponse
    {
        public static M2C_ClickUnitResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ClickUnitResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        /// <summary>
        /// 任务信息
        /// </summary>
        [MemoryPackOrder(4)]
        public List<Show_QuestInfo> questInfo { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.questInfo.Clear();

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

    // ========================= ECA / Container =========================
    [MemoryPackable]
    [Message(Opcode.C2M_ECAInteract)]
    [ResponseType(nameof(M2C_ECAInteract))]
    public partial class C2M_ECAInteract : MessageObject, ILocationRequest
    {
        public static C2M_ECAInteract Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_ECAInteract>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string PointId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.PointId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_ECAInteract)]
    public partial class M2C_ECAInteract : MessageObject, ILocationResponse
    {
        public static M2C_ECAInteract Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ECAInteract>(isFromPool);
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
    [Message(Opcode.C2M_ECASearchCancel)]
    public partial class C2M_ECASearchCancel : MessageObject, ILocationMessage
    {
        public static C2M_ECASearchCancel Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_ECASearchCancel>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string PointId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.PointId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_ECAInteractHint)]
    public partial class M2C_ECAInteractHint : MessageObject, IMessage
    {
        public static M2C_ECAInteractHint Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ECAInteractHint>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public string PointId { get; set; }
        [MemoryPackOrder(1)]
        public bool InRange { get; set; }
        [MemoryPackOrder(2)]
        public int ButtonTextId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PointId = default;
            this.InRange = default;
            this.ButtonTextId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_ECASearchState)]
    public partial class M2C_ECASearchState : MessageObject, IMessage
    {
        public static M2C_ECASearchState Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ECASearchState>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public string PointId { get; set; }
        [MemoryPackOrder(1)]
        public int State { get; set; }
        [MemoryPackOrder(2)]
        public long RemainMs { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PointId = default;
            this.State = default;
            this.RemainMs = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ContainerItemData)]
    public partial class ContainerItemData : MessageObject
    {
        public static ContainerItemData Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ContainerItemData>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int SlotIndex { get; set; }
        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int Count { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SlotIndex = default;
            this.ConfigId = default;
            this.Count = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_ContainerOpen)]
    public partial class M2C_ContainerOpen : MessageObject, IMessage
    {
        public static M2C_ContainerOpen Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ContainerOpen>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public string PointId { get; set; }
        [MemoryPackOrder(1)]
        public int OutputMode { get; set; }
        [MemoryPackOrder(2)]
        public bool IsFirstOpen { get; set; }
        [MemoryPackOrder(3)]
        public List<ContainerItemData> Items { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PointId = default;
            this.OutputMode = default;
            this.IsFirstOpen = default;
            this.Items.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_ContainerUpdate)]
    public partial class M2C_ContainerUpdate : MessageObject, IMessage
    {
        public static M2C_ContainerUpdate Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ContainerUpdate>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public string PointId { get; set; }
        [MemoryPackOrder(1)]
        public List<ContainerItemData> Items { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PointId = default;
            this.Items.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_ContainerTakeItem)]
    [ResponseType(nameof(M2C_ContainerTakeItem))]
    public partial class C2M_ContainerTakeItem : MessageObject, ILocationRequest
    {
        public static C2M_ContainerTakeItem Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_ContainerTakeItem>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string PointId { get; set; }
        [MemoryPackOrder(2)]
        public int SlotIndex { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.PointId = default;
            this.SlotIndex = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_ContainerTakeItem)]
    public partial class M2C_ContainerTakeItem : MessageObject, ILocationResponse
    {
        public static M2C_ContainerTakeItem Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ContainerTakeItem>(isFromPool);
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
    [Message(Opcode.C2M_ContainerTakeAll)]
    [ResponseType(nameof(M2C_ContainerTakeAll))]
    public partial class C2M_ContainerTakeAll : MessageObject, ILocationRequest
    {
        public static C2M_ContainerTakeAll Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_ContainerTakeAll>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string PointId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.PointId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_ContainerTakeAll)]
    public partial class M2C_ContainerTakeAll : MessageObject, ILocationResponse
    {
        public static M2C_ContainerTakeAll Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_ContainerTakeAll>(isFromPool);
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
    [Message(Opcode.C2M_ContainerClose)]
    public partial class C2M_ContainerClose : MessageObject, ILocationMessage
    {
        public static C2M_ContainerClose Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_ContainerClose>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string PointId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.PointId = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort C2G_EnterMap = 11001;
        public const ushort G2C_EnterMap = 11002;
        public const ushort MoveInfo = 11003;
        public const ushort PetInfo = 11004;
        public const ushort UnitInfo = 11005;
        public const ushort M2C_CreateUnits = 11006;
        public const ushort M2C_CreateMyUnit = 11007;
        public const ushort M2C_StartSceneChange = 11008;
        public const ushort M2C_RemoveUnits = 11009;
        public const ushort C2M_PathfindingResult = 11010;
        public const ushort C2M_Stop = 11011;
        public const ushort M2C_PathfindingResult = 11012;
        public const ushort M2C_Stop = 11013;
        public const ushort M2C_Error = 11014;
        public const ushort M2C_NumericChange = 11015;
        public const ushort M2C_Turn = 11016;
        public const ushort C2M_ClickUnitRequest = 11017;
        public const ushort M2C_ClickUnitResponse = 11018;
        public const ushort C2M_TransferMap = 11019;
        public const ushort M2C_TransferMap = 11020;
        public const ushort C2M_ECAInteract = 11021;
        public const ushort M2C_ECAInteract = 11022;
        public const ushort C2M_ECASearchCancel = 11023;
        public const ushort M2C_ECAInteractHint = 11024;
        public const ushort M2C_ECASearchState = 11025;
        public const ushort ContainerItemData = 11026;
        public const ushort M2C_ContainerOpen = 11027;
        public const ushort M2C_ContainerUpdate = 11028;
        public const ushort C2M_ContainerTakeItem = 11029;
        public const ushort M2C_ContainerTakeItem = 11030;
        public const ushort C2M_ContainerTakeAll = 11031;
        public const ushort M2C_ContainerTakeAll = 11032;
        public const ushort C2M_ContainerClose = 11033;
    }
}