using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 地图 POI 运行时状态。
    /// 挂在当前地图场景上，供小地图与大地图共享。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MapPoiRuntimeComponent : Entity, IAwake, IDestroy
    {
        public string MapName;
        public string LoadedMapName;
        public string SelectedPoiId;
        public Dictionary<string, MapPoiRuntimeData> Pois = new();
    }

    public enum MapPoiType
    {
        None = 0,
        Evacuation = 1,
        HighContainer = 2,
        BossSpawn = 3,
        MissionTask = 4,
    }

    public struct MapPoiRuntimeData
    {
        public string PoiId;
        public MapPoiType PoiType;
        public int PointType;
        public float3 Position;
        public int SideId;
        public bool ShowMinimap;
        public bool ShowWorldmap;
        public int TipTextId;
        public string IconName;
        public bool Visible;
    }
}
