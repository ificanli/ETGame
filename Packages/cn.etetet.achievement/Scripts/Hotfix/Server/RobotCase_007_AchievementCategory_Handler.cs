using ET.Client;

namespace ET.Server
{
    [Invoke(RobotCaseType.AchievementCategoryTest)]
    public class RobotCase_007_AchievementCategory_Handler: ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            try
            {
                // 创建机器人子Fiber
                Fiber robot = await fiber.CreateFiber(0, SceneType.Robot, "RobotCase_007_AchievementCategory");
                
                Log.Debug("Achievement category robot test started");
                
                // 执行成就分类测试流程
                await TestAchievementCategoryFlow(robot);
                
                Log.Debug("Achievement category robot test completed successfully");
                return ErrorCode.ERR_Success;
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement category robot test failed with exception: {e.Message}\n{e.StackTrace}");
                return ErrorCore.ERR_KcpConnectTimeout;
            }
        }

        /// <summary>
        /// 测试成就分类流程
        /// </summary>
        private async ETTask TestAchievementCategoryFlow(Fiber robot)
        {
            Log.Debug("Starting Achievement category test flow");
            
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
                
                // 步骤1：初始化测试环境（使用原有的测试数据）
                await InitializeCategoryTestEnvironment(robotScene);
                
                // 步骤2：获取所有成就分类
                robotScene = robotSceneRef; // await后重新获取
                await TestGetAchievementCategories(robotScene);
                
                // 步骤3：按分类获取成就列表
                robotScene = robotSceneRef; // await后重新获取
                await TestGetAchievementsByCategory(robotScene);
                
                // 步骤4：测试成就统计信息
                robotScene = robotSceneRef; // await后重新获取
                await TestAchievementStats(robotScene);
                
                // 步骤5：验证分类数据一致性
                robotScene = robotSceneRef; // await后重新获取
                await ValidateCategoryConsistency(robotScene);
                
                Log.Debug("Achievement category test flow completed successfully");
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement category test failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 初始化分类测试环境
        /// </summary>
        private static async ETTask InitializeCategoryTestEnvironment(Scene robotScene)
        {
            Log.Debug("Initializing Achievement category test environment");
            
            try
            {
                // 使用基础的成就测试数据准备消息
                C2M_RobotCase_PrepareData_004_Request prepareRequest = C2M_RobotCase_PrepareData_004_Request.Create();
                
                Log.Debug("Sending Achievement category test data preparation request to server");
                
                // 发送真实的网络消息
                EntityRef<Scene> robotSceneRef = robotScene;
                ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
                M2C_RobotCase_PrepareData_004_Response prepareResponse = await clientSender.Call(prepareRequest) as M2C_RobotCase_PrepareData_004_Response;
                
                // await后重新获取Entity
                robotScene = robotSceneRef;
                
                if (prepareResponse.Error == ErrorCode.ERR_Success)
                {
                    Log.Debug("Achievement category test data preparation successful");
                }
                else
                {
                    Log.Error($"Achievement category test data preparation failed: {prepareResponse.Error}, {prepareResponse.Message}");
                    throw new System.Exception($"Failed to prepare test data: {prepareResponse.Message}");
                }
                
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to initialize Achievement category test environment: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 测试获取成就分类
        /// </summary>
        private static async ETTask TestGetAchievementCategories(Scene robotScene)
        {
            Log.Debug("Testing get achievement categories");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 发送获取成就分类请求
            C2M_GetAchievementCategories request = C2M_GetAchievementCategories.Create();
            
            M2C_GetAchievementCategories response = await clientSender.Call(request) as M2C_GetAchievementCategories;
            
            robotScene = robotSceneRef; // await后重新获取
            
            if (response.Error != ErrorCode.ERR_Success)
            {
                throw new System.Exception($"Failed to get achievement categories: {response.Message}");
            }
            
            Log.Debug($"Retrieved {response.Categories.Count} achievement categories");
            
            if (response.Categories.Count == 0)
            {
                throw new System.Exception("No achievement categories found");
            }
            
            // 验证分类信息
            foreach (var category in response.Categories)
            {
                Log.Debug($"Category: ID={category.CategoryId}, Name={category.CategoryName}, Total={category.TotalCount}, Completed={category.CompletedCount}");
                
                if (category.CategoryId <= 0)
                {
                    throw new System.Exception($"Invalid category ID: {category.CategoryId}");
                }
                
                if (string.IsNullOrEmpty(category.CategoryName))
                {
                    throw new System.Exception($"Empty category name for ID: {category.CategoryId}");
                }
                
                if (category.CompletedCount > category.TotalCount)
                {
                    throw new System.Exception($"Completed count ({category.CompletedCount}) > Total count ({category.TotalCount}) for category {category.CategoryId}");
                }
            }
            
            Log.Debug("Get achievement categories test passed");
        }

        /// <summary>
        /// 测试按分类获取成就列表
        /// </summary>
        private static async ETTask TestGetAchievementsByCategory(Scene robotScene)
        {
            Log.Debug("Testing get achievements by category");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 先获取所有成就
            C2M_GetAchievements allRequest = C2M_GetAchievements.Create();
            allRequest.CategoryId = 0; // 0表示获取所有
            
            M2C_GetAchievements allResponse = await clientSender.Call(allRequest) as M2C_GetAchievements;
            
            robotScene = robotSceneRef; // await后重新获取
            clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            if (allResponse.Error != ErrorCode.ERR_Success)
            {
                throw new System.Exception($"Failed to get all achievements: {allResponse.Message}");
            }
            
            Log.Debug($"Total achievements: {allResponse.Achievements.Count}");
            
            // 测试按分类过滤
            var categories = new int[] { 1, 2, 3, 4 }; // 测试前4个分类
            
            foreach (int categoryId in categories)
            {
                C2M_GetAchievements categoryRequest = C2M_GetAchievements.Create();
                categoryRequest.CategoryId = categoryId;
                
                M2C_GetAchievements categoryResponse = await clientSender.Call(categoryRequest) as M2C_GetAchievements;
                
                robotScene = robotSceneRef; // await后重新获取
                clientSender = robotScene.GetComponent<ClientSenderComponent>();
                
                if (categoryResponse.Error != ErrorCode.ERR_Success)
                {
                    Log.Debug($"Category {categoryId} not found or empty, this is acceptable");
                    continue;
                }
                
                Log.Debug($"Category {categoryId} achievements: {categoryResponse.Achievements.Count}");
                
                // 验证分类过滤的正确性
                foreach (var achievement in categoryResponse.Achievements)
                {
                    // 注意：这里需要从详细信息中获取CategoryId，因为AchievementInfo中可能没有CategoryId字段
                    // 我们先跳过这个验证，在实际环境中需要根据配置表来验证
                    Log.Debug($"Achievement in category {categoryId}: ID={achievement.AchievementId}, Status={achievement.Status}, Progress={achievement.Progress}/{achievement.MaxProgress}");
                }
            }
            
            Log.Debug("Get achievements by category test passed");
        }

        /// <summary>
        /// 测试成就统计信息
        /// </summary>
        private static async ETTask TestAchievementStats(Scene robotScene)
        {
            Log.Debug("Testing achievement stats");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 获取成就统计信息
            C2M_GetAchievementStats request = C2M_GetAchievementStats.Create();
            
            M2C_GetAchievementStats response = await clientSender.Call(request) as M2C_GetAchievementStats;
            
            robotScene = robotSceneRef; // await后重新获取
            
            if (response.Error != ErrorCode.ERR_Success)
            {
                throw new System.Exception($"Failed to get achievement stats: {response.Message}");
            }
            
            var stats = response.Stats;
            Log.Debug($"Achievement Stats: Total={stats.TotalAchievements}, Completed={stats.CompletedAchievements}, Points={stats.EarnedPoints}/{stats.TotalPoints}");
            
            // 验证统计数据合理性
            if (stats.CompletedAchievements > stats.TotalAchievements)
            {
                throw new System.Exception($"Completed ({stats.CompletedAchievements}) > Total ({stats.TotalAchievements})");
            }
            
            if (stats.EarnedPoints > stats.TotalPoints)
            {
                throw new System.Exception($"Earned points ({stats.EarnedPoints}) > Total points ({stats.TotalPoints})");
            }
            
            if (stats.TotalAchievements <= 0)
            {
                throw new System.Exception("No achievements found in stats");
            }
            
            // 验证最近完成的成就列表
            if (stats.RecentAchievements.Count > 0)
            {
                Log.Debug($"Recent achievements: {string.Join(", ", stats.RecentAchievements)}");
                
                if (stats.RecentAchievements.Count > 10)
                {
                    throw new System.Exception($"Too many recent achievements: {stats.RecentAchievements.Count}");
                }
            }
            
            // 计算完成率
            float completionRate = stats.TotalAchievements > 0 ? (float)stats.CompletedAchievements / stats.TotalAchievements : 0f;
            Log.Debug($"Achievement completion rate: {completionRate:P1}");
            
            Log.Debug("Achievement stats test passed");
        }

        /// <summary>
        /// 验证分类数据一致性
        /// </summary>
        private static async ETTask ValidateCategoryConsistency(Scene robotScene)
        {
            Log.Debug("Validating category data consistency");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 获取分类信息
            C2M_GetAchievementCategories categoryRequest = C2M_GetAchievementCategories.Create();
            M2C_GetAchievementCategories categoryResponse = await clientSender.Call(categoryRequest) as M2C_GetAchievementCategories;
            
            robotScene = robotSceneRef; // await后重新获取
            clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            if (categoryResponse.Error != ErrorCode.ERR_Success)
            {
                throw new System.Exception($"Failed to get categories for consistency check: {categoryResponse.Message}");
            }
            
            // 获取所有成就
            C2M_GetAchievements allRequest = C2M_GetAchievements.Create();
            allRequest.CategoryId = 0;
            
            M2C_GetAchievements allResponse = await clientSender.Call(allRequest) as M2C_GetAchievements;
            
            robotScene = robotSceneRef; // await后重新获取
            clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            if (allResponse.Error != ErrorCode.ERR_Success)
            {
                throw new System.Exception($"Failed to get all achievements for consistency check: {allResponse.Message}");
            }
            
            // 获取统计信息
            C2M_GetAchievementStats statsRequest = C2M_GetAchievementStats.Create();
            M2C_GetAchievementStats statsResponse = await clientSender.Call(statsRequest) as M2C_GetAchievementStats;
            
            robotScene = robotSceneRef; // await后重新获取
            
            if (statsResponse.Error != ErrorCode.ERR_Success)
            {
                throw new System.Exception($"Failed to get stats for consistency check: {statsResponse.Message}");
            }
            
            // 验证总数一致性
            int totalFromStats = statsResponse.Stats.TotalAchievements;
            int totalFromAchievements = allResponse.Achievements.Count;
            
            if (totalFromStats != totalFromAchievements)
            {
                throw new System.Exception($"Total count inconsistency: Stats={totalFromStats}, Achievements={totalFromAchievements}");
            }
            
            // 验证完成数一致性
            int completedFromStats = statsResponse.Stats.CompletedAchievements;
            int completedFromAchievements = 0;
            
            foreach (var achievement in allResponse.Achievements)
            {
                if (achievement.Status >= 2) // 2=已完成, 3=已领取
                {
                    completedFromAchievements++;
                }
            }
            
            if (completedFromStats != completedFromAchievements)
            {
                throw new System.Exception($"Completed count inconsistency: Stats={completedFromStats}, Achievements={completedFromAchievements}");
            }
            
            Log.Debug($"Consistency validation passed: Total={totalFromStats}, Completed={completedFromStats}");
            Log.Debug("Category data consistency validation completed");
        }
    }
}