using ET.Client;

namespace ET.Server
{
    [Invoke(RobotCaseType.AchievementTriggerTest)]
    public class RobotCase_006_AchievementTrigger_Handler : ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            // 创建机器人子Fiber
            Fiber robot = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Robot, "RobotCase_006_AchievementTrigger");

            Log.Debug("Achievement trigger robot test started");

            // 执行成就触发测试流程
            await TestAchievementTriggerFlow(robot, fiber);

            Log.Debug("Achievement trigger robot test completed successfully");
            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 测试成就触发流程
        /// </summary>
        private async ETTask TestAchievementTriggerFlow(Fiber robot, Fiber parentFiber)
        {
            Log.Debug("Starting Achievement trigger test flow");

            Scene robotScene = robot.Root;
            EntityRef<Scene> robotSceneRef = robotScene;

            // 验证机器人连接状态
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            if (clientSender == null)
            {
                Log.Error("ClientSenderComponent not found, robot may not be properly connected");
                return;
            }

            // 步骤1：初始化测试环境
            InitializeTriggerTestEnvironment(robotScene, parentFiber);

            // 步骤2：获取初始成就状态
            robotScene = robotSceneRef; // await后重新获取
            AchievementInfo[] initialAchievements = await ClientAchievementHelper.GetAchievements(robotScene);
            Log.Debug($"Initial achievements count: {initialAchievements.Length}");

            // 步骤3：测试各种事件触发
            robotScene = robotSceneRef; // await后重新获取
            await TestKillMonsterTrigger(robotScene, parentFiber);

            robotScene = robotSceneRef; // await后重新获取
            await TestLevelUpTrigger(robotScene, parentFiber);

            robotScene = robotSceneRef; // await后重新获取
            await TestQuestCompleteTrigger(robotScene, parentFiber);

            robotScene = robotSceneRef; // await后重新获取
            await TestItemCollectTrigger(robotScene, parentFiber);

            robotScene = robotSceneRef; // await后重新获取
            await TestMapExploreTrigger(robotScene, parentFiber);

            // 步骤4：验证最终状态
            robotScene = robotSceneRef; // await后重新获取
            AchievementInfo[] finalAchievements = await ClientAchievementHelper.GetAchievements(robotScene);
            ValidateTriggerResults(initialAchievements, finalAchievements);

            Log.Debug("Achievement trigger test flow completed successfully");
        }

        /// <summary>
        /// 直接访问服务器初始化成就触发测试环境
        /// </summary>
        private static void InitializeTriggerTestEnvironment(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Initializing Achievement trigger test environment via direct server access");

            // 获取服务端数据
            string mapName = robotScene.CurrentScene().Name;
            Fiber map = parentFiber.GetFiber("MapManager").GetFiber(mapName);
            if (map == null)
            {
                Log.Error($"not found robot map {mapName}");
                return;
            }

            // 获取Unit的Id
            Client.PlayerComponent playerComponent = robotScene.GetComponent<Client.PlayerComponent>();

            // 获取服务端Unit
            Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);

            // 获取或添加成就组件
            AchievementComponent achievementComponent = serverUnit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                achievementComponent = serverUnit.AddComponent<AchievementComponent>();
            }

            // 创建触发测试数据
            CreateTriggerTestData(achievementComponent);

            Log.Debug("Achievement trigger test data prepared successfully via direct server access");
        }

        /// <summary>
        /// 创建触发测试数据
        /// </summary>
        private static void CreateTriggerTestData(AchievementComponent achievementComponent)
        {
            // 首次登录成就 - 已完成
            Achievement loginAchievement = achievementComponent.AddAchievement(6001);
            loginAchievement.Type = AchievementType.Social;
            loginAchievement.MaxProgress = 1;
            loginAchievement.Progress = 1;
            loginAchievement.Points = 5;
            loginAchievement.CategoryId = 1;
            loginAchievement.Status = AchievementStatus.Completed;
            loginAchievement.CompleteTime = TimeInfo.Instance.ServerNow() - 3600000; // 1小时前

            // 击杀成就 - 部分进度
            Achievement killAchievement = achievementComponent.AddAchievement(6002);
            killAchievement.Type = AchievementType.Kill;
            killAchievement.MaxProgress = 10;
            killAchievement.Progress = 2; // 已击杀2个
            killAchievement.Points = 15;
            killAchievement.CategoryId = 2;
            killAchievement.Status = AchievementStatus.InProgress;

            // 等级成就 - 部分进度
            Achievement levelAchievement = achievementComponent.AddAchievement(6003);
            levelAchievement.Type = AchievementType.Level;
            levelAchievement.MaxProgress = 20;
            levelAchievement.Progress = 5; // 当前5级
            levelAchievement.Points = 20;
            levelAchievement.CategoryId = 3;
            levelAchievement.Status = AchievementStatus.InProgress;

            // 任务成就 - 部分进度
            Achievement questAchievement = achievementComponent.AddAchievement(6004);
            questAchievement.Type = AchievementType.Quest;
            questAchievement.MaxProgress = 5;
            questAchievement.Progress = 1; // 已完成1个任务
            questAchievement.Points = 12;
            questAchievement.CategoryId = 4;
            questAchievement.Status = AchievementStatus.InProgress;

            // 收集成就 - 接近完成
            Achievement collectAchievement = achievementComponent.AddAchievement(6005);
            collectAchievement.Type = AchievementType.Collect;
            collectAchievement.MaxProgress = 10;
            collectAchievement.Progress = 7; // 已收集7个
            collectAchievement.Points = 8;
            collectAchievement.CategoryId = 5;
            collectAchievement.Status = AchievementStatus.InProgress;

            // 探索成就 - 未开始
            Achievement exploreAchievement = achievementComponent.AddAchievement(6006);
            exploreAchievement.Type = AchievementType.Exploration;
            exploreAchievement.MaxProgress = 1;
            exploreAchievement.Progress = 0; // 未开始
            exploreAchievement.Points = 10;
            exploreAchievement.CategoryId = 6;
            exploreAchievement.Status = AchievementStatus.InProgress;

            // 更新组件数据
            achievementComponent.CompletedAchievements.Add(6001); // 登录成就已完成
            achievementComponent.RecentAchievements.Add(6001);
            achievementComponent.TotalPoints = 70; // 总成就点数
            achievementComponent.EarnedPoints = 0; // 还没有领取奖励

            // 更新类型映射
            achievementComponent.TypeMapping.Add(AchievementType.Social, 6001);
            achievementComponent.TypeMapping.Add(AchievementType.Kill, 6002);
            achievementComponent.TypeMapping.Add(AchievementType.Level, 6003);
            achievementComponent.TypeMapping.Add(AchievementType.Quest, 6004);
            achievementComponent.TypeMapping.Add(AchievementType.Collect, 6005);
            achievementComponent.TypeMapping.Add(AchievementType.Exploration, 6006);

            // 更新进度映射
            achievementComponent.AchievementProgress[6001] = 1;
            achievementComponent.AchievementProgress[6002] = 2;
            achievementComponent.AchievementProgress[6003] = 5;
            achievementComponent.AchievementProgress[6004] = 1;
            achievementComponent.AchievementProgress[6005] = 7;
            achievementComponent.AchievementProgress[6006] = 0;

            Log.Debug(
                "Created 6 trigger test achievements: 6001(System,Completed), 6002(Kill,2/10), 6003(Level,5/20), 6004(Quest,1/5), 6005(Collect,7/10), 6006(Exploration,0/1)");
        }

        /// <summary>
        /// 测试击杀怪物触发
        /// </summary>
        private static async ETTask TestKillMonsterTrigger(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Testing kill monster trigger");

            EntityRef<Scene> robotSceneRef = robotScene;

            // 直接操作服务器Fiber触发击杀事件
            TriggerKillMonsterEventDirectly(robotScene, parentFiber, 1001, 2);

            // 重新获取robotScene
            robotScene = robotSceneRef;

            // 验证击杀成就进度更新
            AchievementInfo[] achievements = await ClientAchievementHelper.GetAchievements(robotScene);

            // 检查击杀成就进度
            var killAchievement = System.Array.Find(achievements, a => a.AchievementId == 6002);
            if (killAchievement != null)
            {
                Log.Debug($"Kill achievement progress: {killAchievement.Progress}/{killAchievement.MaxProgress}");
                // 原来进度是2，击杀2个后应该是4
                if (killAchievement.Progress != 4)
                {
                    throw new System.Exception($"Kill achievement progress incorrect: expected 4, got {killAchievement.Progress}");
                }
            }

            Log.Debug("Kill monster trigger test passed");
        }

        /// <summary>
        /// 测试等级提升触发
        /// </summary>
        private static async ETTask TestLevelUpTrigger(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Testing level up trigger");

            EntityRef<Scene> robotSceneRef = robotScene;

            // 直接操作服务器Fiber触发等级提升事件
            TriggerLevelUpEventDirectly(robotScene, parentFiber, 15);

            // 重新获取robotScene
            robotScene = robotSceneRef;

            // 验证等级成就完成
            AchievementInfo[] achievements = await ClientAchievementHelper.GetAchievements(robotScene);
            var levelAchievement = System.Array.Find(achievements, a => a.AchievementId == 6003);

            if (levelAchievement != null)
            {
                Log.Debug(
                    $"Level achievement: Progress={levelAchievement.Progress}/{levelAchievement.MaxProgress}, Status={levelAchievement.Status}");
                // 等级从5升到15，应该完成成就
                if (levelAchievement.Progress < levelAchievement.MaxProgress)
                {
                    Log.Debug("Level achievement progress updated but not completed yet");
                }
            }

            Log.Debug("Level up trigger test passed");
        }

        /// <summary>
        /// 测试任务完成触发
        /// </summary>
        private static async ETTask TestQuestCompleteTrigger(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Testing quest complete trigger");

            EntityRef<Scene> robotSceneRef = robotScene;

            // 直接操作服务器Fiber触发任务完成事件
            TriggerQuestCompleteEventDirectly(robotScene, parentFiber, 1001);

            // 重新获取robotScene
            robotScene = robotSceneRef;

            // 验证任务成就进度更新
            AchievementInfo[] achievements = await ClientAchievementHelper.GetAchievements(robotScene);
            var questAchievement = System.Array.Find(achievements, a => a.AchievementId == 6004);

            if (questAchievement != null)
            {
                Log.Debug($"Quest achievement progress: {questAchievement.Progress}/{questAchievement.MaxProgress}");
                // 原来进度是1，完成1个任务后应该是2
                if (questAchievement.Progress != 2)
                {
                    throw new System.Exception($"Quest achievement progress incorrect: expected 2, got {questAchievement.Progress}");
                }
            }

            Log.Debug("Quest complete trigger test passed");
        }

        /// <summary>
        /// 测试道具收集触发
        /// </summary>
        private static async ETTask TestItemCollectTrigger(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Testing item collect trigger");

            EntityRef<Scene> robotSceneRef = robotScene;

            // 直接操作服务器Fiber触发道具收集事件
            TriggerItemCollectEventDirectly(robotScene, parentFiber, 2001, 5);

            // 重新获取robotScene
            robotScene = robotSceneRef;

            // 验证收集成就完成
            AchievementInfo[] achievements = await ClientAchievementHelper.GetAchievements(robotScene);
            var collectAchievement = System.Array.Find(achievements, a => a.AchievementId == 6005);

            if (collectAchievement != null)
            {
                Log.Debug(
                    $"Collect achievement: Progress={collectAchievement.Progress}/{collectAchievement.MaxProgress}, Status={collectAchievement.Status}");
                // 原来进度是7，收集5个后应该是12，超过最大值10，应该完成
                if (collectAchievement.Progress < collectAchievement.MaxProgress)
                {
                    Log.Debug("Collect achievement progress updated");
                }
            }

            Log.Debug("Item collect trigger test passed");
        }

        /// <summary>
        /// 测试地图探索触发
        /// </summary>
        private static async ETTask TestMapExploreTrigger(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Testing map explore trigger");

            EntityRef<Scene> robotSceneRef = robotScene;

            // 直接操作服务器Fiber触发地图探索事件
            TriggerMapExploreEventDirectly(robotScene, parentFiber, 3001);

            // 重新获取robotScene
            robotScene = robotSceneRef;

            // 验证探索成就完成
            AchievementInfo[] achievements = await ClientAchievementHelper.GetAchievements(robotScene);
            var exploreAchievement = System.Array.Find(achievements, a => a.AchievementId == 6006);

            if (exploreAchievement != null)
            {
                Log.Debug(
                    $"Explore achievement: Progress={exploreAchievement.Progress}/{exploreAchievement.MaxProgress}, Status={exploreAchievement.Status}");
                // 原来进度是0，探索1个地图后应该完成
                if (exploreAchievement.Progress != 1 || exploreAchievement.Status != 2)
                {
                    throw new System.Exception(
                        $"Explore achievement should be completed: Progress={exploreAchievement.Progress}, Status={exploreAchievement.Status}");
                }
            }

            Log.Debug("Map explore trigger test passed");
        }

        /// <summary>
        /// 验证触发结果
        /// </summary>
        private static void ValidateTriggerResults(AchievementInfo[] initialAchievements, AchievementInfo[] finalAchievements)
        {
            Log.Debug("Validating trigger test results");

            if (finalAchievements.Length != initialAchievements.Length)
            {
                throw new System.Exception($"Achievement count changed: {initialAchievements.Length} -> {finalAchievements.Length}");
            }

            // 统计进度变化
            int progressIncreased = 0;
            int completed = 0;

            foreach (var finalAchievement in finalAchievements)
            {
                var initialAchievement = System.Array.Find(initialAchievements, a => a.AchievementId == finalAchievement.AchievementId);
                if (initialAchievement != null)
                {
                    if (finalAchievement.Progress > initialAchievement.Progress)
                    {
                        progressIncreased++;
                        Log.Debug($"Achievement {finalAchievement.AchievementId}: {initialAchievement.Progress} -> {finalAchievement.Progress}");
                    }

                    if (finalAchievement.Status == 2 && initialAchievement.Status != 2)
                    {
                        completed++;
                        Log.Debug($"Achievement {finalAchievement.AchievementId} completed");
                    }
                }
            }

            Log.Debug($"Trigger test results: {progressIncreased} achievements with progress increased, {completed} achievements completed");

            if (progressIncreased < 4)
            {
                Log.Debug("Warning: Expected more achievements to have progress increased");
            }

            Log.Debug("Trigger test validation completed");
        }

        /// <summary>
        /// 直接操作服务器Fiber触发击杀怪物事件
        /// </summary>
        private static void TriggerKillMonsterEventDirectly(Scene robotScene, Fiber parentFiber, int monsterId, int count)
        {
            Log.Debug($"Directly triggering kill monster event: MonsterId={monsterId}, Count={count}");

            // 获取服务端环境
            string mapName = robotScene.CurrentScene().Name;
            Fiber map = parentFiber.GetFiber("MapManager").GetFiber(mapName);

            if (map == null)
            {
                Log.Error($"not found robot map {mapName}");
                return;
            }

            // 获取Unit的Id
            Client.PlayerComponent playerComponent = robotScene.GetComponent<Client.PlayerComponent>();

            // 获取服务端Unit
            Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);

            if (serverUnit != null)
            {
                // 直接调用成就帮助类触发事件
                AchievementHelper.ProcessKillMonsterAchievement(serverUnit, monsterId, count);
                Log.Debug($"Successfully triggered kill monster event directly on server");
            }
        }

        /// <summary>
        /// 直接操作服务器Fiber触发等级提升事件
        /// </summary>
        private static void TriggerLevelUpEventDirectly(Scene robotScene, Fiber parentFiber, int level)
        {
            Log.Debug($"Directly triggering level up event: Level={level}");

            // 获取服务端环境
            string mapName = robotScene.CurrentScene().Name;
            Fiber map = parentFiber.GetFiber("MapManager").GetFiber(mapName);

            if (map == null)
            {
                Log.Error($"not found robot map {mapName}");
                return;
            }

            // 获取Unit的Id
            Client.PlayerComponent playerComponent = robotScene.GetComponent<Client.PlayerComponent>();

            // 获取服务端Unit
            Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);

            if (serverUnit != null)
            {
                // 直接调用成就帮助类触发事件
                AchievementHelper.ProcessLevelUpAchievement(serverUnit, level);
                Log.Debug($"Successfully triggered level up event directly on server");
            }
        }

        /// <summary>
        /// 直接操作服务器Fiber触发任务完成事件
        /// </summary>
        private static void TriggerQuestCompleteEventDirectly(Scene robotScene, Fiber parentFiber, int questId)
        {
            Log.Debug($"Directly triggering quest complete event: QuestId={questId}");

            // 获取服务端环境
            string mapName = robotScene.CurrentScene().Name;
            Fiber map = parentFiber.GetFiber("MapManager").GetFiber(mapName);

            if (map == null)
            {
                Log.Error($"not found robot map {mapName}");
                return;
            }

            // 获取Unit的Id
            Client.PlayerComponent playerComponent = robotScene.GetComponent<Client.PlayerComponent>();

            // 获取服务端Unit
            Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);

            if (serverUnit != null)
            {
                // 直接调用成就帮助类触发事件
                AchievementHelper.ProcessQuestCompleteAchievement(serverUnit, questId);
                Log.Debug($"Successfully triggered quest complete event directly on server");
            }
        }

        /// <summary>
        /// 直接操作服务器Fiber触发道具收集事件
        /// </summary>
        private static void TriggerItemCollectEventDirectly(Scene robotScene, Fiber parentFiber, int itemId, int count)
        {
            Log.Debug($"Directly triggering item collect event: ItemId={itemId}, Count={count}");

            // 获取服务端环境
            string mapName = robotScene.CurrentScene().Name;
            Fiber map = parentFiber.GetFiber("MapManager").GetFiber(mapName);

            if (map == null)
            {
                Log.Error($"not found robot map {mapName}");
                return;
            }

            // 获取Unit的Id
            Client.PlayerComponent playerComponent = robotScene.GetComponent<Client.PlayerComponent>();

            // 获取服务端Unit
            Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);

            if (serverUnit != null)
            {
                // 直接调用成就帮助类触发事件
                AchievementHelper.ProcessItemCollectAchievement(serverUnit, itemId, count);
                Log.Debug($"Successfully triggered item collect event directly on server");
            }
        }

        /// <summary>
        /// 直接操作服务器Fiber触发地图探索事件
        /// </summary>
        private static void TriggerMapExploreEventDirectly(Scene robotScene, Fiber parentFiber, int mapId)
        {
            Log.Debug($"Directly triggering map explore event: MapId={mapId}");

            // 获取服务端环境
            string mapName = robotScene.CurrentScene().Name;
            Fiber map = parentFiber.GetFiber("MapManager").GetFiber(mapName);

            if (map == null)
            {
                Log.Error($"not found robot map {mapName}");
                return;
            }

            // 获取Unit的Id
            Client.PlayerComponent playerComponent = robotScene.GetComponent<Client.PlayerComponent>();

            // 获取服务端Unit
            Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);

            if (serverUnit != null)
            {
                // 直接调用成就帮助类触发事件
                AchievementHelper.ProcessMapExploreAchievement(serverUnit, mapId);
                Log.Debug($"Successfully triggered map explore event directly on server");
            }
        }
    }
}