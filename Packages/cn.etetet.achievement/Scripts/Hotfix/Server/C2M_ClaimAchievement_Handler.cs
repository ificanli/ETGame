namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_ClaimAchievement_Handler : MessageLocationHandler<Unit, C2M_ClaimAchievement, M2C_ClaimAchievement>
    {
        protected override async ETTask Run(Unit unit, C2M_ClaimAchievement request, M2C_ClaimAchievement response)
        {
            try
            {
                AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
                if (achievementComponent == null)
                {
                    response.Error = ErrorCode.ERR_Cancel;
                    response.Message = "Achievement component not found";
                    return;
                }

                // 尝试领取成就奖励
                bool success = achievementComponent.ClaimAchievementReward(request.AchievementId);
                if (!success)
                {
                    response.Error = ErrorCode.ERR_Cancel;
                    response.Message = "Achievement cannot be claimed";
                    return;
                }

                // TODO: 添加实际奖励到rewards列表
                // 这里模拟一些奖励
                AchievementReward reward = AchievementReward.Create();
                reward.Type = 1; // 经验
                reward.Count = 100;
                response.Rewards.Add(reward);
                
                response.Error = ErrorCode.ERR_Success;
                Log.Debug($"Achievement {request.AchievementId} reward claimed successfully");
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to claim achievement reward: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to claim achievement reward: {e.Message}";
            }
        }
    }
}