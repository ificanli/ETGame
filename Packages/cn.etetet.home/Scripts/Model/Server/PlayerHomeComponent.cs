using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 基地元信息组件，挂在 Unit 上
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class PlayerHomeComponent : Entity, IAwake, IDestroy, IDeserialize
    {
        /// <summary>
        /// 数据版本号，用于后续数据迁移
        /// </summary>
        public long HomeVersion;

        /// <summary>
        /// 上次离线结算时间戳(ms)
        /// </summary>
        public long LastSettleTime;

        /// <summary>
        /// 已解锁建筑配置Id集合
        /// </summary>
        public HashSet<int> UnlockedBuildingConfigIds = new();

        /// <summary>
        /// 已解锁槽位Id集合。
        /// </summary>
        public HashSet<int> UnlockedSlotIds = new();

        /// <summary>
        /// 主城建筑Id。
        /// </summary>
        public long MainCityBuildingId;

        /// <summary>
        /// 家园镜像的总金币。
        /// </summary>
        public long TotalWealth;

        /// <summary>
        /// 仓库当前物品堆数量。
        /// </summary>
        public int WarehouseItemCount;

        /// <summary>
        /// 仓库当前已占用格子数。
        /// </summary>
        public int WarehouseOccupiedCellCount;

        /// <summary>
        /// 已完成的回收收取次数。
        /// </summary>
        public int RecycleCollectedCount;

        /// <summary>
        /// 已完成的农场收取次数。
        /// </summary>
        public int FarmCollectedCount;

        /// <summary>
        /// 收藏馆已摆放展示单位。
        /// </summary>
        public List<HomeMuseumDisplayData> MuseumDisplays = new();
    }
}
