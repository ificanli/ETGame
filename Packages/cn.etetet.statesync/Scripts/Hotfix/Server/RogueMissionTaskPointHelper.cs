using System.Collections.Generic;
using System.Globalization;

namespace ET.Server
{
    public static class RogueMissionTaskPointHelper
    {
        public static bool HasTaskConfig(ECAPointComponent point)
        {
            return HasTaskConfig(point?.Params);
        }

        public static bool HasTaskConfig(List<FlowParam> pointParams)
        {
            return TryResolveTaskConfig(pointParams, out _, out _, out _);
        }

        public static async ETTask HandleStartTaskAsync(ECAFlowActionInvoke args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            await StartTaskAsync(point, player);
        }

        public static async ETTask<int> StartTaskAsync(ECAPointComponent point, Unit player)
        {
            if (point == null || point.IsDisposed || player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return 0;
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null || pointUnit.IsDisposed)
            {
                return 0;
            }

            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<Unit> playerRef = player;
            using (await player.Root().CoroutineLockComponent.Wait(CoroutineLockType.RogueMissionTask, pointUnit.Id))
            {
                point = pointRef;
                player = playerRef;
                if (point == null || point.IsDisposed || player == null || player.IsDisposed || player.UnitType != UnitType.Player)
                {
                    return 0;
                }

                pointUnit = point.GetParent<Unit>();
                if (pointUnit == null || pointUnit.IsDisposed || !point.IsActive)
                {
                    return 0;
                }

                if (!TryResolveTaskConfig(point.Params, out int monsterUnitConfigId, out int monsterCount, out int rewardGold))
                {
                    Log.Warning($"[RogueMissionTask] invalid point config: point={point.PointId}");
                    return 0;
                }

                UnitConfig monsterConfig = UnitConfigCategory.Instance.GetOrDefault(monsterUnitConfigId);
                if (monsterConfig == null || monsterConfig.UnitType != UnitType.Monster)
                {
                    Log.Warning($"[RogueMissionTask] invalid monster config: point={point.PointId}, unitConfigId={monsterUnitConfigId}");
                    return 0;
                }

                RogueMissionTaskPointComponent taskPoint = pointUnit.GetComponent<RogueMissionTaskPointComponent>();
                if (taskPoint != null)
                {
                    if (taskPoint.Completed)
                    {
                        Log.Info($"[RogueMissionTask] start ignored: point already completed, point={point.PointId}, owner={taskPoint.OwnerPlayerId}");
                        return 0;
                    }

                    if (taskPoint.Started)
                    {
                        Log.Info($"[RogueMissionTask] start ignored: point already started, point={point.PointId}, owner={taskPoint.OwnerPlayerId}");
                        return 0;
                    }
                }

                List<Unit> spawnedMonsters = new();
                int spawned = SpawnMonstersHelper.SpawnAtPoint(
                    point.Scene(),
                    pointUnit,
                    monsterUnitConfigId.ToString(CultureInfo.InvariantCulture),
                    monsterCount,
                    spawnedMonsters);
                if (spawned <= 0)
                {
                    Log.Warning($"[RogueMissionTask] start failed: no monster spawned, point={point.PointId}, unitConfigId={monsterUnitConfigId}, count={monsterCount}");
                    return 0;
                }

                taskPoint ??= pointUnit.AddComponent<RogueMissionTaskPointComponent>();
                taskPoint.PointId = point.PointId;
                taskPoint.OwnerPlayerId = player.Id;
                taskPoint.RewardGold = rewardGold;
                taskPoint.MonsterUnitConfigId = monsterUnitConfigId;
                taskPoint.SpawnCount = spawned;
                taskPoint.RemainingMonsterCount = spawned;
                taskPoint.Started = true;
                taskPoint.Completed = false;

                foreach (Unit spawnedMonster in spawnedMonsters)
                {
                    if (spawnedMonster == null || spawnedMonster.IsDisposed)
                    {
                        continue;
                    }

                    RogueMissionTaskMonsterComponent monsterComponent = spawnedMonster.GetComponent<RogueMissionTaskMonsterComponent>() ??
                            spawnedMonster.AddComponent<RogueMissionTaskMonsterComponent>();
                    monsterComponent.TaskPointUnitId = pointUnit.Id;
                    monsterComponent.OwnerPlayerId = player.Id;
                }

                point.IsActive = false;
                HideInteractHints(point);
                Log.Info(
                    $"[RogueMissionTask] start success: point={point.PointId}, owner={player.Id}, monsterUnitConfigId={monsterUnitConfigId}, spawnCount={spawned}, rewardGold={rewardGold}");
                return spawned;
            }
        }

