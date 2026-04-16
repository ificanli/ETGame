using System.Collections.Generic;

namespace ET.Client
{
    [EnableClass]
    public sealed class HomeClientBuildingData
    {
        public long BuildingId;
        public int ConfigId;
        public int Level;
        public int State;
        public int SlotId;
        public long LastCollectTime;
        public long LastProductionTime;
    }

    [EnableClass]
    public sealed class HomeClientSlotData
    {
        public int SlotId;
        public int SlotType;
        public string SceneAnchorKey;
        public bool Unlocked;
        public readonly List<int> CanBuildTypes = new();
        public int SortOrder;
        public long BuildingId;
        public int BuildingConfigId;
    }

    [EnableClass]
    public sealed class HomeClientMainCitySummaryData
    {
        public long BuildingId;
        public int BuildingConfigId;
        public int Level;
        public int MaxLevel;
        public int UnlockedSlotCount;
        public int OtherBuildingMaxLevel;
        public int TaskGroupId;
        public int NextUpgradeGoldCost;
        public bool CanUpgrade;
        public string PreviewText;
        public int TaskFinishedCount;
        public int TaskTotalCount;
    }

    [EnableClass]
    public sealed class HomeClientWarehouseSummaryData
    {
        public int Level;
        public int Capacity;
        public int OccupiedCellCount;
        public int ItemCount;
    }

    [EnableClass]
    public sealed class HomeClientProductionOrderData
    {
        public long OrderId;
        public long BuildingEntityId;
        public int RecipeId;
        public int State;
        public long StartTime;
        public long FinishTime;
    }

    [EnableClass]
    public sealed class HomeClientMainCityTaskData
    {
        public int TaskId;
        public int TaskGroupId;
        public int TaskType;
        public int Param1;
        public int Param2;
        public string Title;
        public string Desc;
        public int Progress;
        public int Target;
        public bool Completed;
        public int SortOrder;
    }

    [EnableClass]
    public sealed class HomeClientMuseumDisplayData
    {
        public long DisplayId;
        public long BuildingId;
        public int SlotIndex;
        public int ItemConfigId;
    }

    [EnableClass]
    public sealed class HomeClientContractData
    {
        public long ContractId;
        public int ConfigId;
        public int State;
        public long AcceptTime;
        public long ExpireTime;
        public int Progress;
        public int Target;
    }

    [ComponentOf(typeof(Scene))]
    public class HomeClientComponent : Entity, IAwake, IDestroy
    {
        public long HomeVersion;
        public long LastSettleTime;
        public long TotalWealth;

        public HomeClientMainCitySummaryData MainCitySummary = new();
        public HomeClientWarehouseSummaryData WarehouseSummary = new();

        public readonly List<int> UnlockedBuildingConfigIds = new();
        public readonly List<HomeClientSlotData> Slots = new();
        public readonly List<HomeClientBuildingData> Buildings = new();
        public readonly List<HomeClientProductionOrderData> ProductionOrders = new();
        public readonly List<HomeClientMainCityTaskData> MainCityTasks = new();
        public readonly List<HomeClientMuseumDisplayData> MuseumDisplays = new();
        public readonly List<HomeClientContractData> AvailableContracts = new();
        public readonly List<HomeClientContractData> ActiveContracts = new();
    }
}
