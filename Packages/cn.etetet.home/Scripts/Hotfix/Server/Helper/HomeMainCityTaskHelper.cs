using System.Collections.Generic;

namespace ET.Server
{
    public static class HomeMainCityTaskHelper
    {
        public static int GetCurrentTaskGroupId(PlayerHomeComponent homeComp)
        {
            int mainCityLevel = HomeRuntimeHelper.GetMainCityLevel(homeComp);
            HomeMainCityLevelConfig levelConfig = HomeConfigHelper.GetMainCityLevelConfig(mainCityLevel);
            return levelConfig?.TaskGroupId ?? 0;
        }

        public static List<HomeMainCityTaskConfig> GetCurrentTaskConfigs(PlayerHomeComponent homeComp)
        {
            return HomeConfigHelper.GetMainCityTaskConfigs(GetCurrentTaskGroupId(homeComp));
        }

        public static bool IsCurrentTaskGroupCompleted(PlayerHomeComponent homeComp, out int taskGroupId, out int finishedCount, out int totalCount)
        {
            taskGroupId = GetCurrentTaskGroupId(homeComp);
            CountTaskGroupProgress(homeComp, taskGroupId, out finishedCount, out totalCount);
            return totalCount <= 0 || finishedCount >= totalCount;
        }

        public static void CountTaskGroupProgress(PlayerHomeComponent homeComp, int taskGroupId, out int finishedCount, out int totalCount)
        {
            finishedCount = 0;
            List<HomeMainCityTaskConfig> configs = HomeConfigHelper.GetMainCityTaskConfigs(taskGroupId);
            totalCount = configs.Count;
            for (int i = 0; i < configs.Count; ++i)
            {
                EvaluateTask(homeComp, configs[i], out _, out _, out bool completed);
                if (completed)
                {
                    ++finishedCount;
                }
            }
        }

        public static void EvaluateTask(
            PlayerHomeComponent homeComp,
            HomeMainCityTaskConfig config,
            out int progress,
            out int target,
            out bool completed)
        {
            progress = 0;
            target = 1;
            completed = false;
            if (homeComp == null || config == null)
            {
                return;
            }

            switch (config.TaskType)
            {
                case HomeMainCityTaskType.BuildBuilding:
                {
                    target = config.Param2 > 0 ? config.Param2 : 1;
                    progress = HomeRuntimeHelper.CountBuildingsByType(homeComp, config.Param1);
                    break;
                }
                case HomeMainCityTaskType.BuildingLevel:
                {
                    target = config.Param2 > 0 ? config.Param2 : 1;
                    progress = HomeRuntimeHelper.GetHighestBuildingLevelByType(homeComp, config.Param1);
                    break;
                }
                case HomeMainCityTaskType.MuseumDisplayCount:
                {
                    target = config.Param1 > 0 ? config.Param1 : 1;
                    progress = HomeMuseumHelper.CountDisplays(homeComp, 0);
                    break;
                }
                case HomeMainCityTaskType.RecycleCollectCount:
                {
                    target = config.Param1 > 0 ? config.Param1 : 1;
                    progress = homeComp.RecycleCollectedCount;
                    break;
                }
                case HomeMainCityTaskType.FarmCollectCount:
                {
                    target = config.Param1 > 0 ? config.Param1 : 1;
                    progress = homeComp.FarmCollectedCount;
                    break;
                }
            }

            if (progress < 0)
            {
                progress = 0;
            }

            completed = progress >= target;
        }

        public static string BuildFirstIncompleteTaskHint(PlayerHomeComponent homeComp)
        {
            int taskGroupId = GetCurrentTaskGroupId(homeComp);
            List<HomeMainCityTaskConfig> configs = HomeConfigHelper.GetMainCityTaskConfigs(taskGroupId);
            for (int i = 0; i < configs.Count; ++i)
            {
                HomeMainCityTaskConfig config = configs[i];
                EvaluateTask(homeComp, config, out int progress, out int target, out bool completed);
                if (!completed)
                {
                    return $"请先完成主城任务：{config.Title}（{progress}/{target}）。";
                }
            }

            return taskGroupId > 0
                ? $"主城任务组 {taskGroupId} 已完成，可升级主城。"
                : "当前没有主城升级任务。";
        }
    }
}
