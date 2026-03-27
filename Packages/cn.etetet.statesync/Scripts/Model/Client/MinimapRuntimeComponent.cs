using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 小地图运行时状态。
    /// 挂在当前地图场景上，供折叠态与展开态共享。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MinimapRuntimeComponent : Entity, IAwake, IDestroy
    {
        public string MapName;
        public MinimapDisplayMode DisplayMode;
        public long MyUnitId;
        public float CompactRange;
        public float WorldMinX;
        public float WorldMaxX;
        public float WorldMinZ;
        public float WorldMaxZ;
        public bool WorldBoundsResolvedFromTerrain;
        public bool WorldBoundsAutoResolveAttempted;
        public int WorldBoundsAutoResolveRetryCount;
        public float FogCellSize;
        public float FogVisionRadius;
        public Dictionary<long, MinimapMarkerRuntime> Markers = new();
        public HashSet<int> CurrentVisibleCells = new();
        public HashSet<int> ExploredCells = new();
    }

    public enum MinimapDisplayMode
    {
        Compact = 0,
        Expanded = 1,
    }

    public struct MinimapMarkerRuntime
    {
        public long UnitId;
        public int ConfigId;
        public UnitType UnitType;
        public float3 Position;
        public float3 Forward;
        public bool IsVisible;
        public long LastSeenTime;
    }
}
