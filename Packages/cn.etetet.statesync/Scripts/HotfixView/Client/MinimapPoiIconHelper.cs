namespace ET.Client
{
    /// <summary>
    /// 地图 POI 图标解析辅助。
    /// </summary>
    public static class MinimapPoiIconHelper
    {
        public static string ResolveIconName(MapPoiRuntimeData poi)
        {
            if (!string.IsNullOrWhiteSpace(poi.IconName))
            {
                return poi.IconName;
            }

            string key = poi.PoiType switch
            {
                MapPoiType.Evacuation => global::ET.MinimapConstKey.PoiIconEvacuation,
                MapPoiType.HighContainer => global::ET.MinimapConstKey.PoiIconHighContainer,
                MapPoiType.BossSpawn => global::ET.MinimapConstKey.PoiIconBossSpawn,
                MapPoiType.MissionTask => global::ET.MinimapConstKey.PoiIconMissionTask,
                _ => string.Empty,
            };

            return string.IsNullOrWhiteSpace(key)
                ? string.Empty
                : global::ET.MinimapConstConfigHelper.GetString(key, string.Empty);
        }

        public static string ResolveTrackedIconName(MapPoiRuntimeData poi)
        {
            string trackedIcon = global::ET.MinimapConstConfigHelper.GetString(global::ET.MinimapConstKey.PoiIconTracked, string.Empty);
            if (!string.IsNullOrWhiteSpace(trackedIcon))
            {
                return trackedIcon;
            }

            return ResolveIconName(poi);
        }
    }
}
