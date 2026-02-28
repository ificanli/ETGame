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
        /// 消耗品配置ID列表
        /// </summary>
        public List<int> ConsumableConfigIds = new();

        /// <summary>
        /// 是否已确认起装
        /// </summary>
        public bool IsConfirmed;
    }
}
