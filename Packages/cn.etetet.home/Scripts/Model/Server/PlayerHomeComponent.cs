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
    }
}
