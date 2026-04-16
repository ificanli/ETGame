using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(MapPoiRuntimeComponent))]
    public static partial class MapPoiRuntimeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MapPoiRuntimeComponent self)
        {
            self.ResetRuntime();
        }

        [EntitySystem]
        private static void Destroy(this MapPoiRuntimeComponent self)
        {
            self.ClearRuntime();
        }

        public static void ResetRuntime(this MapPoiRuntimeComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            self.MapName = scene?.Name.GetSceneConfigName() ?? string.Empty;
            self.LoadedMapName = string.Empty;
            self.SelectedPoiId = string.Empty;
            self.Pois.Clear();
            self.EnsureConfigLoaded();
        }

        public static void ClearRuntime(this MapPoiRuntimeComponent self)
        {
            self.MapName = string.Empty;
            self.LoadedMapName = string.Empty;
            self.SelectedPoiId = string.Empty;
            self.Pois.Clear();
        }

        public static void EnsureConfigLoaded(this MapPoiRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Scene scene = self.GetParent<Scene>();
            if (scene == null || scene.IsDisposed)
            {
                return;
            }

            string mapName = scene.Name.GetSceneConfigName();
            if (string.IsNullOrWhiteSpace(mapName) || self.LoadedMapName == mapName)
            {
                return;
            }

            self.MapName = mapName;
            self.LoadedMapName = mapName;
            self.Pois.Clear();

            string path = $"Packages/cn.etetet.map/Bundles/ECA/{mapName}.txt";
            string json = EventSystem.Instance.Invoke<ECAConcealmentConfigLoader, string>(new ECAConcealmentConfigLoader
            {
                Location = path
            });
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            List<ECAConfig> configs = MongoHelper.FromJson<List<ECAConfig>>(json);
            if (configs == null || configs.Count == 0)
            {
                return;
            }

            foreach (ECAConfig config in configs)
            {
                if (!TryBuildPoi(config, out MapPoiRuntimeData poi))
                {
                    continue;
                }

                self.Pois[poi.PoiId] = poi;
            }

            self.ValidateSelection();
        }

        public static Dictionary<string, MapPoiRuntimeData> GetPois(this MapPoiRuntimeComponent self)
        {
            return self.Pois;
        }

        public static bool ShouldDisplayOnMinimap(this MapPoiRuntimeComponent self, MapPoiRuntimeData poi)
        {
            return self.ShouldDisplayPoi(poi) && poi.ShowMinimap;
        }

        public static bool ShouldDisplayOnWorldmap(this MapPoiRuntimeComponent self, MapPoiRuntimeData poi)
        {
            return self.ShouldDisplayPoi(poi) && poi.ShowWorldmap;
        }

        public static bool ShouldDisplayPoi(this MapPoiRuntimeComponent self, MapPoiRuntimeData poi)
        {
            if (self == null || self.IsDisposed || !poi.Visible)
            {
                return false;
            }

            if (poi.SideId <= 0)
            {
                return true;
            }

            return self.ResolveMySideId() == poi.SideId;
        }

        public static void SelectPoi(this MapPoiRuntimeComponent self, string poiId)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (!self.Pois.TryGetValue(poiId, out MapPoiRuntimeData poi) || !self.ShouldDisplayPoi(poi))
            {
                self.SelectedPoiId = string.Empty;
                return;
            }

            self.SelectedPoiId = poiId ?? string.Empty;
        }

        public static bool TryGetSelectedPoi(this MapPoiRuntimeComponent self, out MapPoiRuntimeData poi)
        {
            poi = default;
            if (self == null || self.IsDisposed || string.IsNullOrWhiteSpace(self.SelectedPoiId))
            {
                return false;
            }

            if (!self.Pois.TryGetValue(self.SelectedPoiId, out poi))
            {
                self.SelectedPoiId = string.Empty;
                return false;
            }

            if (!self.ShouldDisplayPoi(poi))
            {
                self.SelectedPoiId = string.Empty;
                return false;
            }

            return true;
        }

        public static int ResolveMySideId(this MapPoiRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 0;
            }

            Scene scene = self.GetParent<Scene>();
            Unit myUnit = scene?.GetComponent<MinimapRuntimeComponent>()?.GetMyUnit();
            if (myUnit == null || myUnit.IsDisposed)
            {
                return 0;
            }

            SideComponent side = myUnit.GetComponent<SideComponent>();
            return side?.SideId ?? 0;
        }

        private static void ValidateSelection(this MapPoiRuntimeComponent self)
        {
            if (self == null || self.IsDisposed || string.IsNullOrWhiteSpace(self.SelectedPoiId))
            {
                return;
            }

            if (!self.Pois.TryGetValue(self.SelectedPoiId, out MapPoiRuntimeData poi) || !self.ShouldDisplayPoi(poi))
            {
                self.SelectedPoiId = string.Empty;
            }
        }

        private static bool TryBuildPoi(ECAConfig config, out MapPoiRuntimeData poi)
        {
            poi = default;
            if (config == null || string.IsNullOrWhiteSpace(config.ConfigId))
            {
                return false;
            }

            MapPoiType poiType = ResolvePoiType(config);
            bool? visibleOverride = TryGetBoolOverride(config.Params, ECAPointParamKey.MapPoiVisible);
            bool isSemanticPoi = poiType != MapPoiType.None;
            if (visibleOverride == false)
            {
                return false;
            }

            if (!isSemanticPoi && visibleOverride != true)
            {
                return false;
            }

            bool showMinimap = TryGetBoolOverride(config.Params, ECAPointParamKey.MapPoiShowMinimap) ?? true;
            bool showWorldmap = TryGetBoolOverride(config.Params, ECAPointParamKey.MapPoiShowWorldmap) ?? true;
            int pointType = config.GetPointType();
            int tipTextId = FlowParamHelper.GetIntParamOrDefault(config.Params, ECAPointParamKey.MapPoiTipTextId, ResolveDefaultTipTextId(poiType));
            string iconName = FlowParamHelper.GetStringParamOrDefault(config.Params, ECAPointParamKey.MapPoiIcon, ResolveDefaultIconName(poiType));

            poi = new MapPoiRuntimeData
            {
                PoiId = config.ConfigId,
                PoiType = poiType,
                PointType = pointType,
                Position = new float3(config.PosX, config.PosY, config.PosZ),
                SideId = FlowParamHelper.GetIntParamOrDefault(config.Params, ECAPointParamKey.SideId, 0),
                ShowMinimap = showMinimap,
                ShowWorldmap = showWorldmap,
                TipTextId = tipTextId,
                IconName = iconName,
                Visible = true,
            };
            return true;
        }

        private static MapPoiType ResolvePoiType(ECAConfig config)
        {
            if (config == null)
            {
                return MapPoiType.None;
            }

            switch (config.GetPointType())
            {
                case ECAPointType.EvacuationPoint:
                    return MapPoiType.Evacuation;
                case ECAPointType.Container:
                    {
                        string containerProfile = FlowParamHelper.GetStringParamOrDefault(config.Params, ECAPointParamKey.ContainerProfile, string.Empty);
                        return string.Equals(containerProfile, "Epic", System.StringComparison.OrdinalIgnoreCase)
                            ? MapPoiType.HighContainer
                            : MapPoiType.None;
                    }
                case ECAPointType.MonsterSpawnPoint:
                    {
                        string spawnProfile = FlowParamHelper.GetStringParamOrDefault(config.Params, ECAPointParamKey.SpawnProfile, string.Empty);
                        return string.Equals(spawnProfile, "Boss", System.StringComparison.OrdinalIgnoreCase)
                            ? MapPoiType.BossSpawn
                            : MapPoiType.None;
                    }
                case ECAPointType.RangeTrigger:
                    return IsMissionTaskPoi(config.Params)
                        ? MapPoiType.MissionTask
                        : MapPoiType.None;
                default:
                    return MapPoiType.None;
            }
        }

        private static bool IsMissionTaskPoi(List<FlowParam> source)
        {
            return FlowParamHelper.GetIntParamOrDefault(source, RogueMissionTaskPointParamKey.MonsterUnitConfigId, 0) > 0 &&
                FlowParamHelper.GetIntParamOrDefault(source, RogueMissionTaskPointParamKey.MonsterCount, 0) > 0 &&
                FlowParamHelper.GetIntParamOrDefault(source, RogueMissionTaskPointParamKey.RewardGold, 0) > 0;
        }

        private static bool? TryGetBoolOverride(List<FlowParam> source, string key)
        {
            if (source == null || source.Count == 0 || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            foreach (FlowParam param in source)
            {
                if (param == null || param.Key != key)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(param.Value))
                {
                    return null;
                }

                if (bool.TryParse(param.Value, out bool boolValue))
                {
                    return boolValue;
                }

                if (int.TryParse(param.Value, out int intValue))
                {
                    return intValue != 0;
                }

                return null;
            }

            return null;
        }

        private static int ResolveDefaultTipTextId(MapPoiType poiType)
        {
            string key = poiType switch
            {
                MapPoiType.Evacuation => global::ET.MinimapConstKey.PoiTipTextEvacuation,
                MapPoiType.HighContainer => global::ET.MinimapConstKey.PoiTipTextHighContainer,
                MapPoiType.BossSpawn => global::ET.MinimapConstKey.PoiTipTextBossSpawn,
                MapPoiType.MissionTask => global::ET.MinimapConstKey.PoiTipTextMissionTask,
                _ => string.Empty,
            };

            if (string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            return (int)global::ET.MinimapConstConfigHelper.GetFloat(key, 0f);
        }

        private static string ResolveDefaultIconName(MapPoiType poiType)
        {
            string key = poiType switch
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
    }
}
