using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    // ==================== 数据结构 ====================
    [MemoryPackable]
    [Message(Opcode.HomeBuildingInfo)]
    public partial class HomeBuildingInfo : MessageObject
    {
        public static HomeBuildingInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HomeBuildingInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long BuildingId { get; set; }
        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int Level { get; set; }
        [MemoryPackOrder(3)]
        public int State { get; set; }
        [MemoryPackOrder(4)]
        public int SlotId { get; set; }
        [MemoryPackOrder(5)]
        public long LastCollectTime { get; set; }
        [MemoryPackOrder(6)]
        public long LastProductionTime { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.BuildingId = default;
            this.ConfigId = default;
            this.Level = default;
            this.State = default;
            this.SlotId = default;
            this.LastCollectTime = default;
            this.LastProductionTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.HomeProductionOrderInfo)]
    public partial class HomeProductionOrderInfo : MessageObject
    {
        public static HomeProductionOrderInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HomeProductionOrderInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long OrderId { get; set; }
        [MemoryPackOrder(1)]
        public long BuildingEntityId { get; set; }
        [MemoryPackOrder(2)]
        public int RecipeId { get; set; }
        [MemoryPackOrder(3)]
        public int State { get; set; }
        [MemoryPackOrder(4)]
        public long StartTime { get; set; }
        [MemoryPackOrder(5)]
        public long FinishTime { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.OrderId = default;
            this.BuildingEntityId = default;
            this.RecipeId = default;
            this.State = default;
            this.StartTime = default;
            this.FinishTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.HomeContractInfo)]
    public partial class HomeContractInfo : MessageObject
    {
        public static HomeContractInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HomeContractInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long ContractId { get; set; }
        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int State { get; set; }
        [MemoryPackOrder(3)]
        public long AcceptTime { get; set; }
        [MemoryPackOrder(4)]
        public long ExpireTime { get; set; }
        [MemoryPackOrder(5)]
        public int Progress { get; set; }
        [MemoryPackOrder(6)]
        public int Target { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ContractId = default;
            this.ConfigId = default;
            this.State = default;
            this.AcceptTime = default;
            this.ExpireTime = default;
            this.Progress = default;
            this.Target = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.HomeSlotInfo)]
    public partial class HomeSlotInfo : MessageObject
    {
        public static HomeSlotInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HomeSlotInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int SlotId { get; set; }
        [MemoryPackOrder(1)]
        public int SlotType { get; set; }
        [MemoryPackOrder(2)]
        public string SceneAnchorKey { get; set; }
        [MemoryPackOrder(3)]
        public bool Unlocked { get; set; }
        [MemoryPackOrder(4)]
        public List<int> CanBuildTypes { get; set; } = new();

        [MemoryPackOrder(5)]
        public int SortOrder { get; set; }
        [MemoryPackOrder(6)]
        public long BuildingId { get; set; }
        [MemoryPackOrder(7)]
        public int BuildingConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SlotId = default;
            this.SlotType = default;
            this.SceneAnchorKey = default;
            this.Unlocked = default;
            this.CanBuildTypes.Clear();
            this.SortOrder = default;
            this.BuildingId = default;
            this.BuildingConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.HomeMainCitySummary)]
    public partial class HomeMainCitySummary : MessageObject
    {
        public static HomeMainCitySummary Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HomeMainCitySummary>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long BuildingId { get; set; }
        [MemoryPackOrder(1)]
        public int BuildingConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int Level { get; set; }
        [MemoryPackOrder(3)]
        public int MaxLevel { get; set; }
        [MemoryPackOrder(4)]
        public int UnlockedSlotCount { get; set; }
        [MemoryPackOrder(5)]
        public int OtherBuildingMaxLevel { get; set; }
        [MemoryPackOrder(6)]
        public int TaskGroupId { get; set; }
        [MemoryPackOrder(7)]
        public int NextUpgradeGoldCost { get; set; }
        [MemoryPackOrder(8)]
        public bool CanUpgrade { get; set; }
        [MemoryPackOrder(9)]
        public string PreviewText { get; set; }
        [MemoryPackOrder(10)]
        public int TaskFinishedCount { get; set; }
        [MemoryPackOrder(11)]
        public int TaskTotalCount { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.BuildingId = default;
            this.BuildingConfigId = default;
            this.Level = default;
            this.MaxLevel = default;
            this.UnlockedSlotCount = default;
            this.OtherBuildingMaxLevel = default;
            this.TaskGroupId = default;
            this.NextUpgradeGoldCost = default;
            this.CanUpgrade = default;
            this.PreviewText = default;
            this.TaskFinishedCount = default;
            this.TaskTotalCount = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.HomeWarehouseSummary)]
    public partial class HomeWarehouseSummary : MessageObject
    {
        public static HomeWarehouseSummary Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HomeWarehouseSummary>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int Level { get; set; }
        [MemoryPackOrder(1)]
        public int Capacity { get; set; }
        [MemoryPackOrder(2)]
        public int OccupiedCellCount { get; set; }
        [MemoryPackOrder(3)]
        public int ItemCount { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Level = default;
            this.Capacity = default;
            this.OccupiedCellCount = default;
            this.ItemCount = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.HomeMainCityTaskInfo)]
    public partial class HomeMainCityTaskInfo : MessageObject
    {
        public static HomeMainCityTaskInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HomeMainCityTaskInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int TaskId { get; set; }
        [MemoryPackOrder(1)]
        public int TaskGroupId { get; set; }
        [MemoryPackOrder(2)]
        public int TaskType { get; set; }
        [MemoryPackOrder(3)]
        public int Param1 { get; set; }
        [MemoryPackOrder(4)]
        public int Param2 { get; set; }
        [MemoryPackOrder(5)]
        public string Title { get; set; }
        [MemoryPackOrder(6)]
        public string Desc { get; set; }
        [MemoryPackOrder(7)]
        public int Progress { get; set; }
        [MemoryPackOrder(8)]
        public int Target { get; set; }
        [MemoryPackOrder(9)]
        public bool Completed { get; set; }
        [MemoryPackOrder(10)]
        public int SortOrder { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.TaskId = default;
            this.TaskGroupId = default;
            this.TaskType = default;
            this.Param1 = default;
            this.Param2 = default;
            this.Title = default;
            this.Desc = default;
            this.Progress = default;
            this.Target = default;
            this.Completed = default;
            this.SortOrder = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.HomeMuseumDisplayInfo)]
    public partial class HomeMuseumDisplayInfo : MessageObject
    {
        public static HomeMuseumDisplayInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HomeMuseumDisplayInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long DisplayId { get; set; }
        [MemoryPackOrder(1)]
        public long BuildingId { get; set; }
        [MemoryPackOrder(2)]
        public int SlotIndex { get; set; }
        [MemoryPackOrder(3)]
        public int ItemConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.DisplayId = default;
            this.BuildingId = default;
            this.SlotIndex = default;
            this.ItemConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    // ==================== 进入Home快照 ====================
    [MemoryPackable]
    [Message(Opcode.M2C_HomeSnapshot)]
    public partial class M2C_HomeSnapshot : MessageObject, IMessage
    {
        public static M2C_HomeSnapshot Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeSnapshot>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long Id { get; set; }
        [MemoryPackOrder(1)]
        public long HomeVersion { get; set; }
        [MemoryPackOrder(2)]
        public long LastSettleTime { get; set; }
        [MemoryPackOrder(3)]
        public List<int> UnlockedBuildingConfigIds { get; set; } = new();

        [MemoryPackOrder(4)]
        public List<HomeBuildingInfo> Buildings { get; set; } = new();

        [MemoryPackOrder(5)]
        public List<HomeProductionOrderInfo> ProductionOrders { get; set; } = new();

        [MemoryPackOrder(6)]
        public List<HomeContractInfo> AvailableContracts { get; set; } = new();

        [MemoryPackOrder(7)]
        public List<HomeContractInfo> ActiveContracts { get; set; } = new();

        [MemoryPackOrder(8)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(9)]
        public HomeMainCitySummary MainCitySummary { get; set; }
        [MemoryPackOrder(10)]
        public List<HomeSlotInfo> Slots { get; set; } = new();

        [MemoryPackOrder(11)]
        public HomeWarehouseSummary WarehouseSummary { get; set; }
        [MemoryPackOrder(12)]
        public List<HomeMainCityTaskInfo> MainCityTasks { get; set; } = new();

        [MemoryPackOrder(13)]
        public List<HomeMuseumDisplayInfo> MuseumDisplays { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.HomeVersion = default;
            this.LastSettleTime = default;
            this.UnlockedBuildingConfigIds.Clear();
            this.Buildings.Clear();
            this.ProductionOrders.Clear();
            this.AvailableContracts.Clear();
            this.ActiveContracts.Clear();
            this.TotalWealth = default;
            this.MainCitySummary = default;
            this.Slots.Clear();
            this.WarehouseSummary = default;
            this.MainCityTasks.Clear();
            this.MuseumDisplays.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // ==================== 建造 ====================
    [MemoryPackable]
    [Message(Opcode.C2M_HomeBuildRequest)]
    [ResponseType(nameof(M2C_HomeBuildResponse))]
    public partial class C2M_HomeBuildRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeBuildRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeBuildRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int SlotId { get; set; }
        [MemoryPackOrder(2)]
        public int ConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SlotId = default;
            this.ConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeBuildResponse)]
    public partial class M2C_HomeBuildResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeBuildResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeBuildResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public HomeBuildingInfo Building { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Building = default;

            ObjectPool.Recycle(this);
        }
    }

    // ==================== 升级 ====================
    [MemoryPackable]
    [Message(Opcode.C2M_HomeUpgradeRequest)]
    [ResponseType(nameof(M2C_HomeUpgradeResponse))]
    public partial class C2M_HomeUpgradeRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeUpgradeRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeUpgradeRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long BuildingId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.BuildingId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeUpgradeResponse)]
    public partial class M2C_HomeUpgradeResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeUpgradeResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeUpgradeResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public HomeBuildingInfo Building { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Building = default;

            ObjectPool.Recycle(this);
        }
    }

    // ==================== 拆除 ====================
    [MemoryPackable]
    [Message(Opcode.C2M_HomeDemolishRequest)]
    [ResponseType(nameof(M2C_HomeDemolishResponse))]
    public partial class C2M_HomeDemolishRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeDemolishRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeDemolishRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long BuildingId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.BuildingId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeDemolishResponse)]
    public partial class M2C_HomeDemolishResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeDemolishResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeDemolishResponse>(isFromPool);
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

    // ==================== 收取 ====================
    [MemoryPackable]
    [Message(Opcode.C2M_HomeCollectRequest)]
    [ResponseType(nameof(M2C_HomeCollectResponse))]
    public partial class C2M_HomeCollectRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeCollectRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeCollectRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long BuildingId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.BuildingId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeCollectResponse)]
    public partial class M2C_HomeCollectResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeCollectResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeCollectResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public List<int> ItemConfigIds { get; set; } = new();

        [MemoryPackOrder(4)]
        public List<int> ItemCounts { get; set; } = new();

        [MemoryPackOrder(5)]
        public long WealthDelta { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ItemConfigIds.Clear();
            this.ItemCounts.Clear();
            this.WealthDelta = default;

            ObjectPool.Recycle(this);
        }
    }

    // ==================== 生产 ====================
    [MemoryPackable]
    [Message(Opcode.C2M_HomeStartProductionRequest)]
    [ResponseType(nameof(M2C_HomeStartProductionResponse))]
    public partial class C2M_HomeStartProductionRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeStartProductionRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeStartProductionRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long BuildingId { get; set; }
        [MemoryPackOrder(2)]
        public int RecipeId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.BuildingId = default;
            this.RecipeId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeStartProductionResponse)]
    public partial class M2C_HomeStartProductionResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeStartProductionResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeStartProductionResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public HomeProductionOrderInfo Order { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Order = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_HomeCollectProductionRequest)]
    [ResponseType(nameof(M2C_HomeCollectProductionResponse))]
    public partial class C2M_HomeCollectProductionRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeCollectProductionRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeCollectProductionRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long OrderId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.OrderId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeCollectProductionResponse)]
    public partial class M2C_HomeCollectProductionResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeCollectProductionResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeCollectProductionResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public List<int> ItemConfigIds { get; set; } = new();

        [MemoryPackOrder(4)]
        public List<int> ItemCounts { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ItemConfigIds.Clear();
            this.ItemCounts.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // ==================== 收藏馆 ====================
    [MemoryPackable]
    [Message(Opcode.C2M_HomeMuseumPlaceRequest)]
    [ResponseType(nameof(M2C_HomeMuseumPlaceResponse))]
    public partial class C2M_HomeMuseumPlaceRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeMuseumPlaceRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeMuseumPlaceRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long BuildingId { get; set; }
        [MemoryPackOrder(2)]
        public long ItemUid { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.BuildingId = default;
            this.ItemUid = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeMuseumPlaceResponse)]
    public partial class M2C_HomeMuseumPlaceResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeMuseumPlaceResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeMuseumPlaceResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public HomeMuseumDisplayInfo Display { get; set; }
        [MemoryPackOrder(4)]
        public int ItemConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Display = default;
            this.ItemConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_HomeMuseumTakeDownRequest)]
    [ResponseType(nameof(M2C_HomeMuseumTakeDownResponse))]
    public partial class C2M_HomeMuseumTakeDownRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeMuseumTakeDownRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeMuseumTakeDownRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long DisplayId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.DisplayId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeMuseumTakeDownResponse)]
    public partial class M2C_HomeMuseumTakeDownResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeMuseumTakeDownResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeMuseumTakeDownResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long DisplayId { get; set; }
        [MemoryPackOrder(4)]
        public int ItemConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.DisplayId = default;
            this.ItemConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    // ==================== 悬赏合同 ====================
    [MemoryPackable]
    [Message(Opcode.C2M_HomeAcceptContractRequest)]
    [ResponseType(nameof(M2C_HomeAcceptContractResponse))]
    public partial class C2M_HomeAcceptContractRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeAcceptContractRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeAcceptContractRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long ContractId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ContractId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeAcceptContractResponse)]
    public partial class M2C_HomeAcceptContractResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeAcceptContractResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeAcceptContractResponse>(isFromPool);
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
    [Message(Opcode.C2M_HomeCollectContractRewardRequest)]
    [ResponseType(nameof(M2C_HomeCollectContractRewardResponse))]
    public partial class C2M_HomeCollectContractRewardRequest : MessageObject, ILocationRequest
    {
        public static C2M_HomeCollectContractRewardRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_HomeCollectContractRewardRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long ContractId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ContractId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_HomeCollectContractRewardResponse)]
    public partial class M2C_HomeCollectContractRewardResponse : MessageObject, ILocationResponse
    {
        public static M2C_HomeCollectContractRewardResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeCollectContractRewardResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public List<int> ItemConfigIds { get; set; } = new();

        [MemoryPackOrder(4)]
        public List<int> ItemCounts { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ItemConfigIds.Clear();
            this.ItemCounts.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // 合同列表变更推送
    [MemoryPackable]
    [Message(Opcode.M2C_HomeContractsChanged)]
    public partial class M2C_HomeContractsChanged : MessageObject, IMessage
    {
        public static M2C_HomeContractsChanged Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_HomeContractsChanged>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long Id { get; set; }
        [MemoryPackOrder(1)]
        public List<HomeContractInfo> AvailableContracts { get; set; } = new();

        [MemoryPackOrder(2)]
        public List<HomeContractInfo> ActiveContracts { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.AvailableContracts.Clear();
            this.ActiveContracts.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // ==================== Map <-> Gate 家园仓储桥接 ====================
    [MemoryPackable]
    [Message(Opcode.Map2G_HomeStorageSummaryRequest)]
    [ResponseType(nameof(G2Map_HomeStorageSummaryResponse))]
    public partial class Map2G_HomeStorageSummaryRequest : MessageObject, ILocationRequest
    {
        public static Map2G_HomeStorageSummaryRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2G_HomeStorageSummaryRequest>(isFromPool);
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
    [Message(Opcode.G2Map_HomeStorageSummaryResponse)]
    public partial class G2Map_HomeStorageSummaryResponse : MessageObject, ILocationResponse
    {
        public static G2Map_HomeStorageSummaryResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Map_HomeStorageSummaryResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(4)]
        public int WarehouseItemCount { get; set; }
        [MemoryPackOrder(5)]
        public int WarehouseOccupiedCellCount { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.TotalWealth = default;
            this.WarehouseItemCount = default;
            this.WarehouseOccupiedCellCount = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Map2G_HomeStorageOperateRequest)]
    [ResponseType(nameof(G2Map_HomeStorageOperateResponse))]
    public partial class Map2G_HomeStorageOperateRequest : MessageObject, ILocationRequest
    {
        public static Map2G_HomeStorageOperateRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2G_HomeStorageOperateRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int OperationType { get; set; }
        [MemoryPackOrder(2)]
        public long WealthDelta { get; set; }
        [MemoryPackOrder(3)]
        public int ItemConfigId { get; set; }
        [MemoryPackOrder(4)]
        public int ItemCount { get; set; }
        [MemoryPackOrder(5)]
        public long ItemUid { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.OperationType = default;
            this.WealthDelta = default;
            this.ItemConfigId = default;
            this.ItemCount = default;
            this.ItemUid = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2Map_HomeStorageOperateResponse)]
    public partial class G2Map_HomeStorageOperateResponse : MessageObject, ILocationResponse
    {
        public static G2Map_HomeStorageOperateResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Map_HomeStorageOperateResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(4)]
        public int WarehouseItemCount { get; set; }
        [MemoryPackOrder(5)]
        public int WarehouseOccupiedCellCount { get; set; }
        [MemoryPackOrder(6)]
        public int ResultItemConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.TotalWealth = default;
            this.WarehouseItemCount = default;
            this.WarehouseOccupiedCellCount = default;
            this.ResultItemConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort HomeBuildingInfo = 15601;
        public const ushort HomeProductionOrderInfo = 15602;
        public const ushort HomeContractInfo = 15603;
        public const ushort HomeSlotInfo = 15604;
        public const ushort HomeMainCitySummary = 15605;
        public const ushort HomeWarehouseSummary = 15606;
        public const ushort HomeMainCityTaskInfo = 15607;
        public const ushort HomeMuseumDisplayInfo = 15608;
        public const ushort M2C_HomeSnapshot = 15609;
        public const ushort C2M_HomeBuildRequest = 15610;
        public const ushort M2C_HomeBuildResponse = 15611;
        public const ushort C2M_HomeUpgradeRequest = 15612;
        public const ushort M2C_HomeUpgradeResponse = 15613;
        public const ushort C2M_HomeDemolishRequest = 15614;
        public const ushort M2C_HomeDemolishResponse = 15615;
        public const ushort C2M_HomeCollectRequest = 15616;
        public const ushort M2C_HomeCollectResponse = 15617;
        public const ushort C2M_HomeStartProductionRequest = 15618;
        public const ushort M2C_HomeStartProductionResponse = 15619;
        public const ushort C2M_HomeCollectProductionRequest = 15620;
        public const ushort M2C_HomeCollectProductionResponse = 15621;
        public const ushort C2M_HomeMuseumPlaceRequest = 15622;
        public const ushort M2C_HomeMuseumPlaceResponse = 15623;
        public const ushort C2M_HomeMuseumTakeDownRequest = 15624;
        public const ushort M2C_HomeMuseumTakeDownResponse = 15625;
        public const ushort C2M_HomeAcceptContractRequest = 15626;
        public const ushort M2C_HomeAcceptContractResponse = 15627;
        public const ushort C2M_HomeCollectContractRewardRequest = 15628;
        public const ushort M2C_HomeCollectContractRewardResponse = 15629;
        public const ushort M2C_HomeContractsChanged = 15630;
        public const ushort Map2G_HomeStorageSummaryRequest = 15631;
        public const ushort G2Map_HomeStorageSummaryResponse = 15632;
        public const ushort Map2G_HomeStorageOperateRequest = 15633;
        public const ushort G2Map_HomeStorageOperateResponse = 15634;
    }
}