namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_RobotCase_PrepareData_004_Handler : MessageLocationHandler<Unit, C2M_RobotCase_PrepareData_004_Request, M2C_RobotCase_PrepareData_004_Response>
    {
        protected override async ETTask Run(Unit unit, C2M_RobotCase_PrepareData_004_Request request, M2C_RobotCase_PrepareData_004_Response response)
        {
            try
            {
                Log.Debug("Preparing Achievement test data for robot");

                // 获取或添加成就组件
                AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
                if (achievementComponent == null)
                {
                    achievementComponent = unit.AddComponent<AchievementComponent>();
                }

                // 创建测试成就数据
                CreateTestAchievementData(achievementComponent);

                Log.Debug("Achievement test data prepared successfully");
                
                response.Error = ErrorCode.ERR_Success;
                response.Message = "Achievement test data prepared";
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to prepare Achievement test data: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to prepare test data: {e.Message}";
            }
        }

        /// <summary>
        /// 创建测试成就数据
        /// </summary>
        private void CreateTestAchievementData(AchievementComponent achievementComponent)
        {
            // 清理现有数据
            achievementComponent.ActiveAchievements.Clear();
            achievementComponent.CompletedAchievements.Clear();
            achievementComponent.ClaimedAchievements.Clear();
            achievementComponent.RecentAchievements.Clear();
            achievementComponent.TypeMapping.Clear();
            achievementComponent.AchievementProgress.Clear();

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
    }
}