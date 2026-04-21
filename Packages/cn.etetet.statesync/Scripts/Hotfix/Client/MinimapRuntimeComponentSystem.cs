using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(MinimapRuntimeComponent))]
    public static partial class MinimapRuntimeComponentSystem
    {
        private const float WorldBoundsComparisonTolerance = 0.001f;
        private const int MaxWorldBoundsAutoResolveRetryCount = 60;

        [EntitySystem]
        private static void Awake(this MinimapRuntimeComponent self)
        {
            self.ResetRuntime();
        }

        [EntitySystem]
        private static void Destroy(this MinimapRuntimeComponent self)
        {
            self.ClearRuntime();
        }

        public static void ResetRuntime(this MinimapRuntimeComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            string mapName = scene?.Name.GetSceneConfigName() ?? string.Empty;
            self.MapName = mapName;
            self.DisplayMode = MinimapDisplayMode.Compact;
            self.MyUnitId = 0;
            self.CompactRange = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.CompactRange), 40f);
            self.FogCellSize = global::ET.MinimapConstConfigHelper.GetFloat(
                global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.FogCellSize),
                global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.FogCellSize, 4f));
            self.FogVisionRadius = math.max(
                0f,
                global::ET.MinimapConstConfigHelper.GetFloat(
                    global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.SceneFogVisionRadius),
                    global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.SceneFogVisionRadius, 0f)));
            self.WorldBoundsResolvedFromTerrain = false;
            self.WorldBoundsAutoResolveAttempted = false;
            self.WorldBoundsAutoResolveRetryCount = 0;
            self.ApplyConfiguredWorldBounds();
            self.Markers.Clear();
            self.CurrentVisibleCells.Clear();
            self.ExploredCells.Clear();
        }

        public static bool TryGetConfiguredWorldBounds(
            this MinimapRuntimeComponent self,
            out float minX,
            out float maxX,
            out float minZ,
            out float maxZ)
        {
            string mapName = self?.MapName ?? string.Empty;
            minX = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.WorldMinX), 0f);
            maxX = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.WorldMaxX), 0f);
            minZ = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.WorldMinZ), 0f);
            maxZ = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.WorldMaxZ), 0f);
            return maxX > minX && maxZ > minZ;
        }

        public static void ApplyConfiguredWorldBounds(this MinimapRuntimeComponent self)
        {
            if (!self.TryGetConfiguredWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ))
            {
                self.UpdateWorldBounds(0f, 0f, 0f, 0f, false);
                return;
            }

            self.UpdateWorldBounds(minX, maxX, minZ, maxZ, false);
        }

        public static void UpdateWorldBounds(
            this MinimapRuntimeComponent self,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            bool resolvedFromTerrain)
        {
            float normalizedMinX = math.min(minX, maxX);
            float normalizedMaxX = math.max(minX, maxX);
            float normalizedMinZ = math.min(minZ, maxZ);
            float normalizedMaxZ = math.max(minZ, maxZ);
            bool boundsChanged =
                !ApproximatelyEqual(self.WorldMinX, normalizedMinX) ||
                !ApproximatelyEqual(self.WorldMaxX, normalizedMaxX) ||
                !ApproximatelyEqual(self.WorldMinZ, normalizedMinZ) ||
                !ApproximatelyEqual(self.WorldMaxZ, normalizedMaxZ);

            self.WorldMinX = normalizedMinX;
            self.WorldMaxX = normalizedMaxX;
            self.WorldMinZ = normalizedMinZ;
            self.WorldMaxZ = normalizedMaxZ;
            self.WorldBoundsResolvedFromTerrain = resolvedFromTerrain;

            if (!boundsChanged)
            {
                return;
            }

            // 网格尺寸依赖世界边界，边界发生变化时清空迷雾缓存，避免沿用旧索引。
            self.CurrentVisibleCells.Clear();
            self.ExploredCells.Clear();
        }

        public static void BindMyUnit(this MinimapRuntimeComponent self, Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            self.MyUnitId = unit.Id;
        }

        public static void SetDisplayMode(this MinimapRuntimeComponent self, MinimapDisplayMode displayMode)
        {
            self.DisplayMode = displayMode;
        }

        public static void ClearRuntime(this MinimapRuntimeComponent self)
        {
            self.MapName = string.Empty;
            self.DisplayMode = MinimapDisplayMode.Compact;
            self.MyUnitId = 0;
            self.CompactRange = 0f;
            self.WorldMinX = 0f;
            self.WorldMaxX = 0f;
            self.WorldMinZ = 0f;
            self.WorldMaxZ = 0f;
            self.WorldBoundsResolvedFromTerrain = false;
            self.WorldBoundsAutoResolveAttempted = false;
            self.WorldBoundsAutoResolveRetryCount = 0;
            self.FogCellSize = 0f;
            self.FogVisionRadius = 0f;
            self.Markers.Clear();
            self.CurrentVisibleCells.Clear();
            self.ExploredCells.Clear();
        }

        public static bool ShouldRetryResolveWorldBounds(this MinimapRuntimeComponent self)
        {
            return self != null &&
                    !self.WorldBoundsResolvedFromTerrain &&
                    self.WorldBoundsAutoResolveRetryCount < MaxWorldBoundsAutoResolveRetryCount;
        }

        public static float2 WorldToNormalizedPosition(this MinimapRuntimeComponent self, float3 worldPosition)
        {
            float width = self.WorldMaxX - self.WorldMinX;
            float height = self.WorldMaxZ - self.WorldMinZ;
            if (width <= 0f || height <= 0f)
            {
                return new float2(0.5f, 0.5f);
            }

            float x = math.saturate((worldPosition.x - self.WorldMinX) / width);
            float y = math.saturate((worldPosition.z - self.WorldMinZ) / height);
            return new float2(x, y);
        }

        public static Unit GetMyUnit(this MinimapRuntimeComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            if (scene == null || scene.IsDisposed || self.MyUnitId == 0)
            {
                return null;
            }

            return scene.GetComponent<UnitComponent>()?.Get(self.MyUnitId);
        }

        public static bool TryGetMyPosition(this MinimapRuntimeComponent self, out float3 position)
        {
            position = float3.zero;
            Unit unit = self.GetMyUnit();
            if (unit == null || unit.IsDisposed)
            {
                return false;
            }

            position = unit.Position;
            return true;
        }

        public static Dictionary<long, MinimapMarkerRuntime> GetMarkers(this MinimapRuntimeComponent self)
        {
            return self.Markers;
        }

        public static bool TryGetFogGridSize(this MinimapRuntimeComponent self, out int gridWidth, out int gridHeight)
        {
            gridWidth = 0;
            gridHeight = 0;
            float width = self.WorldMaxX - self.WorldMinX;
            float height = self.WorldMaxZ - self.WorldMinZ;
            if (width <= 0f || height <= 0f || self.FogCellSize <= 0f)
            {
                return false;
            }

            gridWidth = math.max(1, (int)math.ceil(width / self.FogCellSize));
            gridHeight = math.max(1, (int)math.ceil(height / self.FogCellSize));
            return true;
        }

        public static void RefreshLocalFog(this MinimapRuntimeComponent self)
        {
            self.CurrentVisibleCells.Clear();

            Scene scene = self.GetParent<Scene>();
            Unit myUnit = self.GetMyUnit();
            if (scene == null || scene.IsDisposed || myUnit == null || myUnit.IsDisposed)
            {
                return;
            }

            if (!self.TryGetFogGridSize(out int gridWidth, out int gridHeight))
            {
                return;
            }

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            foreach (Unit unit in unitComponent.Children.Values)
            {
                if (unit == null || unit.IsDisposed)
                {
                    continue;
                }

                if (unit.Id != myUnit.Id && !CampHelper.IsFriendly(myUnit, unit))
                {
                    continue;
                }

                float radius = self.ResolveFogVisionRadius(unit);
                if (radius <= 0f)
                {
                    continue;
                }

                self.AppendFogCells(gridWidth, gridHeight, unit, radius);
            }

            foreach (int cellIndex in self.CurrentVisibleCells)
            {
                self.ExploredCells.Add(cellIndex);
            }
        }

        public static int ResolveAoiCellRadius(int rawViewDistance)
        {
            int viewDistance = rawViewDistance;
            if (viewDistance <= 0)
            {
                viewDistance = 1;
            }

            return (viewDistance - 1) / global::ET.AOIConst.CellSizePermille + 1;
        }

        private static float ResolveFogVisionRadius(this MinimapRuntimeComponent self, Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return 0f;
            }

            if (unit.UnitType == UnitType.Player)
            {
                if (WeaponVisionRangeHelper.TryResolveCurrentSniperVisionRange(unit, out float sniperRange))
                {
                    return sniperRange;
                }

                if (self.FogVisionRadius > 0f)
                {
                    return self.FogVisionRadius;
                }

                return VisibilityLineOfSightHelper.ResolveRawAoiWorldRadius(unit, 0f);
            }

            return VisibilityLineOfSightHelper.ResolveRawAoiWorldRadius(unit);
        }

        private static void AppendFogCells(this MinimapRuntimeComponent self, int gridWidth, int gridHeight, Unit source, float radius)
        {
            if (source == null || source.IsDisposed)
            {
                return;
            }

            float cellSize = self.FogCellSize;
            if (cellSize <= 0f)
            {
                return;
            }

            float3 centerPosition = source.Position;
            int minX = math.max(0, (int)math.floor((centerPosition.x - radius - self.WorldMinX) / cellSize));
            int maxX = math.min(gridWidth - 1, (int)math.floor((centerPosition.x + radius - self.WorldMinX) / cellSize));
            int minY = math.max(0, (int)math.floor((centerPosition.z - radius - self.WorldMinZ) / cellSize));
            int maxY = math.min(gridHeight - 1, (int)math.floor((centerPosition.z + radius - self.WorldMinZ) / cellSize));
            float radiusSqr = radius * radius;

            for (int y = minY; y <= maxY; ++y)
            {
                float cellCenterZ = self.WorldMinZ + (y + 0.5f) * cellSize;
                for (int x = minX; x <= maxX; ++x)
                {
                    float cellCenterX = self.WorldMinX + (x + 0.5f) * cellSize;
                    float deltaX = cellCenterX - centerPosition.x;
                    float deltaZ = cellCenterZ - centerPosition.z;
                    if (deltaX * deltaX + deltaZ * deltaZ > radiusSqr)
                    {
                        continue;
                    }

                    float3 cellCenter = new float3(cellCenterX, centerPosition.y, cellCenterZ);
                    if (!VisibilityLineOfSightHelper.HasLineOfSight(source, cellCenter))
                    {
                        continue;
                    }

                    self.CurrentVisibleCells.Add(y * gridWidth + x);
                }
            }
        }

        private static bool ApproximatelyEqual(float left, float right)
        {
            return math.abs(left - right) <= WorldBoundsComparisonTolerance;
        }
    }
}
