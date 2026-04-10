using System.Collections.Generic;
using System.Globalization;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 玩家死亡后生成尸体盒，并将非保险槽物品转入尸体盒。
    /// </summary>
    public static class PlayerCorpseLootHelper
    {
        private const string GroundDropPointPrefix = "ground_drop_";

        public static bool TryCreateCorpse(Unit target)
        {
            if (target == null || target.IsDisposed || target.UnitType != UnitType.Player)
            {
                return false;
            }

            if (target.GetComponent<PlayerCorpseLootComponent>() != null)
            {
                return false;
            }

            if (target.GetComponent<ECAPointComponent>() != null)
            {
                Log.Warning($"[CorpseLoot] skip create: target already has ECAPointComponent, unitId={target.Id}");
                return false;
            }

            if (target.GetComponent<ContainerComponent>() != null)
            {
                Log.Warning($"[CorpseLoot] skip create: target already has ContainerComponent, unitId={target.Id}");
                return false;
            }

            Scene scene = target.Scene();
            ItemComponent itemComponent = target.GetComponent<ItemComponent>();
            ECAManagerComponent ecaManager = scene?.GetComponent<ECAManagerComponent>();
            if (scene == null || itemComponent == null || ecaManager == null)
            {
                Log.Warning($"[CorpseLoot] skip create: missing scene/item/eca manager, unitId={target.Id}");
                return false;
            }

            string pointId = $"corpse_{target.Id}";
            float interactRange = ExtractionInventoryConfig.GetCorpseInteractRange();
            PlayerCorpseLootComponent corpseLoot = target.AddComponent<PlayerCorpseLootComponent>();
            corpseLoot.PointId = pointId;
            corpseLoot.CreatedTime = TimeInfo.Instance.ServerNow();

            ECAPointComponent point = target.AddComponent<ECAPointComponent, string, int, float>(pointId, ECAPointType.Container, interactRange);
            point.Params = CreatePointParams(interactRange);
            point.FlowGraph = CreateOpenContainerFlowGraph(ExtractionInventoryConfig.GetCorpseButtonTextId());

            ContainerComponent container = target.AddComponent<ContainerComponent, string>(pointId);
            container.PointId = pointId;
            container.OutputMode = ContainerOutputMode.ContainerPanel;
            container.LootGenerated = true;
            container.HasOpenedOnce = false;
            container.ClearItems();
            container.SearchTimerIds.Clear();
            container.SearchStartTimes.Clear();
            container.SearchDurations.Clear();

            List<(long ItemId, int ConfigId, int Count)> dropItems = CollectDropItems(itemComponent);
            int containerSlotIndex = 0;
            foreach ((long itemId, int configId, int count) in dropItems)
            {
                container.SetItem(containerSlotIndex++, configId, count);
                ItemHelper.RemoveItemById(itemComponent, itemId, ItemChangeReason.DropItem);
            }

            container.State = container.HasAnyItem() ? ContainerState.Closed : ContainerState.Empty;
            ecaManager.AddECAPoint(pointId, target.Id);

            Log.Info($"[CorpseLoot] created corpse box: unitId={target.Id}, pointId={pointId}, dropCount={dropItems.Count}, runtimeSecureKept=true");
            return true;
        }

        public static bool TryCreateGroundDrop(Unit player, int itemConfigId, int count, out string pointId, out long pointUnitId)
        {
            pointId = null;
            pointUnitId = 0;
            if (player == null || player.IsDisposed || itemConfigId <= 0 || count <= 0)
            {
                return false;
            }

            Scene scene = player.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            ECAManagerComponent ecaManager = scene?.GetComponent<ECAManagerComponent>();
            if (scene == null || unitComponent == null || ecaManager == null)
            {
                Log.Warning($"[GroundDrop] skip create: missing scene/unit/eca manager, player={player.Id}, itemConfigId={itemConfigId}, count={count}");
                return false;
            }

            pointUnitId = IdGenerater.Instance.GenerateId();
            pointId = $"{GroundDropPointPrefix}{player.Id}_{pointUnitId}";

            Unit pointUnit = unitComponent.AddChildWithId<Unit, int>(pointUnitId, 0);
            pointUnit.UnitType = UnitType.Virtual;
            pointUnit.Position = ResolveGroundDropPosition(player);
            pointUnit.Rotation = player.Rotation;

            float interactRange = ExtractionInventoryConfig.GetGroundDropInteractRange();
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>(pointId, ECAPointType.Container, interactRange);
            point.Params = CreatePointParams(interactRange);
            point.FlowGraph = CreateOpenContainerFlowGraph(ExtractionInventoryConfig.GetGroundDropButtonTextId());

            ContainerComponent container = pointUnit.AddComponent<ContainerComponent, string>(pointId);
            container.PointId = pointId;
            container.OutputMode = ContainerOutputMode.GroundDrop;
            container.LootGenerated = true;
            container.HasOpenedOnce = false;
            container.CreatorPlayerId = player.Id;
            container.CreateTime = TimeInfo.Instance.ServerNow();
            container.ClearItems();
            container.SearchTimerIds.Clear();
            container.SearchStartTimes.Clear();
            container.SearchDurations.Clear();
            container.SetItem(0, itemConfigId, count);
            container.State = ContainerState.Closed;

            ECAPointStateHelper.SetState(point, container.State);
            ecaManager.AddECAPoint(pointId, pointUnitId);
            ECAHelper.CheckPlayerInRange(player);

            Log.Info($"[GroundDrop] created point: point={pointId}, unitId={pointUnitId}, player={player.Id}, itemConfigId={itemConfigId}, count={count}, pos={pointUnit.Position}");
            return true;
        }

        private static List<(long ItemId, int ConfigId, int Count)> CollectDropItems(ItemComponent itemComponent)
        {
            List<(long ItemId, int ConfigId, int Count)> result = new();
            if (itemComponent == null)
            {
                return result;
            }

            for (int slotIndex = 0; slotIndex < itemComponent.Capacity; ++slotIndex)
            {
                Item item = itemComponent.GetItemBySlot(slotIndex);
                if (item == null || item.Count <= 0)
                {
                    continue;
                }

                result.Add((item.Id, item.ConfigId, item.Count));
            }

            return result;
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

        private static float3 ResolveGroundDropPosition(Unit player)
        {
            float3 fallback = player.Position;
            float3 forward = player.Forward;
            if (!math.all(math.isfinite(forward)))
            {
                return fallback;
            }

            forward.y = 0f;
            if (math.lengthsq(forward) <= 0.0001f)
            {
                return fallback;
            }

            float3 candidate = fallback + math.normalize(forward) * ExtractionInventoryConfig.GetGroundDropForwardDistance();
            if (TryProjectGroundDropPosition(player, candidate, out float3 projected))
            {
                return projected;
            }

            return fallback;
        }

        private static bool TryProjectGroundDropPosition(Unit player, float3 candidate, out float3 projected)
        {
            projected = candidate;
            PathfindingComponent pathfinding = player.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                return false;
            }

            float unitRadius = player.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            return pathfinding.TryRecastFindNearestPointForMovement(candidate, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPointForSpawn(candidate, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPoint(player.Position, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPointForSpawn(player.Position, unitRadius, out projected, out _);
        }
    }
}
