using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 英雄信息（客户端本地存储，避免持有 MessageObject）
    /// </summary>
    public struct HeroInfo
    {
        public int HeroConfigId;
        public string Name;
        public int UnitConfigId;
    }

    /// <summary>
    /// 客户端起装组件（挂在 Scene 上，存储本地选择的起装配置，用于 UI 展示和提交）
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class LoadoutComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前选择的英雄列表（从服务端获取，用 struct 避免持有 MessageObject）
        /// </summary>
        public List<HeroInfo> Heroes = new();

        /// <summary>
        /// 选择的英雄配置ID
        /// </summary>
        public int SelectedHeroConfigId;

        /// <summary>
        /// 主武器配置ID
        /// </summary>
        public int MainWeaponConfigId;

        /// <summary>
        /// 副武器配置ID
        /// </summary>
        public int SubWeaponConfigId;

        /// <summary>
        /// 护甲配置ID
        /// </summary>
        public int ArmorConfigId;

        /// <summary>
        /// 背包装备配置ID
        /// </summary>
        public int BackpackConfigId;

        /// <summary>
        /// 背包格子宽
        /// </summary>
        public int BagWidth;

        /// <summary>
        /// 背包格子高
        /// </summary>
        public int BagHeight;

        /// <summary>
        /// 安全格宽
        /// </summary>
        public int SecureWidth;

        /// <summary>
        /// 安全格高
        /// </summary>
        public int SecureHeight;

        /// <summary>
        /// 当前携带背包中的二维布局物品
        /// </summary>
        public List<LoadoutGridItemInfo> CarriedBagItems = new();

        /// <summary>
        /// 当前安全格中的二维布局物品
        /// </summary>
        public List<LoadoutGridItemInfo> CarriedSecureItems = new();

        /// <summary>
        /// 旧版兼容：消耗品配置ID列表
        /// </summary>
        public List<int> ConsumableConfigIds = new();

        /// <summary>
        /// 局外仓库库存（ConfigId -> Count）
        /// </summary>
        public Dictionary<int, int> StorageItemCounts = new();

        /// <summary>
        /// 局外累计财富
        /// </summary>
        public long TotalWealth;

        /// <summary>
        /// 是否允许首次自由起装
        /// </summary>
        public bool AllowFreeSelection;

        /// <summary>
        /// 是否已确认起装
        /// </summary>
        public bool IsConfirmed;

        /// <summary>
        /// 最近一次确认时间
        /// </summary>
        public long ConfirmedAt;
    }
}
