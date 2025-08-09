namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class RobotCase_005_PrepareData_Handler : MessageLocationHandler<Unit, RobotCase_005_PrepareData_Request, RobotCase_005_PrepareData_Response>
    {
        protected override async ETTask Run(Unit unit, RobotCase_005_PrepareData_Request request, RobotCase_005_PrepareData_Response response)
        {
            try
            {
                Log.Debug("Preparing Achievement progress test data for robot");

                // 获取或添加成就组件
                AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
                if (achievementComponent == null)
                {
                    achievementComponent = unit.AddComponent<AchievementComponent>();
                }

                // 创建成就进度测试数据
                CreateProgressTestData(achievementComponent);

                Log.Debug("Achievement progress test data prepared successfully");
                
                response.Error = ErrorCode.ERR_Success;
                response.Message = "Achievement progress test data prepared";
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to prepare Achievement progress test data: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to prepare test data: {e.Message}";
            }
        }

        /// <summary>
        /// 创建成就进度测试数据
        /// </summary>
        private void CreateProgressTestData(AchievementComponent achievementComponent)
        {
            // 清理现有数据
            achievementComponent.ActiveAchievements.Clear();
            achievementComponent.CompletedAchievements.Clear();
            achievementComponent.ClaimedAchievements.Clear();
            achievementComponent.RecentAchievements.Clear();
            achievementComponent.TypeMapping.Clear();
            achievementComponent.AchievementProgress.Clear();

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
    }
}