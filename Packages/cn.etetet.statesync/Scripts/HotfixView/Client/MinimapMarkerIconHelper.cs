namespace ET.Client
{
    /// <summary>
    /// 小地图标记图标解析辅助。
    /// </summary>
    public static class MinimapMarkerIconHelper
    {
        public static string ResolveIconName(MinimapMarkerRuntime marker)
        {
            if (marker.UnitType != UnitType.NPC || marker.ConfigId <= 0)
            {
                return string.Empty;
            }

            string configKey = global::ET.MinimapConstKey.GetMarkerIconUnitConfigKey(marker.ConfigId);
            string configuredIconName = global::ET.MinimapConstConfigHelper.GetString(configKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(configuredIconName))
            {
                return configuredIconName;
            }

            UnitConfig unitConfig = UnitConfigCategory.Instance.GetOrDefault(marker.ConfigId);
            return unitConfig?.HeadIcon ?? string.Empty;
        }
    }
}
