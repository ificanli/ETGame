using System;
using System.Collections.Generic;

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

            return point.PlayersInRange.Contains(player.Id);
        }

        public static void SendInteractHint(ECAPointComponent point, Unit player, bool inRange, int buttonTextId = 0)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            M2C_ECAInteractHint hint = M2C_ECAInteractHint.Create();
            hint.PointId = point.PointId;
            hint.InRange = inRange;
            hint.ButtonTextId = buttonTextId;
            MapMessageHelper.NoticeClient(player, hint, NoticeType.Self);
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

            ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
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

            ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
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

        public static void OpenContainerUI(ECAPointComponent point, Unit player)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
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

            M2C_ContainerOpen msg = M2C_ContainerOpen.Create();
            msg.PointId = point.PointId;
            msg.OutputMode = container.OutputMode;
            msg.IsFirstOpen = !container.HasOpenedOnce;
            container.FillItemMessage(msg.Items);
            MapMessageHelper.NoticeClient(player, msg, NoticeType.Self);

            if (!container.HasOpenedOnce)
            {
                container.HasOpenedOnce = true;
            }
        }

        public static void GenerateLoot(ECAPointComponent point, Unit player, string outputModeRaw, string lootTable, int count, float radius)
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
            Dictionary<int, int> aggregated = BuildAggregatedLoot(lootTable, count);
            int slotIndex = 0;
            foreach (KeyValuePair<int, int> kv in aggregated)
            {
                container.SetItem(slotIndex++, kv.Key, kv.Value);
            }

            container.LootGenerated = true;
            Log.Info($"[ECAContainer] loot generated point={point.PointId}, mode={container.OutputMode}, itemCount={container.ItemEntries.Count}, player={playerId}");
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
                NotifyContainerUpdateToInRangePlayers(point, container);
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

                NotifyContainerUpdateToInRangePlayers(point, container);
                return hasBagFailure ? ErrorCode.ERR_ECAContainerBagFull : ErrorCode.ERR_Success;
            }
        }

        public static void CloseContainer(ECAPointComponent point, Unit player)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
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
            if (point == null || container == null)
            {
                return;
            }

            Scene scene = point.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            foreach (long playerId in point.PlayersInRange)
            {
                Unit player = unitComponent.Get(playerId);
                if (player == null || player.IsDisposed || player.UnitType != UnitType.Player)
                {
                    continue;
                }

                M2C_ContainerUpdate update = M2C_ContainerUpdate.Create();
                update.PointId = point.PointId;
                container.FillItemMessage(update.Items);
                MapMessageHelper.NoticeClient(player, update, NoticeType.Self);
            }
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

        private static Dictionary<int, int> BuildAggregatedLoot(string lootTable, int count)
        {
            Dictionary<int, int> aggregated = new Dictionary<int, int>();
            List<ContainerItemEntry> pool = ParseLootPool(lootTable);
            if (pool.Count == 0)
            {
                return aggregated;
            }

            int rollCount = count > 0 ? count : pool.Count;
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