        public static async ETTask OnTaskMonsterKilledAsync(Scene scene, long taskPointUnitId)
        {
            if (scene == null || scene.IsDisposed || taskPointUnitId <= 0)
            {
                return;
            }

            using (await scene.Root().CoroutineLockComponent.Wait(CoroutineLockType.RogueMissionTask, taskPointUnitId))
            {
                UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
                Unit pointUnit = unitComponent?.Get(taskPointUnitId);
                RogueMissionTaskPointComponent taskPoint = pointUnit?.GetComponent<RogueMissionTaskPointComponent>();
                if (taskPoint == null || taskPoint.IsDisposed || taskPoint.Completed || !taskPoint.Started)
                {
                    return;
                }

                if (taskPoint.RemainingMonsterCount > 0)
                {
                    taskPoint.RemainingMonsterCount -= 1;
                }

                if (taskPoint.RemainingMonsterCount > 0)
                {
                    Log.Info(
                        $"[RogueMissionTask] monster killed: point={taskPoint.PointId}, remain={taskPoint.RemainingMonsterCount}, owner={taskPoint.OwnerPlayerId}");
                    return;
                }

                taskPoint.RemainingMonsterCount = 0;
                taskPoint.Started = false;
                taskPoint.Completed = true;

                ECAPointComponent point = pointUnit?.GetComponent<ECAPointComponent>();
                if (point != null && !point.IsDisposed)
                {
                    point.IsActive = false;
                    HideInteractHints(point);
                }

                Unit ownerPlayer = unitComponent?.Get(taskPoint.OwnerPlayerId);
                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(ownerPlayer, false);
                if (progress == null)
                {
                    Log.Warning($"[RogueMissionTask] complete without reward target: point={taskPoint.PointId}, owner={taskPoint.OwnerPlayerId}");
                    return;
                }

                int finalRewardGold = RogueGoldHelper.GetGoldDeltaBySource(progress, taskPoint.RewardGold, RogueGoldSourceType.RogueCard);
                RogueGoldHelper.AddGold(progress, finalRewardGold);
                RogueProgressHelper.SyncProgress(ownerPlayer, progress);
                Log.Info(
                    $"[RogueMissionTask] complete: point={taskPoint.PointId}, owner={taskPoint.OwnerPlayerId}, rewardGold={finalRewardGold}, spawnCount={taskPoint.SpawnCount}");
            }
        }

        private static bool TryResolveTaskConfig(List<FlowParam> pointParams, out int monsterUnitConfigId, out int monsterCount, out int rewardGold)
        {
            monsterUnitConfigId = 0;
            monsterCount = 0;
            rewardGold = 0;
            return FlowParamHelper.TryGetIntParam(pointParams, RogueMissionTaskPointParamKey.MonsterUnitConfigId, out monsterUnitConfigId) &&
                   monsterUnitConfigId > 0 &&
                   FlowParamHelper.TryGetIntParam(pointParams, RogueMissionTaskPointParamKey.MonsterCount, out monsterCount) &&
                   monsterCount > 0 &&
                   FlowParamHelper.TryGetIntParam(pointParams, RogueMissionTaskPointParamKey.RewardGold, out rewardGold) &&
                   rewardGold > 0;
        }

        private static void HideInteractHints(ECAPointComponent point)
        {
            Scene scene = point?.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            point.CleanupInvalidPlayersInRange(unitComponent);
            foreach (long playerId in point.PlayersInRange)
            {
                Unit player = unitComponent.Get(playerId);
                if (player == null || player.IsDisposed || player.UnitType != UnitType.Player)
                {
                    continue;
                }

                ContainerRuntimeHelper.SendInteractHint(point, player, false);
            }
        }
    }
}
