using System;
using System.Collections.Generic;

namespace ET.Server
{
    public static class HomeConfigHelper
    {
        public static HomeBuildingConfig GetBuildingConfig(int configId)
        {
            return HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
        }

        public static HomeBuildingConfig GetBuildingConfigByType(int buildingType)
        {
            foreach (HomeBuildingConfig config in HomeBuildingConfigCategory.Instance.DataList)
            {
                if (config != null && config.BuildingType == buildingType)
                {
                    return config;
                }
            }

            return null;
        }

        public static HomeBuildingConfig GetMainCityConfig()
        {
            return GetBuildingConfigByType(HomeBuildingType.MainCity);
        }

        public static HomeSlotConfig GetSlotConfig(int slotId)
        {
            return HomeSlotConfigCategory.Instance.GetOrDefault(slotId);
        }

        public static HomeSlotConfig GetMainCitySlotConfig()
        {
            foreach (HomeSlotConfig config in HomeSlotConfigCategory.Instance.DataList)
            {
                if (config != null && config.SlotType == HomeSlotType.MainCity)
                {
                    return config;
                }
            }

            return null;
        }

        public static List<HomeSlotConfig> GetSortedSlotConfigs()
        {
            List<HomeSlotConfig> result = new();
            foreach (HomeSlotConfig config in HomeSlotConfigCategory.Instance.DataList)
            {
                if (config != null)
                {
                    result.Add(config);
                }
            }

            result.Sort(static (a, b) =>
            {
                int sortCompare = a.SortOrder.CompareTo(b.SortOrder);
                return sortCompare != 0 ? sortCompare : a.Id.CompareTo(b.Id);
            });
            return result;
        }

        public static HomeMainCityLevelConfig GetMainCityLevelConfig(int level)
        {
            return HomeMainCityLevelConfigCategory.Instance.GetOrDefault(level);
        }

        public static int GetMaxMainCityLevel()
        {
            int maxLevel = 0;
            foreach (HomeMainCityLevelConfig config in HomeMainCityLevelConfigCategory.Instance.DataList)
            {
                if (config != null && config.Level > maxLevel)
                {
                    maxLevel = config.Level;
                }
            }

            return maxLevel;
        }

        public static HomeBuildingLevelConfig GetBuildingLevelConfig(int buildingConfigId, int level)
        {
            foreach (HomeBuildingLevelConfig config in HomeBuildingLevelConfigCategory.Instance.DataList)
            {
                if (config != null && config.BuildingConfigId == buildingConfigId && config.Level == level)
                {
                    return config;
                }
            }

            return null;
        }

        public static List<HomeMainCityTaskConfig> GetMainCityTaskConfigs(int taskGroupId)
        {
            List<HomeMainCityTaskConfig> result = new();
            if (taskGroupId <= 0)
            {
                return result;
            }

            foreach (HomeMainCityTaskConfig config in HomeMainCityTaskConfigCategory.Instance.DataList)
            {
                if (config != null && config.TaskGroupId == taskGroupId)
                {
                    result.Add(config);
                }
            }

            result.Sort(static (a, b) =>
            {
                int sortCompare = a.SortOrder.CompareTo(b.SortOrder);
                return sortCompare != 0 ? sortCompare : a.Id.CompareTo(b.Id);
            });
            return result;
        }

        public static bool IsSlotUnlocked(int mainCityLevel, HomeSlotConfig slotConfig)
        {
            return slotConfig != null && mainCityLevel >= slotConfig.UnlockMainCityLevel;
        }

        public static bool CanBuildInSlot(HomeSlotConfig slotConfig, int buildingType)
        {
            if (slotConfig == null)
            {
                return false;
            }

            if (slotConfig.CanBuildTypes == null || slotConfig.CanBuildTypes.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < slotConfig.CanBuildTypes.Length; ++i)
            {
                if (slotConfig.CanBuildTypes[i] == buildingType)
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetExtraInt(HomeBuildingLevelConfig config, string key, int defaultValue)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ExtraParams) || string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            string token = $"\"{key}\"";
            int keyIndex = config.ExtraParams.IndexOf(token, StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                return defaultValue;
            }

            int colonIndex = config.ExtraParams.IndexOf(':', keyIndex + token.Length);
            if (colonIndex < 0)
            {
                return defaultValue;
            }

            int startIndex = colonIndex + 1;
            while (startIndex < config.ExtraParams.Length && char.IsWhiteSpace(config.ExtraParams[startIndex]))
            {
                ++startIndex;
            }

            int endIndex = startIndex;
            if (endIndex < config.ExtraParams.Length && config.ExtraParams[endIndex] == '-')
            {
                ++endIndex;
            }

            while (endIndex < config.ExtraParams.Length && char.IsDigit(config.ExtraParams[endIndex]))
            {
                ++endIndex;
            }

            if (endIndex <= startIndex)
            {
                return defaultValue;
            }

            string numberText = config.ExtraParams.Substring(startIndex, endIndex - startIndex);
            return int.TryParse(numberText, out int value) ? value : defaultValue;
        }
    }
}
