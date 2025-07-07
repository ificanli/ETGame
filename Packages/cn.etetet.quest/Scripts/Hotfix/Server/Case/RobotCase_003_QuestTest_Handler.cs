using ET.Client;

namespace ET.Server
{
    [Invoke(RobotCaseType.QuestTest)]
    public class RobotCase_003_QuestTest_Handler : ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            try
            {
                // 创建机器人子Fiber
                Fiber robot = await fiber.CreateFiber(0, SceneType.Robot, "RobotCase_003_QuestTest");
                
                Log.Debug("Quest robot test started - Using ClientQuestHelper");
                
                // 执行Quest测试流程
                await TestQuestFlowWithClientHelper(robot);
                
                Log.Debug("Quest robot test completed successfully");
                return ErrorCode.ERR_Success;
            }
            catch (System.Exception e)
            {
                Log.Error($"Quest robot test failed with exception: {e.Message}\n{e.StackTrace}");
                return ErrorCore.ERR_KcpConnectTimeout;
            }
        }

        /// <summary>
        /// 使用ClientQuestHelper测试Quest系统完整流程
        /// </summary>
        private async ETTask TestQuestFlowWithClientHelper(Fiber robot)
        {
            Log.Debug("Starting Quest system test with ClientQuestHelper");
            
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
                
                // 初始化Quest测试环境
                await InitializeQuestTestEnvironment(robotScene);
                
                // 执行完整的Quest流程测试
                robotScene = robotSceneRef; // await后重新获取
                await ExecuteQuestFlowWithClientHelper(robotScene);
                
                Log.Debug("Quest system test completed");
            }
            catch (System.Exception e)
            {
                Log.Error($"Quest test failed: {e.Message}");
            }
        }

        /// <summary>
        /// 通过网络消息初始化Quest测试环境
        /// </summary>
        private static async ETTask InitializeQuestTestEnvironment(Scene robotScene)
        {
            Log.Debug("Initializing Quest test environment via network");
            
            try
            {
                // 发送专用的Quest测试数据准备消息到服务器
                C2M_RobotCase_PrepareData_003_Request prepareRequest = C2M_RobotCase_PrepareData_003_Request.Create();
                
                Log.Debug("Sending Quest test data preparation request to server");
                
                // 发送真实的网络消息
                EntityRef<Scene> robotSceneRef = robotScene;
                ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
                M2C_RobotCase_PrepareData_003_Response prepareResponse = await clientSender.Call(prepareRequest) as M2C_RobotCase_PrepareData_003_Response;
                
                // await后重新获取Entity
                robotScene = robotSceneRef;
                
                if (prepareResponse.Error == ErrorCode.ERR_Success)
                {
                    Log.Debug("Quest test data preparation successful");
                }
                else
                {
                    Log.Error($"Quest test data preparation failed: {prepareResponse.Error}, {prepareResponse.Message}");
                }
                
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to initialize Quest test environment: {e.Message}");
            }
        }

        /// <summary>
        /// 使用ClientQuestHelper执行完整的Quest流程测试
        /// </summary>
        private static async ETTask ExecuteQuestFlowWithClientHelper(Scene robotScene)
        {
            Log.Debug("Executing complete Quest flow test with ClientQuestHelper");
            
            try
            {
                // 测试参数 - 使用准备的测试数据
                const int testQuestId = 1001;  // 测试Quest ID (与服务器准备的数据一致)
                const int testNPCId = 20001;
                
                EntityRef<Scene> robotSceneRef = robotScene;
                
                // 步骤1：查询可接取任务
                Log.Debug("Step 1: Querying available quests");
                AvailableQuestInfo[] availableQuests = await ClientQuestHelper.QueryAvailableQuests(robotScene, testNPCId);
                Log.Debug($"Found {availableQuests.Length} available quests");
                
                // 验证可接取任务列表
                if (availableQuests.Length == 0)
                {
                    Log.Error("No available quests found, test failed");
                    return;
                }
                
                // 步骤2：接取任务
                Log.Debug("Step 2: Accepting quest");
                robotScene = robotSceneRef; // await后重新获取
                bool acceptResult = await ClientQuestHelper.AcceptQuest(robotScene, testQuestId, testNPCId);
                Log.Debug($"Accept quest result: {acceptResult}");
                
                if (!acceptResult)
                {
                    Log.Error("Failed to accept quest, test failed");
                    return;
                }
                
                // 验证客户端任务数据 - 检查任务是否已接取
                robotScene = robotSceneRef; // await后重新获取
                await ValidateClientQuestData(robotScene, testQuestId, QuestStatus.InProgress, "接取任务后");
                
                // 步骤3：同步任务数据（检查任务是否已接取）
                Log.Debug("Step 3: Syncing quest data");
                robotScene = robotSceneRef; // await后重新获取
                bool syncResult = await ClientQuestHelper.SyncQuestData(robotScene);
                Log.Debug($"Sync quest data result: {syncResult}");
                
                if (!syncResult)
                {
                    Log.Error("Failed to sync quest data, test failed");
                    return;
                }
                
                // 再次验证客户端任务数据 - 确认同步后数据正确
                robotScene = robotSceneRef; // await后重新获取
                await ValidateClientQuestData(robotScene, testQuestId, QuestStatus.InProgress, "同步任务数据后");
                
                // 步骤4：提交任务（如果任务已完成）
                Log.Debug("Step 4: Submitting quest");
                robotScene = robotSceneRef; // await后重新获取
                bool submitResult = await ClientQuestHelper.SubmitQuest(robotScene, testQuestId, testNPCId);
                Log.Debug($"Submit quest result: {submitResult}");
                
                if (!submitResult)
                {
                    Log.Error("Failed to submit quest, test failed");
                    return;
                }
                
                // 验证客户端任务数据 - 检查任务是否已被移除
                robotScene = robotSceneRef; // await后重新获取
                await ValidateQuestRemoved(robotScene, testQuestId, "提交任务后");
                
                // 步骤5：再次同步任务数据（检查任务是否已完成）
                Log.Debug("Step 5: Final sync quest data");
                robotScene = robotSceneRef; // await后重新获取
                bool finalSyncResult = await ClientQuestHelper.SyncQuestData(robotScene);
                Log.Debug($"Final sync quest data result: {finalSyncResult}");
                
                if (!finalSyncResult)
                {
                    Log.Error("Failed to final sync quest data, test failed");
                    return;
                }
                
                Log.Debug("Complete Quest flow test executed successfully using ClientQuestHelper");
            }
            catch (System.Exception e)
            {
                Log.Error($"Quest flow test failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 验证客户端任务数据
        /// </summary>
        private static async ETTask ValidateClientQuestData(Scene robotScene, int questId, QuestStatus expectedStatus, string context)
        {
            Log.Debug($"Validating client quest data - {context}");
            
            ClientQuestComponent questComponent = robotScene.GetComponent<ClientQuestComponent>();
            if (questComponent == null)
            {
                throw new System.Exception($"ClientQuestComponent not found - {context}");
            }
            
            ClientQuestData questData = questComponent.GetQuestData(questId);
            if (questData == null)
            {
                throw new System.Exception($"Quest data not found for questId {questId} - {context}");
            }
            
            if (questData.Status != expectedStatus)
            {
                throw new System.Exception($"Quest status mismatch - Expected: {expectedStatus}, Actual: {questData.Status} - {context}");
            }
            
            Log.Debug($"Client quest data validation successful - QuestId: {questId}, Status: {questData.Status} - {context}");
            await ETTask.CompletedTask;
        }
        
        /// <summary>
        /// 验证任务已被移除
        /// </summary>
        private static async ETTask ValidateQuestRemoved(Scene robotScene, int questId, string context)
        {
            Log.Debug($"Validating quest removed - {context}");
            
            ClientQuestComponent questComponent = robotScene.GetComponent<ClientQuestComponent>();
            if (questComponent == null)
            {
                throw new System.Exception($"ClientQuestComponent not found - {context}");
            }
            
            bool hasQuest = questComponent.HasQuest(questId);
            if (hasQuest)
            {
                throw new System.Exception($"Quest {questId} still exists but should be removed - {context}");
            }
            
            Log.Debug($"Quest removal validation successful - QuestId: {questId} - {context}");
            await ETTask.CompletedTask;
        }

    }
}