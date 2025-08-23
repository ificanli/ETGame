using System;
using System.Collections.Generic;
using ET.Client;

namespace ET.Server
{
    [Invoke(RobotCaseType.QuestTest)]
    public class RobotCase_003_Quest_Handler : ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber parentFiber, RobotCaseArgs args)
        {
            try
            {
                Log.Debug("Starting Quest robot test case");
                
                // 创建机器人子Fiber
                Fiber robot = await parentFiber.CreateFiber(0, SceneType.Robot, "RobotCase_003_QuestTest");
                
                // 执行Quest测试流程
                await TestQuestFlow(robot, parentFiber);
                
                Log.Debug("Quest robot test case completed successfully");
                return ErrorCode.ERR_Success;
            }
            catch (Exception e)
            {
                Log.Error($"Quest robot test case failed with exception: {e.Message}\n{e.StackTrace}");
                return ErrorCore.ERR_KcpConnectTimeout;
            }
        }

        /// <summary>
        /// 测试Quest系统完整流程
        /// </summary>
        private async ETTask TestQuestFlow(Fiber robot, Fiber parentFiber)
        {
            Log.Debug("Starting Quest system test flow");
            
            try
            {
                Scene robotScene = robot.Root;
                EntityRef<Scene> robotSceneRef = robotScene;
                
                // 验证机器人连接状态
                ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
                if (clientSender == null)
                {
                    throw new Exception("ClientSenderComponent not found, robot may not be properly connected");
                }
                
                // 准备测试数据 - 直接操作服务端数据
                await CreateTestData(robotScene, parentFiber);
                
                // 执行完整的Quest流程测试
                robotScene = robotSceneRef; // await后重新获取
                await ExecuteQuestFlow(robotScene);
                
                Log.Debug("Quest system test flow completed");
            }
            catch (Exception e)
            {
                Log.Error($"Quest test flow failed: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 创建测试数据 - 直接操作服务端Fiber
        /// </summary>
        private async ETTask CreateTestData(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Creating Quest test data via direct server access");
            
            try
            {
                // 获取玩家信息
                string mapName = robotScene.CurrentScene().Name;
                Client.PlayerComponent playerComponent = robotScene.GetComponent<Client.PlayerComponent>();
                
                // 获取服务端Map Fiber
                Fiber map = parentFiber.GetFiber("MapManager").GetFiber(mapName);
                if (map == null)
                {
                    throw new Exception($"Map fiber not found for {mapName}");
                }
                
                // 获取服务端Unit
                Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);
                if (serverUnit == null)
                {
                    throw new Exception($"Server unit not found for player {playerComponent.MyId}");
                }
                
                // 为玩家添加QuestComponent
                if (serverUnit.GetComponent<QuestComponent>() == null)
                {
                    serverUnit.AddComponent<QuestComponent>();
                    Log.Debug($"Added QuestComponent to player unit {serverUnit.Id}");
                }
                
                QuestComponent questComponent = serverUnit.GetComponent<QuestComponent>();
                
                // 创建测试Quest配置
                CreateTestQuestConfigs();
                
                // 添加测试任务到可用任务列表
                int testQuestId = 1001;
                if (!questComponent.AvailableQuests.Contains(testQuestId))
                {
                    questComponent.AvailableQuests.Add(testQuestId);
                    Log.Debug($"Added quest {testQuestId} to available quests");
                }
                
                int secondQuestId = 1002;
                if (!questComponent.AvailableQuests.Contains(secondQuestId))
                {
                    questComponent.AvailableQuests.Add(secondQuestId);
                    Log.Debug($"Added quest {secondQuestId} to available quests");
                }
                
                Log.Debug("Quest test data creation completed");
                await ETTask.CompletedTask;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to create Quest test data: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建测试用的Quest配置数据
        /// </summary>
        private void CreateTestQuestConfigs()
        {
            try
            {
                // 检查QuestConfigCategory是否已经有数据
                QuestConfigCategory questConfigCategory = QuestConfigCategory.Instance;
                if (questConfigCategory.GetOrDefault(1001) != null)
                {
                    Log.Debug("Quest config 1001 already exists");
                    return;
                }
                
                // 根据CLAUDE.md规范，在代码中写json串，然后使用MongoHelper.FromJson反序列化
                string questConfigCategoryJson = @"{
                    ""_dataMap"": {
                        ""1001"": {
                            ""Id"": 1001,
                            ""Name"": ""Test Quest 1"",
                            ""Desc"": ""This is a test quest for robot case"",
                            ""ObjectiveIds"": [10001, 10002],
                            ""PreQuestIds"": [],
                            ""NextQuestId"": [1002],
                            ""Title"": ""Test Quest Title"",
                            ""Content"": ""Complete this test quest"",
                            ""FinishContent"": ""Quest completed successfully"",
                            ""AcceptNPC"": 20001,
                            ""AcceptNPCMap"": 1,
                            ""SubmitNPC"": 20001,
                            ""SubmitNPCMap"": 1
                        },
                        ""1002"": {
                            ""Id"": 1002,
                            ""Name"": ""Test Quest 2"",
                            ""Desc"": ""This is a second test quest"",
                            ""ObjectiveIds"": [10003],
                            ""PreQuestIds"": [1001],
                            ""NextQuestId"": [],
                            ""Title"": ""Test Quest 2 Title"",
                            ""Content"": ""Complete this second test quest"",
                            ""FinishContent"": ""Second quest completed"",
                            ""AcceptNPC"": 20001,
                            ""AcceptNPCMap"": 1,
                            ""SubmitNPC"": 20001,
                            ""SubmitNPCMap"": 1
                        }
                    }
                }";
                
                // 使用MongoHelper.FromJson反序列化QuestConfigCategory配置数据
                QuestConfigCategory testConfigCategory = MongoHelper.FromJson<QuestConfigCategory>(questConfigCategoryJson);
                
                Log.Debug($"Created test quest configs with {testConfigCategory.DataList.Count} quests using MongoHelper.FromJson");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to create test quest configs: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 执行完整的Quest流程测试
        /// </summary>
        private async ETTask ExecuteQuestFlow(Scene robotScene)
        {
            Log.Debug("Executing complete Quest flow test");
            
            try
            {
                // 测试参数
                const int testQuestId = 1001;
                const int testNPCId = 20001;
                
                EntityRef<Scene> robotSceneRef = robotScene;
                
                // 步骤1：查询可接取任务
                Log.Debug("Step 1: Querying available quests");
                AvailableQuestInfo[] availableQuests = await ClientQuestHelper.QueryAvailableQuests(robotScene, testNPCId);
                Log.Debug($"Found {availableQuests.Length} available quests");
                
                if (availableQuests.Length == 0)
                {
                    throw new Exception("No available quests found");
                }
                
                // 步骤2：接取任务
                Log.Debug("Step 2: Accepting quest");
                robotScene = robotSceneRef; // await后重新获取
                bool acceptResult = await ClientQuestHelper.AcceptQuest(robotScene, testQuestId, testNPCId);
                Log.Debug($"Accept quest result: {acceptResult}");
                
                if (!acceptResult)
                {
                    throw new Exception("Failed to accept quest");
                }
                
                // 验证客户端任务数据 - 检查任务是否已接取
                robotScene = robotSceneRef; // await后重新获取
                ValidateClientQuestData(robotScene, testQuestId, QuestStatus.InProgress, "接取任务后");
                
                // 步骤3：同步任务数据
                Log.Debug("Step 3: Syncing quest data");
                robotScene = robotSceneRef; // await后重新获取
                bool syncResult = await ClientQuestHelper.SyncQuestData(robotScene);
                Log.Debug($"Sync quest data result: {syncResult}");
                
                if (!syncResult)
                {
                    throw new Exception("Failed to sync quest data");
                }
                
                // 再次验证客户端任务数据
                robotScene = robotSceneRef; // await后重新获取
                ValidateClientQuestData(robotScene, testQuestId, QuestStatus.InProgress, "同步任务数据后");
                
                // 步骤4：提交任务
                Log.Debug("Step 4: Submitting quest");
                robotScene = robotSceneRef; // await后重新获取
                bool submitResult = await ClientQuestHelper.SubmitQuest(robotScene, testQuestId, testNPCId);
                Log.Debug($"Submit quest result: {submitResult}");
                
                if (!submitResult)
                {
                    throw new Exception("Failed to submit quest");
                }
                
                // 验证任务已被移除
                robotScene = robotSceneRef; // await后重新获取
                ValidateQuestRemoved(robotScene, testQuestId, "提交任务后");
                
                // 步骤5：最终同步检查
                Log.Debug("Step 5: Final sync quest data");
                robotScene = robotSceneRef; // await后重新获取
                bool finalSyncResult = await ClientQuestHelper.SyncQuestData(robotScene);
                Log.Debug($"Final sync quest data result: {finalSyncResult}");
                
                if (!finalSyncResult)
                {
                    throw new Exception("Failed to final sync quest data");
                }
                
                Log.Debug("Complete Quest flow test executed successfully");
            }
            catch (Exception e)
            {
                Log.Error($"Quest flow test failed: {e.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 验证客户端任务数据
        /// </summary>
        private void ValidateClientQuestData(Scene robotScene, int questId, QuestStatus expectedStatus, string context)
        {
            Log.Debug($"Validating client quest data - {context}");
            
            ClientQuestComponent questComponent = robotScene.GetComponent<ClientQuestComponent>();
            if (questComponent == null)
            {
                throw new Exception($"ClientQuestComponent not found - {context}");
            }
            
            ClientQuestData questData = questComponent.GetQuestData(questId);
            if (questData == null)
            {
                throw new Exception($"Quest data not found for questId {questId} - {context}");
            }
            
            if (questData.Status != expectedStatus)
            {
                throw new Exception($"Quest status mismatch - Expected: {expectedStatus}, Actual: {questData.Status} - {context}");
            }
            
            Log.Debug($"Client quest data validation successful - QuestId: {questId}, Status: {questData.Status} - {context}");
        }
        
        /// <summary>
        /// 验证任务已被移除
        /// </summary>
        private void ValidateQuestRemoved(Scene robotScene, int questId, string context)
        {
            Log.Debug($"Validating quest removed - {context}");
            
            ClientQuestComponent questComponent = robotScene.GetComponent<ClientQuestComponent>();
            if (questComponent == null)
            {
                throw new Exception($"ClientQuestComponent not found - {context}");
            }
            
            bool hasQuest = questComponent.HasQuest(questId);
            if (hasQuest)
            {
                throw new Exception($"Quest {questId} still exists but should be removed - {context}");
            }
            
            Log.Debug($"Quest removal validation successful - QuestId: {questId} - {context}");
        }
    }
}