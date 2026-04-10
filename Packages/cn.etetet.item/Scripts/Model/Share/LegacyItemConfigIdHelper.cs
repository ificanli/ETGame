namespace ET
{
    /// <summary>
    /// 旧物品配置ID兼容辅助类。
    /// 统一把历史掉落表里仍在使用的 10001~10012 归一到当前有效配置ID。
    /// </summary>
    public static class LegacyItemConfigIdHelper
    {
        public static int NormalizeConfigId(int configId)
        {
            if (configId <= 0)
            {
                return configId;
            }

            if (ItemConfigCategory.Instance.GetOrDefault(configId) != null)
            {
                return configId;
            }

            int mappedConfigId = GetLegacyMappedConfigId(configId);
            if (mappedConfigId > 0 && ItemConfigCategory.Instance.GetOrDefault(mappedConfigId) != null)
            {
                return mappedConfigId;
            }

            return configId;
        }

        public static bool MatchesConfigId(int leftConfigId, int rightConfigId)
        {
            if (leftConfigId <= 0 || rightConfigId <= 0)
            {
                return false;
            }

            return NormalizeConfigId(leftConfigId) == NormalizeConfigId(rightConfigId);
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
