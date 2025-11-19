using ET.Client;
using ET.Server;

namespace ET.Test
{
    [Invoke(RobotCaseType.AchievementTest)]
    public class RobotCase_004_Achievement_Handler : ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            // 创建机器人子Fiber
            Fiber robot = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Robot, "RobotCase_004_Achievement");

            Log.Debug("Achievement robot test started - Using ClientAchievementHelper");

            // 执行Achievement测试流程
            await TestAchievementFlowWithClientHelper(robot, fiber);

            Log.Debug("Achievement robot test completed successfully");
            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 使用ClientAchievementHelper测试Achievement系统完整流程
        /// </summary>
        private async ETTask TestAchievementFlowWithClientHelper(Fiber robot, Fiber parentFiber)
        {
            Log.Debug("Starting Achievement system test with ClientAchievementHelper");

            Scene robotScene = robot.Root;
            EntityRef<Scene> robotSceneRef = robotScene;

            // 验证机器人连接状态
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            if (clientSender == null)
            {
                Log.Error("ClientSenderComponent not found, robot may not be properly connected");
                return;
            }

            // 初始化Achievement测试环境
            InitializeAchievementTestEnvironment(robotScene, parentFiber);

            // 执行完整的Achievement流程测试
            robotScene = robotSceneRef; // await后重新获取
            await ExecuteAchievementFlowWithClientHelper(robotScene);

            Log.Debug("Achievement system test completed");
        }

        /// <summary>
        /// 直接访问服务器初始化Achievement测试环境
        /// </summary>
        private static void InitializeAchievementTestEnvironment(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Initializing Achievement test environment via direct server access");

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

            // 创建测试成就数据
            CreateTestAchievementData(achievementComponent);

            Log.Debug("Achievement test data prepared successfully via direct server access");
        }

        /// <summary>
        /// 创建测试成就数据
        /// </summary>
        private static void CreateTestAchievementData(AchievementComponent achievementComponent)
        {
            // 击杀成就 - 进行中
            Achievement killAchievement = achievementComponent.AddAchievement(1001);
            killAchievement.Type = AchievementType.Kill;
            killAchievement.MaxProgress = 10;
            killAchievement.Progress = 5; // 设置为进行中
            killAchievement.Points = 10;
            killAchievement.CategoryId = 1;
            killAchievement.Status = AchievementStatus.InProgress;

            // 等级成就 - 已完成但未领取
            Achievement levelAchievement = achievementComponent.AddAchievement(2001);
            levelAchievement.Type = AchievementType.Level;
            levelAchievement.MaxProgress = 10;
            levelAchievement.Progress = 10; // 设置为已完成
            levelAchievement.Points = 20;
            levelAchievement.CategoryId = 2;
            levelAchievement.Status = AchievementStatus.Completed;
            levelAchievement.CompleteTime = TimeInfo.Instance.ServerNow();

            // 任务成就 - 进行中
            Achievement questAchievement = achievementComponent.AddAchievement(3001);
            questAchievement.Type = AchievementType.Quest;
            questAchievement.MaxProgress = 5;
            questAchievement.Progress = 3; // 设置为进行中
            questAchievement.Points = 15;
            questAchievement.CategoryId = 3;
            questAchievement.Status = AchievementStatus.InProgress;

            // 探索成就 - 已领取
            Achievement exploreAchievement = achievementComponent.AddAchievement(4001);
            exploreAchievement.Type = AchievementType.Exploration;
            exploreAchievement.MaxProgress = 1;
            exploreAchievement.Progress = 1;
            exploreAchievement.Points = 5;
            exploreAchievement.CategoryId = 4;
            exploreAchievement.Status = AchievementStatus.Claimed;
            exploreAchievement.CompleteTime = TimeInfo.Instance.ServerNow() - 3600000; // 1小时前
            exploreAchievement.ClaimTime = TimeInfo.Instance.ServerNow() - 1800000; // 30分钟前

            // 更新组件数据
            achievementComponent.CompletedAchievements.Add(2001); // 等级成就已完成
            achievementComponent.CompletedAchievements.Add(4001); // 探索成就也已完成（领取前必须先完成）
            achievementComponent.ClaimedAchievements.Add(4001); // 探索成就已领取
            achievementComponent.RecentAchievements.Add(4001); // 最近完成的成就
            achievementComponent.TotalPoints = 50; // 总成就点数
            achievementComponent.EarnedPoints = 5; // 已获得点数（只有探索成就）

            // 更新类型映射
            achievementComponent.TypeMapping.Add(AchievementType.Kill, 1001);
            achievementComponent.TypeMapping.Add(AchievementType.Level, 2001);
            achievementComponent.TypeMapping.Add(AchievementType.Quest, 3001);
            achievementComponent.TypeMapping.Add(AchievementType.Exploration, 4001);

            // 更新进度映射
            achievementComponent.AchievementProgress[1001] = 5;
            achievementComponent.AchievementProgress[2001] = 10;
            achievementComponent.AchievementProgress[3001] = 3;
            achievementComponent.AchievementProgress[4001] = 1;

            Log.Debug("Created 4 test achievements: 1001(Kill,InProgress), 2001(Level,Completed), 3001(Quest,InProgress), 4001(Exploration,Claimed)");
        }

        /// <summary>
        /// 使用ClientAchievementHelper执行完整的Achievement流程测试
        /// </summary>
        private static async ETTask ExecuteAchievementFlowWithClientHelper(Scene robotScene)
        {
            Log.Debug("Executing complete Achievement flow test with ClientAchievementHelper");

            EntityRef<Scene> robotSceneRef = robotScene;

            // 步骤1：获取成就列表
            Log.Debug("Step 1: Getting achievement list");
            AchievementInfo[] achievements = await ClientAchievementHelper.GetAchievements(robotScene);
            Log.Debug($"Found {achievements.Length} achievements");

            if (achievements.Length == 0)
            {
                Log.Error("No achievements found, test failed");
                return;
            }

            // 步骤2：获取成就统计
            Log.Debug("Step 2: Getting achievement stats");
            robotScene = robotSceneRef; // await后重新获取
            AchievementStatsInfo stats = await ClientAchievementHelper.GetAchievementStats(robotScene);
            if (stats != null)
            {
                Log.Debug(
                    $"Achievement stats: {stats.CompletedAchievements}/{stats.TotalAchievements} completed, {stats.EarnedPoints}/{stats.TotalPoints} points");
            }

            // 步骤3：尝试领取已完成的成就奖励
            Log.Debug("Step 3: Claiming completed achievement reward");
            robotScene = robotSceneRef; // await后重新获取

            // 查找已完成但未领取的成就
            var completedAchievement = System.Array.Find(achievements, a => a.Status == 2); // Status=2表示已完成
            if (completedAchievement != null)
            {
                bool claimResult = await ClientAchievementHelper.ClaimAchievementReward(robotScene, completedAchievement.AchievementId);
                Log.Debug($"Claim achievement {completedAchievement.AchievementId} reward result: {claimResult}");
            }
            else
            {
                Log.Debug("No completed achievements to claim");
            }

            // 步骤4：获取成就详情
            Log.Debug("Step 4: Getting achievement details");
            robotScene = robotSceneRef; // await后重新获取
            if (achievements.Length > 0)
            {
                AchievementDetailInfo detail = await ClientAchievementHelper.GetAchievementDetail(robotScene, achievements[0].AchievementId);
                if (detail != null)
                {
                    Log.Debug($"Achievement detail: {detail.AchievementId} - {detail.AchievementName}");
                }
            }

            // 步骤5：测试成就分类功能
            Log.Debug("Step 5: Testing achievement categories");
            robotScene = robotSceneRef; // await后重新获取
            await TestAchievementCategories(robotScene);

            // 步骤6：测试按分类获取成就
            Log.Debug("Step 6: Testing get achievements by category");
            robotScene = robotSceneRef; // await后重新获取
            await TestGetAchievementsByCategory(robotScene);

            // 步骤7：再次获取统计信息验证变化
            Log.Debug("Step 7: Final stats check");
            robotScene = robotSceneRef; // await后重新获取
            AchievementStatsInfo finalStats = await ClientAchievementHelper.GetAchievementStats(robotScene);
            if (finalStats != null)
            {
                Log.Debug(
                    $"Final achievement stats: {finalStats.CompletedAchievements}/{finalStats.TotalAchievements} completed, {finalStats.EarnedPoints}/{finalStats.TotalPoints} points");
            }

            // 步骤8：验证数据一致性
            Log.Debug("Step 8: Validating data consistency");
            robotScene = robotSceneRef; // await后重新获取
            await ValidateDataConsistency(robotScene, achievements, finalStats);

            Log.Debug("Complete Achievement flow test executed successfully using ClientAchievementHelper");
        }

        /// <summary>
        /// 测试成就分类功能
        /// </summary>
        private static async ETTask TestAchievementCategories(Scene robotScene)
        {
            Log.Debug("Testing achievement categories");

            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();

            // 获取成就分类
            C2M_GetAchievementCategories request = C2M_GetAchievementCategories.Create();
            M2C_GetAchievementCategories response = await clientSender.Call(request) as M2C_GetAchievementCategories;

            robotScene = robotSceneRef; // await后重新获取

            if (response.Error == ErrorCode.ERR_Success)
            {
                Log.Debug($"Retrieved {response.Categories.Count} achievement categories");
                foreach (var category in response.Categories)
                {
                    Log.Debug($"Category: {category.CategoryName} ({category.CategoryId}) - {category.CompletedCount}/{category.TotalCount}");
                }
            }
            else
            {
                Log.Debug($"Get achievement categories returned: {response.Error} - {response.Message}");
            }
        }

        /// <summary>
        /// 测试按分类获取成就
        /// </summary>
        private static async ETTask TestGetAchievementsByCategory(Scene robotScene)
        {
            Log.Debug("Testing get achievements by category");

            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();

            // 测试获取分类1的成就
            C2M_GetAchievements request = C2M_GetAchievements.Create();
            request.CategoryId = 1;

            M2C_GetAchievements response = await clientSender.Call(request) as M2C_GetAchievements;

            robotScene = robotSceneRef; // await后重新获取

            if (response.Error == ErrorCode.ERR_Success)
            {
                Log.Debug($"Category 1 has {response.Achievements.Count} achievements");
                foreach (var achievement in response.Achievements)
                {
                    Log.Debug(
                        $"Achievement in category 1: {achievement.AchievementId}, Status: {achievement.Status}, Progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }
            else
            {
                Log.Debug($"Get achievements by category returned: {response.Error} - {response.Message}");
            }
        }

        /// <summary>
        /// 验证数据一致性
        /// </summary>
        private static async ETTask ValidateDataConsistency(Scene robotScene, AchievementInfo[] initialAchievements, AchievementStatsInfo finalStats)
        {
            Log.Debug("Validating achievement data consistency");

            EntityRef<Scene> robotSceneRef = robotScene;

            // 重新获取当前成就列表
            AchievementInfo[] currentAchievements = await ClientAchievementHelper.GetAchievements(robotScene);
            robotScene = robotSceneRef; // await后重新获取

            // 验证成就数量一致性
            if (currentAchievements.Length != finalStats.TotalAchievements)
            {
                Log.Error($"Achievement count inconsistency: List={currentAchievements.Length}, Stats={finalStats.TotalAchievements}");
            }

            // 计算实际完成的成就数量
            int actualCompleted = 0;
            int actualClaimed = 0;

            foreach (var achievement in currentAchievements)
            {
                if (achievement.Status >= 2) // 2=已完成, 3=已领取
                {
                    actualCompleted++;
                }

                if (achievement.Status == 3) // 3=已领取
                {
                    actualClaimed++;
                }
            }

            // 验证完成数量一致性
            if (actualCompleted != finalStats.CompletedAchievements)
            {
                Log.Error($"Completed count inconsistency: Actual={actualCompleted}, Stats={finalStats.CompletedAchievements}");
            }
            else
            {
                Log.Debug($"Completed count validation passed: {actualCompleted}");
            }

            // 比较初始状态和最终状态的变化
            int progressChanged = 0;
            int statusChanged = 0;

            foreach (var currentAchievement in currentAchievements)
            {
                var initialAchievement = System.Array.Find(initialAchievements, a => a.AchievementId == currentAchievement.AchievementId);
                if (initialAchievement != null)
                {
                    if (currentAchievement.Progress != initialAchievement.Progress)
                    {
                        progressChanged++;
                        Log.Debug(
                            $"Progress changed for {currentAchievement.AchievementId}: {initialAchievement.Progress} -> {currentAchievement.Progress}");
                    }

                    if (currentAchievement.Status != initialAchievement.Status)
                    {
                        statusChanged++;
                        Log.Debug(
                            $"Status changed for {currentAchievement.AchievementId}: {initialAchievement.Status} -> {currentAchievement.Status}");
                    }
                }
            }

            Log.Debug($"Data consistency validation completed: {progressChanged} progress changes, {statusChanged} status changes");
        }
    }
}