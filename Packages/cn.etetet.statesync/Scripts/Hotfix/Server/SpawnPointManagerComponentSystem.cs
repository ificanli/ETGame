using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(SpawnPointManagerComponent))]
    [FriendOf(typeof(SpawnPointManagerComponent))]
    public static partial class SpawnPointManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpawnPointManagerComponent self)
        {
            self.LoadSpawnPoints();
        }

        [EntitySystem]
        private static void Destroy(this SpawnPointManagerComponent self)
        {
            self.TeamSpawnPoints.Clear();
            self.OccupiedTeamIds.Clear();
            self.PlayerTeamAssignments.Clear();
        }

        /// <summary>
        /// 从地图的ECA配置中加载出生点
        /// </summary>
        private static void LoadSpawnPoints(this SpawnPointManagerComponent self)
        {
            Scene scene = self.Scene();

            // 获取地图名称
            string mapName = scene.Name.GetSceneConfigName();
            if (string.IsNullOrEmpty(mapName))
            {
                Log.Error("SpawnPointManager: Scene name is empty");
                return;
            }

            // 从文件加载ECA配置
            string path = $"Packages/cn.etetet.map/Bundles/ECA/{mapName}.txt";
            Log.Info($"SpawnPointManager: load spawn points, scene={scene.Name}, map={mapName}, path={path}");
            if (!System.IO.File.Exists(path))
            {
                Log.Warning($"SpawnPointManager: ECA config file not found: {path}, scene={scene.Name}, map={mapName}");
                return;
            }

            string json = System.IO.File.ReadAllText(path);
            List<ECAConfig> allConfigs = MongoHelper.FromJson<List<ECAConfig>>(json);
            if (allConfigs == null || allConfigs.Count == 0)
            {
                Log.Warning($"SpawnPointManager: No ECA configs loaded from {path}, scene={scene.Name}, map={mapName}");
                return;
            }

            Log.Info($"SpawnPointManager: loaded ECA config count={allConfigs.Count}, scene={scene.Name}, map={mapName}");

            foreach (ECAConfig config in allConfigs)
            {
                if (config.GetPointType() == ECAPointType.SpawnPoint)
                {
                    int teamId = config.GetTeamId();
                    if (!self.TeamSpawnPoints.ContainsKey(teamId))
                    {
                        self.TeamSpawnPoints[teamId] = new List<ECAConfig>();
                    }
                    self.TeamSpawnPoints[teamId].Add(config);
                    Log.Info($"SpawnPointManager: add spawn point configId={config.ConfigId}, team={teamId}, pos=({config.PosX},{config.PosY},{config.PosZ})");
                }
            }

            Log.Info($"SpawnPointManager: Loaded {self.TeamSpawnPoints.Count} team spawn point groups");
            foreach (var kvp in self.TeamSpawnPoints)
            {
                Log.Info($"  Team {kvp.Key}: {kvp.Value.Count} spawn points");
            }
        }

        /// <summary>
        /// 获取一个可用的小队ID（未被占用的出生点组）
        /// </summary>
        public static int GetAvailableTeamId(this SpawnPointManagerComponent self)
        {
            foreach (int teamId in self.TeamSpawnPoints.Keys)
            {
                if (!self.OccupiedTeamIds.Contains(teamId))
                {
                    return teamId;
                }
            }

            Log.Warning("SpawnPointManager: No available team spawn points!");
            return 0; // 返回0表示没有可用的小队出生点
        }

        /// <summary>
        /// 为指定小队分配出生点（标记为已占用）
        /// </summary>
        public static bool AssignTeamSpawnPoint(this SpawnPointManagerComponent self, int teamId)
        {
            if (!self.TeamSpawnPoints.ContainsKey(teamId))
            {
                Log.Error($"SpawnPointManager: Team {teamId} has no spawn points configured");
                return false;
            }

            if (self.OccupiedTeamIds.Contains(teamId))
            {
                Log.Warning($"SpawnPointManager: Team {teamId} spawn point already occupied");
                return false;
            }

            self.OccupiedTeamIds.Add(teamId);
            return true;
        }

        /// <summary>
        /// 获取指定小队的一个随机出生位置
        /// </summary>
        public static float3 GetSpawnPosition(this SpawnPointManagerComponent self, int teamId)
        {
            if (!self.TeamSpawnPoints.TryGetValue(teamId, out List<ECAConfig> spawnPoints))
            {
                Log.Error($"SpawnPointManager: Team {teamId} has no spawn points");
                return float3.zero;
            }

            if (spawnPoints.Count == 0)
            {
                Log.Error($"SpawnPointManager: Team {teamId} spawn point list is empty");
                return float3.zero;
            }

            // 随机选择一个出生点
            int randomIndex = RandomGenerator.RandomNumber(0, spawnPoints.Count);
            ECAConfig spawnPoint = spawnPoints[randomIndex];

            return new float3(spawnPoint.PosX, spawnPoint.PosY, spawnPoint.PosZ);
        }

        /// <summary>
        /// 记录玩家的小队分配
        /// </summary>
        public static void AssignPlayerToTeam(this SpawnPointManagerComponent self, long playerId, int teamId)
        {
            self.PlayerTeamAssignments[playerId] = teamId;
        }

        /// <summary>
        /// 获取玩家所属的小队ID
        /// </summary>
        public static int GetPlayerTeamId(this SpawnPointManagerComponent self, long playerId)
        {
            return self.PlayerTeamAssignments.TryGetValue(playerId, out int teamId) ? teamId : 0;
        }

        /// <summary>
        /// 释放小队出生点（当小队全部离开时）
        /// </summary>
        public static void ReleaseTeamSpawnPoint(this SpawnPointManagerComponent self, int teamId)
        {
            self.OccupiedTeamIds.Remove(teamId);
        }
    }
}
