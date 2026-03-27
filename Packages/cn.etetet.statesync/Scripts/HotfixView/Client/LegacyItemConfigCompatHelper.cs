namespace ET.Client
{
    /// <summary>
    /// 兼容旧掉落配置ID，避免地图容器仍使用历史ID时客户端界面无法正确显示。
    /// </summary>
    public static class LegacyItemConfigCompatHelper
    {
        public static ItemConfig GetDisplayItemConfig(int configId)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                return itemConfig;
            }

            int legacyMappedConfigId = GetLegacyMappedConfigId(configId);
            return legacyMappedConfigId > 0 ? ItemConfigCategory.Instance.GetOrDefault(legacyMappedConfigId) : null;
        }

        public static int GetDisplayConfigId(int configId)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                return configId;
            }

            int legacyMappedConfigId = GetLegacyMappedConfigId(configId);
            return legacyMappedConfigId > 0 ? legacyMappedConfigId : configId;
        }

        private static int GetLegacyMappedConfigId(int configId)
        {
            return configId switch
            {
                10001 => 22001,
                10002 => 22002,
                10003 => 22003,
                10004 => 22004,
                10005 => 22005,
                10006 => 22006,
                10007 => 22007,
                10008 => 22008,
                10009 => 22009,
                10010 => 22010,
                10011 => 22011,
                10012 => 22012,
                _ => 0,
            };
        }
    }
}
