using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 地图单位进入后的公共初始化辅助逻辑。
    /// </summary>
    public static class MapUnitEnterHelper
    {
        private const int MatchRobotDefaultAIBuffConfigId = 300011;

        public static void EnsureMapRuntimeComponents(Scene scene, Unit unit)
        {
            if (scene == null || unit == null || unit.IsDisposed)
            {
                return;
            }

            if (unit.GetComponent<TurnComponent>() == null)
            {
                unit.AddComponent<TurnComponent>();
            }

            if (unit.GetComponent<MoveComponent>() == null)
            {
                unit.AddComponent<MoveComponent>();
            }

            string mapName = scene.Name.GetSceneConfigName();
            if (mapName != "Home" && unit.GetComponent<PathfindingComponent>() == null)
            {
                unit.AddComponent<PathfindingComponent, string>(mapName);
            }

            if (unit.GetComponent<MailBoxComponent>() == null)
            {
                unit.AddComponent<MailBoxComponent, int>(MailBoxType.OrderedMessage);
            }

            if (unit.GetComponent<TargetComponent>() == null)
            {
                unit.AddComponent<TargetComponent>();
            }

            EnsureCampAndThreatComponent(unit);
            EnsureConfiguredAIBuff(unit);
            LogCampTrace(unit, "MapUnitEnter.EnsureRuntime");
        }

        public static void InitializePlayerGameplay(Unit unit)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                return;
            }

            WeaponInitHelper.InitializeWeaponsFromUnit(unit);
            WeaponInitHelper.InitializeHeroPassiveBuffFromUnitConfig(unit, true);
            RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, true);
            RogueUnitDisplayLevelHelper.RefreshPlayerDisplayLevel(unit, progress, true);

            Scene scene = unit.Scene();
            string mapName = scene?.Name.GetSceneConfigName();
            Log.Info(
                $"[RogueInit] InitializePlayerGameplay unit={unit.Id}, scene={scene?.Name ?? "null"}, map={mapName ?? "null"}, level={progress?.Level ?? 0}, choicePending={progress?.ChoicePending ?? false}, pendingOptions={progress?.PendingOptionIds.Count ?? 0}, selectedOptions={progress?.SelectedOptionIds.Count ?? 0}");
            if (mapName != "Home" && mapName != "GateMap")
            {
                RogueProgressHelper.TryInitializeChoicePopupAfterEnter(unit).Coroutine();
                RogueProgressHelper.TryEnsureChoicePopupVisibleLater(unit).Coroutine();
            }
            else
            {
                Log.Info($"[RogueInit] skip initial choice by map unit={unit.Id}, map={mapName ?? "null"}");
            }

            if (scene != null && !scene.IsDisposed)
            {
                EventSystem.Instance.Publish(scene, new PlayerEnterMap
                {
                    Unit = unit,
                    MapName = mapName ?? string.Empty,
                });
            }
        }

        public static void ApplyAssignedTeamIfNeeded(Scene scene, Unit unit, int configuredTeamIdOrOrder)
        {
            if (scene == null || unit == null || unit.IsDisposed)
            {
                return;
            }

            SpawnPointManagerComponent spawnPointManager = scene.GetComponent<SpawnPointManagerComponent>();
            if (spawnPointManager == null || spawnPointManager.TeamSpawnPoints.Count == 0)
            {
                Log.Warning($"[SpawnAssign] preset team assignment skipped: manager missing or empty, scene={scene?.Name}, unitId={unit?.Id ?? 0}, configuredTeamIdOrOrder={configuredTeamIdOrOrder}");
                return;
            }

            if (configuredTeamIdOrOrder <= 0 &&
                !spawnPointManager.TeamSpawnPoints.ContainsKey(configuredTeamIdOrOrder))
            {
                return;
            }

            if (!TryResolveConfiguredTeamId(spawnPointManager, configuredTeamIdOrOrder, out int actualTeamId))
            {
                Log.Warning($"[SpawnAssign] preset team assignment skipped: invalid team input, scene={scene.Name}, unitId={unit.Id}, configuredTeamIdOrOrder={configuredTeamIdOrOrder}");
                return;
            }

            spawnPointManager.PlayerTeamAssignments[unit.Id] = actualTeamId;
            spawnPointManager.OccupiedTeamIds.Add(actualTeamId);
            Log.Info($"[SpawnAssign] preset team assignment, scene={scene.Name}, unitId={unit.Id}, configuredTeamIdOrOrder={configuredTeamIdOrOrder}, teamId={actualTeamId}");
        }

        public static void ApplySpawnPointIfNeeded(Scene scene, Unit unit, int configuredTeamIdOrOrder = 0)
        {
            if (scene == null || unit == null || unit.IsDisposed)
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
                if (TryApplyFallbackSpawnPoint(scene, unit, configuredTeamIdOrOrder))
                {
                    return;
                }

                Log.Warning($"[SpawnAssign] skip: manager missing or empty, scene={scene.Name}, unitId={unit.Id}, teamGroupCount={spawnPointManager?.TeamSpawnPoints.Count ?? 0}");
                return;
            }

            int teamId = GetOrAssignTeamId(spawnPointManager, unit.Id, configuredTeamIdOrOrder);
            if (!spawnPointManager.TeamSpawnPoints.TryGetValue(teamId, out List<SpawnPointECAConfig> spawnPoints) || spawnPoints.Count == 0)
            {
                if (TryApplyFallbackSpawnPoint(scene, unit, configuredTeamIdOrOrder))
                {
                    return;
                }

                Log.Warning($"[SpawnAssign] skip: no spawn points for team, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}");
                return;
            }

            if (!spawnPointManager.TryGetNextSpawnPoint(teamId, out SpawnPointECAConfig spawnPoint))
            {
                if (TryApplyFallbackSpawnPoint(scene, unit, configuredTeamIdOrOrder))
                {
                    return;
                }

                Log.Warning($"[SpawnAssign] skip: failed to get next spawn point, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}");
                return;
            }

            float3 oldPos = unit.Position;
            float3 configuredPos = new float3(spawnPoint.PosX, spawnPoint.PosY, spawnPoint.PosZ);
            if (!TryResolveSpawnPositionOnNavmesh(unit, configuredPos, out float3 resolvedPos, out float projectedDistance))
            {
                Log.Warning($"[SpawnAssign] configured spawn point is not on navmesh, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}, configId={spawnPoint.ConfigId}, configuredPos={configuredPos}");
                if (TryApplyFallbackSpawnPoint(scene, unit, configuredTeamIdOrOrder))
                {
                    return;
                }

                if (TryResolveSpawnPositionOnNavmesh(unit, oldPos, out float3 oldResolvedPos, out float oldProjectedDistance))
                {
                    resolvedPos = oldResolvedPos;
                    projectedDistance = oldProjectedDistance;
                    Log.Warning($"[SpawnAssign] fallback to previous position projection, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}, oldPos={oldPos}, projectedPos={resolvedPos}, projectedDeltaXZ={projectedDistance:F3}");
                }
                else
                {
                    resolvedPos = configuredPos;
                    projectedDistance = 0f;
                    Log.Warning($"[SpawnAssign] all spawn projections failed, fallback to configured position, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}, oldPos={oldPos}, configuredPos={configuredPos}");
                }
            }

            unit.Position = resolvedPos;
            TrySnapCurrentPositionToNavmesh(unit, "ApplySpawnPointIfNeeded", out float snapDeltaXZ, out float snapDeltaY);
            RecordSpawnPoint(unit);
            ApplyUnitCampByTeam(spawnPointManager, unit, teamId);
            CampComponent camp = unit.GetComponent<CampComponent>();
            Log.Info($"[SpawnAssign] apply spawn point, scene={scene.Name}, unitId={unit.Id}, teamId={teamId}, campId={camp?.CampId ?? 0}, configId={spawnPoint.ConfigId}, oldPos={oldPos}, configuredPos={configuredPos}, newPos={unit.Position}, projectedDeltaXZ={projectedDistance:F3}, snapDeltaXZ={snapDeltaXZ:F3}, snapDeltaY={snapDeltaY:F3}");
        }

        public static void SetupMatchRobotIfNeeded(Scene scene, Unit unit)
        {
            if (scene == null || unit == null || unit.IsDisposed)
            {
                return;
            }

            MatchRobotComponent matchRobot = unit.GetComponent<MatchRobotComponent>();
            if (matchRobot == null)
            {
                return;
            }

            int gameMode = scene.GetComponent<MatchCopyContextComponent>()?.GameMode ?? 0;
            int robotAIBuffConfigId = MatchRobotRuntimeHelper.ResolveMatchRobotAIBuffConfigId(
                scene.Name.GetSceneConfigName(),
                gameMode,
                matchRobot.AIBuffConfigId);
            if (robotAIBuffConfigId <= 0 && BuffConfigCategory.Instance.Contain(MatchRobotDefaultAIBuffConfigId))
            {
                robotAIBuffConfigId = MatchRobotDefaultAIBuffConfigId;
            }

            if (robotAIBuffConfigId <= 0)
            {
                Log.Warning($"[MatchRobot] setup skipped: invalid ai buff, unitId={unit.Id}, map={scene.Name}, gameMode={gameMode}");
                return;
            }

            if (unit.GetComponent<ThreatComponent>() == null)
            {
                unit.AddComponent<ThreatComponent>();
            }

            NumericComponent numeric = unit.NumericComponent;
            numeric.SetNoEvent(NumericType.AI, robotAIBuffConfigId);
            matchRobot.AIBuffConfigId = robotAIBuffConfigId;
            EnsureConfiguredAIBuff(unit, true);
            Log.Info($"[MatchRobot] setup complete: unitId={unit.Id}, pos={unit.Position}, camp={unit.GetComponent<CampComponent>()?.CampId ?? 0}, ai={robotAIBuffConfigId}");
        }

        public static void EnsureConfiguredAIBuff(Unit unit, bool forceRecreate = false)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            NumericComponent numeric = unit.NumericComponent;
            if (numeric == null)
            {
                Log.Warning($"[UnitAI] ensure skipped: NumericComponent missing, unitId={unit.Id}");
                return;
            }

            int aiBuffConfigId = numeric.GetAsInt(NumericType.AI);
            if (aiBuffConfigId <= 0)
            {
                return;
            }

            if (!BuffConfigCategory.Instance.Contain(aiBuffConfigId))
            {
                Log.Warning($"[UnitAI] ensure skipped: buff config missing, unitId={unit.Id}, aiBuffConfigId={aiBuffConfigId}");
                return;
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                Log.Warning($"[UnitAI] ensure skipped: BuffComponent missing, unitId={unit.Id}, aiBuffConfigId={aiBuffConfigId}");
                return;
            }

            if (forceRecreate && buffComponent.HasBuff(aiBuffConfigId))
            {
                BuffHelper.RemoveBuffByConfigId(unit, aiBuffConfigId, BuffFlags.SameConfigIdReplaceRemove);
            }

            if (buffComponent.HasBuff(aiBuffConfigId))
            {
                Log.Debug($"[UnitAI] already active, unitId={unit.Id}, aiBuffConfigId={aiBuffConfigId}, forceRecreate={forceRecreate}");
                return;
            }

            BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), aiBuffConfigId, null);
            Log.Info($"[UnitAI] ensured, unitId={unit.Id}, unitType={unit.UnitType}, aiBuffConfigId={aiBuffConfigId}, forceRecreate={forceRecreate}, pos={unit.Position}");
        }

        public static bool TrySnapCurrentPositionToNavmesh(Unit unit, string reason, out float projectedDistanceXZ, out float projectedDistanceY)
        {
            projectedDistanceXZ = 0f;
            projectedDistanceY = 0f;
            if (unit == null || unit.IsDisposed)
            {
                return false;
            }

            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                return false;
            }

            float3 oldPos = unit.Position;
            float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;

            bool projected = pathfinding.TryRecastFindNearestPointForMovement(oldPos, unitRadius, out float3 projectedPos, out projectedDistanceXZ) ||
                pathfinding.TryRecastFindNearestPointForSpawn(oldPos, unitRadius, out projectedPos, out projectedDistanceXZ);
            if (!projected)
            {
                projectedDistanceXZ = 0f;
                projectedDistanceY = 0f;
                Log.Warning($"[SpawnAssign] ground snap failed: scene={unit.Scene()?.Name}, unitId={unit.Id}, reason={reason}, pos={oldPos}");
                return false;
            }

            projectedDistanceY = math.abs(projectedPos.y - oldPos.y);
            unit.Position = projectedPos;
            if (projectedDistanceXZ > 0.01f || projectedDistanceY > 0.01f)
            {
                Log.Info($"[SpawnAssign] ground snapped: scene={unit.Scene()?.Name}, unitId={unit.Id}, reason={reason}, oldPos={oldPos}, newPos={projectedPos}, projectedDeltaXZ={projectedDistanceXZ:F3}, projectedDeltaY={projectedDistanceY:F3}");
            }

            return true;
        }

        private static void ApplyUnitCampByTeam(SpawnPointManagerComponent spawnPointManager, Unit unit, int teamId)
        {
            if (unit == null)
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
            LogCampTrace(unit, "MapUnitEnter.ApplyCampByTeam");
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

            if (orderedTeamIds.Count <= 2)
            {
                return (teamIndex % 2 == 0) ? 1 : 2;
            }

            return teamIndex + 1;
        }

        private static int GetOrAssignTeamId(SpawnPointManagerComponent spawnPointManager, long playerId, int configuredTeamIdOrOrder)
        {
            if (spawnPointManager.PlayerTeamAssignments.TryGetValue(playerId, out int assignedTeamId))
            {
                Log.Info($"[SpawnAssign] reuse team assignment, unitId={playerId}, teamId={assignedTeamId}");
                return assignedTeamId;
            }

            if (TryResolveConfiguredTeamId(spawnPointManager, configuredTeamIdOrOrder, out int mappedTeamId))
            {
                spawnPointManager.OccupiedTeamIds.Add(mappedTeamId);
                spawnPointManager.PlayerTeamAssignments[playerId] = mappedTeamId;
                Log.Info($"[SpawnAssign] assign preset team input, unitId={playerId}, configuredTeamIdOrOrder={configuredTeamIdOrOrder}, teamId={mappedTeamId}");
                return mappedTeamId;
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
                teamId = orderedTeamIds[0];
                Log.Warning($"[SpawnAssign] all teams occupied, fallback to first team, unitId={playerId}, fallbackTeamId={teamId}");
            }

            spawnPointManager.OccupiedTeamIds.Add(teamId);

            spawnPointManager.PlayerTeamAssignments[playerId] = teamId;
            Log.Info($"[SpawnAssign] assign team, unitId={playerId}, teamId={teamId}, teams=[{string.Join(",", orderedTeamIds)}], occupied=[{string.Join(",", spawnPointManager.OccupiedTeamIds)}]");
            return teamId;
        }

        private static bool TryResolveConfiguredTeamId(SpawnPointManagerComponent spawnPointManager, int configuredTeamIdOrOrder, out int actualTeamId)
        {
            actualTeamId = 0;
            if (spawnPointManager == null || spawnPointManager.TeamSpawnPoints.Count == 0)
            {
                return false;
            }

            if (spawnPointManager.TeamSpawnPoints.ContainsKey(configuredTeamIdOrOrder))
            {
                actualTeamId = configuredTeamIdOrOrder;
                return true;
            }

            if (configuredTeamIdOrOrder <= 0)
            {
                return false;
            }

            List<int> orderedTeamIds = new List<int>(spawnPointManager.TeamSpawnPoints.Keys);
            orderedTeamIds.Sort();

            int teamIndex = configuredTeamIdOrOrder - 1;
            if (teamIndex < 0 || teamIndex >= orderedTeamIds.Count)
            {
                Log.Warning($"[SpawnAssign] invalid team input, configuredTeamIdOrOrder={configuredTeamIdOrOrder}, availableTeams=[{string.Join(",", orderedTeamIds)}]");
                return false;
            }

            actualTeamId = orderedTeamIds[teamIndex];
            return true;
        }

        private static bool TryApplyFallbackSpawnPoint(Scene scene, Unit unit, int teamOrder)
        {
            if (scene == null || unit == null || unit.IsDisposed)
            {
                return false;
            }

            float3 anchor = ResolveFallbackSpawnAnchor(scene, unit);
            float angle = ResolveFallbackSpawnAngle(unit.Id, teamOrder);
            int spreadIndex = math.abs((int)(unit.Id % 3));
            float radius = 8f + spreadIndex * 3f;
            float radians = math.radians(angle);
            float3 candidate = anchor + new float3(math.cos(radians) * radius, 0f, math.sin(radians) * radius);

            if (!TryResolveSpawnPositionOnNavmesh(unit, candidate, out float3 resolvedCandidate, out float projectedDistance))
            {
                Log.Warning($"[SpawnAssign] fallback spawn rejected by navmesh, scene={scene.Name}, unitId={unit.Id}, teamOrder={teamOrder}, candidate={candidate}, anchor={anchor}");
                return false;
            }

            float3 oldPos = unit.Position;
            unit.Position = resolvedCandidate;
            TrySnapCurrentPositionToNavmesh(unit, "TryApplyFallbackSpawnPoint", out float snapDeltaXZ, out float snapDeltaY);
            RecordSpawnPoint(unit);
            ApplyFallbackCampByTeamOrder(unit, teamOrder);
            Log.Warning($"[SpawnAssign] fallback spawn applied, scene={scene.Name}, unitId={unit.Id}, teamOrder={teamOrder}, oldPos={oldPos}, configuredPos={candidate}, newPos={unit.Position}, anchor={anchor}, projectedDeltaXZ={projectedDistance:F3}, snapDeltaXZ={snapDeltaXZ:F3}, snapDeltaY={snapDeltaY:F3}");
            return true;
        }

        private static float3 ResolveFallbackSpawnAnchor(Scene scene, Unit unit)
        {
            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;

            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
            if (ecaManager != null)
            {
                float3 firstPointPos = default;
                bool hasFirstPoint = false;
                foreach (ECAPointComponent point in ecaManager.GetAllECAPoints())
                {
                    Unit pointUnit = point?.GetParent<Unit>();
                    if (pointUnit == null)
                    {
                        continue;
                    }

                    float3 candidatePos = pointUnit.Position;
                    if (pathfinding != null)
                    {
                        if (!pathfinding.TryRecastFindNearestPointForSpawn(candidatePos, unitRadius, out float3 projectedPointPos, out _))
                        {
                            continue;
                        }

                        candidatePos = projectedPointPos;
                    }

                    if (!hasFirstPoint)
                    {
                        firstPointPos = candidatePos;
                        hasFirstPoint = true;
                    }

                    if (point.PointType == ECAPointType.EvacuationPoint)
                    {
                        return candidatePos;
                    }
                }

                if (hasFirstPoint)
                {
                    return firstPointPos;
                }
            }

            if (pathfinding != null)
            {
                if (pathfinding.TryRecastFindNearestPointForSpawn(unit.Position, unitRadius, out float3 projectedPos, out _))
                {
                    return projectedPos;
                }

                if (pathfinding.TryRecastFindNearestPointForSpawn(float3.zero, unitRadius, out projectedPos, out _))
                {
                    return projectedPos;
                }
            }

            return unit.Position;
        }

        private static bool TryResolveSpawnPositionOnNavmesh(Unit unit, float3 configuredPos, out float3 resolvedPos, out float projectedDistance)
        {
            resolvedPos = configuredPos;
            projectedDistance = 0f;
            if (unit == null || unit.IsDisposed)
            {
                return false;
            }

            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                return true;
            }

            float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            if (pathfinding.TryRecastFindNearestPoint(configuredPos, unitRadius, out resolvedPos, out projectedDistance))
            {
                return true;
            }

            if (pathfinding.TryRecastFindNearestPointForMovement(configuredPos, unitRadius, out resolvedPos, out projectedDistance))
            {
                Log.Warning($"[SpawnAssign] recover spawn point by movement projection, scene={unit.Scene()?.Name}, unitId={unit.Id}, configuredPos={configuredPos}, recoveredPos={resolvedPos}, projectedDeltaXZ={projectedDistance:F3}");
                return true;
            }

            if (pathfinding.TryRecastFindNearestPointForSpawn(configuredPos, unitRadius, out resolvedPos, out projectedDistance))
            {
                Log.Warning($"[SpawnAssign] recover spawn point by wide projection, scene={unit.Scene()?.Name}, unitId={unit.Id}, configuredPos={configuredPos}, recoveredPos={resolvedPos}, projectedDeltaXZ={projectedDistance:F3}");
                return true;
            }

            return false;
        }

        private static float ResolveFallbackSpawnAngle(long unitId, int teamOrder)
        {
            int intraTeamIndex = math.abs((int)(unitId % 3)) - 1;
            float intraTeamOffset = intraTeamIndex * 20f;

            if (teamOrder == 1)
            {
                return 0f + intraTeamOffset;
            }

            if (teamOrder == 2)
            {
                return 180f + intraTeamOffset;
            }

            if (teamOrder > 2)
            {
                return (teamOrder - 1) * 60f + intraTeamOffset;
            }

            return (math.abs((int)(unitId % 8)) * 45f) + intraTeamOffset;
        }

        private static void ApplyFallbackCampByTeamOrder(Unit unit, int teamOrder)
        {
            if (unit == null || unit.IsDisposed || teamOrder <= 0)
            {
                return;
            }

            int campId = teamOrder <= 2 ? teamOrder : teamOrder;
            CampComponent currentCamp = unit.GetComponent<CampComponent>();
            if (currentCamp != null && currentCamp.CampId == campId)
            {
                return;
            }

            if (currentCamp != null)
            {
                unit.RemoveComponent<CampComponent>();
            }

            unit.AddComponent<CampComponent, int>(campId);
            Log.Info($"[SpawnAssign] apply fallback camp, unitId={unit.Id}, teamOrder={teamOrder}, campId={campId}");
        }

        private static bool TryResolveRobotRuntimeConfig(string mapName, out int robotAIBuffConfigId)
        {
            if (TryResolveRobotRuntimeConfigByMap(mapName, out robotAIBuffConfigId))
            {
                return true;
            }

            return TryResolveRobotRuntimeConfigByUnitCatalog(out robotAIBuffConfigId);
        }

        private static bool TryResolveRobotRuntimeConfigByMap(string mapName, out int robotAIBuffConfigId)
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

                if (!TryResolveAIBuffConfigId(unitConfig, out robotAIBuffConfigId))
                {
                    continue;
                }

                return true;
            }

            robotAIBuffConfigId = 0;
            return false;
        }

        private static bool TryResolveRobotRuntimeConfigByUnitCatalog(out int robotAIBuffConfigId)
        {
            foreach (UnitConfig unitConfig in UnitConfigCategory.Instance.GetAll().Values)
            {
                if (unitConfig.UnitType == UnitType.Player)
                {
                    continue;
                }

                if (!TryResolveAIBuffConfigId(unitConfig, out robotAIBuffConfigId))
                {
                    continue;
                }

                return true;
            }

            robotAIBuffConfigId = 0;
            return false;
        }

        private static bool TryResolveAIBuffConfigId(UnitConfig unitConfig, out int aiBuffConfigId)
        {
            aiBuffConfigId = 0;

            if (unitConfig != null &&
                unitConfig.KV.TryGetValue(NumericType.AI, out long unitAI) &&
                unitAI > 0)
            {
                aiBuffConfigId = (int)unitAI;
                return true;
            }

            return false;
        }

        private static void RecordSpawnPoint(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            UnitSpawnPointComponent spawnPointComponent = unit.GetComponent<UnitSpawnPointComponent>();
            if (spawnPointComponent == null)
            {
                unit.AddComponent<UnitSpawnPointComponent, float3, quaternion>(unit.Position, unit.Rotation);
                return;
            }

            spawnPointComponent.SetSpawnPoint(unit.Position, unit.Rotation);
        }

        private static void EnsureCampAndThreatComponent(Unit unit)
        {
            switch (unit.UnitType)
            {
                case UnitType.Player:
                    if (unit.GetComponent<CampComponent>() == null)
                    {
                        unit.AddComponent<CampComponent, int>(1);
                        LogCampTrace(unit, "MapUnitEnter.PlayerDefaultCamp");
                    }
                    break;
                case UnitType.Monster:
                    if (unit.GetComponent<CampComponent>() == null)
                    {
                        unit.AddComponent<CampComponent, int>(2);
                        LogCampTrace(unit, "MapUnitEnter.MonsterDefaultCamp");
                    }

                    if (unit.GetComponent<ThreatComponent>() == null)
                    {
                        unit.AddComponent<ThreatComponent>();
                    }
                    break;
            }
        }

        private static void LogCampTrace(Unit unit, string stage)
        {
            CampComponent camp = unit?.GetComponent<CampComponent>();
            Log.Info($"[CampTrace][{stage}] unitId={unit?.Id ?? 0}, unitType={unit?.UnitType}, campId={camp?.CampId ?? 0}, campType={camp?.CampType.ToString() ?? "None"}");
        }
    }
}
