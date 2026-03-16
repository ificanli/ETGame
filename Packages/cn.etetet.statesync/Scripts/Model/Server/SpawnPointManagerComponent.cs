using System.Collections.Generic;

namespace ET.Server
{
    [EnableClass]
    public class SpawnPointConfigFile
    {
        public string _t;

        public List<SpawnPointECAConfig> _v = new();
    }

    public struct SpawnPointFlowParam
    {
        public string Key;

        public string Value;
    }

    public struct SpawnPointECAConfig
    {
        public const float DefaultInteractRange = 3f;

        public string ConfigId;

        public int Type;

        public List<SpawnPointFlowParam> Params;

        public float PosX;

        public float PosY;

        public float PosZ;

        public int PointType;

        public int TeamId;

        public float InteractRange;

        public int GetPointType()
        {
            return this.Type != 0 ? this.Type : this.PointType;
        }

        public int GetTeamId()
        {
            if (this.TryGetIntParam("team_id", out int teamId))
            {
                return teamId;
            }

            return this.TeamId;
        }

        private bool TryGetIntParam(string key, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(key) || this.Params == null || this.Params.Count == 0)
            {
                return false;
            }

            foreach (SpawnPointFlowParam param in this.Params)
            {
                if (string.IsNullOrWhiteSpace(param.Key) || !string.Equals(param.Key, key, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return int.TryParse(param.Value, out value);
            }

            return false;
        }
    }

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
        public Dictionary<int, List<SpawnPointECAConfig>> TeamSpawnPoints = new();

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

        /// <summary>
        /// 小队内下一个出生点索引
        /// Key: TeamId
        /// Value: 下次要使用的出生点下标
        /// </summary>
        public Dictionary<int, int> TeamNextSpawnIndices = new();
    }
}
