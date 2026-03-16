using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.M2A_Reload)]
    [ResponseType(nameof(A2M_Reload))]
    public partial class M2A_Reload : MessageObject, IRequest
    {
        public static M2A_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2A_Reload>(isFromPool);
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
    [Message(Opcode.A2M_Reload)]
    public partial class A2M_Reload : MessageObject, IResponse
    {
        public static A2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2M_Reload>(isFromPool);
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

    // 匹配成功后通知 Unit 应用起装数据
    [MemoryPackable]
    [Message(Opcode.A2Map_ApplyLoadoutRequest)]
    [ResponseType(nameof(A2Map_ApplyLoadoutResponse))]
    public partial class A2Map_ApplyLoadoutRequest : MessageObject, ILocationRequest
    {
        public static A2Map_ApplyLoadoutRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2Map_ApplyLoadoutRequest>(isFromPool);
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

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.A2Map_ApplyLoadoutResponse)]
    public partial class A2Map_ApplyLoadoutResponse : MessageObject, ILocationResponse
    {
        public static A2Map_ApplyLoadoutResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2Map_ApplyLoadoutResponse>(isFromPool);
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
        public const ushort M2A_Reload = 20701;
        public const ushort A2M_Reload = 20702;
        public const ushort A2Map_ApplyLoadoutRequest = 20703;
        public const ushort A2Map_ApplyLoadoutResponse = 20704;
    }
}