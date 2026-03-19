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

            float distance = math.distance(player.Position, pointUnit.Position);
            float interactRange = point.InteractRange + ECAInteractionModifierHelper.GetInteractRangeBonus(player);
            if (interactRange < 0f)
            {
                interactRange = 0f;
            }

            return distance <= interactRange;
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

                ItemComponent itemComponent = player.GetComponent<ItemComponent>();
                if (itemComponent == null)
                {
                    return ErrorCode.ERR_ECAContainerBagFull;
                }

                try
                {
                    ItemHelper.AddItem(itemComponent, item.ConfigId, item.Count, ItemChangeReason.MonsterDrop);
                }
                catch (Exception e)
                {
                    Log.Warning($"[ECAContainer] take item failed by bag state: player={player.Id}, point={point.PointId}, slot={slotIndex}, error={e.Message}");
                    return ErrorCode.ERR_ECAContainerBagFull;
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
                if (itemComponent == null)
                {
                    return ErrorCode.ERR_ECAContainerBagFull;
                }

                bool hasBagFailure = false;
                List<int> slots = new List<int>(container.ItemEntries.Keys);
                slots.Sort();
                foreach (int slotIndex in slots)
                {
                    if (!container.TryGetItem(slotIndex, out ContainerItemEntry item))
                    {
                        continue;
                    }

                    try
                    {
                        ItemHelper.AddItem(itemComponent, item.ConfigId, item.Count, ItemChangeReason.MonsterDrop);
                        container.RemoveItem(slotIndex);
                    }
                    catch (Exception e)
                    {
                        hasBagFailure = true;
                        Log.Warning($"[ECAContainer] take all partial by bag: player={player.Id}, point={point.PointId}, slot={slotIndex}, error={e.Message}");
                    }
                }

                UpdateContainerState(point, container);
                if (point != null && !point.IsDisposed)
                {
                    NotifyContainerUpdateToInRangePlayers(point, container);
                }
                return hasBagFailure ? ErrorCode.ERR_ECAContainerBagFull : ErrorCode.ERR_Success;
            }
        }

        public static async ETTask<int> MoveItem(
            Unit player,
            ECAPointComponent point,
            bool sourceIsBag,
            int sourceSlot,
            long sourceItemId,
            bool targetIsBag,
            int targetSlot)
        {
            if (player == null || player.IsDisposed || point == null || point.IsDisposed)
            {
                return ErrorCode.ERR_Cancel;
            }

            if (sourceSlot < 0 || targetSlot < 0)
            {
                return ErrorCode.ERR_ItemSlotInvalid;
            }

            if (sourceIsBag == targetIsBag && sourceSlot == targetSlot)
            {
                return ErrorCode.ERR_Success;
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
                if (itemComponent == null)
                {
                    return ErrorCode.ERR_ECAContainerBagFull;
                }

                if (targetIsBag && targetSlot >= itemComponent.Capacity)
                {
                    return ErrorCode.ERR_ItemSlotInvalid;
                }

                if (sourceIsBag)
                {
                    Item sourceBagItem = itemComponent.GetItemById(sourceItemId);
                    if (sourceBagItem == null || sourceBagItem.IsDisposed || sourceBagItem.SlotIndex != sourceSlot)
                    {
                        return ErrorCode.ERR_ItemNotFound;
                    }

                    if (targetIsBag)
                    {
                        return ItemHelper.MoveItem(itemComponent, sourceItemId, targetSlot);
                    }

                    return MoveBagToContainer(point, container, itemComponent, sourceBagItem, targetSlot);
                }

                if (!container.TryGetItem(sourceSlot, out ContainerItemEntry sourceContainerItem))
                {
                    return ErrorCode.ERR_ECAContainerItemNotFound;
                }

                if (targetIsBag)
                {
                    return MoveContainerToBag(point, container, itemComponent, sourceSlot, sourceContainerItem, targetSlot);
                }

                return MoveContainerToContainer(point, container, sourceSlot, sourceContainerItem, targetSlot);
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
                container.SetItem(targetSlot, sourceItem.ConfigId, sourceItem.Count);
                container.ItemEntries.Remove(sourceSlot);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.Get(sourceItem.ConfigId);
            if (sourceItem.ConfigId != targetItem.ConfigId || itemConfig == null || itemConfig.MaxStack <= 1)
            {
                container.SetItem(sourceSlot, targetItem.ConfigId, targetItem.Count);
                container.SetItem(targetSlot, sourceItem.ConfigId, sourceItem.Count);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return itemConfig == null && sourceItem.ConfigId == targetItem.ConfigId ? ErrorCode.ERR_ItemNotFound : ErrorCode.ERR_Success;
            }

            int stackCount = Math.Min(itemConfig.MaxStack - targetItem.Count, sourceItem.Count);
            if (stackCount <= 0)
            {
                container.SetItem(sourceSlot, targetItem.ConfigId, targetItem.Count);
                container.SetItem(targetSlot, sourceItem.ConfigId, sourceItem.Count);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            targetItem.Count += stackCount;
            sourceItem.Count -= stackCount;
            container.SetItem(targetSlot, targetItem.ConfigId, targetItem.Count);
            if (sourceItem.Count > 0)
            {
                container.SetItem(sourceSlot, sourceItem.ConfigId, sourceItem.Count);
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
            Item targetBagItem = itemComponent.GetItemBySlot(targetSlot);
            if (targetBagItem == null)
            {
                CreateBagItem(itemComponent, targetSlot, sourceItem.ConfigId, sourceItem.Count);
                container.ItemEntries.Remove(sourceSlot);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.Get(sourceItem.ConfigId);
            if (sourceItem.ConfigId == targetBagItem.ConfigId && itemConfig != null && itemConfig.MaxStack > 1)
            {
                int stackCount = Math.Min(itemConfig.MaxStack - targetBagItem.Count, sourceItem.Count);
                if (stackCount > 0)
                {
                    targetBagItem.AddCount(stackCount);
                    ItemHelper.NotifyItemUpdate(itemComponent, targetBagItem);

                    sourceItem.Count -= stackCount;
                    if (sourceItem.Count > 0)
                    {
                        container.SetItem(sourceSlot, sourceItem.ConfigId, sourceItem.Count);
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

            container.SetItem(sourceSlot, targetBagItem.ConfigId, targetBagItem.Count);
            RemoveBagItem(itemComponent, targetBagItem);
            CreateBagItem(itemComponent, targetSlot, sourceItem.ConfigId, sourceItem.Count);
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
            if (!container.TryGetItem(targetSlot, out ContainerItemEntry targetItem))
            {
                container.SetItem(targetSlot, sourceBagItem.ConfigId, sourceBagItem.Count);
                RemoveBagItem(itemComponent, sourceBagItem);
                UpdateContainerState(point, container);
                NotifyContainerUpdateToInRangePlayers(point, container);
                return ErrorCode.ERR_Success;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.Get(sourceBagItem.ConfigId);
            if (sourceBagItem.ConfigId == targetItem.ConfigId && itemConfig != null && itemConfig.MaxStack > 1)
            {
                int stackCount = Math.Min(itemConfig.MaxStack - targetItem.Count, sourceBagItem.Count);
                if (stackCount > 0)
                {
                    targetItem.Count += stackCount;
                    container.SetItem(targetSlot, targetItem.ConfigId, targetItem.Count);

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

            container.SetItem(targetSlot, sourceBagItem.ConfigId, sourceBagItem.Count);
            RemoveBagItem(itemComponent, sourceBagItem);
            CreateBagItem(itemComponent, sourceSlot, targetItem.ConfigId, targetItem.Count);
            UpdateContainerState(point, container);
            NotifyContainerUpdateToInRangePlayers(point, container);
            return ErrorCode.ERR_Success;
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

        private static void CreateBagItem(ItemComponent itemComponent, int slotIndex, int configId, int count)
        {
            Item item = itemComponent.AddChild<Item>();
            item.ConfigId = configId;
            item.Count = count;
            itemComponent.SetSlotItem(slotIndex, item);
            ItemHelper.NotifyItemUpdate(itemComponent, item);
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
