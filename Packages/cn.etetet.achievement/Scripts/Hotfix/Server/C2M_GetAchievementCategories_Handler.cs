namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_GetAchievementCategories_Handler : MessageLocationHandler<Unit, C2M_GetAchievementCategories, M2C_GetAchievementCategories>
    {
        protected override async ETTask Run(Unit unit, C2M_GetAchievementCategories request, M2C_GetAchievementCategories response)
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

                // 从配置表或现有成就数据中获取分类信息
                var categories = GetAchievementCategories(achievementComponent);
                response.Categories.AddRange(categories);

                response.Error = ErrorCode.ERR_Success;
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Get achievement categories failed: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Get categories failed: {e.Message}";
            }
        }

        /// <summary>
        /// 获取成就分类信息 - 从现有成就数据中动态生成
        /// </summary>
        private System.Collections.Generic.List<AchievementCategoryInfo> GetAchievementCategories(AchievementComponent achievementComponent)
        {
            var categories = new System.Collections.Generic.List<AchievementCategoryInfo>();
            var categoryMap = new System.Collections.Generic.Dictionary<int, AchievementCategoryInfo>();

            // 从现有成就中收集分类信息
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null) continue;

                int categoryId = achievement.CategoryId;
                if (categoryId <= 0) continue;

                // 如果分类不存在，创建新分类
                if (!categoryMap.ContainsKey(categoryId))
                {
                    var categoryInfo = AchievementCategoryInfo.Create();
                    categoryInfo.CategoryId = categoryId;
                    categoryInfo.CategoryName = GetCategoryName(categoryId);
                    categoryInfo.Icon = GetCategoryIcon(categoryId);
                    categoryInfo.Order = categoryId;
                    categoryInfo.TotalCount = 0;
                    categoryInfo.CompletedCount = 0;
                    categoryMap[categoryId] = categoryInfo;
                }

                // 更新分类统计
                categoryMap[categoryId].TotalCount++;
                if (achievement.Status >= AchievementStatus.Completed)
                {
                    categoryMap[categoryId].CompletedCount++;
                }
            }

            // 转换为列表并排序
            foreach (var kvp in categoryMap)
            {
                categories.Add(kvp.Value);
            }

            // 按Order排序
            categories.Sort((a, b) => a.Order.CompareTo(b.Order));

            return categories;
        }

        /// <summary>
        /// 获取分类名称
        /// </summary>
        private string GetCategoryName(int categoryId)
        {
            // TODO: 从配置表获取分类名称
            // 这里提供默认名称
            return categoryId switch
            {
                1 => "Battle",
                2 => "Level", 
                3 => "Quest",
                4 => "Exploration",
                5 => "Collection",
                _ => $"Category {categoryId}"
            };
        }

        /// <summary>
        /// 获取分类图标
        /// </summary>
        private string GetCategoryIcon(int categoryId)
        {
            // TODO: 从配置表获取分类图标
            // 这里提供默认图标
            return categoryId switch
            {
                1 => "battle_icon",
                2 => "level_icon",
                3 => "quest_icon", 
                4 => "explore_icon",
                5 => "collect_icon",
                _ => "default_icon"
            };
        }

    }
}