using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 玩家存储组件（挂在 Gate 侧 Player 上）
    /// M0 阶段：记录上一局撤离带出的物品和财富，供下一局 UI 展示
    /// </summary>
    [ComponentOf(typeof(Player))]
    public class PlayerStorageComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 上一局撤离带出的物品列表（ConfigId -> Count）
        /// </summary>
        public Dictionary<int, int> LastEvacuationItems = new();

        /// <summary>
        /// 上一局撤离带出的总财富值
        /// </summary>
        public long LastEvacuationWealth;

        /// <summary>
        /// 累计总财富（跨局累加）
        /// </summary>
        public long TotalWealth;
    }
}
