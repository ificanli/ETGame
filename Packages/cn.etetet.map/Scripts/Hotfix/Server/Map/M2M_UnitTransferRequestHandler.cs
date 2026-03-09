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
            if (unit != null)  // 姒涙垹顫栭幎鈧敍宀€娲块幒銉ょ炊闁箒nit鐎电钖?
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

            // 闁氨鐓＄€广垺鍩涚粩顖氱磻婵鍨忛崷鐑樻珯
            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneId = scene.Id;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.NoticeClient(unit, m2CStartSceneChange, NoticeType.Self);

            if (request.ChangeScene)
            {
                // 闁氨鐓＄€广垺鍩涚粩顖氬灡瀵ょ瘲y Unit
                M2C_CreateMyUnit m2CCreateUnits = M2C_CreateMyUnit.Create();
                m2CCreateUnits.Unit = UnitHelper.CreateUnitInfo(unit);
                MapMessageHelper.NoticeClient(unit, m2CCreateUnits, NoticeType.Self);
            }

            // 閸旂姴鍙哸oi
            unit.AddComponent<AOIEntity>();

            // Unit 瀹告彃婀?Map 閸︾儤娅欐稉顓ㄧ礉濮濄倖妞傞崚婵嗩潗閸栨牗顒熼崳銊ユ嫲閼婚亶娉熼幎鈧懗鏂ょ礉绾喕绻?Timer 濞夈劌鍞介崷?Map 閻?TimerComponent 娑?
            // 閿涘牐瀚㈤崷?GateMap 閸掓繂顫愰崠鏍电礉GateMap 闁库偓濮ｄ礁鎮?Timer 娴兼艾銇戦弫鍫礉鐎佃壈鍤?BuffTick 閸欘亝澧界悰灞肩濞嗏槄绱?
            if (unit.UnitType == UnitType.Player)
            {
                WeaponInitHelper.InitializeWeaponsFromUnit(unit);
                WeaponInitHelper.InitializeHeroPassiveBuffFromUnitConfig(unit, true);

                RogueProgressHelper.EnsureProgress(unit, true);
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
            ApplyPlayerCampByTeam(spawnPointManager, unit, teamId);
            CampComponent camp = unit.GetComponent<CampComponent>();
            Log.Info($"[SpawnAssign] apply spawn point, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}, campId={camp?.CampId ?? 0}, configId={spawnPoint.ConfigId}, oldPos={oldPos}, newPos={unit.Position}");
        }

        private static void ApplyPlayerCampByTeam(SpawnPointManagerComponent spawnPointManager, Unit unit, int teamId)
        {
            if (unit == null || unit.UnitType != UnitType.Player)
            {
                return;
            }

            int targetCampId = ResolveCampIdByTeamOrder(spawnPointManager, teamId);
            CampComponent currentCamp = unit.GetComponent<CampComponent>();
            if (currentCamp != null && currentCamp.CampId == targetCampId)
            {
                return;
            }

            if (currentCamp != null)
            {
                unit.RemoveComponent<CampComponent>();
            }

            unit.AddComponent<CampComponent, int>(targetCampId);
            Log.Info($"[SpawnAssign] apply camp by team, unitId={unit.Id}, teamId={teamId}, campId={targetCampId}");
        }

        private static int ResolveCampIdByTeamOrder(SpawnPointManagerComponent spawnPointManager, int teamId)
        {
            if (spawnPointManager == null || spawnPointManager.TeamSpawnPoints.Count == 0)
            {
                return 1;
            }

            List<int> orderedTeamIds = new List<int>(spawnPointManager.TeamSpawnPoints.Keys);
            orderedTeamIds.Sort();

            int teamIndex = orderedTeamIds.IndexOf(teamId);
            if (teamIndex < 0)
            {
                Log.Warning($"[SpawnAssign] resolve camp failed: team not found, teamId={teamId}, teams=[{string.Join(",", orderedTeamIds)}]");
                return 1;
            }

            // 娴犮儵妲︽导宥夈€庢惔蹇旀Ё鐏忓嫬鍩屾稉銈呫亣闂冧絻鎯€閿涘瞼鈥樻穱婵堝偍閺佸矁鍏橀幎濠傤嚠閹靛顫嬫稉鐑樻櫕閸愭稏鈧?
            return (teamIndex % 2 == 0) ? 1 : 2;
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
                // 閸忋劏顫﹂崡鐘垫暏閺冭泛鍘规惔鏇烆槻閻劎顑囨稉鈧稉顏勫毉閻㈢喓鍋ｇ紒鍕剁礉闁灝鍘ら弮鐘崇《鏉╂稑鍙嗛崷鏉挎禈
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

            // 閺堝搫娅掓禍楦胯泲閺佸苯顕梼浣冩儉閿涘矂浼╅崗宥堫潶閸掋倕鐣炬稉鍝勫几閸?
            CampComponent camp = unit.GetComponent<CampComponent>();
            if (camp == null || camp.CampId != robotCampId)
            {
                if (camp != null)
                {
                    unit.RemoveComponent<CampComponent>();
                }
                unit.AddComponent<CampComponent, int>(robotCampId);
            }

            // 閹亞澧緼I娓氭繆绂?ThreatComponent
            if (unit.GetComponent<ThreatComponent>() == null)
            {
                unit.AddComponent<ThreatComponent>();
            }

            // 鐏忓棗缍嬮崜宥堟儰閻愮懓鍟撻崶鐐插毉閻㈢喎娼楅弽鍥风礉娓氭睔I閸ョ偛缍婇柅鏄忕帆娴ｈ法鏁?
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

