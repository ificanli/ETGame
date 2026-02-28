using MemoryPack;
using System.Collections.Generic;

namespace ET
{
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

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Heroes?.Clear();

            ObjectPool.Recycle(this);
        }
    }

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
            this.ConsumableConfigIds?.Clear();

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
            this.Items?.Clear();
            this.TotalWealth = default;

            ObjectPool.Recycle(this);
        }
    }

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

    public static partial class Opcode
    {
        public const ushort HeroInfoData = 4801;
        public const ushort C2G_GetHeroList = 4802;
        public const ushort G2C_GetHeroList = 4803;
        public const ushort C2G_ConfirmLoadout = 4804;
        public const ushort G2C_ConfirmLoadout = 4805;
        public const ushort M2C_EvacuationSettlement = 4806;
        public const ushort M2C_DeathSettlement = 4807;
        public const ushort Map2G_EvacuationSettlement = 4808;
    }

    /// <summary>
    /// Map 侧向 Gate 侧 Player 发送的撤离结算通知（Actor 消息）
    /// </summary>
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
            this.Items?.Clear();
            this.TotalWealth = default;

            ObjectPool.Recycle(this);
        }
    }
}

