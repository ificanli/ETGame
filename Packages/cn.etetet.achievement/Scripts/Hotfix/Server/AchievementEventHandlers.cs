namespace ET.Server
{
    /// <summary>
    /// 击杀怪物事件处理器
    /// </summary>
    [Event(SceneType.Map)]
    public class KillMonsterEvent_AchievementHandler : AEvent<Scene, KillMonsterEvent>
    {
        protected override async ETTask Run(Scene scene, KillMonsterEvent args)
        {
            // 获取玩家Unit
            Unit unit = scene.GetComponent<UnitComponent>()?.Get(args.UnitId);
            if (unit == null)
            {
                return;
            }

            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有击杀类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Kill)
                {
                    continue;
                }

                // 检查是否是目标怪物（这里简化处理，实际应该从配置表获取）
                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + args.Count;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Kill monster achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 等级提升事件处理器
    /// </summary>
    [Event(SceneType.Map)]
    public class LevelUpEvent_AchievementHandler : AEvent<Scene, LevelUpEvent>
    {
        protected override async ETTask Run(Scene scene, LevelUpEvent args)
        {
            // 获取玩家Unit
            Unit unit = scene.GetComponent<UnitComponent>()?.Get(args.UnitId);
            if (unit == null)
            {
                return;
            }

            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有等级类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Level)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    // 等级成就以达到目标等级为条件
                    if (args.Level >= achievement.MaxProgress)
                    {
                        achievementComponent.UpdateAchievementProgress(achievement.ConfigId, achievement.MaxProgress);
                    }
                    else
                    {
                        achievementComponent.UpdateAchievementProgress(achievement.ConfigId, args.Level);
                    }
                    
                    Log.Debug($"Level achievement progress updated: {achievement.ConfigId}, level: {args.Level}");
                }
            }

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 任务完成事件处理器
    /// </summary>
    [Event(SceneType.Map)]
    public class QuestCompleteEvent_AchievementHandler : AEvent<Scene, QuestCompleteEvent>
    {
        protected override async ETTask Run(Scene scene, QuestCompleteEvent args)
        {
            // 获取玩家Unit
            Unit unit = scene.GetComponent<UnitComponent>()?.Get(args.UnitId);
            if (unit == null)
            {
                return;
            }

            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有任务类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Quest)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + 1;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Quest achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 道具收集事件处理器
    /// </summary>
    [Event(SceneType.Map)]
    public class ItemCollectEvent_AchievementHandler : AEvent<Scene, ItemCollectEvent>
    {
        protected override async ETTask Run(Scene scene, ItemCollectEvent args)
        {
            // 获取玩家Unit
            Unit unit = scene.GetComponent<UnitComponent>()?.Get(args.UnitId);
            if (unit == null)
            {
                return;
            }

            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有收集类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Collect)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + args.Count;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Collect achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 地图探索事件处理器
    /// </summary>
    [Event(SceneType.Map)]
    public class MapExploreEvent_AchievementHandler : AEvent<Scene, MapExploreEvent>
    {
        protected override async ETTask Run(Scene scene, MapExploreEvent args)
        {
            // 获取玩家Unit
            Unit unit = scene.GetComponent<UnitComponent>()?.Get(args.UnitId);
            if (unit == null)
            {
                return;
            }

            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有探索类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Exploration)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + 1;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Exploration achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }

            await ETTask.CompletedTask;
        }
    }
}