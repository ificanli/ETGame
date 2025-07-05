using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    // Spell相关消息定义
    [MemoryPackable]
    [Message(Opcode.C2M_SpellCast)]
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
    [Message(Opcode.M2C_SpellAdd)]
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
    [Message(Opcode.M2C_SpellRemove)]
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
    [Message(Opcode.SpellTarget)]
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

    // Buff相关消息定义
    [MemoryPackable]
    [Message(Opcode.M2C_BuffAdd)]
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
    [Message(Opcode.M2C_BuffUpdate)]
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
    [Message(Opcode.M2C_BuffRemove)]
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

    // CD相关消息定义
    [MemoryPackable]
    [Message(Opcode.M2C_UpdateCD)]
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

    // Pet攻击相关消息定义
    [MemoryPackable]
    [Message(Opcode.C2M_PetAttack)]
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

    public static partial class Opcode
    {
        public const ushort C2M_SpellCast = 4301;
        public const ushort M2C_SpellAdd = 4302;
        public const ushort M2C_SpellRemove = 4303;
        public const ushort SpellTarget = 4304;
        public const ushort M2C_BuffAdd = 4305;
        public const ushort M2C_BuffUpdate = 4306;
        public const ushort M2C_BuffRemove = 4307;
        public const ushort M2C_UpdateCD = 4308;
        public const ushort C2M_PetAttack = 4309;
    }
}