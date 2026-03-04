using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 出生点管理组件
    /// 用于管理多人玩法下的小队出生点分配
    /// 3v3模式：2个小队，每个小队使用一个出生点组
    /// SDC模式：6个小队，每个小队使用一个出生点组
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class SpawnPointManagerComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 小队出生点配置
        /// Key: TeamId (小队ID)
        /// Value: 该小队可用的出生点列表
        /// </summary>
        public Dictionary<int, List<ECAConfig>> TeamSpawnPoints = new();

        /// <summary>
        /// 已被占用的小队ID集合
        /// </summary>
        public HashSet<int> OccupiedTeamIds = new();

        /// <summary>
        /// 玩家到小队的分配记录
        /// Key: PlayerId
        /// Value: TeamId
        /// </summary>
        public Dictionary<long, int> PlayerTeamAssignments = new();
    }
}
