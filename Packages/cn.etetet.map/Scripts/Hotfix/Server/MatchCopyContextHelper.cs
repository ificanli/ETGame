namespace ET.Server
{
    /// <summary>
    /// 匹配副本上下文辅助逻辑。
    /// </summary>
    public static class MatchCopyContextHelper
    {
        public static void Initialize(Scene scene, MatchCopyContextComponent context, Match2Map_InitMatchCopyRequest request)
        {
            if (scene == null || context == null || request == null)
            {
                return;
            }

            context.GameMode = request.GameMode;
            context.MapName = request.MapName;
            context.MapId = request.MapId;
            context.RobotsSpawned = false;
            context.HumanPlayerIds.Clear();
            context.EnteredHumanPlayerIds.Clear();
            context.RobotPlayerIds.Clear();
            context.PlayerTeamIds.Clear();

            foreach (long playerId in request.HumanPlayerIds)
            {
                context.HumanPlayerIds.Add(playerId);
            }

            context.RobotPlayerIds.AddRange(request.RobotPlayerIds);
            int playerCount = request.PlayerIds?.Count ?? 0;
            for (int i = 0; i < playerCount; ++i)
            {
                int teamId = i < request.TeamIds.Count ? request.TeamIds[i] : 0;
                context.PlayerTeamIds[request.PlayerIds[i]] = teamId;
            }

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent != null)
            {
                foreach (Entity entity in unitComponent.Children.Values)
                {
                    if (entity is not Unit unit || unit.UnitType != UnitType.Player)
                    {
                        continue;
                    }

                    if (context.HumanPlayerIds.Contains(unit.Id))
                    {
                        context.EnteredHumanPlayerIds.Add(unit.Id);
                    }
                }
            }

            Log.Info(
                $"[MatchCopy] init context, scene={scene.Name}, map={context.MapName}@{context.MapId}, gameMode={context.GameMode}, humanCount={context.HumanPlayerIds.Count}, robotCount={context.RobotPlayerIds.Count}");

            TrySpawnRobots(scene, context);
        }

        public static void OnHumanPlayerEntered(Scene scene, Unit unit)
        {
            if (scene == null || unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                return;
            }

            MatchCopyContextComponent context = scene.GetComponent<MatchCopyContextComponent>();
            if (context == null || context.RobotsSpawned)
            {
                return;
            }

            if (!context.HumanPlayerIds.Contains(unit.Id))
            {
                return;
            }

            if (!context.EnteredHumanPlayerIds.Add(unit.Id))
            {
                return;
            }

            Log.Info(
                $"[MatchCopy] human entered, scene={scene.Name}, map={context.MapName}@{context.MapId}, unitId={unit.Id}, entered={context.EnteredHumanPlayerIds.Count}/{context.HumanPlayerIds.Count}");

            TrySpawnRobots(scene, context);
        }

        private static void TrySpawnRobots(Scene scene, MatchCopyContextComponent context)
        {
            if (scene == null || context == null || context.RobotsSpawned)
            {
                return;
            }

            if (context.EnteredHumanPlayerIds.Count < context.HumanPlayerIds.Count)
            {
                return;
            }

            context.RobotsSpawned = true;
            if (context.RobotPlayerIds.Count == 0)
            {
                Log.Info($"[MatchCopy] no robot fill needed, map={context.MapName}@{context.MapId}");
                return;
            }

            foreach (long robotPlayerId in context.RobotPlayerIds)
            {
                SpawnRobot(scene, context, robotPlayerId);
            }
        }

        private static void SpawnRobot(Scene scene, MatchCopyContextComponent context, long robotPlayerId)
        {
            string mapName = context?.MapName;
            if (string.IsNullOrWhiteSpace(mapName))
            {
                mapName = scene.Name.GetSceneConfigName();
            }

            if (!MatchRobotRuntimeHelper.TryBuildSpawnProfile(
                    mapName,
                    context?.GameMode ?? 0,
                    robotPlayerId,
                    out int matchRobotConfigId,
                    out int heroConfigId,
                    out int unitConfigId,
                    out int mainWeaponConfigId,
                    out int aiBuffConfigId,
                    out int autoChooseDelayMinMs,
                    out int autoChooseDelayMaxMs))
            {
                Log.Error($"[MatchCopy] spawn robot failed: profile invalid, map={mapName}, robotId={robotPlayerId}, gameMode={context?.GameMode ?? 0}");
                return;
            }

            Unit robotUnit = UnitFactory.Create(scene, robotPlayerId, unitConfigId);
            MatchRobotComponent matchRobot = robotUnit.AddComponent<MatchRobotComponent>();
            matchRobot.MatchRobotConfigId = matchRobotConfigId;
            matchRobot.HeroConfigId = heroConfigId;
            matchRobot.MainWeaponConfigId = mainWeaponConfigId;
            matchRobot.AIBuffConfigId = aiBuffConfigId;
            matchRobot.AutoChooseDelayMinMs = autoChooseDelayMinMs;
            matchRobot.AutoChooseDelayMaxMs = autoChooseDelayMaxMs;
            matchRobot.AutoChooseScheduledSerial = 0;
            matchRobot.AutoChooseCompletedSerial = 0;

            if (context != null &&
                context.PlayerTeamIds.TryGetValue(robotPlayerId, out int teamId) &&
                teamId > 0)
            {
                MapUnitEnterHelper.ApplyAssignedTeamIfNeeded(scene, robotUnit, teamId);
            }

            MapUnitEnterHelper.EnsureMapRuntimeComponents(scene, robotUnit);
            MatchRobotRuntimeHelper.ApplyLoadout(robotUnit, matchRobot);
            MapUnitEnterHelper.InitializePlayerGameplay(robotUnit);
            MapUnitEnterHelper.ApplySpawnPointIfNeeded(scene, robotUnit, context != null && context.PlayerTeamIds.TryGetValue(robotPlayerId, out int teamOrder) ? teamOrder : 0);
            MapUnitEnterHelper.SetupMatchRobotIfNeeded(scene, robotUnit);

            Log.Info(
                $"[MatchCopy] spawned local robot, map={scene.Name}, robotId={robotPlayerId}, unitConfigId={unitConfigId}, heroConfigId={heroConfigId}, mainWeaponConfigId={mainWeaponConfigId}, aiBuffConfigId={aiBuffConfigId}");
        }
    }
}
