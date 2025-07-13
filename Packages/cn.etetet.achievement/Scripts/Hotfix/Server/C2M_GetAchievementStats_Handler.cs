namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_GetAchievementStats_Handler : MessageLocationHandler<Unit, C2M_GetAchievementStats, M2C_GetAchievementStats>
    {
        protected override async ETTask Run(Unit unit, C2M_GetAchievementStats request, M2C_GetAchievementStats response)
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

                // 获取成就统计信息
                response.Stats = achievementComponent.GetAchievementStats();
                
                response.Error = ErrorCode.ERR_Success;
                Log.Debug($"Retrieved achievement stats: {response.Stats.CompletedAchievements}/{response.Stats.TotalAchievements}");
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to get achievement stats: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to get achievement stats: {e.Message}";
            }
        }
    }
}