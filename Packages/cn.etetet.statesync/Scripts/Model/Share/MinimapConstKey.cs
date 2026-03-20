namespace ET
{
    /// <summary>
    /// 小地图常量配置 Key 定义。
    /// </summary>
    public static class MinimapConstKey
    {
        public const string CompactRange = "CompactRange";
        public const string WorldMinX = "WorldMinX";
        public const string WorldMaxX = "WorldMaxX";
        public const string WorldMinZ = "WorldMinZ";
        public const string WorldMaxZ = "WorldMaxZ";
        public const string FogCellSize = "FogCellSize";
        public const string MarkerSize = "Minimap.MarkerSize";
        public const string MarkerColorSelf = "Minimap.MarkerColor.Self";
        public const string MarkerColorPlayer = "Minimap.MarkerColor.Player";
        public const string MarkerColorFriendlyPlayer = "Minimap.MarkerColor.FriendlyPlayer";
        public const string MarkerColorEnemyPlayer = "Minimap.MarkerColor.EnemyPlayer";
        public const string MarkerColorMonster = "Minimap.MarkerColor.Monster";
        public const string MarkerColorOther = "Minimap.MarkerColor.Other";
        public const string MarkerIconUnitConfigPrefix = "Minimap.MarkerIcon.UnitConfig";
        public const string FogColorVisible = "Minimap.FogColor.Visible";
        public const string FogColorExplored = "Minimap.FogColor.Explored";
        public const string FogColorUnexplored = "Minimap.FogColor.Unexplored";
        public const string SceneFogVisionRadius = "Minimap.SceneFog.VisionRadius";
        public const string SceneFogRefreshInterval = "Minimap.SceneFog.RefreshInterval";
        public const string SceneFogEdgeSoftness = "Minimap.SceneFog.EdgeSoftness";
        public const string SceneFogExploredAlphaScale = "Minimap.SceneFog.ExploredAlphaScale";

        public static string GetMapKey(string mapName, string key)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return key;
            }

            return $"{mapName}.{key}";
        }

        public static string GetMarkerIconUnitConfigKey(int configId)
        {
            return configId > 0 ? $"{MarkerIconUnitConfigPrefix}.{configId}" : string.Empty;
        }
    }
}
