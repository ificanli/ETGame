using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    // 英雄信息（用于列表展示）
    [MemoryPackable]
    [Message(Opcode.HeroInfoData)]
    public partial class HeroInfoData : MessageObject
    {
        public static HeroInfoData Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<HeroInfoData>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int HeroConfigId { get; set; }
        [MemoryPackOrder(1)]
        public string Name { get; set; }
        [MemoryPackOrder(2)]
        public int UnitConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.HeroConfigId = default;
            this.Name = default;
            this.UnitConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.LoadoutGridItemData)]
    public partial class LoadoutGridItemData : MessageObject
    {
        public static LoadoutGridItemData Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<LoadoutGridItemData>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(1)]
        public int Count { get; set; }
        [MemoryPackOrder(2)]
        public int AnchorSlotIndex { get; set; }
        [MemoryPackOrder(3)]
        public int GridWidth { get; set; }
        [MemoryPackOrder(4)]
        public int GridHeight { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ConfigId = default;
            this.Count = default;
            this.AnchorSlotIndex = default;
            this.GridWidth = default;
            this.GridHeight = default;

            ObjectPool.Recycle(this);
        }
    }

    // 请求可用英雄列表
    [MemoryPackable]
    [Message(Opcode.C2G_GetHeroList)]
    [ResponseType(nameof(G2C_GetHeroList))]
    public partial class C2G_GetHeroList : MessageObject, ISessionRequest
    {
        public static C2G_GetHeroList Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_GetHeroList>(isFromPool);
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
    [Message(Opcode.G2C_GetHeroList)]
    public partial class G2C_GetHeroList : MessageObject, ISessionResponse
    {
        public static G2C_GetHeroList Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_GetHeroList>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public List<HeroInfoData> Heroes { get; set; } = new();

        [MemoryPackOrder(4)]
        public List<int> StorageConfigIds { get; set; } = new();

        [MemoryPackOrder(5)]
        public List<int> StorageCounts { get; set; } = new();

        [MemoryPackOrder(6)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(7)]
        public int CurrentMainWeaponConfigId { get; set; }
        [MemoryPackOrder(8)]
        public int CurrentSubWeaponConfigId { get; set; }
        [MemoryPackOrder(9)]
        public int CurrentArmorConfigId { get; set; }
        [MemoryPackOrder(10)]
        public List<int> CurrentConsumableConfigIds { get; set; } = new();

        [MemoryPackOrder(11)]
        public int CurrentHeroConfigId { get; set; }
        [MemoryPackOrder(12)]
        public int CurrentBackpackConfigId { get; set; }
        [MemoryPackOrder(13)]
        public int CurrentBagWidth { get; set; }
        [MemoryPackOrder(14)]
        public int CurrentBagHeight { get; set; }
        [MemoryPackOrder(15)]
        public int SecureWidth { get; set; }
        [MemoryPackOrder(16)]
        public int SecureHeight { get; set; }
        [MemoryPackOrder(17)]
        public List<LoadoutGridItemData> CurrentBagItems { get; set; } = new();

        [MemoryPackOrder(18)]
        public List<LoadoutGridItemData> CurrentSecureItems { get; set; } = new();

        [MemoryPackOrder(19)]
        public bool IsConfirmed { get; set; }
        [MemoryPackOrder(20)]
        public long ConfirmedAt { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Heroes.Clear();
            this.StorageConfigIds.Clear();
            this.StorageCounts.Clear();
            this.TotalWealth = default;
            this.CurrentMainWeaponConfigId = default;
            this.CurrentSubWeaponConfigId = default;
            this.CurrentArmorConfigId = default;
            this.CurrentConsumableConfigIds.Clear();
            this.CurrentHeroConfigId = default;
            this.CurrentBackpackConfigId = default;
            this.CurrentBagWidth = default;
            this.CurrentBagHeight = default;
            this.SecureWidth = default;
            this.SecureHeight = default;
            this.CurrentBagItems.Clear();
            this.CurrentSecureItems.Clear();
            this.IsConfirmed = default;
            this.ConfirmedAt = default;

            ObjectPool.Recycle(this);
        }
    }

    // 确认起装（英雄+4个装备槽位）
    [MemoryPackable]
    [Message(Opcode.C2G_ConfirmLoadout)]
    [ResponseType(nameof(G2C_ConfirmLoadout))]
    public partial class C2G_ConfirmLoadout : MessageObject, ISessionRequest
    {
        public static C2G_ConfirmLoadout Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_ConfirmLoadout>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int HeroConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int MainWeaponConfigId { get; set; }
        [MemoryPackOrder(3)]
        public int SubWeaponConfigId { get; set; }
        [MemoryPackOrder(4)]
        public int ArmorConfigId { get; set; }
        [MemoryPackOrder(5)]
        public List<int> ConsumableConfigIds { get; set; } = new();

        [MemoryPackOrder(6)]
        public int BackpackConfigId { get; set; }
        [MemoryPackOrder(7)]
        public int BagWidth { get; set; }
        [MemoryPackOrder(8)]
        public int BagHeight { get; set; }
        [MemoryPackOrder(9)]
        public int SecureWidth { get; set; }
        [MemoryPackOrder(10)]
        public int SecureHeight { get; set; }
        [MemoryPackOrder(11)]
        public List<LoadoutGridItemData> FinalBagItems { get; set; } = new();

        [MemoryPackOrder(12)]
        public List<LoadoutGridItemData> FinalSecureItems { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.HeroConfigId = default;
            this.MainWeaponConfigId = default;
            this.SubWeaponConfigId = default;
            this.ArmorConfigId = default;
            this.ConsumableConfigIds.Clear();
            this.BackpackConfigId = default;
            this.BagWidth = default;
            this.BagHeight = default;
            this.SecureWidth = default;
            this.SecureHeight = default;
            this.FinalBagItems.Clear();
            this.FinalSecureItems.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_ConfirmLoadout)]
    public partial class G2C_ConfirmLoadout : MessageObject, ISessionResponse
    {
        public static G2C_ConfirmLoadout Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_ConfirmLoadout>(isFromPool);
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

    // 从仓库取出到当前携带态
    [MemoryPackable]
    [Message(Opcode.C2G_LoadoutTakeFromWarehouse)]
    [ResponseType(nameof(G2C_LoadoutTakeFromWarehouse))]
    public partial class C2G_LoadoutTakeFromWarehouse : MessageObject, ISessionRequest
    {
        public static C2G_LoadoutTakeFromWarehouse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_LoadoutTakeFromWarehouse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int Count { get; set; }
        [MemoryPackOrder(3)]
        public int TargetAreaType { get; set; }
        [MemoryPackOrder(4)]
        public int TargetSlotType { get; set; }
        [MemoryPackOrder(5)]
        public int TargetAnchorSlotIndex { get; set; }
        [MemoryPackOrder(6)]
        public int TargetBagWidth { get; set; }
        [MemoryPackOrder(7)]
        public int TargetBagHeight { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ConfigId = default;
            this.Count = default;
            this.TargetAreaType = default;
            this.TargetSlotType = default;
            this.TargetAnchorSlotIndex = default;
            this.TargetBagWidth = default;
            this.TargetBagHeight = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_LoadoutTakeFromWarehouse)]
    public partial class G2C_LoadoutTakeFromWarehouse : MessageObject, ISessionResponse
    {
        public static G2C_LoadoutTakeFromWarehouse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_LoadoutTakeFromWarehouse>(isFromPool);
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

    // 从当前携带态放回仓库
    [MemoryPackable]
    [Message(Opcode.C2G_LoadoutPutToWarehouse)]
    [ResponseType(nameof(G2C_LoadoutPutToWarehouse))]
    public partial class C2G_LoadoutPutToWarehouse : MessageObject, ISessionRequest
    {
        public static C2G_LoadoutPutToWarehouse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_LoadoutPutToWarehouse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int SourceAreaType { get; set; }
        [MemoryPackOrder(2)]
        public int SourceSlotType { get; set; }
        [MemoryPackOrder(3)]
        public int SourceAnchorSlotIndex { get; set; }
        [MemoryPackOrder(4)]
        public int Count { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SourceAreaType = default;
            this.SourceSlotType = default;
            this.SourceAnchorSlotIndex = default;
            this.Count = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_LoadoutPutToWarehouse)]
    public partial class G2C_LoadoutPutToWarehouse : MessageObject, ISessionResponse
    {
        public static G2C_LoadoutPutToWarehouse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_LoadoutPutToWarehouse>(isFromPool);
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

    // 当前携带态内部移动
    [MemoryPackable]
    [Message(Opcode.C2G_LoadoutMoveOwnedItem)]
    [ResponseType(nameof(G2C_LoadoutMoveOwnedItem))]
    public partial class C2G_LoadoutMoveOwnedItem : MessageObject, ISessionRequest
    {
        public static C2G_LoadoutMoveOwnedItem Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_LoadoutMoveOwnedItem>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int SourceAreaType { get; set; }
        [MemoryPackOrder(2)]
        public int SourceSlotType { get; set; }
        [MemoryPackOrder(3)]
        public int SourceAnchorSlotIndex { get; set; }
        [MemoryPackOrder(4)]
        public int TargetAreaType { get; set; }
        [MemoryPackOrder(5)]
        public int TargetSlotType { get; set; }
        [MemoryPackOrder(6)]
        public int TargetAnchorSlotIndex { get; set; }
        [MemoryPackOrder(7)]
        public int TargetBagWidth { get; set; }
        [MemoryPackOrder(8)]
        public int TargetBagHeight { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SourceAreaType = default;
            this.SourceSlotType = default;
            this.SourceAnchorSlotIndex = default;
            this.TargetAreaType = default;
            this.TargetSlotType = default;
            this.TargetAnchorSlotIndex = default;
            this.TargetBagWidth = default;
            this.TargetBagHeight = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_LoadoutMoveOwnedItem)]
    public partial class G2C_LoadoutMoveOwnedItem : MessageObject, ISessionResponse
    {
        public static G2C_LoadoutMoveOwnedItem Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_LoadoutMoveOwnedItem>(isFromPool);
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

    // 一键卸下
    [MemoryPackable]
    [Message(Opcode.C2G_LoadoutOneKeyUnload)]
    [ResponseType(nameof(G2C_LoadoutOneKeyUnload))]
    public partial class C2G_LoadoutOneKeyUnload : MessageObject, ISessionRequest
    {
        public static C2G_LoadoutOneKeyUnload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_LoadoutOneKeyUnload>(isFromPool);
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
    [Message(Opcode.G2C_LoadoutOneKeyUnload)]
    public partial class G2C_LoadoutOneKeyUnload : MessageObject, ISessionResponse
    {
        public static G2C_LoadoutOneKeyUnload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_LoadoutOneKeyUnload>(isFromPool);
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

    // 当前携带态与仓库快照变化推送
    [MemoryPackable]
    [Message(Opcode.G2C_LoadoutStateChanged)]
    public partial class G2C_LoadoutStateChanged : MessageObject, IMessage
    {
        public static G2C_LoadoutStateChanged Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_LoadoutStateChanged>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public List<int> StorageConfigIds { get; set; } = new();

        [MemoryPackOrder(1)]
        public List<int> StorageCounts { get; set; } = new();

        [MemoryPackOrder(2)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(3)]
        public int CurrentHeroConfigId { get; set; }
        [MemoryPackOrder(4)]
        public int CurrentMainWeaponConfigId { get; set; }
        [MemoryPackOrder(5)]
        public int CurrentSubWeaponConfigId { get; set; }
        [MemoryPackOrder(6)]
        public int CurrentArmorConfigId { get; set; }
        [MemoryPackOrder(7)]
        public List<int> CurrentConsumableConfigIds { get; set; } = new();

        [MemoryPackOrder(8)]
        public int CurrentBackpackConfigId { get; set; }
        [MemoryPackOrder(9)]
        public int CurrentBagWidth { get; set; }
        [MemoryPackOrder(10)]
        public int CurrentBagHeight { get; set; }
        [MemoryPackOrder(11)]
        public int SecureWidth { get; set; }
        [MemoryPackOrder(12)]
        public int SecureHeight { get; set; }
        [MemoryPackOrder(13)]
        public List<LoadoutGridItemData> CurrentBagItems { get; set; } = new();

        [MemoryPackOrder(14)]
        public List<LoadoutGridItemData> CurrentSecureItems { get; set; } = new();

        [MemoryPackOrder(15)]
        public bool IsConfirmed { get; set; }
        [MemoryPackOrder(16)]
        public long ConfirmedAt { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.StorageConfigIds.Clear();
            this.StorageCounts.Clear();
            this.TotalWealth = default;
            this.CurrentHeroConfigId = default;
            this.CurrentMainWeaponConfigId = default;
            this.CurrentSubWeaponConfigId = default;
            this.CurrentArmorConfigId = default;
            this.CurrentConsumableConfigIds.Clear();
            this.CurrentBackpackConfigId = default;
            this.CurrentBagWidth = default;
            this.CurrentBagHeight = default;
            this.SecureWidth = default;
            this.SecureHeight = default;
            this.CurrentBagItems.Clear();
            this.CurrentSecureItems.Clear();
            this.IsConfirmed = default;
            this.ConfirmedAt = default;

            ObjectPool.Recycle(this);
        }
    }

    // 撤离结算通知（服务端推送）
    [MemoryPackable]
    [Message(Opcode.M2C_EvacuationSettlement)]
    public partial class M2C_EvacuationSettlement : MessageObject, IMessage
    {
        public static M2C_EvacuationSettlement Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_EvacuationSettlement>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public bool Success { get; set; }
        [MemoryPackOrder(1)]
        public List<ItemData> Items { get; set; } = new();

        [MemoryPackOrder(2)]
        public long TotalWealth { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Items.Clear();
            this.TotalWealth = default;

            ObjectPool.Recycle(this);
        }
    }

    // 死亡结算通知（服务端推送）
    [MemoryPackable]
    [Message(Opcode.M2C_DeathSettlement)]
    public partial class M2C_DeathSettlement : MessageObject, IMessage
    {
        public static M2C_DeathSettlement Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_DeathSettlement>(isFromPool);
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

    // Map 侧向 Gate 侧 Player 发送的撤离结算通知
    [MemoryPackable]
    [Message(Opcode.Map2G_EvacuationSettlement)]
    public partial class Map2G_EvacuationSettlement : MessageObject, IMessage
    {
        public static Map2G_EvacuationSettlement Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2G_EvacuationSettlement>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public bool Success { get; set; }
        [MemoryPackOrder(1)]
        public List<ItemData> Items { get; set; } = new();

        [MemoryPackOrder(2)]
        public long TotalWealth { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Items.Clear();
            this.TotalWealth = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Map2G_LoadoutCarryResult)]
    public partial class Map2G_LoadoutCarryResult : MessageObject, IMessage
    {
        public static Map2G_LoadoutCarryResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2G_LoadoutCarryResult>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int ResultType { get; set; }
        [MemoryPackOrder(1)]
        public int MainWeaponConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int SubWeaponConfigId { get; set; }
        [MemoryPackOrder(3)]
        public int ArmorConfigId { get; set; }
        [MemoryPackOrder(4)]
        public int BackpackConfigId { get; set; }
        [MemoryPackOrder(5)]
        public int BagWidth { get; set; }
        [MemoryPackOrder(6)]
        public int BagHeight { get; set; }
        [MemoryPackOrder(7)]
        public List<LoadoutGridItemData> FinalBagItems { get; set; } = new();

        [MemoryPackOrder(8)]
        public long TotalWealthDelta { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ResultType = default;
            this.MainWeaponConfigId = default;
            this.SubWeaponConfigId = default;
            this.ArmorConfigId = default;
            this.BackpackConfigId = default;
            this.BagWidth = default;
            this.BagHeight = default;
            this.FinalBagItems.Clear();
            this.TotalWealthDelta = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort HeroInfoData = 11101;
        public const ushort LoadoutGridItemData = 11102;
        public const ushort C2G_GetHeroList = 11103;
        public const ushort G2C_GetHeroList = 11104;
        public const ushort C2G_ConfirmLoadout = 11105;
        public const ushort G2C_ConfirmLoadout = 11106;
        public const ushort C2G_LoadoutTakeFromWarehouse = 11107;
        public const ushort G2C_LoadoutTakeFromWarehouse = 11108;
        public const ushort C2G_LoadoutPutToWarehouse = 11109;
        public const ushort G2C_LoadoutPutToWarehouse = 11110;
        public const ushort C2G_LoadoutMoveOwnedItem = 11111;
        public const ushort G2C_LoadoutMoveOwnedItem = 11112;
        public const ushort C2G_LoadoutOneKeyUnload = 11113;
        public const ushort G2C_LoadoutOneKeyUnload = 11114;
        public const ushort G2C_LoadoutStateChanged = 11115;
        public const ushort M2C_EvacuationSettlement = 11116;
        public const ushort M2C_DeathSettlement = 11117;
        public const ushort Map2G_EvacuationSettlement = 11118;
        public const ushort Map2G_LoadoutCarryResult = 11119;
    }
}