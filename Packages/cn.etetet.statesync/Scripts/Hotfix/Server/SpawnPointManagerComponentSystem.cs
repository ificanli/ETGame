using System.Collections.Generic;
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
            self.TeamNextSpawnIndices.Clear();
            self.RogueMerchantSpawnPoints.Clear();
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
                if (config == null || config.GetPointType() != ECAPointType.SpawnPoint)
                {
                    continue;
                }

                int teamId = config.GetTeamId();
                SpawnPointECAConfig spawnPoint = new SpawnPointECAConfig
                {
                    ConfigId = config.ConfigId,
                    Type = config.Type,
                    Params = config.Params != null ? new List<SpawnPointFlowParam>(config.Params.Count) : null,
                    PosX = config.PosX,
                    PosY = config.PosY,
                    PosZ = config.PosZ,
                    PointType = config.PointType,
                    TeamId = config.TeamId,
                    InteractRange = config.InteractRange,
                };

                if (config.Params != null)
                {
                    foreach (FlowParam param in config.Params)
                    {
                        spawnPoint.Params.Add(new SpawnPointFlowParam
                        {
                            Key = param?.Key,
                            Value = param?.Value,
                        });
                    }
                }

                if (spawnPoint.TryGetBoolParam(SpawnPointECAConfig.RogueMerchantSpawnParamKey, out bool isMerchantSpawn) && isMerchantSpawn)
                {
                    self.RogueMerchantSpawnPoints.Add(spawnPoint);
                    Log.Info($"SpawnPointManager: add rogue merchant spawn point configId={config.ConfigId}, pos=({config.PosX},{config.PosY},{config.PosZ})");
                    continue;
                }

                if (!self.TeamSpawnPoints.ContainsKey(teamId))
                {
                    self.TeamSpawnPoints[teamId] = new List<SpawnPointECAConfig>();
                }

                self.TeamSpawnPoints[teamId].Add(spawnPoint);
                Log.Info($"SpawnPointManager: add spawn point configId={config.ConfigId}, team={teamId}, pos=({config.PosX},{config.PosY},{config.PosZ})");
            }

            Log.Info($"SpawnPointManager: Loaded {self.TeamSpawnPoints.Count} team spawn point groups");
            foreach (var kvp in self.TeamSpawnPoints)
            {
                Log.Info($"  Team {kvp.Key}: {kvp.Value.Count} spawn points");
            }

            Log.Info($"SpawnPointManager: Loaded {self.RogueMerchantSpawnPoints.Count} rogue merchant spawn points");
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
        /// 按轮转顺序获取指定小队的下一个出生点配置
        /// </summary>
        public static bool TryGetNextSpawnPoint(this SpawnPointManagerComponent self, int teamId, out SpawnPointECAConfig spawnPoint)
        {
            spawnPoint = default;
            if (!self.TeamSpawnPoints.TryGetValue(teamId, out List<SpawnPointECAConfig> spawnPoints))
            {
                Log.Error($"SpawnPointManager: Team {teamId} has no spawn points");
                return false;
            }

            if (spawnPoints.Count == 0)
            {
                Log.Error($"SpawnPointManager: Team {teamId} spawn point list is empty");
                return false;
            }

            self.TeamNextSpawnIndices.TryGetValue(teamId, out int currentIndex);
            if (currentIndex < 0 || currentIndex >= spawnPoints.Count)
            {
                currentIndex = 0;
            }

            spawnPoint = spawnPoints[currentIndex];
            self.TeamNextSpawnIndices[teamId] = (currentIndex + 1) % spawnPoints.Count;
            return true;
        }

        /// <summary>
        /// 按轮转顺序获取指定小队的下一个出生位置
        /// </summary>
        public static float3 GetSpawnPosition(this SpawnPointManagerComponent self, int teamId)
        {
            return self.TryGetNextSpawnPoint(teamId, out SpawnPointECAConfig spawnPoint)
                ? new float3(spawnPoint.PosX, spawnPoint.PosY, spawnPoint.PosZ)
                : float3.zero;
        }

        public static bool TryGetRandomRogueMerchantSpawnPoint(this SpawnPointManagerComponent self, out SpawnPointECAConfig spawnPoint)
        {
            spawnPoint = default;
            if (self == null || self.RogueMerchantSpawnPoints == null || self.RogueMerchantSpawnPoints.Count == 0)
            {
                return false;
            }

            int index = RandomGenerator.RandomNumber(0, self.RogueMerchantSpawnPoints.Count);
            spawnPoint = self.RogueMerchantSpawnPoints[index];
            return true;
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
