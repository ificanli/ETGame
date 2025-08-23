using ET.Client;

namespace ET.Server
{
    [Invoke(RobotCaseType.AchievementProgressTest)]
    public class RobotCase_005_AchievementProgress_Handler: ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            try
            {
                // 创建机器人子Fiber
                Fiber robot = await fiber.CreateFiber(0, SceneType.Robot, "RobotCase_005_AchievementProgress");
                
                Log.Debug("Achievement progress robot test started");
                
                // 执行成就进度测试流程
                await TestAchievementProgressFlow(robot, fiber);
                
                Log.Debug("Achievement progress robot test completed successfully");
                return ErrorCode.ERR_Success;
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement progress robot test failed with exception: {e.Message}\n{e.StackTrace}");
                return ErrorCore.ERR_KcpConnectTimeout;
            }
        }

        /// <summary>
        /// 测试成就进度更新流程
        /// </summary>
        private async ETTask TestAchievementProgressFlow(Fiber robot, Fiber parentFiber)
        {
            Log.Debug("Starting Achievement progress test flow");
            
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
                InitializeProgressTestEnvironment(robotScene, parentFiber);
                
                // 步骤2：获取初始成就状态
                robotScene = robotSceneRef; // await后重新获取
                AchievementInfo[] initialAchievements = await ClientAchievementHelper.GetAchievements(robotScene);
                Log.Debug($"Initial achievements count: {initialAchievements.Length}");
                
                // 验证初始状态
                ValidateInitialProgress(initialAchievements);
                
                // 步骤3：测试成就进度更新
                robotScene = robotSceneRef; // await后重新获取
                await TestProgressUpdate(robotScene, 5001, "Kill Achievement");
                
                // 步骤4：测试成就自动完成
                robotScene = robotSceneRef; // await后重新获取
                await TestAutoCompletion(robotScene, 5003, "Level Achievement");
                
                // 步骤5：验证最终状态
                robotScene = robotSceneRef; // await后重新获取
                AchievementInfo[] finalAchievements = await ClientAchievementHelper.GetAchievements(robotScene);
                ValidateFinalProgress(finalAchievements);
                
                Log.Debug("Achievement progress test flow completed successfully");
            }
            catch (System.Exception e)
            {
                Log.Error($"Achievement progress test failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 直接访问服务器初始化成就进度测试环境
        /// </summary>
        private static void InitializeProgressTestEnvironment(Scene robotScene, Fiber parentFiber)
        {
            Log.Debug("Initializing Achievement progress test environment via direct server access");
            
            try
            {
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
                
                // 创建成就进度测试数据
                CreateProgressTestData(achievementComponent);
                
                Log.Debug("Achievement progress test data prepared successfully via direct server access");
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to initialize Achievement progress test environment via direct access: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建成就进度测试数据
        /// </summary>
        private static void CreateProgressTestData(AchievementComponent achievementComponent)
        {
            // 击杀成就 - 刚开始，进度为0
            Achievement killAchievement = achievementComponent.AddAchievement(5001);
            killAchievement.Type = AchievementType.Kill;
            killAchievement.MaxProgress = 5;
            killAchievement.Progress = 0; // 从0开始
            killAchievement.Points = 10;
            killAchievement.CategoryId = 1;
            killAchievement.Status = AchievementStatus.InProgress;

            // 收集成就 - 部分进度
            Achievement collectAchievement = achievementComponent.AddAchievement(5002);
            collectAchievement.Type = AchievementType.Collect;
            collectAchievement.MaxProgress = 10;
            collectAchievement.Progress = 3; // 部分进度
            collectAchievement.Points = 15;
            collectAchievement.CategoryId = 2;
            collectAchievement.Status = AchievementStatus.InProgress;

            // 等级成就 - 即将完成
            Achievement levelAchievement = achievementComponent.AddAchievement(5003);
            levelAchievement.Type = AchievementType.Level;
            levelAchievement.MaxProgress = 10;
            levelAchievement.Progress = 9; // 即将完成
            levelAchievement.Points = 20;
            levelAchievement.CategoryId = 3;
            levelAchievement.Status = AchievementStatus.InProgress;

            // PVP成就 - 单次完成型
            Achievement pvpAchievement = achievementComponent.AddAchievement(5004);
            pvpAchievement.Type = AchievementType.PVP;
            pvpAchievement.MaxProgress = 1;
            pvpAchievement.Progress = 0; // 未开始
            pvpAchievement.Points = 30;
            pvpAchievement.CategoryId = 4;
            pvpAchievement.Status = AchievementStatus.InProgress;

            // 更新组件数据
            achievementComponent.TotalPoints = 75; // 总成就点数
            achievementComponent.EarnedPoints = 0; // 还没有获得点数

            // 更新类型映射
            achievementComponent.TypeMapping.Add(AchievementType.Kill, 5001);
            achievementComponent.TypeMapping.Add(AchievementType.Collect, 5002);
            achievementComponent.TypeMapping.Add(AchievementType.Level, 5003);
            achievementComponent.TypeMapping.Add(AchievementType.PVP, 5004);

            // 更新进度映射
            achievementComponent.AchievementProgress[5001] = 0;
            achievementComponent.AchievementProgress[5002] = 3;
            achievementComponent.AchievementProgress[5003] = 9;
            achievementComponent.AchievementProgress[5004] = 0;

            Log.Debug("Created 4 progress test achievements: 5001(Kill,0/5), 5002(Collect,3/10), 5003(Level,9/10), 5004(PVP,0/1)");
        }

        /// <summary>
        /// 验证初始进度状态
        /// </summary>
        private static void ValidateInitialProgress(AchievementInfo[] achievements)
        {
            Log.Debug("Validating initial progress state");
            
            if (achievements.Length != 4)
            {
                throw new System.Exception($"Expected 4 achievements, but got {achievements.Length}");
            }
            
            // 验证特定成就的初始状态
            var killAchievement = System.Array.Find(achievements, a => a.AchievementId == 5001);
            if (killAchievement == null || killAchievement.Progress != 0 || killAchievement.MaxProgress != 5)
            {
                throw new System.Exception($"Kill achievement initial state incorrect: Progress={killAchievement?.Progress}, Max={killAchievement?.MaxProgress}");
            }
            
            var levelAchievement = System.Array.Find(achievements, a => a.AchievementId == 5003);
            if (levelAchievement == null || levelAchievement.Progress != 9 || levelAchievement.MaxProgress != 10)
            {
                throw new System.Exception($"Level achievement initial state incorrect: Progress={levelAchievement?.Progress}, Max={levelAchievement?.MaxProgress}");
            }
            
            Log.Debug("Initial progress validation successful");
        }

        /// <summary>
        /// 测试进度更新
        /// </summary>
        private static async ETTask TestProgressUpdate(Scene robotScene, int achievementId, string achievementName)
        {
            Log.Debug($"Testing progress update for {achievementName} (ID: {achievementId})");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 模拟击杀怪物，更新成就进度
            for (int i = 1; i <= 3; i++)
            {
                // await前重新获取clientSender
                robotScene = robotSceneRef;
                clientSender = robotScene.GetComponent<ClientSenderComponent>();
                
                // 触发击杀事件
                RobotCase_TriggerAchievementEvent_Request triggerRequest = RobotCase_TriggerAchievementEvent_Request.Create();
                triggerRequest.EventType = 1; // 击杀事件
                triggerRequest.ParamId = 1001; // 怪物ID
                triggerRequest.Count = 1; // 击杀数量
                
                RobotCase_TriggerAchievementEvent_Response triggerResponse = await clientSender.Call(triggerRequest) as RobotCase_TriggerAchievementEvent_Response;
                
                robotScene = robotSceneRef; // await后重新获取
                
                if (triggerResponse.Error != ErrorCode.ERR_Success)
                {
                    throw new System.Exception($"Failed to trigger kill event: {triggerResponse.Message}");
                }
                
                // 获取更新后的成就状态
                AchievementInfo[] updatedAchievements = await ClientAchievementHelper.GetAchievements(robotScene);
                robotScene = robotSceneRef; // await后重新获取
                
                var achievement = System.Array.Find(updatedAchievements, a => a.AchievementId == achievementId);
                if (achievement == null)
                {
                    throw new System.Exception($"Achievement {achievementId} not found after update");
                }
                
                Log.Debug($"Progress update {i}: {achievement.Progress}/{achievement.MaxProgress}");
                
                if (achievement.Progress != i)
                {
                    throw new System.Exception($"Expected progress {i}, but got {achievement.Progress}");
                }
            }
            
            Log.Debug($"Progress update test completed for {achievementName}");
        }

        /// <summary>
        /// 测试自动完成
        /// </summary>
        private static async ETTask TestAutoCompletion(Scene robotScene, int achievementId, string achievementName)
        {
            Log.Debug($"Testing auto completion for {achievementName} (ID: {achievementId})");
            
            EntityRef<Scene> robotSceneRef = robotScene;
            ClientSenderComponent clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            // 获取当前状态
            AchievementInfo[] beforeAchievements = await ClientAchievementHelper.GetAchievements(robotScene);
            robotScene = robotSceneRef; // await后重新获取
            clientSender = robotScene.GetComponent<ClientSenderComponent>();
            
            var beforeAchievement = System.Array.Find(beforeAchievements, a => a.AchievementId == achievementId);
            if (beforeAchievement == null)
            {
                throw new System.Exception($"Achievement {achievementId} not found before completion test");
            }
            
            Log.Debug($"Before completion: Progress={beforeAchievement.Progress}/{beforeAchievement.MaxProgress}, Status={beforeAchievement.Status}");
            
            // 触发等级提升事件，应该完成成就
            RobotCase_TriggerAchievementEvent_Request triggerRequest = RobotCase_TriggerAchievementEvent_Request.Create();
            triggerRequest.EventType = 2; // 等级提升事件
            triggerRequest.ParamId = 10; // 等级
            triggerRequest.Count = 1;
            
            RobotCase_TriggerAchievementEvent_Response triggerResponse = await clientSender.Call(triggerRequest) as RobotCase_TriggerAchievementEvent_Response;
            
            robotScene = robotSceneRef; // await后重新获取
            
            if (triggerResponse.Error != ErrorCode.ERR_Success)
            {
                throw new System.Exception($"Failed to trigger level up event: {triggerResponse.Message}");
            }
            
            // 获取更新后的成就状态
            AchievementInfo[] afterAchievements = await ClientAchievementHelper.GetAchievements(robotScene);
            var afterAchievement = System.Array.Find(afterAchievements, a => a.AchievementId == achievementId);
            
            if (afterAchievement == null)
            {
                throw new System.Exception($"Achievement {achievementId} not found after completion test");
            }
            
            Log.Debug($"After completion: Progress={afterAchievement.Progress}/{afterAchievement.MaxProgress}, Status={afterAchievement.Status}");
            
            // 验证自动完成
            if (afterAchievement.Progress != afterAchievement.MaxProgress)
            {
                throw new System.Exception($"Expected progress {afterAchievement.MaxProgress}, but got {afterAchievement.Progress}");
            }
            
            if (afterAchievement.Status != 2) // Status=2表示已完成
            {
                throw new System.Exception($"Expected status 2 (completed), but got {afterAchievement.Status}");
            }
            
            Log.Debug($"Auto completion test successful for {achievementName}");
        }

        /// <summary>
        /// 验证最终进度状态
        /// </summary>
        private static void ValidateFinalProgress(AchievementInfo[] achievements)
        {
            Log.Debug("Validating final progress state");
            
            // 验证击杀成就进度
            var killAchievement = System.Array.Find(achievements, a => a.AchievementId == 5001);
            if (killAchievement == null || killAchievement.Progress != 3)
            {
                throw new System.Exception($"Kill achievement final progress incorrect: {killAchievement?.Progress}");
            }
            
            // 验证等级成就已完成
            var levelAchievement = System.Array.Find(achievements, a => a.AchievementId == 5003);
            if (levelAchievement == null || levelAchievement.Status != 2 || levelAchievement.Progress != 10)
            {
                throw new System.Exception($"Level achievement should be completed: Status={levelAchievement?.Status}, Progress={levelAchievement?.Progress}");
            }
            
            Log.Debug("Final progress validation successful");
        }
    }
}