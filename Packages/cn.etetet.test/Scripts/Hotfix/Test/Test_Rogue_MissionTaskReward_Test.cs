using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_MissionTaskReward_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_MissionTaskReward_Test));
            Scene scene = scope.TestFiber.Root;

            scene.AddComponent<TimerComponent>();
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();

            UnitConfig playerConfig = null;
            UnitConfig monsterConfig = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (config == null)
                {
                    continue;
                }

                if (playerConfig == null && config.UnitType == UnitType.Player)
                {
                    playerConfig = config;
                }

                if (monsterConfig == null && config.UnitType == UnitType.Monster)
                {
                    monsterConfig = config;
                }

                if (playerConfig != null && monsterConfig != null)
                {
                    break;
                }
            }

            if (playerConfig == null)
            {
                Log.Console("player unit config is null");
                return 1;
            }

            if (monsterConfig == null)
            {
                Log.Console("monster unit config is null");
                return 2;
            }

            Unit player = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            player.UnitType = UnitType.Player;

            RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(player, false);
            if (progress == null)
            {
                Log.Console("rogue progress is null");
                return 3;
            }

            Unit pointUnit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 0);
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>("mission_task_point_001", ECAPointType.RangeTrigger, 3f);
            RogueMissionTaskPointComponent taskPoint = pointUnit.AddComponent<RogueMissionTaskPointComponent>();
            taskPoint.PointId = point.PointId;
            taskPoint.OwnerPlayerId = player.Id;
            taskPoint.RewardGold = 300;
            taskPoint.MonsterUnitConfigId = monsterConfig.Id;
            taskPoint.SpawnCount = 2;
            taskPoint.RemainingMonsterCount = 2;
            taskPoint.Started = true;

            Unit monsterA = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), monsterConfig.Id);
            monsterA.UnitType = UnitType.Monster;
            RogueMissionTaskMonsterComponent monsterTaskA = monsterA.AddComponent<RogueMissionTaskMonsterComponent>();
            monsterTaskA.TaskPointUnitId = pointUnit.Id;
            monsterTaskA.OwnerPlayerId = player.Id;

            Unit monsterB = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), monsterConfig.Id);
            monsterB.UnitType = UnitType.Monster;
            RogueMissionTaskMonsterComponent monsterTaskB = monsterB.AddComponent<RogueMissionTaskMonsterComponent>();
            monsterTaskB.TaskPointUnitId = pointUnit.Id;
            monsterTaskB.OwnerPlayerId = player.Id;

            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            EntityRef<Unit> monsterBRef = monsterB;
            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<RogueMissionTaskPointComponent> taskPointRef = taskPoint;
            EntityRef<RogueProgressComponent> progressRef = progress;

            EventSystem.Instance.Publish(scene, new UnitDie
            {
                Unit = player,
                Target = monsterA,
                TargetId = monsterA.Id,
                TargetUnitType = (int)monsterA.UnitType,
            });
            await scene.TimerComponent.WaitAsync(10);
            scene = sceneRef;
            taskPoint = taskPointRef;
            progress = progressRef;
            if (scene == null || taskPoint == null || progress == null)
            {
                Log.Console("entities disposed after first kill wait");
                return 4;
            }

            if (taskPoint.RemainingMonsterCount != 1)
            {
                Log.Console($"remaining monster count after first kill mismatch: {taskPoint.RemainingMonsterCount}");
                return 5;
            }

            if (taskPoint.Completed || progress.CurrentGold != 0)
            {
                Log.Console($"task should not complete after first kill, completed={taskPoint.Completed}, gold={progress.CurrentGold}");
                return 6;
            }

            player = playerRef;
            monsterB = monsterBRef;
            if (player == null || monsterB == null)
            {
                Log.Console("player or monsterB disposed before second kill publish");
                return 7;
            }

            EventSystem.Instance.Publish(scene, new UnitDie
            {
                Unit = player,
                Target = monsterB,
                TargetId = monsterB.Id,
                TargetUnitType = (int)monsterB.UnitType,
            });
            await scene.TimerComponent.WaitAsync(10);
            scene = sceneRef;
            point = pointRef;
            taskPoint = taskPointRef;
            progress = progressRef;
            if (scene == null || point == null || taskPoint == null || progress == null)
            {
                Log.Console("entities disposed after second kill wait");
                return 8;
            }

            if (!taskPoint.Completed || taskPoint.Started)
            {
                Log.Console($"task completion flags mismatch, completed={taskPoint.Completed}, started={taskPoint.Started}");
                return 9;
            }

            if (progress.CurrentGold != 300)
            {
                Log.Console($"reward gold mismatch after task complete: {progress.CurrentGold}");
                return 10;
            }

            if (point.IsActive)
            {
                Log.Console("point should be inactive after task complete");
                return 11;
            }

            player = playerRef;
            monsterB = monsterBRef;
            if (player == null || monsterB == null)
            {
                Log.Console("player or monsterB disposed before duplicate kill publish");
                return 12;
            }

            EventSystem.Instance.Publish(scene, new UnitDie
            {
                Unit = player,
                Target = monsterB,
                TargetId = monsterB.Id,
                TargetUnitType = (int)monsterB.UnitType,
            });
            await scene.TimerComponent.WaitAsync(10);
            progress = progressRef;
            if (progress == null)
            {
                Log.Console("progress disposed after duplicate kill wait");
                return 13;
            }

            if (progress.CurrentGold != 300)
            {
                Log.Console($"task reward should not repeat, gold={progress.CurrentGold}");
                return 14;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
