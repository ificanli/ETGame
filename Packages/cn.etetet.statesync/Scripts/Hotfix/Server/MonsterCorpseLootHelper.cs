using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 怪物死亡后生成共享尸体盒，复用现有 SearchPanel 容器链路。
    /// </summary>
    public static class MonsterCorpseLootHelper
    {
        public static bool TryCreateCorpse(Unit target)
        {
            ResolveSpawnArguments(
                target,
                out int defaultLootBoxUnitConfigId,
                out int lootBoxUnitConfigId,
                out string lootTable,
                out int dropCountMin,
                out int dropCountMax,
                out bool allowRepeat);

            return TryCreateCorpse(
                target,
                defaultLootBoxUnitConfigId,
                lootBoxUnitConfigId,
                lootTable,
                dropCountMin,
                dropCountMax,
                allowRepeat,
                out _,
                out _);
        }

        public static bool TryCreateCorpse(
            Unit target,
            int defaultLootBoxUnitConfigId,
            int lootBoxUnitConfigId,
            string lootTable,
            int dropCountMin,
            int dropCountMax,
            bool allowRepeat,
            out string pointId,
            out long pointUnitId)
        {
            pointId = null;
            pointUnitId = 0;
            if (target == null || target.IsDisposed || target.UnitType != UnitType.Monster)
            {
                return false;
            }

            Scene scene = target.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            ECAManagerComponent ecaManager = scene?.GetComponent<ECAManagerComponent>();
            if (scene == null || scene.IsDisposed || unitComponent == null || ecaManager == null)
            {
                Log.Warning($"[MonsterCorpseLoot] skip create: missing scene/unit/eca manager, unitId={target.Id}, configId={target.ConfigId}");
                return false;
            }

            int resolvedLootBoxUnitConfigId = ResolveLootBoxUnitConfigId(lootBoxUnitConfigId, defaultLootBoxUnitConfigId);
            if (resolvedLootBoxUnitConfigId <= 0)
            {
                Log.Warning($"[MonsterCorpseLoot] skip create: invalid loot box config, unitId={target.Id}, configId={target.ConfigId}");
                return false;
            }

            pointUnitId = IdGenerater.Instance.GenerateId();
            pointId = $"{SearchPanelOpenConst.MonsterCorpsePointPrefix}{target.Id}_{pointUnitId}";

            Unit pointUnit = UnitFactory.Create(scene, pointUnitId, resolvedLootBoxUnitConfigId, target.Position, target.Rotation, false);
            if (pointUnit == null || pointUnit.IsDisposed)
            {
                Log.Warning($"[MonsterCorpseLoot] skip create: failed to create point unit, configId={resolvedLootBoxUnitConfigId}, target={target.Id}");
                return false;
            }

            pointUnit.UnitType = UnitType.Virtual;
            pointUnit.Position = target.Position;
            pointUnit.Rotation = target.Rotation;
            pointUnit.GetComponent<UnitSpawnPointComponent>()?.SetSpawnPoint(pointUnit.Position, pointUnit.Rotation);
            ClearCombatNumeric(pointUnit);

            float interactRange = ExtractionInventoryConfig.GetCorpseInteractRange();
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>(pointId, ECAPointType.Container, interactRange);
            point.Params = CreatePointParams(interactRange);
            point.FlowGraph = CreateOpenContainerFlowGraph(ExtractionInventoryConfig.GetCorpseButtonTextId());

            ContainerComponent container = pointUnit.AddComponent<ContainerComponent, string>(pointId);
            container.PointId = pointId;
            container.OutputMode = ContainerOutputMode.ContainerPanel;
            container.HasOpenedOnce = false;
            container.ClearItems();
            container.SearchTimerIds.Clear();
            container.SearchStartTimes.Clear();
            container.SearchDurations.Clear();

            if (TryResolveDropCount(lootTable, dropCountMin, dropCountMax, out int dropCount))
            {
                ContainerRuntimeHelper.GenerateLoot(
                    point,
                    null,
                    ContainerOutputMode.ContainerPanel.ToString(),
                    lootTable,
                    dropCount,
                    0f,
                    allowRepeat);
            }
            else
            {
                container.LootGenerated = true;
            }

            container.State = container.HasAnyItem() ? ContainerState.Closed : ContainerState.Empty;
            ECAPointStateHelper.SetState(point, container.State);
            ecaManager.AddECAPoint(pointId, pointUnitId);
            NotifyVisiblePlayersInRange(target);

            Log.Info(
                $"[MonsterCorpseLoot] created corpse box: target={target.Id}, targetConfig={target.ConfigId}, point={pointId}, pointUnit={pointUnitId}, boxConfig={resolvedLootBoxUnitConfigId}, state={container.State}, itemCount={container.ItemEntries.Count}");
            return true;
        }

        private static void ResolveSpawnArguments(
            Unit target,
            out int defaultLootBoxUnitConfigId,
            out int lootBoxUnitConfigId,
            out string lootTable,
            out int dropCountMin,
            out int dropCountMax,
            out bool allowRepeat)
        {
            MonsterCorpseLootConfig config = FindLootConfig(target);
            defaultLootBoxUnitConfigId = ExtractionInventoryConfig.GetDefaultMonsterCorpseLootBoxUnitConfigId();
            lootBoxUnitConfigId = config?.LootBoxUnitConfigId ?? 0;
            lootTable = config?.LootTable ?? string.Empty;
            dropCountMin = config?.DropCountMin ?? 0;
            dropCountMax = config?.DropCountMax ?? 0;
            allowRepeat = config?.AllowRepeat ?? true;
        }

        private static MonsterCorpseLootConfig FindLootConfig(Unit target)
        {
            if (target == null || target.IsDisposed || target.ConfigId <= 0)
            {
                return null;
            }

            string mapName = target.Scene()?.Name.GetSceneConfigName();
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return null;
            }

            MonsterCorpseLootConfigCategory category = MonsterCorpseLootConfigCategory.Instance;
            List<MonsterCorpseLootConfig> dataList = category?.DataList;
            if (dataList == null)
            {
                return null;
            }

            foreach (MonsterCorpseLootConfig config in dataList)
            {
                if (config == null || config.MonsterUnitConfigId != target.ConfigId)
                {
                    continue;
                }

                if (string.Equals(config.MapName, mapName, StringComparison.OrdinalIgnoreCase))
                {
                    return config;
                }
            }

            return null;
        }

        private static int ResolveLootBoxUnitConfigId(int configuredLootBoxUnitConfigId, int defaultLootBoxUnitConfigId)
        {
            int configId = ValidateLootBoxUnitConfigId(configuredLootBoxUnitConfigId);
            if (configId > 0)
            {
                return configId;
            }

            return ValidateLootBoxUnitConfigId(defaultLootBoxUnitConfigId);
        }

        private static int ValidateLootBoxUnitConfigId(int configId)
        {
            if (configId <= 0)
            {
                return 0;
            }

            UnitConfig config = UnitConfigCategory.Instance.GetOrDefault(configId);
            if (config == null)
            {
                Log.Warning($"[MonsterCorpseLoot] loot box config missing: configId={configId}");
                return 0;
            }

            if (config.UnitType != UnitType.Virtual)
            {
                Log.Warning($"[MonsterCorpseLoot] loot box config must be virtual: configId={configId}, unitType={config.UnitType}");
                return 0;
            }

            return configId;
        }

        private static bool TryResolveDropCount(string lootTable, int dropCountMin, int dropCountMax, out int dropCount)
        {
            dropCount = 0;
            if (string.IsNullOrWhiteSpace(lootTable))
            {
                return false;
            }

            int min = Math.Max(0, dropCountMin);
            int max = Math.Max(min, dropCountMax);
            if (max <= 0)
            {
                return false;
            }

            dropCount = min == max ? min : RandomGenerator.RandomNumber(min, max + 1);
            return dropCount > 0;
        }

        private static void ClearCombatNumeric(Unit pointUnit)
        {
            NumericComponent numeric = pointUnit?.NumericComponent;
            if (numeric == null)
            {
                return;
            }

            numeric.SetNoEvent(NumericType.HP, 0);
            numeric.SetNoEvent(NumericType.MaxHP, 0);
        }

        private static void NotifyVisiblePlayersInRange(Unit target)
        {
            foreach (AOIEntity viewerAoi in target.GetBeSeePlayers().Values)
            {
                Unit viewer = viewerAoi?.Unit;
                if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
                {
                    continue;
                }

                ECAHelper.CheckPlayerInRange(viewer);
            }
        }

        private static List<FlowParam> CreatePointParams(float interactRange)
        {
            return new List<FlowParam>
            {
                new FlowParam
                {
                    Key = ECAPointParamKey.InteractRange,
                    Value = interactRange.ToString(CultureInfo.InvariantCulture)
                }
            };
        }

        private static FlowGraphData CreateOpenContainerFlowGraph(int buttonTextId)
        {
            return new FlowGraphData
            {
                Nodes = new List<FlowNodeData>
                {
                    new FlowNodeData
                    {
                        NodeId = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerEnterRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 2,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.ShowInteractButton,
                        Params = new List<FlowParam>
                        {
                            new FlowParam
                            {
                                Key = "button_text_id",
                                Value = buttonTextId.ToString()
                            }
                        }
                    },
                    new FlowNodeData
                    {
                        NodeId = 3,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerLeaveRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 4,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.HideInteractButton
                    },
                    new FlowNodeData
                    {
                        NodeId = 5,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerInteract
                    },
                    new FlowNodeData
                    {
                        NodeId = 6,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.OpenContainerUI,
                        Params = new List<FlowParam>
                        {
                            new FlowParam
                            {
                                Key = global::ET.SearchPanelOpenConst.OpenContainerUiKeyParam,
                                Value = global::ET.SearchPanelOpenConst.SearchPanelUiKey
                            }
                        }
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new FlowConnectionData { FromNodeId = 1, ToNodeId = 2, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 3, ToNodeId = 4, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 5, ToNodeId = 6, Branch = "Out" }
                }
            };
        }
    }
}
