namespace ET.Client
{
    /// <summary>
    /// 客户端成就帮助类
    /// </summary>
    public static class ClientAchievementHelper
    {
        /// <summary>
        /// 获取成就列表
        /// </summary>
        public static async ETTask<AchievementInfo[]> GetAchievements(Scene scene, int categoryId = 0)
        {
            // 在await前创建EntityRef
            EntityRef<Scene> sceneRef = scene;
            
            C2M_GetAchievements request = C2M_GetAchievements.Create();
            request.CategoryId = categoryId;

            M2C_GetAchievements response = (M2C_GetAchievements)await scene.GetComponent<ClientSenderComponent>().Call(request);
            
            if (response.Error != 0)
            {
                Log.Error($"获取成就列表失败: {response.Message}");
                return new AchievementInfo[0];
            }

            // await后重新获取Scene
            scene = sceneRef;
            
            Log.Debug($"成功获取成就列表: {response.Achievements.Count} 个成就");
            return response.Achievements.ToArray();
        }

        /// <summary>
        /// 领取成就奖励
        /// </summary>
        public static async ETTask<bool> ClaimAchievementReward(Scene scene, int achievementId)
        {
            // 在await前创建EntityRef
            EntityRef<Scene> sceneRef = scene;
            
            C2M_ClaimAchievement request = C2M_ClaimAchievement.Create();
            request.AchievementId = achievementId;

            M2C_ClaimAchievement response = (M2C_ClaimAchievement)await scene.GetComponent<ClientSenderComponent>().Call(request);
            
            if (response.Error != 0)
            {
                Log.Error($"领取成就奖励失败: {response.Message}");
                return false;
            }

            // await后重新获取Scene
            scene = sceneRef;
            
            Log.Debug($"成功领取成就奖励: {achievementId}, 获得 {response.Rewards.Count} 个奖励");
            return true;
        }

        /// <summary>
        /// 获取成就详情
        /// </summary>
        public static async ETTask<AchievementDetailInfo> GetAchievementDetail(Scene scene, int achievementId)
        {
            // 在await前创建EntityRef
            EntityRef<Scene> sceneRef = scene;
            
            C2M_GetAchievementDetail request = C2M_GetAchievementDetail.Create();
            request.AchievementId = achievementId;

            M2C_GetAchievementDetail response = (M2C_GetAchievementDetail)await scene.GetComponent<ClientSenderComponent>().Call(request);
            
            if (response.Error != 0)
            {
                Log.Error($"获取成就详情失败: {response.Message}");
                return null;
            }

            // await后重新获取Scene
            scene = sceneRef;
            
            Log.Debug($"成功获取成就详情: {response.Detail.AchievementName}");
            return response.Detail;
        }

        /// <summary>
        /// 获取成就统计
        /// </summary>
        public static async ETTask<AchievementStatsInfo> GetAchievementStats(Scene scene)
        {
            // 在await前创建EntityRef
            EntityRef<Scene> sceneRef = scene;
            
            C2M_GetAchievementStats request = C2M_GetAchievementStats.Create();

            M2C_GetAchievementStats response = (M2C_GetAchievementStats)await scene.GetComponent<ClientSenderComponent>().Call(request);
            
            if (response.Error != 0)
            {
                Log.Error($"获取成就统计失败: {response.Message}");
                return null;
            }

            // await后重新获取Scene
            scene = sceneRef;
            
            Log.Debug($"成功获取成就统计: {response.Stats.CompletedAchievements}/{response.Stats.TotalAchievements}");
            return response.Stats;
        }
    }
}