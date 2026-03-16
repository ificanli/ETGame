namespace ET
{
    /// <summary>
    /// 小地图常量配置读取辅助类。
    /// </summary>
    public static class MinimapConstConfigHelper
    {
        public static MinimapConstConfig GetByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            MinimapConstConfigCategory category = MinimapConstConfigCategory.Instance;
            if (category == null)
            {
                return null;
            }

            foreach (MinimapConstConfig config in category.DataList)
            {
                if (config != null && config.Key == key)
                {
                    return config;
                }
            }

            return null;
        }

        public static float GetFloat(string key, float fallback = 0f)
        {
            MinimapConstConfig config = GetByKey(key);
            return config != null ? config.FloatValue : fallback;
        }

        public static string GetString(string key, string fallback = "")
        {
            MinimapConstConfig config = GetByKey(key);
            if (config == null || string.IsNullOrWhiteSpace(config.StringValue))
            {
                return fallback;
            }

            return config.StringValue;
        }
    }
}
