namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_GetAchievementDetail_Handler : MessageLocationHandler<Unit, C2M_GetAchievementDetail, M2C_GetAchievementDetail>
    {
        protected override async ETTask Run(Unit unit, C2M_GetAchievementDetail request, M2C_GetAchievementDetail response)
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

                Achievement achievement = achievementComponent.GetAchievement(request.AchievementId);
                if (achievement == null)
                {
                    response.Error = ErrorCode.ERR_Cancel;
                    response.Message = "Achievement not found";
                    return;
                }

                // 创建成就详情
                AchievementDetailInfo detail = AchievementDetailInfo.Create();
                detail.AchievementId = achievement.ConfigId;
                detail.AchievementName = $"Achievement {achievement.ConfigId}";
                detail.Description = $"This is achievement {achievement.ConfigId}";
                detail.Icon = "achievement_icon";
                detail.CategoryId = achievement.CategoryId;
                detail.Type = (int)achievement.Type;
                detail.MaxProgress = achievement.MaxProgress;
                detail.Points = achievement.Points;
                
                // TODO: 从配置表获取前置成就和奖励信息
                
                response.Detail = detail;
                response.Error = ErrorCode.ERR_Success;
                Log.Debug($"Retrieved achievement detail for {request.AchievementId}");
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to get achievement detail: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to get achievement detail: {e.Message}";
            }
        }
    }
}