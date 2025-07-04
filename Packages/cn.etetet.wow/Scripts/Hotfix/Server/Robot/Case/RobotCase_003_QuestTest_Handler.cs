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
                
                Log.Debug("Quest robot test started - Using existing network connection");
                
                // 执行Quest测试流程
                await TestQuestFlow(robot);
                
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
        /// 测试Quest系统完整流程
        /// </summary>
        private async ETTask TestQuestFlow(Fiber robot)
        {
            Log.Debug("Starting Quest system test");
            
            try
            {
                Scene robotScene = robot.Root;
                
                // 机器人已经通过FiberInit_Robot建立了连接，直接获取ClientSenderComponent
                ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
                if (clientSender == null)
                {
                    Log.Error("ClientSenderComponent not found, robot may not be properly connected");
                    return;
                }
                
                EntityRef<ClientSenderComponent> clientSenderRef = clientSender;
                
                // 初始化Quest测试环境
                await InitializeQuestTestEnvironment(clientSenderRef);
                
                // 执行完整的Quest流程测试
                await ExecuteQuestFlowTest(clientSenderRef);
                
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
        private static async ETTask InitializeQuestTestEnvironment(EntityRef<ClientSenderComponent> clientSenderRef)
        {
            Log.Debug("Initializing Quest test environment via network");
            
            try
            {
                // 发送专用的Quest测试数据准备消息到服务器
                C2M_RobotCase_PrepareData_003_Request prepareRequest = C2M_RobotCase_PrepareData_003_Request.Create();
                
                Log.Debug("Sending Quest test data preparation request to server");
                
                // 发送真实的网络消息
                ClientSenderComponent clientSender = clientSenderRef;
                M2C_RobotCase_PrepareData_003_Response prepareResponse = await clientSender.Call(prepareRequest) as M2C_RobotCase_PrepareData_003_Response;
                
                if (prepareResponse.Error == ErrorCode.ERR_Success)
                {
                    Log.Debug("Quest test data preparation successful");
                }
                else
                {
                    Log.Error($"Quest test data preparation failed: {prepareResponse.Error}, {prepareResponse.Message}");
                }
                
                prepareResponse.Dispose();
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to initialize Quest test environment: {e.Message}");
            }
        }

        /// <summary>
        /// 执行完整的Quest流程测试
        /// </summary>
        private static async ETTask ExecuteQuestFlowTest(EntityRef<ClientSenderComponent> clientSenderRef)
        {
            Log.Debug("Executing complete Quest flow test");
            
            try
            {
                // 测试参数
                const int testQuestId = 1001;
                const long testNPCId = 20001;
                
                // 步骤1：查询可接取任务
                await TestQueryAvailableQuests(clientSenderRef, testNPCId);
                
                // 步骤2：接取任务
                await TestAcceptQuest(clientSenderRef, testQuestId, testNPCId);
                
                // 步骤3：同步任务数据（检查任务是否已接取）
                await TestSyncQuestData(clientSenderRef);
                
                // 步骤4：提交任务（如果任务已完成）
                await TestSubmitQuest(clientSenderRef, testQuestId, testNPCId);
                
                // 步骤5：再次同步任务数据（检查任务是否已完成）
                await TestSyncQuestDataAfterSubmit(clientSenderRef);
                
                Log.Debug("Complete Quest flow test executed successfully");
            }
            catch (System.Exception e)
            {
                Log.Error($"Quest flow test failed: {e.Message}");
            }
        }

        /// <summary>
        /// 测试查询可接取任务
        /// </summary>
        private static async ETTask TestQueryAvailableQuests(EntityRef<ClientSenderComponent> clientSenderRef, long npcId)
        {
            Log.Debug($"Testing query available quests for NPC: {npcId}");
            
            try
            {
                C2M_QueryAvailableQuests request = C2M_QueryAvailableQuests.Create();
                request.NPCId = npcId;
                
                Log.Debug("Sending QueryAvailableQuests request to server");
                
                // 发送真实的网络消息
                ClientSenderComponent clientSender = clientSenderRef;
                M2C_QueryAvailableQuests response = await clientSender.Call(request) as M2C_QueryAvailableQuests;
                
                if (response.Error == ErrorCode.ERR_Success)
                {
                    int questCount = response.AvailableQuests?.Count ?? 0;
                    Log.Debug($"Query available quests successful, Found {questCount} available quests");
                }
                else
                {
                    Log.Error($"Query available quests failed, Error: {response.Error}, Message: {response.Message}");
                }
                
                response.Dispose();
            }
            catch (System.Exception e)
            {
                Log.Error($"TestQueryAvailableQuests exception: {e.Message}");
            }
        }

        /// <summary>
        /// 测试接取任务
        /// </summary>
        private static async ETTask TestAcceptQuest(EntityRef<ClientSenderComponent> clientSenderRef, int questId, long npcId)
        {
            Log.Debug($"Testing accept quest: QuestId={questId}, NPCId={npcId}");
            
            try
            {
                C2M_AcceptQuest request = C2M_AcceptQuest.Create();
                request.QuestId = questId;
                request.NPCId = npcId;
                
                Log.Debug("Sending AcceptQuest request to server");
                
                // 发送真实的网络消息
                ClientSenderComponent clientSender = clientSenderRef;
                M2C_AcceptQuest response = await clientSender.Call(request) as M2C_AcceptQuest;
                
                if (response.Error == ErrorCode.ERR_Success)
                {
                    Log.Debug($"Accept quest successful: QuestId={questId}");
                }
                else
                {
                    Log.Error($"Accept quest failed, Error: {response.Error}, Message: {response.Message}");
                }
                
                response.Dispose();
            }
            catch (System.Exception e)
            {
                Log.Error($"TestAcceptQuest exception: {e.Message}");
            }
        }

        /// <summary>
        /// 测试同步任务数据
        /// </summary>
        private static async ETTask TestSyncQuestData(EntityRef<ClientSenderComponent> clientSenderRef)
        {
            Log.Debug("Testing sync quest data");
            
            try
            {
                C2M_SyncQuestData request = C2M_SyncQuestData.Create();
                
                Log.Debug("Sending SyncQuestData request to server");
                
                // 发送真实的网络消息
                ClientSenderComponent clientSender = clientSenderRef;
                M2C_SyncQuestData response = await clientSender.Call(request) as M2C_SyncQuestData;
                
                if (response.Error == ErrorCode.ERR_Success)
                {
                    int questCount = response.QuestList?.Count ?? 0;
                    Log.Debug($"Sync quest data successful, Found {questCount} active quests");
                }
                else
                {
                    Log.Error($"Sync quest data failed, Error: {response.Error}, Message: {response.Message}");
                }
                
                response.Dispose();
            }
            catch (System.Exception e)
            {
                Log.Error($"TestSyncQuestData exception: {e.Message}");
            }
        }

        /// <summary>
        /// 测试提交任务
        /// </summary>
        private static async ETTask TestSubmitQuest(EntityRef<ClientSenderComponent> clientSenderRef, int questId, long npcId)
        {
            Log.Debug($"Testing submit quest: QuestId={questId}, NPCId={npcId}");
            
            try
            {
                C2M_SubmitQuest request = C2M_SubmitQuest.Create();
                request.QuestId = questId;
                request.NPCId = npcId;
                
                Log.Debug("Sending SubmitQuest request to server");
                
                // 发送真实的网络消息
                ClientSenderComponent clientSender = clientSenderRef;
                M2C_SubmitQuest response = await clientSender.Call(request) as M2C_SubmitQuest;
                
                if (response.Error == ErrorCode.ERR_Success)
                {
                    Log.Debug($"Submit quest successful: QuestId={questId}");
                }
                else
                {
                    Log.Error($"Submit quest failed, Error: {response.Error}, Message: {response.Message}");
                }
                
                response.Dispose();
            }
            catch (System.Exception e)
            {
                Log.Error($"TestSubmitQuest exception: {e.Message}");
            }
        }

        /// <summary>
        /// 提交后再次同步任务数据
        /// </summary>
        private static async ETTask TestSyncQuestDataAfterSubmit(EntityRef<ClientSenderComponent> clientSenderRef)
        {
            Log.Debug("Testing sync quest data after submit");
            
            try
            {
                C2M_SyncQuestData request = C2M_SyncQuestData.Create();
                
                Log.Debug("Sending final SyncQuestData request to server");
                
                // 发送真实的网络消息
                ClientSenderComponent clientSender = clientSenderRef;
                M2C_SyncQuestData response = await clientSender.Call(request) as M2C_SyncQuestData;
                
                if (response.Error == ErrorCode.ERR_Success)
                {
                    int questCount = response.QuestList?.Count ?? 0;
                    Log.Debug($"Final sync quest data successful, Found {questCount} active quests");
                    
                    if (questCount == 0)
                    {
                        Log.Debug("Quest successfully completed and removed from active list");
                    }
                }
                else
                {
                    Log.Error($"Final sync quest data failed, Error: {response.Error}, Message: {response.Message}");
                }
                
                response.Dispose();
            }
            catch (System.Exception e)
            {
                Log.Error($"TestSyncQuestDataAfterSubmit exception: {e.Message}");
            }
        }
    }
}