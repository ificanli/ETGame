using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(MinimapRuntimeComponent))]
    public static partial class MinimapRuntimeComponentSystem
    {
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
            self.WorldMinX = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.WorldMinX), 0f);
            self.WorldMaxX = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.WorldMaxX), 0f);
            self.WorldMinZ = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.WorldMinZ), 0f);
            self.WorldMaxZ = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.WorldMaxZ), 0f);
            self.FogCellSize = global::ET.MinimapConstConfigHelper.GetFloat(
                global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.FogCellSize),
                global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.FogCellSize, 4f));
            self.Markers.Clear();
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
            self.FogCellSize = 0f;
            self.Markers.Clear();
            self.CurrentVisibleCells.Clear();
            self.ExploredCells.Clear();
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
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            Unit myUnit = self.GetMyUnit();
            if (unitComponent == null || myUnit == null || myUnit.IsDisposed)
            {
                return;
            }

            if (!self.TryGetFogGridSize(out int gridWidth, out int gridHeight))
            {
                return;
            }

            foreach (Unit unit in unitComponent.Children.Values)
            {
                if (unit == null || unit.IsDisposed)
                {
                    continue;
                }

                if (!CampHelper.IsFriendly(myUnit, unit))
                {
                    continue;
                }

                NumericComponent numeric = unit.NumericComponent;
                if (numeric == null)
                {
                    continue;
                }

                float visionRadius = numeric.GetAsFloat(NumericType.AOI);
                if (visionRadius <= 0f)
                {
                    continue;
                }

                self.AppendFogCells(gridWidth, gridHeight, unit.Position, visionRadius);
            }

            foreach (int cellIndex in self.CurrentVisibleCells)
            {
                self.ExploredCells.Add(cellIndex);
            }
        }

        private static void AppendFogCells(this MinimapRuntimeComponent self, int gridWidth, int gridHeight, float3 centerPosition, float radius)
        {
            float cellSize = self.FogCellSize;
            if (cellSize <= 0f)
            {
                return;
            }

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

                    self.CurrentVisibleCells.Add(y * gridWidth + x);
                }
            }
        }
    }
}
