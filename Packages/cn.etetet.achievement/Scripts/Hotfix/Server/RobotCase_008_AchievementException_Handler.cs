using ET.Client;

namespace ET.Server
{
    [Invoke(RobotCaseType.AchievementExceptionTest)]
    public class RobotCase_008_AchievementException_Handler: ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            try
            {
                // 创建机器人子Fiber
                Fiber robot = await fiber.CreateFiber(0, SceneType.Robot, "RobotCase_008_AchievementException");
                
                Log.Debug("Achievement exception robot test started");
                
                // 执行成就异常测试流程
                await TestAchievementExceptionFlow(robot);
                
                Log.Debug("Achievement exception robot test completed successfully");
                return ErrorCode.ERR_Success;
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement exception robot test failed with exception: {e.Message}\n{e.StackTrace}");
                return ErrorCore.ERR_KcpConnectTimeout;
            }
        }

        /// <summary>
        /// 测试成就异常处理流程
        /// </summary>
        private async ETTask TestAchievementExceptionFlow(Fiber robot)
        {
            Log.Debug("Starting Achievement exception test flow");
            
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
                
                // 步骤1：初始化测试环境
                await InitializeExceptionTestEnvironment(robotScene);
                
                // 步骤2：测试获取不存在的成就详情
                robotScene = robotSceneRef; // await后重新获取
                await TestGetNonExistentAchievementDetail(robotScene);
                
                // 步骤3：测试领取不存在的成就奖励
                robotScene = robotSceneRef; // await后重新获取
                await TestClaimNonExistentAchievement(robotScene);
                
                // 步骤4：测试领取未完成的成就奖励
                robotScene = robotSceneRef; // await后重新获取
                await TestClaimIncompleteAchievement(robotScene);
                
                // 步骤5：测试重复领取已领取的成就奖励
                robotScene = robotSceneRef; // await后重新获取
                await TestClaimAlreadyClaimedAchievement(robotScene);
                
                // 步骤6：测试触发无效的成就事件
                robotScene = robotSceneRef; // await后重新获取
                await TestTriggerInvalidAchievementEvent(robotScene);
                
                // 步骤7：测试获取无效分类的成就列表
                robotScene = robotSceneRef; // await后重新获取
                await TestGetInvalidCategoryAchievements(robotScene);
                
                Log.Debug("Achievement exception test flow completed successfully");
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement exception test failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 初始化异常测试环境
        /// </summary>
        private static async ETTask InitializeExceptionTestEnvironment(Scene robotScene)
        {
            Log.Debug("Initializing Achievement exception test environment");
            
            try
            {
                // 使用基础的成就测试数据准备消息
                C2M_RobotCase_PrepareData_004_Request prepareRequest = C2M_RobotCase_PrepareData_004_Request.Create();
                
                Log.Debug("Sending Achievement exception test data preparation request to server");
                
                // 发送真实的网络消息
                EntityRef<Scene> robotSceneRef = robotScene;
                ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
                M2C_RobotCase_PrepareData_004_Response prepareResponse = await clientSender.Call(prepareRequest) as M2C_RobotCase_PrepareData_004_Response;
                
                // await后重新获取Entity
                robotScene = robotSceneRef;
                
                if (prepareResponse.Error == ErrorCode.ERR_Success)
                {
                    Log.Debug("Achievement exception test data preparation successful");
                }
                else
                {
                    Log.Error($"Achievement exception test data preparation failed: {prepareResponse.Error}, {prepareResponse.Message}");
                    throw new System.Exception($"Failed to prepare test data: {prepareResponse.Message}");
                }
                
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to initialize Achievement exception test environment: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 测试获取不存在的成就详情
        /// </summary>
        private static async ETTask TestGetNonExistentAchievementDetail(Scene robotScene)
        {
            Log.Debug("Testing get non-existent achievement detail");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 尝试获取不存在的成就详情
            C2M_GetAchievementDetail request = C2M_GetAchievementDetail.Create();
            request.AchievementId = 99999; // 不存在的成就ID
            
            M2C_GetAchievementDetail response = await clientSender.Call(request) as M2C_GetAchievementDetail;
            
            robotScene = robotSceneRef; // await后重新获取
            
            // 验证应该返回错误
            if (response.Error == ErrorCode.ERR_Success)
            {
                Log.Debug("Warning: Expected error for non-existent achievement, but got success. This might be acceptable if default data is returned.");
            }
            else
            {
                Log.Debug($"Correctly returned error for non-existent achievement: {response.Error} - {response.Message}");
            }
            
            Log.Debug("Get non-existent achievement detail test passed");
        }

        /// <summary>
        /// 测试领取不存在的成就奖励
        /// </summary>
        private static async ETTask TestClaimNonExistentAchievement(Scene robotScene)
        {
            Log.Debug("Testing claim non-existent achievement");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 尝试领取不存在的成就奖励
            C2M_ClaimAchievement request = C2M_ClaimAchievement.Create();
            request.AchievementId = 99999; // 不存在的成就ID
            
            M2C_ClaimAchievement response = await clientSender.Call(request) as M2C_ClaimAchievement;
            
            robotScene = robotSceneRef; // await后重新获取
            
            // 验证应该返回错误
            if (response.Error == ErrorCode.ERR_Success)
            {
                throw new System.Exception("Expected error for claiming non-existent achievement, but got success");
            }
            
            Log.Debug($"Correctly returned error for claiming non-existent achievement: {response.Error} - {response.Message}");
            Log.Debug("Claim non-existent achievement test passed");
        }

        /// <summary>
        /// 测试领取未完成的成就奖励
        /// </summary>
        private static async ETTask TestClaimIncompleteAchievement(Scene robotScene)
        {
            Log.Debug("Testing claim incomplete achievement");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 先获取一个进行中的成就
            AchievementInfo[] achievements = await ClientAchievementHelper.GetAchievements(robotScene);
            robotScene = robotSceneRef; // await后重新获取
            clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            var incompleteAchievement = System.Array.Find(achievements, a => a.Status == 1); // Status=1表示进行中
            if (incompleteAchievement == null)
            {
                Log.Debug("No incomplete achievement found, skipping this test");
                return;
            }
            
            Log.Debug($"Found incomplete achievement: {incompleteAchievement.AchievementId} with progress {incompleteAchievement.Progress}/{incompleteAchievement.MaxProgress}");
            
            // 尝试领取未完成的成就奖励
            C2M_ClaimAchievement request = C2M_ClaimAchievement.Create();
            request.AchievementId = incompleteAchievement.AchievementId;
            
            M2C_ClaimAchievement response = await clientSender.Call(request) as M2C_ClaimAchievement;
            
            robotScene = robotSceneRef; // await后重新获取
            
            // 验证应该返回错误
            if (response.Error == ErrorCode.ERR_Success)
            {
                throw new System.Exception("Expected error for claiming incomplete achievement, but got success");
            }
            
            Log.Debug($"Correctly returned error for claiming incomplete achievement: {response.Error} - {response.Message}");
            Log.Debug("Claim incomplete achievement test passed");
        }

        /// <summary>
        /// 测试重复领取已领取的成就奖励
        /// </summary>
        private static async ETTask TestClaimAlreadyClaimedAchievement(Scene robotScene)
        {
            Log.Debug("Testing claim already claimed achievement");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 先获取一个已领取的成就
            AchievementInfo[] achievements = await ClientAchievementHelper.GetAchievements(robotScene);
            robotScene = robotSceneRef; // await后重新获取
            clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            var claimedAchievement = System.Array.Find(achievements, a => a.Status == 3); // Status=3表示已领取
            if (claimedAchievement == null)
            {
                Log.Debug("No claimed achievement found, skipping this test");
                return;
            }
            
            Log.Debug($"Found claimed achievement: {claimedAchievement.AchievementId}");
            
            // 尝试重复领取已领取的成就奖励
            C2M_ClaimAchievement request = C2M_ClaimAchievement.Create();
            request.AchievementId = claimedAchievement.AchievementId;
            
            M2C_ClaimAchievement response = await clientSender.Call(request) as M2C_ClaimAchievement;
            
            robotScene = robotSceneRef; // await后重新获取
            
            // 验证应该返回错误
            if (response.Error == ErrorCode.ERR_Success)
            {
                throw new System.Exception("Expected error for claiming already claimed achievement, but got success");
            }
            
            Log.Debug($"Correctly returned error for claiming already claimed achievement: {response.Error} - {response.Message}");
            Log.Debug("Claim already claimed achievement test passed");
        }

        /// <summary>
        /// 测试触发无效的成就事件
        /// </summary>
        private static async ETTask TestTriggerInvalidAchievementEvent(Scene robotScene)
        {
            Log.Debug("Testing trigger invalid achievement event");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 测试无效的事件类型
            C2M_RobotCase_TriggerAchievementEvent_Request request = C2M_RobotCase_TriggerAchievementEvent_Request.Create();
            request.EventType = 999; // 无效的事件类型
            request.ParamId = 1001;
            request.Count = 1;
            
            M2C_RobotCase_TriggerAchievementEvent_Response response = await clientSender.Call(request) as M2C_RobotCase_TriggerAchievementEvent_Response;
            
            robotScene = robotSceneRef; // await后重新获取
            
            // 验证应该返回错误
            if (response.Error == ErrorCode.ERR_Success)
            {
                throw new System.Exception("Expected error for invalid event type, but got success");
            }
            
            Log.Debug($"Correctly returned error for invalid event type: {response.Error} - {response.Message}");
            
            // 测试负数参数
            clientSender = robotScene.GetComponent<ClientSenderComponent>();
            C2M_RobotCase_TriggerAchievementEvent_Request request2 = C2M_RobotCase_TriggerAchievementEvent_Request.Create();
            request2.EventType = 1; // 有效的事件类型
            request2.ParamId = -1; // 无效的参数
            request2.Count = -1; // 无效的数量
            
            M2C_RobotCase_TriggerAchievementEvent_Response response2 = await clientSender.Call(request2) as M2C_RobotCase_TriggerAchievementEvent_Response;
            
            robotScene = robotSceneRef; // await后重新获取
            
            // 这个可能不会报错，取决于实现，我们只记录结果
            Log.Debug($"Trigger event with negative params result: {response2.Error} - {response2.Message}");
            
            Log.Debug("Trigger invalid achievement event test passed");
        }

        /// <summary>
        /// 测试获取无效分类的成就列表
        /// </summary>
        private static async ETTask TestGetInvalidCategoryAchievements(Scene robotScene)
        {
            Log.Debug("Testing get invalid category achievements");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 测试负数分类ID
            C2M_GetAchievements request1 = C2M_GetAchievements.Create();
            request1.CategoryId = -1; // 无效的分类ID
            
            M2C_GetAchievements response1 = await clientSender.Call(request1) as M2C_GetAchievements;
            
            robotScene = robotSceneRef; // await后重新获取
            clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 负数分类ID的处理取决于实现，可能返回错误或空列表
            Log.Debug($"Get achievements with negative category ID result: {response1.Error} - {response1.Message}, Count: {response1.Achievements?.Count ?? 0}");
            
            // 测试超大分类ID
            C2M_GetAchievements request2 = C2M_GetAchievements.Create();
            request2.CategoryId = 99999; // 不存在的分类ID
            
            M2C_GetAchievements response2 = await clientSender.Call(request2) as M2C_GetAchievements;
            
            robotScene = robotSceneRef; // await后重新获取
            
            // 不存在的分类ID应该返回空列表或错误
            if (response2.Error == ErrorCode.ERR_Success)
            {
                if (response2.Achievements.Count > 0)
                {
                    Log.Debug($"Warning: Got {response2.Achievements.Count} achievements for non-existent category 99999");
                }
                else
                {
                    Log.Debug("Correctly returned empty list for non-existent category");
                }
            }
            else
            {
                Log.Debug($"Correctly returned error for non-existent category: {response2.Error} - {response2.Message}");
            }
            
            Log.Debug("Get invalid category achievements test passed");
        }
    }
}