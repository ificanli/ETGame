using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public static class ContainerRuntimeHelper
    {
        private const string DefaultSearchTimerId = "container_search";
        private const string OutputModeKeyContainerPanel = "ContainerPanel";
        private const string OutputModeKeyGroundDrop = "GroundDrop";

        public static bool TryGetPoint(Scene scene, string pointId, out ECAPointComponent point)
        {
            point = null;
            if (scene == null || string.IsNullOrWhiteSpace(pointId))
            {
                return false;
            }

            ECAManagerComponent manager = scene.GetComponent<ECAManagerComponent>();
            if (manager == null)
            {
                return false;
            }

            point = manager.GetECAPoint(pointId);
            return point != null && !point.IsDisposed;
        }

        public static bool IsPlayerInRange(ECAPointComponent point, Unit player)
        {
            if (point == null || player == null)
            {
                return false;
            }

            if (point.PlayersInRange.Contains(player.Id))
            {
                return true;
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null || pointUnit.IsDisposed)
            {
                return false;
            }

            // 与 ECA 进圈检测统一使用水平距离，避免高度差导致“提示已进圈但交互仍失败”。
            float distance = ECAHelper.GetHorizontalDistance(player.Position, pointUnit.Position);
            float interactRange = point.InteractRange + ECAInteractionModifierHelper.GetInteractRangeBonus(player);
            if (interactRange < 0f)
            {
                interactRange = 0f;
            }

            return distance <= interactRange;
        }

        public static string BuildInteractRangeDebugInfo(ECAPointComponent point, Unit player)
        {
            if (point == null)
            {
                return "point=null";
            }

            if (player == null)
            {
                return $"point={point.PointId}, player=null";
            }

            Unit pointUnit = point.GetParent<Unit>();
            float3 pointPosition = pointUnit != null && !pointUnit.IsDisposed ? pointUnit.Position : float3.zero;
            float distance = pointUnit != null && !pointUnit.IsDisposed
                ? ECAHelper.GetHorizontalDistance(player.Position, pointPosition)
                : -1f;
            float rangeBonus = ECAInteractionModifierHelper.GetInteractRangeBonus(player);
            float interactRange = point.InteractRange + rangeBonus;
            if (interactRange < 0f)
            {
                interactRange = 0f;
            }

            bool cachedInRange = point.PlayersInRange.Contains(player.Id);
            return
                $"point={point.PointId}, pointType={point.PointType}, player={player.Id}, playerPos={player.Position}, pointPos={pointPosition}, horizontalDistance={distance:F3}, interactRange={interactRange:F3}, baseRange={point.InteractRange:F3}, rangeBonus={rangeBonus:F3}, cachedInRange={cachedInRange}, playersInRangeCount={point.PlayersInRange.Count}";
        }

        public static void SendInteractHint(ECAPointComponent point, Unit player, bool inRange, int buttonTextId = 0, bool canInteract = true)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            M2C_ECAInteractHint hint = M2C_ECAInteractHint.Create();
            hint.PointId = point.PointId;
            hint.InRange = inRange;
            hint.ButtonTextId = buttonTextId;
            hint.CanInteract = canInteract;
            MapMessageHelper.NoticeClient(player, hint, NoticeType.Self);
            Log.Info(
                $"[ECADebug][SendInteractHint] player={player.Id}, point={point.PointId}, inRange={inRange}, buttonTextId={buttonTextId}, canInteract={canInteract}");
        }

        public static void SendPointState(ECAPointComponent point, Unit player)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            M2C_ECAPointState message = M2C_ECAPointState.Create();
            message.PointId = point.PointId;
            message.State = point.CurrentState;
            MapMessageHelper.NoticeClient(player, message, NoticeType.Self);
        }

        public static void NotifyPointStateToPlayers(ECAPointComponent point)
        {
            if (point == null || point.IsDisposed)
            {
                return;
            }

            Scene scene = point.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            point.CleanupInvalidPlayersInRange(unitComponent);
            foreach (long playerId in point.PlayersInRange)
            {
                Unit player = unitComponent.Get(playerId);
                if (player == null || player.IsDisposed)
                {
                    continue;
                }

                SendPointState(point, player);
            }
        }

        public static void SendSearchState(ECAPointComponent point, Unit player, int state, long remainMs)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            M2C_ECASearchState msg = M2C_ECASearchState.Create();
            msg.PointId = point.PointId;
            msg.State = state;
            msg.RemainMs = remainMs;
            MapMessageHelper.NoticeClient(player, msg, NoticeType.Self);
        }

        public static void MarkSearchStarted(ECAPointComponent point, Unit player, string timerId, long durationMs)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            ContainerComponent container = point.GetParent<Unit>()?.GetComponent<ContainerComponent>();
            if (container == null)
            {
                return;
            }

            container.SetSearchSession(player.Id, timerId, durationMs);
            SendSearchState(point, player, ContainerSearchState.Searching, durationMs);
        }

        public static bool CancelSearch(ECAPointComponent point, Unit player, string timerId = null, bool notify = true, int interruptState = ContainerSearchState.Interrupted)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return false;
            }

            ContainerComponent container = point.GetParent<Unit>()?.GetComponent<ContainerComponent>();
            if (container == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(timerId))
            {
                if (!container.TryGetSearchTimerId(player.Id, out timerId))
                {
                    timerId = DefaultSearchTimerId;
                }
            }

            bool canceled = ECAFlowTimerHelper.CancelTimer(point, player, timerId);
            container.ClearSearchSession(player.Id);

            if (notify)
            {
                SendSearchState(point, player, interruptState, 0);
            }

            return canceled;
        }

        public static void OpenContainerUI(ECAPointComponent point, Unit player, string uiKey = null)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            ContainerComponent container = point.GetParent<Unit>()?.GetComponent<ContainerComponent>();
            if (container == null)
            {
                return;
            }

            container.ClearSearchSession(player.Id);
            SendSearchState(point, player, ContainerSearchState.Completed, 0);
            if (container.State == ContainerState.Closed)
            {
                container.State = container.HasAnyItem() ? ContainerState.Opened : ContainerState.Empty;
            }

            ECAPointStateHelper.SetState(point, container.State);
            NotifyPointStateToPlayers(point);

            M2C_ContainerOpen msg = M2C_ContainerOpen.Create();
            msg.PointId = point.PointId;
            msg.OutputMode = container.OutputMode;
            msg.IsFirstOpen = !container.HasOpenedOnce;
            msg.UiKey = uiKey;
            container.FillItemMessage(msg.Items);
            MapMessageHelper.NoticeClient(player, msg, NoticeType.Self);
            Log.Info($"[ECAContainer] open ui point={point.PointId}, player={player.Id}, uiKey={uiKey ?? "null"}");

            bool isFirstOpen = !container.HasOpenedOnce;
            if (isFirstOpen)
            {
                Scene scene = point.Scene();
                if (scene != null && !scene.IsDisposed)
                {
                    EventSystem.Instance.Publish(scene, new ContainerOpenedEvent
                    {
                        Point = point,
                        Player = player,
                        IsFirstOpen = true,
                    });
                }

                container.HasOpenedOnce = true;
            }
        }

        public static void GenerateLoot(ECAPointComponent point, Unit player, string outputModeRaw, string lootTable, int count, float radius, bool allowRepeat = true)
        {
            if (point == null)
            {
                return;
            }

            ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
            if (container == null)
            {
                return;
            }

            long playerId = player?.Id ?? 0;
            container.RecordSpawnItems(lootTable, count, radius, playerId);
            container.OutputMode = ParseOutputMode(outputModeRaw);

            if (container.LootGenerated)
            {
                return;
            }

            container.ClearItems();
            int resultRollMultiplierPermille = 1000;
            Scene scene = point.Scene();
            if (scene != null && !scene.IsDisposed)
            {
                ContainerLootBuildContext context = new ContainerLootBuildContext
                {
                    Point = point,
                    Player = player,
                    LootTable = lootTable,
                    Count = count,
                    AllowRepeat = allowRepeat,
                    ResultRollMultiplierPermille = 1000,
                };
                EventSystem.Instance.Publish(scene, new ContainerLootBuildEvent { Context = context });
                if (context.ResultRollMultiplierPermille > 0)
                {
                    resultRollMultiplierPermille = context.ResultRollMultiplierPermille;
                }
            }

            Dictionary<int, int> aggregated = BuildAggregatedLoot(lootTable, count, allowRepeat, resultRollMultiplierPermille);
            int slotIndex = 0;
            foreach (KeyValuePair<int, int> kv in aggregated)
            {
                container.SetItem(slotIndex++, kv.Key, kv.Value);
            }

            container.LootGenerated = true;
            Log.Info($"[ECAContainer] loot generated point={point.PointId}, mode={container.OutputMode}, itemCount={container.ItemEntries.Count}, player={playerId}, allowRepeat={allowRepeat}");
        }

        public static async ETTask<int> TakeItem(Unit player, ECAPointComponent point, int slotIndex)
        {
            if (player == null || player.IsDisposed || point == null || point.IsDisposed)
            {
                return ErrorCode.ERR_Cancel;
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null)
            {
                return ErrorCode.ERR_ECAPointNotFound;
            }

            EntityRef<Unit> playerRef = player;
            EntityRef<ECAPointComponent> pointRef = point;
            using (await player.Root().CoroutineLockComponent.Wait(CoroutineLockType.ECAContainer, pointUnit.Id))
            {
                player = playerRef;
                point = pointRef;
                if (player == null || player.IsDisposed || point == null || point.IsDisposed)
                {
                    return ErrorCode.ERR_Cancel;
                }

                ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
                if (container == null || (container.State != ContainerState.Opened && container.State != ContainerState.Empty))
                {
                    return ErrorCode.ERR_ECAContainerNotOpened;
                }

                if (!container.TryGetItem(slotIndex, out ContainerItemEntry item))
                {
                    return ErrorCode.ERR_ECAContainerItemNotFound;
                }

                int takeError = TryAutoStoreContainerItem(
                    player,
                    point,
                    slotIndex,
                    item,
                    player.GetComponent<ItemComponent>(),
                    RuntimeSecureInventoryHelper.Get(player));
                if (takeError != ErrorCode.ERR_Success)
                {
                    return takeError;
                }

                container.RemoveItem(slotIndex);
                UpdateContainerState(point, container);
                if (point != null && !point.IsDisposed)
                {
                    NotifyContainerUpdateToInRangePlayers(point, container);
                }

                return ErrorCode.ERR_Success;
            }
        }

        public static async ETTask<int> TakeAll(Unit player, ECAPointComponent point)
        {
            if (player == null || player.IsDisposed || point == null || point.IsDisposed)
            {
                return ErrorCode.ERR_Cancel;
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null)
            {
                return ErrorCode.ERR_ECAPointNotFound;
            }

            EntityRef<Unit> playerRef = player;
            EntityRef<ECAPointComponent> pointRef = point;
            using (await player.Root().CoroutineLockComponent.Wait(CoroutineLockType.ECAContainer, pointUnit.Id))
            {
                player = playerRef;
                point = pointRef;
                if (player == null || player.IsDisposed || point == null || point.IsDisposed)
                {
                    return ErrorCode.ERR_Cancel;
                }

                ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
                if (container == null || (container.State != ContainerState.Opened && container.State != ContainerState.Empty))
                {
                    return ErrorCode.ERR_ECAContainerNotOpened;
                }

                ItemComponent itemComponent = player.GetComponent<ItemComponent>();
                RuntimeSecureInventoryComponent secureInventory = RuntimeSecureInventoryHelper.Get(player);
                bool hasTakeFailure = false;
                List<int> slots = new List<int>(container.ItemEntries.Keys);
                slots.Sort();
                foreach (int slotIndex in slots)
                {
                    if (!container.TryGetItem(slotIndex, out ContainerItemEntry item))
                    {
                        continue;
                    }

                    int takeError = TryAutoStoreContainerItem(player, point, slotIndex, item, itemComponent, secureInventory);
                    if (takeError == ErrorCode.ERR_Success)
                    {
                        container.RemoveItem(slotIndex);
                        continue;
                    }

                    hasTakeFailure = true;
                }

                UpdateContainerState(point, container);
                if (point != null && !point.IsDisposed)
                {
                    NotifyContainerUpdateToInRangePlayers(point, container);
                }

                return hasTakeFailure ? ErrorCode.ERR_ECAContainerBagFull : ErrorCode.ERR_Success;
            }
        }

        public static async ETTask<int> MoveItem(
            Unit player,
            ECAPointComponent point,
            int sourceAreaType,
            int sourceSlot,
            long sourceItemId,
            int targetAreaType,
            int targetSlot)
        {
            if (player == null || player.IsDisposed)
            {
                return ErrorCode.ERR_Cancel;
            }

            if (sourceSlot < 0 || targetSlot < 0)
            {
                return ErrorCode.ERR_ItemSlotInvalid;
            }

            ContainerItemAreaType sourceArea = (ContainerItemAreaType)sourceAreaType;
            ContainerItemAreaType targetArea = (ContainerItemAreaType)targetAreaType;
            if (!IsValidAreaType(sourceArea) || !IsValidAreaType(targetArea))
            {
                return ErrorCode.ERR_ItemSlotInvalid;
            }

            if (sourceArea == targetArea && sourceSlot == targetSlot)
            {
                return ErrorCode.ERR_Success;
            }

            bool requiresContainer = sourceArea == ContainerItemAreaType.Container || targetArea == ContainerItemAreaType.Container;
            Unit pointUnit = point?.GetParent<Unit>();
            if (requiresContainer && pointUnit == null)
            {
                return ErrorCode.ERR_ECAPointNotFound;
            }

            EntityRef<Unit> playerRef = player;
            EntityRef<ECAPointComponent> pointRef = point;
            long lockId = pointUnit?.Id ?? player.Id;
            using (await player.Root().CoroutineLockComponent.Wait(CoroutineLockType.ECAContainer, lockId))
            {
                player = playerRef;
                point = pointRef;
                if (player == null || player.IsDisposed)
                {
                    return ErrorCode.ERR_Cancel;
                }

                ContainerComponent container = null;
                if (requiresContainer)
                {
                    if (point == null || point.IsDisposed)
                    {
                        return ErrorCode.ERR_Cancel;
                    }

                    container = ContainerComponentSystem.GetOrAdd(point);
                    if (container == null || (container.State != ContainerState.Opened && container.State != ContainerState.Empty))
                    {
                        return ErrorCode.ERR_ECAContainerNotOpened;
                    }
                }

                ItemComponent itemComponent = player.GetComponent<ItemComponent>();
                RuntimeSecureInventoryComponent secureInventory = RuntimeSecureInventoryHelper.Get(player);

                if (targetArea == ContainerItemAreaType.Bag)
                {
                    if (itemComponent == null)
                    {
                        return ErrorCode.ERR_ECAContainerBagFull;
                    }

                    if (targetSlot >= itemComponent.Capacity)
                    {
                        return ErrorCode.ERR_ItemSlotInvalid;
                    }
                }

                switch (sourceArea)
                {
                    case ContainerItemAreaType.Container:
                    {
                        if (!container.TryGetItem(sourceSlot, out ContainerItemEntry sourceContainerItem))
                        {
                            return ErrorCode.ERR_ECAContainerItemNotFound;
                        }

                        return targetArea switch
                        {
                            ContainerItemAreaType.Container => MoveContainerToContainer(point, container, sourceSlot, sourceContainerItem, targetSlot),
                            ContainerItemAreaType.Bag => itemComponent == null
                                ? ErrorCode.ERR_ECAContainerBagFull
                                : MoveContainerToBag(point, container, itemComponent, sourceSlot, sourceContainerItem, targetSlot),
                            ContainerItemAreaType.Secure => MoveContainerToSecure(player, point, container, secureInventory, sourceSlot, sourceContainerItem, targetSlot),
                            _ => ErrorCode.ERR_ItemSlotInvalid,
                        };
                    }
                    case ContainerItemAreaType.Bag:
                    {
                        if (itemComponent == null)
                        {
                            return ErrorCode.ERR_ItemNotFound;
                        }

                        Item sourceBagItem = itemComponent.GetItemById(sourceItemId);
                        if (sourceBagItem == null || sourceBagItem.IsDisposed || sourceBagItem.SlotIndex != sourceSlot)
                        {
                            return ErrorCode.ERR_ItemNotFound;
                        }

                        return targetArea switch
                        {
                            ContainerItemAreaType.Bag => ItemHelper.MoveItem(itemComponent, sourceItemId, targetSlot),
                            ContainerItemAreaType.Container => MoveBagToContainer(point, container, itemComponent, sourceBagItem, targetSlot),
                            ContainerItemAreaType.Secure => MoveBagToSecure(player, itemComponent, secureInventory, sourceBagItem, targetSlot),
                            _ => ErrorCode.ERR_ItemSlotInvalid,
                        };
                    }
                    case ContainerItemAreaType.Secure:
                    {
                        if (!TryGetSecureItem(secureInventory, sourceSlot, out int secureSourceIndex, out LoadoutGridItemInfo sourceSecureItem))
                        {
                            return ErrorCode.ERR_ItemNotFound;
                        }

                        return targetArea switch
                        {
                            ContainerItemAreaType.Secure => RuntimeSecureInventoryHelper.MoveItem(player, sourceSlot, targetSlot),
                            ContainerItemAreaType.Container => MoveSecureToContainer(player, point, container, secureInventory, secureSourceIndex, sourceSecureItem, targetSlot),
                            ContainerItemAreaType.Bag => itemComponent == null
                                ? ErrorCode.ERR_ECAContainerBagFull
                                : MoveSecureToBag(player, itemComponent, secureInventory, secureSourceIndex, sourceSecureItem, targetSlot),
                            _ => ErrorCode.ERR_ItemSlotInvalid,
                        };
                    }
                    default:
                        return ErrorCode.ERR_ItemSlotInvalid;
                }
            }
        }

        public static void CloseContainer(ECAPointComponent point, Unit player)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            ContainerComponent container = point.GetParent<Unit>()?.GetComponent<ContainerComponent>();
            if (container == null)
            {
                return;
            }

            if (container.IsSearching(player.Id))
            {
                CancelSearch(point, player, notify: true, interruptState: ContainerSearchState.Interrupted);
            }
        }

        private static void NotifyContainerUpdateToInRangePlayers(ECAPointComponent point, ContainerComponent container)
        {
            if (point == null || point.IsDisposed || container == null)
            {
                return;
            }

            Scene scene = point.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            point.CleanupInvalidPlayersInRange(unitComponent);
            foreach (long playerId in point.PlayersInRange)
            {
                Unit player = unitComponent.Get(playerId);
                if (player == null || player.IsDisposed)
                {
                    continue;
                }

                M2C_ContainerUpdate update = M2C_ContainerUpdate.Create();
                update.PointId = point.PointId;
                container.FillItemMessage(update.Items);
                MapMessageHelper.NoticeClient(player, update, NoticeType.Self);
            }
        }

        private static int MoveContainerToContainer(
            ECAPointComponent point,
            ContainerComponent container,
            int sourceSlot,
            ContainerItemEntry sourceItem,
            int targetSlot)
        {
            if (!container.TryGetItem(targetSlot, out ContainerItemEntry targetItem))
            {
                container.SetItem(targetSlot, sourceItem.ConfigId, sourceItem.Count, sourceItem.ItemUid);
                container.ItemEntries.Remove(sourceSlot);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.Get(sourceItem.ConfigId);
            if (sourceItem.ConfigId != targetItem.ConfigId || itemConfig == null || itemConfig.MaxStack <= 1)
            {
                container.SetItem(sourceSlot, targetItem.ConfigId, targetItem.Count, targetItem.ItemUid);
                container.SetItem(targetSlot, sourceItem.ConfigId, sourceItem.Count, sourceItem.ItemUid);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return itemConfig == null && sourceItem.ConfigId == targetItem.ConfigId ? ErrorCode.ERR_ItemNotFound : ErrorCode.ERR_Success;
            }

            int stackCount = Math.Min(itemConfig.MaxStack - targetItem.Count, sourceItem.Count);
            if (stackCount <= 0)
            {
                container.SetItem(sourceSlot, targetItem.ConfigId, targetItem.Count, targetItem.ItemUid);
                container.SetItem(targetSlot, sourceItem.ConfigId, sourceItem.Count, sourceItem.ItemUid);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            targetItem.Count += stackCount;
            sourceItem.Count -= stackCount;
            container.SetItem(targetSlot, targetItem.ConfigId, targetItem.Count, targetItem.ItemUid);
            if (sourceItem.Count > 0)
            {
                container.SetItem(sourceSlot, sourceItem.ConfigId, sourceItem.Count, sourceItem.ItemUid);
            }
            else
            {
                container.ItemEntries.Remove(sourceSlot);
            }

            UpdateContainerState(point, container);
            NotifyContainerUpdateToInRangePlayers(point, container);
            return ErrorCode.ERR_Success;
        }

        private static int MoveContainerToBag(
            ECAPointComponent point,
            ContainerComponent container,
            ItemComponent itemComponent,
            int sourceSlot,
            ContainerItemEntry sourceItem,
            int targetSlot)
        {
            if (!TryResolveBagItemPlacement(sourceItem.ConfigId, out int resolvedSourceConfigId, out ItemConfig sourceItemConfig, out int sourceGridWidth, out int sourceGridHeight))
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            Item targetBagItem = itemComponent.GetItemBySlot(targetSlot);
            if (targetBagItem == null)
            {
                if (!itemComponent.CanPlaceAtAnchorSlot(targetSlot, sourceGridWidth, sourceGridHeight))
                {
                    return ErrorCode.ERR_ECAContainerBagFull;
                }

                CreateBagItem(itemComponent, targetSlot, resolvedSourceConfigId, sourceItem.Count, sourceGridWidth, sourceGridHeight);
                container.ItemEntries.Remove(sourceSlot);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            if (LegacyItemConfigIdHelper.MatchesConfigId(resolvedSourceConfigId, targetBagItem.ConfigId) && sourceItemConfig.MaxStack > 1)
            {
                if (targetBagItem.ConfigId != resolvedSourceConfigId)
                {
                    targetBagItem.ConfigId = resolvedSourceConfigId;
                }

                int stackCount = Math.Min(sourceItemConfig.MaxStack - targetBagItem.Count, sourceItem.Count);
                if (stackCount > 0)
                {
                    targetBagItem.AddCount(stackCount);
                    ItemHelper.NotifyItemUpdate(itemComponent, targetBagItem);

                    sourceItem.Count -= stackCount;
                    if (sourceItem.Count > 0)
                    {
                        container.SetItem(sourceSlot, sourceItem.ConfigId, sourceItem.Count, sourceItem.ItemUid);
                    }
                    else
                    {
                        container.ItemEntries.Remove(sourceSlot);
                    }

                    UpdateContainerState(point, container);
                    NotifyContainerUpdateToInRangePlayers(point, container);
                    return ErrorCode.ERR_Success;
                }
            }

            if (!itemComponent.CanPlaceAtAnchorSlot(targetSlot, sourceGridWidth, sourceGridHeight, targetBagItem.Id))
            {
                return ErrorCode.ERR_ECAContainerBagFull;
            }

            int resolvedTargetBagConfigId = NormalizeBagItemConfigId(targetBagItem);
            container.SetItem(sourceSlot, resolvedTargetBagConfigId, targetBagItem.Count, targetBagItem.Id);
            RemoveBagItem(itemComponent, targetBagItem);
            CreateBagItem(itemComponent, targetSlot, resolvedSourceConfigId, sourceItem.Count, sourceGridWidth, sourceGridHeight);
            UpdateContainerState(point, container);
            NotifyContainerUpdateToInRangePlayers(point, container);
            return ErrorCode.ERR_Success;
        }

        private static int MoveBagToContainer(
            ECAPointComponent point,
            ContainerComponent container,
            ItemComponent itemComponent,
            Item sourceBagItem,
            int targetSlot)
        {
            int sourceSlot = sourceBagItem.SlotIndex;
            int resolvedSourceConfigId = NormalizeBagItemConfigId(sourceBagItem);
            if (!container.TryGetItem(targetSlot, out ContainerItemEntry targetItem))
            {
                container.SetItem(targetSlot, resolvedSourceConfigId, sourceBagItem.Count, sourceBagItem.Id);
                RemoveBagItem(itemComponent, sourceBagItem);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            if (!TryResolveBagItemPlacement(targetItem.ConfigId, out int resolvedTargetConfigId, out ItemConfig targetItemConfig, out int targetGridWidth, out int targetGridHeight))
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            if (LegacyItemConfigIdHelper.MatchesConfigId(resolvedSourceConfigId, resolvedTargetConfigId) && targetItemConfig.MaxStack > 1)
            {
                int stackCount = Math.Min(targetItemConfig.MaxStack - targetItem.Count, sourceBagItem.Count);
                if (stackCount > 0)
                {
                    targetItem.Count += stackCount;
                    container.SetItem(targetSlot, resolvedTargetConfigId, targetItem.Count, targetItem.ItemUid);

                    sourceBagItem.ReduceCount(stackCount);
                    if (sourceBagItem.Count > 0)
                    {
                        ItemHelper.NotifyItemUpdate(itemComponent, sourceBagItem);
                    }
                    else
                    {
                        RemoveBagItem(itemComponent, sourceBagItem);
                    }

                    UpdateContainerState(point, container);
                    NotifyContainerUpdateToInRangePlayers(point, container);
                    return ErrorCode.ERR_Success;
                }
            }

            if (!itemComponent.CanPlaceAtAnchorSlot(sourceSlot, targetGridWidth, targetGridHeight, sourceBagItem.Id))
            {
                return ErrorCode.ERR_ECAContainerBagFull;
            }

            container.SetItem(targetSlot, resolvedSourceConfigId, sourceBagItem.Count, sourceBagItem.Id);
            RemoveBagItem(itemComponent, sourceBagItem);
            CreateBagItem(itemComponent, sourceSlot, resolvedTargetConfigId, targetItem.Count, targetGridWidth, targetGridHeight);
            UpdateContainerState(point, container);
            NotifyContainerUpdateToInRangePlayers(point, container);
            return ErrorCode.ERR_Success;
        }

        private static int MoveContainerToSecure(
            Unit player,
            ECAPointComponent point,
            ContainerComponent container,
            RuntimeSecureInventoryComponent secureInventory,
            int sourceSlot,
            ContainerItemEntry sourceItem,
            int targetSlot)
        {
            if (!HasSecureInventory(secureInventory))
            {
                return ErrorCode.ERR_ECAContainerBagFull;
            }

            if (!TryResolveBagItemPlacement(sourceItem.ConfigId, out int resolvedSourceConfigId, out ItemConfig sourceItemConfig, out int sourceGridWidth, out int sourceGridHeight))
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            if (!TryGetSecureItem(secureInventory, targetSlot, out int targetIndex, out LoadoutGridItemInfo targetSecureItem))
            {
                if (!RuntimeSecureInventoryHelper.CanPlaceAtAnchorSlot(secureInventory, targetSlot, sourceGridWidth, sourceGridHeight))
                {
                    return ErrorCode.ERR_ECAContainerBagFull;
                }

                AddSecureItem(secureInventory, resolvedSourceConfigId, sourceItem.Count, targetSlot, sourceGridWidth, sourceGridHeight);
                container.ItemEntries.Remove(sourceSlot);
                RuntimeSecureInventoryHelper.NotifyChanged(player);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            if (LegacyItemConfigIdHelper.MatchesConfigId(resolvedSourceConfigId, targetSecureItem.ConfigId) && sourceItemConfig.MaxStack > 1)
            {
                int stackCount = Math.Min(sourceItemConfig.MaxStack - targetSecureItem.Count, sourceItem.Count);
                if (stackCount > 0)
                {
                    targetSecureItem.ConfigId = resolvedSourceConfigId;
                    targetSecureItem.Count += stackCount;
                    secureInventory.Items[targetIndex] = targetSecureItem;

                    sourceItem.Count -= stackCount;
                    if (sourceItem.Count > 0)
                    {
                        container.SetItem(sourceSlot, resolvedSourceConfigId, sourceItem.Count, sourceItem.ItemUid);
                    }
                    else
                    {
                        container.ItemEntries.Remove(sourceSlot);
                    }

                    RuntimeSecureInventoryHelper.NotifyChanged(player);
                    UpdateContainerState(point, container);
                    NotifyContainerUpdateToInRangePlayers(point, container);
                    return ErrorCode.ERR_Success;
                }
            }

            if (!RuntimeSecureInventoryHelper.CanPlaceAtAnchorSlot(secureInventory, targetSlot, sourceGridWidth, sourceGridHeight, targetIndex))
            {
                return ErrorCode.ERR_ECAContainerBagFull;
            }

            container.SetItem(sourceSlot, targetSecureItem.ConfigId, targetSecureItem.Count, ContainerComponentSystem.GenerateContainerItemUid());
            ReplaceSecureItem(secureInventory, targetIndex, resolvedSourceConfigId, sourceItem.Count, targetSlot, sourceGridWidth, sourceGridHeight);
            RuntimeSecureInventoryHelper.NotifyChanged(player);
            UpdateContainerState(point, container);
            NotifyContainerUpdateToInRangePlayers(point, container);
            return ErrorCode.ERR_Success;
        }

        private static int MoveBagToSecure(
            Unit player,
            ItemComponent itemComponent,
            RuntimeSecureInventoryComponent secureInventory,
            Item sourceBagItem,
            int targetSlot)
        {
            if (!HasSecureInventory(secureInventory))
            {
                return ErrorCode.ERR_ECAContainerBagFull;
            }

            int sourceSlot = sourceBagItem.SlotIndex;
            int resolvedSourceConfigId = NormalizeBagItemConfigId(sourceBagItem);
            int sourceGridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(sourceBagItem.GridWidth);
            int sourceGridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(sourceBagItem.GridHeight);

            if (!TryGetSecureItem(secureInventory, targetSlot, out int targetIndex, out LoadoutGridItemInfo targetSecureItem))
            {
                if (!RuntimeSecureInventoryHelper.CanPlaceAtAnchorSlot(secureInventory, targetSlot, sourceGridWidth, sourceGridHeight))
                {
                    return ErrorCode.ERR_ECAContainerBagFull;
                }

                AddSecureItem(secureInventory, resolvedSourceConfigId, sourceBagItem.Count, targetSlot, sourceGridWidth, sourceGridHeight);
                RemoveBagItem(itemComponent, sourceBagItem);
                RuntimeSecureInventoryHelper.NotifyChanged(player);
                return ErrorCode.ERR_Success;
            }

            if (!TryResolveBagItemPlacement(targetSecureItem.ConfigId, out int resolvedTargetSecureConfigId, out ItemConfig targetItemConfig, out int targetGridWidth, out int targetGridHeight))
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            if (LegacyItemConfigIdHelper.MatchesConfigId(resolvedSourceConfigId, resolvedTargetSecureConfigId) && targetItemConfig.MaxStack > 1)
            {
                int stackCount = Math.Min(targetItemConfig.MaxStack - targetSecureItem.Count, sourceBagItem.Count);
                if (stackCount > 0)
                {
                    targetSecureItem.ConfigId = resolvedTargetSecureConfigId;
                    targetSecureItem.Count += stackCount;
                    secureInventory.Items[targetIndex] = targetSecureItem;

                    sourceBagItem.ReduceCount(stackCount);
                    if (sourceBagItem.Count > 0)
                    {
                        ItemHelper.NotifyItemUpdate(itemComponent, sourceBagItem);
                    }
                    else
                    {
                        RemoveBagItem(itemComponent, sourceBagItem);
                    }

                    RuntimeSecureInventoryHelper.NotifyChanged(player);
                    return ErrorCode.ERR_Success;
                }
            }

            if (!itemComponent.CanPlaceAtAnchorSlot(sourceSlot, targetGridWidth, targetGridHeight, sourceBagItem.Id))
            {
                return ErrorCode.ERR_ECAContainerBagFull;
            }

            ReplaceSecureItem(secureInventory, targetIndex, resolvedSourceConfigId, sourceBagItem.Count, targetSlot, sourceGridWidth, sourceGridHeight);
            RemoveBagItem(itemComponent, sourceBagItem);
            CreateBagItem(itemComponent, sourceSlot, resolvedTargetSecureConfigId, targetSecureItem.Count, targetGridWidth, targetGridHeight);
            RuntimeSecureInventoryHelper.NotifyChanged(player);
            return ErrorCode.ERR_Success;
        }

        private static int MoveSecureToContainer(
            Unit player,
            ECAPointComponent point,
            ContainerComponent container,
            RuntimeSecureInventoryComponent secureInventory,
            int sourceIndex,
            LoadoutGridItemInfo sourceSecureItem,
            int targetSlot)
        {
            int sourceAnchorSlot = sourceSecureItem.AnchorSlotIndex;
            if (!container.TryGetItem(targetSlot, out ContainerItemEntry targetItem))
            {
                container.SetItem(targetSlot, sourceSecureItem.ConfigId, sourceSecureItem.Count, ContainerComponentSystem.GenerateContainerItemUid());
                RemoveSecureItem(secureInventory, sourceIndex);
                RuntimeSecureInventoryHelper.NotifyChanged(player);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            if (!TryResolveBagItemPlacement(targetItem.ConfigId, out int resolvedTargetConfigId, out ItemConfig targetItemConfig, out int targetGridWidth, out int targetGridHeight))
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            if (LegacyItemConfigIdHelper.MatchesConfigId(sourceSecureItem.ConfigId, resolvedTargetConfigId) && targetItemConfig.MaxStack > 1)
            {
                int stackCount = Math.Min(targetItemConfig.MaxStack - targetItem.Count, sourceSecureItem.Count);
                if (stackCount > 0)
                {
                    targetItem.Count += stackCount;
                    container.SetItem(targetSlot, resolvedTargetConfigId, targetItem.Count, targetItem.ItemUid);

                    sourceSecureItem.Count -= stackCount;
                    if (sourceSecureItem.Count > 0)
                    {
                        sourceSecureItem.ConfigId = resolvedTargetConfigId;
                        secureInventory.Items[sourceIndex] = sourceSecureItem;
                    }
                    else
                    {
                        RemoveSecureItem(secureInventory, sourceIndex);
                    }

                    RuntimeSecureInventoryHelper.NotifyChanged(player);
                    UpdateContainerState(point, container);
                    NotifyContainerUpdateToInRangePlayers(point, container);
                    return ErrorCode.ERR_Success;
                }
            }

            if (!RuntimeSecureInventoryHelper.CanPlaceAtAnchorSlot(secureInventory, sourceAnchorSlot, targetGridWidth, targetGridHeight, sourceIndex))
            {
                return ErrorCode.ERR_ECAContainerBagFull;
            }

            container.SetItem(targetSlot, sourceSecureItem.ConfigId, sourceSecureItem.Count, ContainerComponentSystem.GenerateContainerItemUid());
            ReplaceSecureItem(secureInventory, sourceIndex, resolvedTargetConfigId, targetItem.Count, sourceAnchorSlot, targetGridWidth, targetGridHeight);
            RuntimeSecureInventoryHelper.NotifyChanged(player);
            UpdateContainerState(point, container);
            NotifyContainerUpdateToInRangePlayers(point, container);
            return ErrorCode.ERR_Success;
        }

        private static int MoveSecureToBag(
            Unit player,
            ItemComponent itemComponent,
            RuntimeSecureInventoryComponent secureInventory,
            int sourceIndex,
            LoadoutGridItemInfo sourceSecureItem,
            int targetSlot)
        {
            if (!TryResolveBagItemPlacement(sourceSecureItem.ConfigId, out int resolvedSourceConfigId, out ItemConfig sourceItemConfig, out int sourceGridWidth, out int sourceGridHeight))
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            Item targetBagItem = itemComponent.GetItemBySlot(targetSlot);
            if (targetBagItem == null)
            {
                if (!itemComponent.CanPlaceAtAnchorSlot(targetSlot, sourceGridWidth, sourceGridHeight))
                {
                    return ErrorCode.ERR_ECAContainerBagFull;
                }

                CreateBagItem(itemComponent, targetSlot, resolvedSourceConfigId, sourceSecureItem.Count, sourceGridWidth, sourceGridHeight);
                RemoveSecureItem(secureInventory, sourceIndex);
                RuntimeSecureInventoryHelper.NotifyChanged(player);
                return ErrorCode.ERR_Success;
            }

            if (LegacyItemConfigIdHelper.MatchesConfigId(resolvedSourceConfigId, targetBagItem.ConfigId) && sourceItemConfig.MaxStack > 1)
            {
                if (targetBagItem.ConfigId != resolvedSourceConfigId)
                {
                    targetBagItem.ConfigId = resolvedSourceConfigId;
                }

                int stackCount = Math.Min(sourceItemConfig.MaxStack - targetBagItem.Count, sourceSecureItem.Count);
                if (stackCount > 0)
                {
                    targetBagItem.AddCount(stackCount);
                    ItemHelper.NotifyItemUpdate(itemComponent, targetBagItem);

                    sourceSecureItem.Count -= stackCount;
                    if (sourceSecureItem.Count > 0)
                    {
                        sourceSecureItem.ConfigId = resolvedSourceConfigId;
                        secureInventory.Items[sourceIndex] = sourceSecureItem;
                    }
                    else
                    {
                        RemoveSecureItem(secureInventory, sourceIndex);
                    }

                    RuntimeSecureInventoryHelper.NotifyChanged(player);
                    return ErrorCode.ERR_Success;
                }
            }

            int sourceAnchorSlot = sourceSecureItem.AnchorSlotIndex;
            int targetGridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(targetBagItem.GridWidth);
            int targetGridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(targetBagItem.GridHeight);
            if (!itemComponent.CanPlaceAtAnchorSlot(targetSlot, sourceGridWidth, sourceGridHeight, targetBagItem.Id) ||
                !RuntimeSecureInventoryHelper.CanPlaceAtAnchorSlot(secureInventory, sourceAnchorSlot, targetGridWidth, targetGridHeight, sourceIndex))
            {
                return ErrorCode.ERR_ECAContainerBagFull;
            }

            int resolvedTargetBagConfigId = NormalizeBagItemConfigId(targetBagItem);
            ReplaceSecureItem(secureInventory, sourceIndex, resolvedTargetBagConfigId, targetBagItem.Count, sourceAnchorSlot, targetGridWidth, targetGridHeight);
            RemoveBagItem(itemComponent, targetBagItem);
            CreateBagItem(itemComponent, targetSlot, resolvedSourceConfigId, sourceSecureItem.Count, sourceGridWidth, sourceGridHeight);
            RuntimeSecureInventoryHelper.NotifyChanged(player);
            return ErrorCode.ERR_Success;
        }

        private static int TryAutoStoreContainerItem(
            Unit player,
            ECAPointComponent point,
            int slotIndex,
            ContainerItemEntry item,
            ItemComponent itemComponent,
            RuntimeSecureInventoryComponent secureInventory)
        {
            bool preferSecure = RuntimeSecureInventoryHelper.ShouldPreferSecure(item.ConfigId, item.Count);
            if (preferSecure && HasSecureInventory(secureInventory))
            {
                if (RuntimeSecureInventoryHelper.TryAddItem(secureInventory, item.ConfigId, item.Count, out string secureMessage))
                {
                    RuntimeSecureInventoryHelper.NotifyChanged(player);
                    return ErrorCode.ERR_Success;
                }

                Log.Warning(
                    $"[ECAContainer] auto take secure fallback: player={player.Id}, point={point?.PointId ?? "null"}, slot={slotIndex}, configId={item.ConfigId}, count={item.Count}, reason={secureMessage}");
            }

            if (TryAddItemToBag(itemComponent, item.ConfigId, item.Count, player, point, slotIndex))
            {
                return ErrorCode.ERR_Success;
            }

            return ErrorCode.ERR_ECAContainerBagFull;
        }

        private static bool TryAddItemToBag(
            ItemComponent itemComponent,
            int configId,
            int count,
            Unit player,
            ECAPointComponent point,
            int slotIndex)
        {
            if (itemComponent == null)
            {
                return false;
            }

            try
            {
                ItemHelper.AddItem(itemComponent, configId, count, ItemChangeReason.MonsterDrop);
                return true;
            }
            catch (Exception e)
            {
                Log.Warning(
                    $"[ECAContainer] take item failed by bag state: player={player?.Id ?? 0}, point={point?.PointId ?? "null"}, slot={slotIndex}, configId={configId}, count={count}, error={e.Message}");
                return false;
            }
        }

        private static bool IsValidAreaType(ContainerItemAreaType areaType)
        {
            return areaType == ContainerItemAreaType.Container ||
                   areaType == ContainerItemAreaType.Bag ||
                   areaType == ContainerItemAreaType.Secure;
        }

        private static bool HasSecureInventory(RuntimeSecureInventoryComponent secureInventory)
        {
            return secureInventory != null && secureInventory.Width > 0 && secureInventory.Height > 0;
        }

        private static bool TryGetSecureItem(
            RuntimeSecureInventoryComponent secureInventory,
            int anchorSlotIndex,
            out int itemIndex,
            out LoadoutGridItemInfo item)
        {
            if (!HasSecureInventory(secureInventory))
            {
                itemIndex = -1;
                item = default;
                return false;
            }

            return RuntimeSecureInventoryHelper.TryGetItem(secureInventory, anchorSlotIndex, out itemIndex, out item);
        }

        private static void AddSecureItem(
            RuntimeSecureInventoryComponent secureInventory,
            int configId,
            int count,
            int anchorSlotIndex,
            int gridWidth,
            int gridHeight)
        {
            secureInventory.Items.Add(new LoadoutGridItemInfo
            {
                ConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(configId),
                Count = count,
                AnchorSlotIndex = anchorSlotIndex,
                GridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(gridWidth),
                GridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(gridHeight),
            });
        }

        private static void ReplaceSecureItem(
            RuntimeSecureInventoryComponent secureInventory,
            int itemIndex,
            int configId,
            int count,
            int anchorSlotIndex,
            int gridWidth,
            int gridHeight)
        {
            secureInventory.Items[itemIndex] = new LoadoutGridItemInfo
            {
                ConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(configId),
                Count = count,
                AnchorSlotIndex = anchorSlotIndex,
                GridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(gridWidth),
                GridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(gridHeight),
            };
        }

        private static void RemoveSecureItem(RuntimeSecureInventoryComponent secureInventory, int itemIndex)
        {
            if (secureInventory == null || itemIndex < 0 || itemIndex >= secureInventory.Items.Count)
            {
                return;
            }

            secureInventory.Items.RemoveAt(itemIndex);
        }

        private static void RemoveBagItem(ItemComponent itemComponent, Item item)
        {
            if (itemComponent == null || item == null || item.IsDisposed)
            {
                return;
            }

            int slotIndex = item.SlotIndex;
            ItemHelper.NotifyItemRemove(itemComponent, item);
            if (slotIndex >= 0)
            {
                itemComponent.ClearSlot(slotIndex);
            }

            item.Dispose();
        }

        private static void CreateBagItem(
            ItemComponent itemComponent,
            int slotIndex,
            int configId,
            int count,
            int gridWidth,
            int gridHeight)
        {
            Item item = itemComponent.AddChild<Item>();
            item.ConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(configId);
            item.Count = count;
            item.GridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(gridWidth);
            item.GridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(gridHeight);
            itemComponent.SetSlotItem(slotIndex, item);
            ItemHelper.NotifyItemUpdate(itemComponent, item);
        }

        private static int NormalizeBagItemConfigId(Item item)
        {
            if (item == null || item.IsDisposed)
            {
                return 0;
            }

            int resolvedConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(item.ConfigId);
            if (item.ConfigId != resolvedConfigId)
            {
                item.ConfigId = resolvedConfigId;
            }

            return resolvedConfigId;
        }

        private static bool TryResolveBagItemPlacement(
            int configId,
            out int resolvedConfigId,
            out ItemConfig itemConfig,
            out int gridWidth,
            out int gridHeight)
        {
            resolvedConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(configId);
            itemConfig = ItemConfigCategory.Instance.GetOrDefault(resolvedConfigId);
            if (itemConfig == null)
            {
                gridWidth = LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
                gridHeight = LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;
                return false;
            }

            gridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(itemConfig.GridWidth);
            gridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(itemConfig.GridHeight);
            return true;
        }

        private static void UpdateContainerState(ECAPointComponent point, ContainerComponent container)
        {
            if (point == null || point.IsDisposed || container == null)
            {
                return;
            }

            if (!container.HasAnyItem())
            {
                container.State = ContainerState.Empty;
                if (container.OutputMode == ContainerOutputMode.GroundDrop)
                {
                    DisposeGroundDropPoint(point);
                    return;
                }
            }
            else
            {
                container.State = ContainerState.Opened;
            }

            ECAPointStateHelper.SetState(point, container.State);
            NotifyPointStateToPlayers(point);
        }

        private static void DisposeGroundDropPoint(ECAPointComponent point)
        {
            if (point == null || point.IsDisposed)
            {
                return;
            }

            Scene scene = point.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent != null)
            {
                point.CleanupInvalidPlayersInRange(unitComponent);
                foreach (long playerId in point.PlayersInRange)
                {
                    Unit player = unitComponent.Get(playerId);
                    if (player == null || player.IsDisposed)
                    {
                        continue;
                    }

                    SendInteractHint(point, player, false);
                }
            }

            Unit pointUnit = point.GetParent<Unit>();
            Log.Info($"[ECAContainer] dispose empty ground drop: point={point.PointId}, unitId={pointUnit?.Id ?? 0}");
            pointUnit?.Dispose();
        }

        private static int ParseOutputMode(string outputModeRaw)
        {
            if (string.IsNullOrWhiteSpace(outputModeRaw))
            {
                return ContainerOutputMode.ContainerPanel;
            }

            if (int.TryParse(outputModeRaw, out int outputMode))
            {
                return outputMode == ContainerOutputMode.GroundDrop ? ContainerOutputMode.GroundDrop : ContainerOutputMode.ContainerPanel;
            }

            if (string.Equals(outputModeRaw, OutputModeKeyGroundDrop, StringComparison.OrdinalIgnoreCase))
            {
                return ContainerOutputMode.GroundDrop;
            }

            if (string.Equals(outputModeRaw, OutputModeKeyContainerPanel, StringComparison.OrdinalIgnoreCase))
            {
                return ContainerOutputMode.ContainerPanel;
            }

            return ContainerOutputMode.ContainerPanel;
        }

        private static Dictionary<int, int> BuildAggregatedLoot(string lootTable, int count, bool allowRepeat, int resultRollMultiplierPermille)
        {
            Dictionary<int, int> best = BuildAggregatedLootOnce(lootTable, count, allowRepeat);
            if (resultRollMultiplierPermille <= 1000)
            {
                return best;
            }

            int extraRollCount = resultRollMultiplierPermille / 1000 - 1;
            int fractionalPermille = resultRollMultiplierPermille % 1000;
            if (fractionalPermille > 0 && RandomGenerator.RandomNumber(0, 1000) < fractionalPermille)
            {
                extraRollCount += 1;
            }

            for (int i = 0; i < extraRollCount; ++i)
            {
                Dictionary<int, int> candidate = BuildAggregatedLootOnce(lootTable, count, allowRepeat);
                if (IsLootCandidateBetter(candidate, best))
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static Dictionary<int, int> BuildAggregatedLootOnce(string lootTable, int count, bool allowRepeat)
        {
            Dictionary<int, int> aggregated = new Dictionary<int, int>();
            List<ContainerItemEntry> pool = ParseLootPool(lootTable);
            if (pool.Count == 0)
            {
                return aggregated;
            }

            int rollCount = count > 0 ? count : pool.Count;
            if (allowRepeat)
            {
                for (int i = 0; i < rollCount; ++i)
                {
                    int index = RandomGenerator.RandomNumber(0, pool.Count);
                    ContainerItemEntry candidate = pool[index];
                    if (!aggregated.TryAdd(candidate.ConfigId, candidate.Count))
                    {
                        aggregated[candidate.ConfigId] += candidate.Count;
                    }
                }

                return aggregated;
            }

            int uniqueRollCount = Math.Min(rollCount, pool.Count);
            for (int i = 0; i < uniqueRollCount; ++i)
            {
                int index = RandomGenerator.RandomNumber(0, pool.Count);
                ContainerItemEntry candidate = pool[index];
                pool.RemoveAt(index);
                if (!aggregated.TryAdd(candidate.ConfigId, candidate.Count))
                {
                    aggregated[candidate.ConfigId] += candidate.Count;
                }
            }

            return aggregated;
        }

        private static bool IsLootCandidateBetter(Dictionary<int, int> candidate, Dictionary<int, int> currentBest)
        {
            return EvaluateLootScore(candidate) > EvaluateLootScore(currentBest);
        }

        private static long EvaluateLootScore(Dictionary<int, int> aggregated)
        {
            if (aggregated == null || aggregated.Count == 0)
            {
                return 0;
            }

            int maxQuality = 0;
            long totalQuality = 0;
            long totalCount = 0;
            foreach (KeyValuePair<int, int> kv in aggregated)
            {
                ItemConfig itemConfig = ItemConfigCategory.Instance.Get(kv.Key);
                int quality = itemConfig?.Quality ?? 0;
                if (quality > maxQuality)
                {
                    maxQuality = quality;
                }

                totalQuality += (long)quality * kv.Value;
                totalCount += kv.Value;
            }

            return ((long)maxQuality << 40) + (totalQuality << 20) + totalCount;
        }

        private static List<ContainerItemEntry> ParseLootPool(string lootTable)
        {
            List<ContainerItemEntry> pool = new List<ContainerItemEntry>();
            if (string.IsNullOrWhiteSpace(lootTable))
            {
                return pool;
            }

            string[] tokens = lootTable.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawToken in tokens)
            {
                string token = rawToken.Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (!TryParseLootToken(token, out int configId, out int count))
                {
                    continue;
                }

                pool.Add(new ContainerItemEntry
                {
                    ConfigId = configId,
                    Count = count
                });
            }

            return pool;
        }

        private static bool TryParseLootToken(string token, out int configId, out int count)
        {
            configId = 0;
            count = 1;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string[] parts;
            if (token.Contains('*'))
            {
                parts = token.Split('*');
            }
            else if (token.Contains(':'))
            {
                parts = token.Split(':');
            }
            else if (token.Contains('x') || token.Contains('X'))
            {
                parts = token.Split(new[] { 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                return int.TryParse(token, out configId) && configId > 0;
            }

            if (parts.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[0].Trim(), out configId) || configId <= 0)
            {
                return false;
            }

            if (!int.TryParse(parts[1].Trim(), out count) || count <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
