using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    // 装备数据
    [MemoryPackable]
    [Message(Opcode.EquipmentData)]
    public partial class EquipmentData : MessageObject
    {
        public static EquipmentData Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<EquipmentData>(isFromPool);
        }

        /// <summary>
        /// 物品ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long ItemId { get; set; }
        /// <summary>
        /// 装备槽位类型
        /// </summary>
        [MemoryPackOrder(1)]
        public int SlotType { get; set; }
        /// <summary>
        /// 物品配置ID
        /// </summary>
        [MemoryPackOrder(2)]
        public int ConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ItemId = default;
            this.SlotType = default;
            this.ConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    // 更新装备通知（服务器推送）
    [MemoryPackable]
    [Message(Opcode.M2C_UpdateEquipment)]
    public partial class M2C_UpdateEquipment : MessageObject, IMessage
    {
        public static M2C_UpdateEquipment Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_UpdateEquipment>(isFromPool);
        }

        /// <summary>
        /// 装备槽位类型（-1表示卸下装备）
        /// </summary>
        [MemoryPackOrder(0)]
        public int SlotType { get; set; }
        /// <summary>
        /// 物品ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long ItemId { get; set; }
        /// <summary>
        /// 物品配置ID
        /// </summary>
        [MemoryPackOrder(2)]
        public int ConfigId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SlotType = default;
            this.ItemId = default;
            this.ConfigId = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort EquipmentData = 4701;
        public const ushort M2C_UpdateEquipment = 4702;
    }
}