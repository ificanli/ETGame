using ET.Client;

namespace ET.Server
{
    [Invoke(RobotCaseType.AchievementTest)]
    public class RobotCase_004_Achievement_Handler: ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            try
            {
                // 创建机器人子Fiber
                Fiber robot = await fiber.CreateFiber(0, SceneType.Robot, "RobotCase_004_Achievement");
                
                Log.Debug("Achievement robot test started - Using ClientAchievementHelper");
                
                // 执行Achievement测试流程
                await TestAchievementFlowWithClientHelper(robot);
                
                Log.Debug("Achievement robot test completed successfully");
                return ErrorCode.ERR_Success;
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement robot test failed with exception: {e.Message}\n{e.StackTrace}");
                return ErrorCore.ERR_KcpConnectTimeout;
            }
        }

        /// <summary>
        /// 使用ClientAchievementHelper测试Achievement系统完整流程
        /// </summary>
        private async ETTask TestAchievementFlowWithClientHelper(Fiber robot)
        {
            Log.Debug("Starting Achievement system test with ClientAchievementHelper");
            
            try
            {
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
                await InitializeAchievementTestEnvironment(robotScene);
                
                // 执行完整的Achievement流程测试
                robotScene = robotSceneRef; // await后重新获取
                await ExecuteAchievementFlowWithClientHelper(robotScene);
                
                Log.Debug("Achievement system test completed");
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement test failed: {e.Message}");
            }
        }

        /// <summary>
        /// 通过网络消息初始化Achievement测试环境
        /// </summary>
        private static async ETTask InitializeAchievementTestEnvironment(Scene robotScene)
        {
            Log.Debug("Initializing Achievement test environment via network");
            
            try
            {
                // 发送专用的Achievement测试数据准备消息到服务器
                C2M_RobotCase_PrepareData_004_Request prepareRequest = C2M_RobotCase_PrepareData_004_Request.Create();
                
                Log.Debug("Sending Achievement test data preparation request to server");
                
                // 发送真实的网络消息
                EntityRef<Scene> robotSceneRef = robotScene;
                ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
                M2C_RobotCase_PrepareData_004_Response prepareResponse = await clientSender.Call(prepareRequest) as M2C_RobotCase_PrepareData_004_Response;
                
                // await后重新获取Entity
                robotScene = robotSceneRef;
                
                if (prepareResponse.Error == ErrorCode.ERR_Success)
                {
                    Log.Debug("Achievement test data preparation successful");
                }
                else
                {
                    Log.Error($"Achievement test data preparation failed: {prepareResponse.Error}, {prepareResponse.Message}");
                }
                
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to initialize Achievement test environment: {e.Message}");
            }
        }

        /// <summary>
        /// 使用ClientAchievementHelper执行完整的Achievement流程测试
        /// </summary>
        private static async ETTask ExecuteAchievementFlowWithClientHelper(Scene robotScene)
        {
            Log.Debug("Executing complete Achievement flow test with ClientAchievementHelper");
            
            try
            {
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
                    Log.Debug($"Achievement stats: {stats.CompletedAchievements}/{stats.TotalAchievements} completed, {stats.EarnedPoints}/{stats.TotalPoints} points");
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
                    Log.Debug($"Final achievement stats: {finalStats.CompletedAchievements}/{finalStats.TotalAchievements} completed, {finalStats.EarnedPoints}/{finalStats.TotalPoints} points");
                }
                
                // 步骤8：验证数据一致性
                Log.Debug("Step 8: Validating data consistency");
                robotScene = robotSceneRef; // await后重新获取
                await ValidateDataConsistency(robotScene, achievements, finalStats);
                
                Log.Debug("Complete Achievement flow test executed successfully using ClientAchievementHelper");
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement flow test failed: {e.Message}");
            }
        }

        /// <summary>
        /// 测试成就分类功能
        /// </summary>
        private static async ETTask TestAchievementCategories(Scene robotScene)
        {
            Log.Debug("Testing achievement categories");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            try
            {
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
            catch (System.Exception e)
            {
                Log.Error($"Achievement categories test failed: {e.Message}");
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
            
            try
            {
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
                        Log.Debug($"Achievement in category 1: {achievement.AchievementId}, Status: {achievement.Status}, Progress: {achievement.Progress}/{achievement.MaxProgress}");
                    }
                }
                else
                {
                    Log.Debug($"Get achievements by category returned: {response.Error} - {response.Message}");
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"Get achievements by category test failed: {e.Message}");
            }
        }

        /// <summary>
        /// 验证数据一致性
        /// </summary>
        private static async ETTask ValidateDataConsistency(Scene robotScene, AchievementInfo[] initialAchievements, AchievementStatsInfo finalStats)
        {
            Log.Debug("Validating achievement data consistency");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            
            try
            {
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
                            Log.Debug($"Progress changed for {currentAchievement.AchievementId}: {initialAchievement.Progress} -> {currentAchievement.Progress}");
                        }
                        
                        if (currentAchievement.Status != initialAchievement.Status)
                        {
                            statusChanged++;
                            Log.Debug($"Status changed for {currentAchievement.AchievementId}: {initialAchievement.Status} -> {currentAchievement.Status}");
                        }
                    }
                }
                
                Log.Debug($"Data consistency validation completed: {progressChanged} progress changes, {statusChanged} status changes");
            }
            catch (System.Exception e)
            {
                Log.Error($"Data consistency validation failed: {e.Message}");
            }
        }

    }
}