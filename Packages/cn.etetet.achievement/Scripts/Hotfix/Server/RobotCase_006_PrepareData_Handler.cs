namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class RobotCase_006_PrepareData_Handler : MessageLocationHandler<Unit, RobotCase_006_PrepareData_Request, RobotCase_006_PrepareData_Response>
    {
        protected override async ETTask Run(Unit unit, RobotCase_006_PrepareData_Request request, RobotCase_006_PrepareData_Response response)
        {
            try
            {
                Log.Debug("Preparing Achievement trigger test data for robot");

                // 获取或添加成就组件
                AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
                if (achievementComponent == null)
                {
                    achievementComponent = unit.AddComponent<AchievementComponent>();
                }

                // 创建成就触发测试数据
                CreateTriggerTestData(achievementComponent);

                Log.Debug("Achievement trigger test data prepared successfully");
                
                response.Error = ErrorCode.ERR_Success;
                response.Message = "Achievement trigger test data prepared";
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to prepare Achievement trigger test data: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to prepare test data: {e.Message}";
            }
        }

        /// <summary>
        /// 创建成就触发测试数据
        /// </summary>
        private void CreateTriggerTestData(AchievementComponent achievementComponent)
        {
            // 清理现有数据
            achievementComponent.ActiveAchievements.Clear();
            achievementComponent.CompletedAchievements.Clear();
            achievementComponent.ClaimedAchievements.Clear();
            achievementComponent.RecentAchievements.Clear();
            achievementComponent.TypeMapping.Clear();
            achievementComponent.AchievementProgress.Clear();

            // 击杀特定怪物成就
            Achievement killSpecificAchievement = achievementComponent.AddAchievement(6001);
            killSpecificAchievement.Type = AchievementType.Kill;
            killSpecificAchievement.MaxProgress = 3;
            killSpecificAchievement.Progress = 0;
            killSpecificAchievement.Points = 10;
            killSpecificAchievement.CategoryId = 1;
            killSpecificAchievement.Status = AchievementStatus.InProgress;

            // 击杀任意怪物成就
            Achievement killAnyAchievement = achievementComponent.AddAchievement(6002);
            killAnyAchievement.Type = AchievementType.Kill;
            killAnyAchievement.MaxProgress = 5;
            killAnyAchievement.Progress = 2; // 已有进度
            killAnyAchievement.Points = 15;
            killAnyAchievement.CategoryId = 1;
            killAnyAchievement.Status = AchievementStatus.InProgress;

            // 达到指定等级成就
            Achievement levelAchievement = achievementComponent.AddAchievement(6003);
            levelAchievement.Type = AchievementType.Level;
            levelAchievement.MaxProgress = 10;
            levelAchievement.Progress = 5;
            levelAchievement.Points = 20;
            levelAchievement.CategoryId = 2;
            levelAchievement.Status = AchievementStatus.InProgress;

            // 完成任务成就
            Achievement questAchievement = achievementComponent.AddAchievement(6004);
            questAchievement.Type = AchievementType.Quest;
            questAchievement.MaxProgress = 3;
            questAchievement.Progress = 1;
            questAchievement.Points = 25;
            questAchievement.CategoryId = 3;
            questAchievement.Status = AchievementStatus.InProgress;

            // 收集道具成就
            Achievement collectAchievement = achievementComponent.AddAchievement(6005);
            collectAchievement.Type = AchievementType.Collect;
            collectAchievement.MaxProgress = 10;
            collectAchievement.Progress = 7;
            collectAchievement.Points = 30;
            collectAchievement.CategoryId = 4;
            collectAchievement.Status = AchievementStatus.InProgress;

            // 探索地图成就
            Achievement exploreAchievement = achievementComponent.AddAchievement(6006);
            exploreAchievement.Type = AchievementType.Exploration;
            exploreAchievement.MaxProgress = 1;
            exploreAchievement.Progress = 0;
            exploreAchievement.Points = 35;
            exploreAchievement.CategoryId = 5;
            exploreAchievement.Status = AchievementStatus.InProgress;

            // 更新组件数据
            achievementComponent.TotalPoints = 135; // 总成就点数
            achievementComponent.EarnedPoints = 0; // 还没有获得点数

            // 更新类型映射
            achievementComponent.TypeMapping.Add(AchievementType.Kill, 6001);
            achievementComponent.TypeMapping.Add(AchievementType.Kill, 6002);
            achievementComponent.TypeMapping.Add(AchievementType.Level, 6003);
            achievementComponent.TypeMapping.Add(AchievementType.Quest, 6004);
            achievementComponent.TypeMapping.Add(AchievementType.Collect, 6005);
            achievementComponent.TypeMapping.Add(AchievementType.Exploration, 6006);

            // 更新进度映射
            achievementComponent.AchievementProgress[6001] = 0;
            achievementComponent.AchievementProgress[6002] = 2;
            achievementComponent.AchievementProgress[6003] = 5;
            achievementComponent.AchievementProgress[6004] = 1;
            achievementComponent.AchievementProgress[6005] = 7;
            achievementComponent.AchievementProgress[6006] = 0;

            Log.Debug("Created 6 trigger test achievements with different types and progress states");
        }
    }
}