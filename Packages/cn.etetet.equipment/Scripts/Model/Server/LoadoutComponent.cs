using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 起装组件（挂在 Gate 上的 Player 上，存储玩家本局选择的英雄和装备配置）
    /// 在 Gate 阶段创建，通过 C2G_ConfirmLoadout 写入，由 C2G_EnterMapHandler 读取
    /// </summary>
    [ComponentOf(typeof(Player))]
    public class LoadoutComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 选择的英雄配置ID
        /// </summary>
        public int HeroConfigId;

        /// <summary>
        /// 主武器装备配置ID
        /// </summary>
        public int MainWeaponConfigId;

        /// <summary>
        /// 副武器装备配置ID
        /// </summary>
        public int SubWeaponConfigId;

        /// <summary>
        /// 护甲装备配置ID
        /// </summary>
        public int ArmorConfigId;

        /// <summary>
        /// 背包装备配置ID
        /// </summary>
        public int BackpackConfigId;

        /// <summary>
        /// 当前携带背包格子宽
        /// </summary>
        public int BagWidth;

        /// <summary>
        /// 当前携带背包格子高
        /// </summary>
        public int BagHeight;

        /// <summary>
        /// 当前安全格宽
        /// </summary>
        public int SecureWidth;

        /// <summary>
        /// 当前安全格高
        /// </summary>
        public int SecureHeight;

        /// <summary>
        /// 当前携带背包中的二维布局物品
        /// </summary>
        [BsonIgnore]
        public List<LoadoutGridItemInfo> CarriedBagItems = new();

        /// <summary>
        /// 当前安全格中的二维布局物品
        /// </summary>
        [BsonIgnore]
        public List<LoadoutGridItemInfo> CarriedSecureItems = new();

        /// <summary>
        /// 旧版兼容：消耗品配置ID列表
        /// </summary>
        [BsonIgnore]
        public List<int> ConsumableConfigIds = new();

        /// <summary>
        /// 是否已确认起装（只有 IsConfirmed=true 才允许进入地图）
        /// </summary>
        public bool IsConfirmed;

        /// <summary>
        /// 最近一次确认时间
        /// </summary>
        public long ConfirmedAt;
    }
}
