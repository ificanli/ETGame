using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class M2M_UnitTransferRequestHandler: MessageHandler<Scene, M2M_UnitTransferRequest, M2M_UnitTransferResponse>
    {
        protected override async ETTask Run(Scene scene, M2M_UnitTransferRequest request, M2M_UnitTransferResponse response)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();

            Unit unit = request.Unit;
            if (unit != null)  // 黑科技，直接传送Unit对象
            {
                unitComponent.AddChild(unit);
                unitComponent.Add(unit);
            }
            else
            {
                unit = MongoHelper.Deserialize<Unit>(request.UnitBytes);

                unitComponent.AddChild(unit);
                unitComponent.Add(unit);

                foreach (byte[] bytes in request.EntityBytes)
                {
                    Entity entity = MongoHelper.Deserialize<Entity>(bytes);
                    unit.AddComponent(entity);
                }
            }

            unit.AddComponent<TurnComponent>();
            unit.AddComponent<MoveComponent>();
            string mapName = scene.Name.GetSceneConfigName();
            if (mapName != "Home")
            {
                unit.AddComponent<PathfindingComponent, string>(mapName);
            }
            unit.AddComponent<MailBoxComponent, int>(MailBoxType.OrderedMessage);
            unit.AddComponent<TargetComponent>();
            EnsureCampAndThreatComponent(unit);
            if (request.ChangeScene)
            {
                ApplySpawnPointIfNeeded(scene, unit);
            }

            // 通知客户端开始切场景
            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneId = scene.Id;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.NoticeClient(unit, m2CStartSceneChange, NoticeType.Self);

            if (request.ChangeScene)
            {
                // 通知客户端创建My Unit
                M2C_CreateMyUnit m2CCreateUnits = M2C_CreateMyUnit.Create();
                m2CCreateUnits.Unit = UnitHelper.CreateUnitInfo(unit);
                MapMessageHelper.NoticeClient(unit, m2CCreateUnits, NoticeType.Self);
            }

            // 加入aoi
            unit.AddComponent<AOIEntity>();

            // Unit 已在 Map 场景中，此时初始化武器和英雄技能，确保 Timer 注册在 Map 的 TimerComponent 上
            // （若在 GateMap 初始化，GateMap 销毁后 Timer 会失效，导致 BuffTick 只执行一次）
            if (unit.UnitType == UnitType.Player)
            {
                WeaponInitHelper.InitializeWeaponsFromUnit(unit);

                HeroSkillComponent heroSkill = unit.GetComponent<HeroSkillComponent>();
                if (heroSkill != null)
                {
                    // 组件从 GateMap 传送过来，Awake 不会再触发，需要手动重启 Timer
                    heroSkill.RestartSkillTimer();
                }
            }

            if (request.ChangeScene)
            {
                SetupMatchRobotIfNeeded(scene, unit);
            }

            response.NewActorId = unit.GetActorId();
            await ETTask.CompletedTask;
        }

        private static void ApplySpawnPointIfNeeded(Scene scene, Unit unit)
        {
            if (unit == null)
            {
                return;
            }

            bool isMatchRobot = unit.GetComponent<MatchRobotComponent>() != null;
            if (unit.UnitType != UnitType.Player && !isMatchRobot)
            {
                return;
            }

            SpawnPointManagerComponent spawnPointManager = scene.GetComponent<SpawnPointManagerComponent>();
            if (spawnPointManager == null || spawnPointManager.TeamSpawnPoints.Count == 0)
            {
                Log.Warning($"[SpawnAssign] skip: manager missing or empty, scene={scene.Name}, unitId={unit.Id}, teamGroupCount={spawnPointManager?.TeamSpawnPoints.Count ?? 0}");
                return;
            }

            int teamId = GetOrAssignTeamId(spawnPointManager, unit.Id);
            if (!spawnPointManager.TeamSpawnPoints.TryGetValue(teamId, out List<ECAConfig> spawnPoints) || spawnPoints.Count == 0)
            {
                Log.Warning($"[SpawnAssign] skip: no spawn points for team, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}");
                return;
            }

            int randomIndex = RandomGenerator.RandomNumber(0, spawnPoints.Count);
            ECAConfig spawnPoint = spawnPoints[randomIndex];
            float3 oldPos = unit.Position;
            unit.Position = new float3(spawnPoint.PosX, spawnPoint.PosY, spawnPoint.PosZ);
            Log.Info($"[SpawnAssign] apply spawn point, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}, configId={spawnPoint.ConfigId}, oldPos={oldPos}, newPos={unit.Position}");
        }

        private static int GetOrAssignTeamId(SpawnPointManagerComponent spawnPointManager, long playerId)
        {
            if (spawnPointManager.PlayerTeamAssignments.TryGetValue(playerId, out int assignedTeamId))
            {
                Log.Info($"[SpawnAssign] reuse team assignment, unitId={playerId}, teamId={assignedTeamId}");
                return assignedTeamId;
            }

            List<int> orderedTeamIds = new List<int>(spawnPointManager.TeamSpawnPoints.Keys);
            orderedTeamIds.Sort();

            if (orderedTeamIds.Count == 0)
            {
                Log.Warning($"[SpawnAssign] no team groups configured, unitId={playerId}");
                spawnPointManager.PlayerTeamAssignments[playerId] = 0;
                return 0;
            }

            int teamId = 0;
            bool found = false;
            foreach (int candidateTeamId in orderedTeamIds)
            {
                if (spawnPointManager.OccupiedTeamIds.Contains(candidateTeamId))
                {
                    continue;
                }

                teamId = candidateTeamId;
                found = true;
                break;
            }

            if (!found)
            {
                // 全被占用时兜底复用第一个出生点组，避免无法进入地图
                teamId = orderedTeamIds[0];
                found = true;
                Log.Warning($"[SpawnAssign] all teams occupied, fallback to first team, unitId={playerId}, fallbackTeamId={teamId}");
            }

            if (found)
            {
                spawnPointManager.OccupiedTeamIds.Add(teamId);
            }

            spawnPointManager.PlayerTeamAssignments[playerId] = teamId;
            Log.Info($"[SpawnAssign] assign team, unitId={playerId}, teamId={teamId}, teams=[{string.Join(",", orderedTeamIds)}], occupied=[{string.Join(",", spawnPointManager.OccupiedTeamIds)}]");
            return teamId;
        }

        private static void SetupMatchRobotIfNeeded(Scene scene, Unit unit)
        {
            MatchRobotComponent matchRobot = unit.GetComponent<MatchRobotComponent>();
            if (matchRobot == null)
            {
                return;
            }

            if (!TryResolveRobotRuntimeConfig(scene.Name.GetSceneConfigName(), out int robotCampId, out int robotAIBuffConfigId))
            {
                Log.Warning($"[MatchRobot] setup skipped: no runtime config, unitId={unit.Id}, map={scene.Name}");
                unit.RemoveComponent<MatchRobotComponent>();
                return;
            }

            // 机器人走敌对阵营，避免被判定为友军
            CampComponent camp = unit.GetComponent<CampComponent>();
            if (camp == null || camp.CampId != robotCampId)
            {
                if (camp != null)
                {
                    unit.RemoveComponent<CampComponent>();
                }
                unit.AddComponent<CampComponent, int>(robotCampId);
            }

            // 怪物AI依赖 ThreatComponent
            if (unit.GetComponent<ThreatComponent>() == null)
            {
                unit.AddComponent<ThreatComponent>();
            }

            // 将当前落点写回出生坐标，供AI回归逻辑使用
            NumericComponent numeric = unit.NumericComponent;
            numeric.SetNoEvent(NumericType.X, (long)(unit.Position.x * 1000));
            numeric.SetNoEvent(NumericType.Y, (long)(unit.Position.y * 1000));
            numeric.SetNoEvent(NumericType.Z, (long)(unit.Position.z * 1000));
            numeric.SetNoEvent(NumericType.AI, robotAIBuffConfigId);

            BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), robotAIBuffConfigId, null);
            Log.Info($"[MatchRobot] setup complete: unitId={unit.Id}, pos={unit.Position}, camp={robotCampId}, ai={robotAIBuffConfigId}");

            unit.RemoveComponent<MatchRobotComponent>();
        }

        private static bool TryResolveRobotRuntimeConfig(string mapName, out int robotCampId, out int robotAIBuffConfigId)
        {
            if (TryResolveRobotRuntimeConfigByMap(mapName, out robotCampId, out robotAIBuffConfigId))
            {
                return true;
            }

            return TryResolveRobotRuntimeConfigByUnitCatalog(out robotCampId, out robotAIBuffConfigId);
        }

        private static bool TryResolveRobotRuntimeConfigByMap(string mapName, out int robotCampId, out int robotAIBuffConfigId)
        {
            foreach (MapUnitConfig mapUnitConfig in MapUnitConfigCategory.Instance.GetAll().Values)
            {
                if (mapUnitConfig.MapName != mapName)
                {
                    continue;
                }

                UnitConfig unitConfig = UnitConfigCategory.Instance.GetOrDefault(mapUnitConfig.UnitConfigId);
                if (unitConfig == null || unitConfig.UnitType == UnitType.Player)
                {
                    continue;
                }

                if (!TryResolveAIBuffConfigId(mapUnitConfig, unitConfig, out robotAIBuffConfigId))
                {
                    continue;
                }

                robotCampId = GetDefaultCampId(unitConfig.UnitType);
                return true;
            }

            robotCampId = 0;
            robotAIBuffConfigId = 0;
            return false;
        }

        private static bool TryResolveRobotRuntimeConfigByUnitCatalog(out int robotCampId, out int robotAIBuffConfigId)
        {
            foreach (UnitConfig unitConfig in UnitConfigCategory.Instance.GetAll().Values)
            {
                if (unitConfig.UnitType == UnitType.Player)
                {
                    continue;
                }

                if (!TryResolveAIBuffConfigId(null, unitConfig, out robotAIBuffConfigId))
                {
                    continue;
                }

                robotCampId = GetDefaultCampId(unitConfig.UnitType);
                return true;
            }

            robotCampId = 0;
            robotAIBuffConfigId = 0;
            return false;
        }

        private static bool TryResolveAIBuffConfigId(MapUnitConfig mapUnitConfig, UnitConfig unitConfig, out int aiBuffConfigId)
        {
            aiBuffConfigId = 0;

            if (mapUnitConfig != null &&
                mapUnitConfig.KV.TryGetValue(NumericType.AI, out long mapAI) &&
                mapAI > 0)
            {
                aiBuffConfigId = (int)mapAI;
                return true;
            }

            if (unitConfig != null &&
                unitConfig.KV.TryGetValue(NumericType.AI, out long unitAI) &&
                unitAI > 0)
            {
                aiBuffConfigId = (int)unitAI;
                return true;
            }

            return false;
        }

        private static int GetDefaultCampId(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Player => 1,
                UnitType.Monster => 2,
                _ => 2,
            };
        }

        private static void EnsureCampAndThreatComponent(Unit unit)
        {
            switch (unit.UnitType)
            {
                case UnitType.Player:
                    if (unit.GetComponent<CampComponent>() == null)
                    {
                        unit.AddComponent<CampComponent, int>(1);
                    }
                    break;
                case UnitType.Monster:
                    if (unit.GetComponent<CampComponent>() == null)
                    {
                        unit.AddComponent<CampComponent, int>(2);
                    }
                    if (unit.GetComponent<ThreatComponent>() == null)
                    {
                        unit.AddComponent<ThreatComponent>();
                    }
                    break;
            }
        }
    }
}
