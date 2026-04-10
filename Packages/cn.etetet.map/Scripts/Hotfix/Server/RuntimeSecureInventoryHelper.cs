using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET.Server
{
    /// <summary>
    /// 局内运行时安全格辅助方法。
    /// </summary>
    public static class RuntimeSecureInventoryHelper
    {
        public static RuntimeSecureInventoryComponent GetOrAdd(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return null;
            }

            return unit.GetComponent<RuntimeSecureInventoryComponent>() ?? unit.AddComponent<RuntimeSecureInventoryComponent>();
        }

        public static RuntimeSecureInventoryComponent Get(Unit unit)
        {
            return unit == null || unit.IsDisposed ? null : unit.GetComponent<RuntimeSecureInventoryComponent>();
        }

        public static void Apply(Unit unit, int width, int height, IList<LoadoutGridItemInfo> items)
        {
            RuntimeSecureInventoryComponent component = GetOrAdd(unit);
            if (component == null)
            {
                return;
            }

            component.Width = Math.Max(0, width);
            component.Height = Math.Max(0, height);
            component.Items.Clear();

            if (component.Width <= 0 || component.Height <= 0 || items == null || items.Count == 0)
            {
                return;
            }

            List<GridPlacementItemInfo> placements = BuildValidationItems(items);
            if (!LoadoutGridPlacementHelper.ArePlacementsValid(placements, component.Width, component.Height))
            {
                Log.Error($"[RuntimeSecure] invalid secure placements on apply: unit={unit?.Id ?? 0}, size={component.Width}x{component.Height}, itemCount={items.Count}");
                component.Items.Clear();
                return;
            }

            for (int i = 0; i < items.Count; ++i)
            {
                component.Items.Add(items[i]);
            }
        }

        public static void Clear(Unit unit, bool notifyClient = false)
        {
            RuntimeSecureInventoryComponent component = Get(unit);
            if (component == null)
            {
                return;
            }

            component.Items.Clear();
            component.Width = 0;
            component.Height = 0;

            if (notifyClient)
            {
                NotifyChanged(unit, component);
            }
        }

        public static List<LoadoutGridItemInfo> BuildSnapshot(RuntimeSecureInventoryComponent component)
        {
            List<LoadoutGridItemInfo> result = new();
            if (component == null)
            {
                return result;
            }

            for (int i = 0; i < component.Items.Count; ++i)
            {
                result.Add(component.Items[i]);
            }

            return result;
        }

        public static void FillMessage(RuntimeSecureInventoryComponent component, IList<LoadoutGridItemData> target)
        {
            target?.Clear();
            if (component == null || target == null)
            {
                return;
            }

            for (int i = 0; i < component.Items.Count; ++i)
            {
                LoadoutGridItemInfo item = component.Items[i];
                LoadoutGridItemData data = LoadoutGridItemData.Create();
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                data.AnchorSlotIndex = item.AnchorSlotIndex;
                data.GridWidth = item.GridWidth;
                data.GridHeight = item.GridHeight;
                target.Add(data);
            }
        }

        public static bool TryGetItem(RuntimeSecureInventoryComponent component, int anchorSlotIndex, out int itemIndex, out LoadoutGridItemInfo item)
        {
            itemIndex = -1;
            item = default;
            if (component == null || anchorSlotIndex < 0)
            {
                return false;
            }

            for (int i = 0; i < component.Items.Count; ++i)
            {
                if (component.Items[i].AnchorSlotIndex != anchorSlotIndex)
                {
                    continue;
                }

                itemIndex = i;
                item = component.Items[i];
                return true;
            }

            return false;
        }

        public static bool CanPlaceAtAnchorSlot(
            RuntimeSecureInventoryComponent component,
            int anchorSlotIndex,
            int gridWidth,
            int gridHeight,
            int ignoreIndex = -1,
            int ignoreIndex2 = -1)
        {
            if (component == null || component.Width <= 0 || component.Height <= 0)
            {
                return false;
            }

            List<GridPlacementItemInfo> validationItems = BuildValidationItems(component.Items, ignoreIndex, ignoreIndex2);
            return LoadoutGridPlacementHelper.CanPlaceAtAnchorSlot(
                validationItems,
                component.Width,
                component.Height,
                anchorSlotIndex,
                gridWidth,
                gridHeight);
        }

        public static bool TryFindFirstFitAnchorSlot(RuntimeSecureInventoryComponent component, int gridWidth, int gridHeight, out int anchorSlotIndex)
        {
            anchorSlotIndex = -1;
            if (component == null || component.Width <= 0 || component.Height <= 0)
            {
                return false;
            }

            List<GridPlacementItemInfo> validationItems = BuildValidationItems(component.Items);
            return LoadoutGridPlacementHelper.TryFindFirstFitAnchorSlot(
                validationItems,
                component.Width,
                component.Height,
                gridWidth,
                gridHeight,
                out anchorSlotIndex);
        }

        public static bool TryResolveItemMetrics(
            int configId,
            out int resolvedConfigId,
            out ItemConfig itemConfig,
            out int maxStack,
            out int gridWidth,
            out int gridHeight)
        {
            resolvedConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(configId);
            itemConfig = ItemConfigCategory.Instance.GetOrDefault(resolvedConfigId);
            if (itemConfig == null)
            {
                maxStack = 1;
                gridWidth = LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
                gridHeight = LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;
                return false;
            }

            maxStack = itemConfig.MaxStack > 0 ? itemConfig.MaxStack : 1;
            gridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(itemConfig.GridWidth);
            gridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(itemConfig.GridHeight);
            return true;
        }

        public static bool TryAddItem(Unit unit, int configId, int count, out string message)
        {
            RuntimeSecureInventoryComponent component = GetOrAdd(unit);
            if (!TryAddItem(component, configId, count, out message))
            {
                return false;
            }

            NotifyChanged(unit, component);
            return true;
        }

        public static bool TryAddItem(RuntimeSecureInventoryComponent component, int configId, int count, out string message)
        {
            message = string.Empty;
            if (component == null)
            {
                message = "secure component missing";
                return false;
            }

            if (count <= 0)
            {
                message = "count invalid";
                return false;
            }

            if (component.Width <= 0 || component.Height <= 0)
            {
                message = "secure size invalid";
                return false;
            }

            if (!TryResolveItemMetrics(configId, out int resolvedConfigId, out ItemConfig itemConfig, out int maxStack, out int gridWidth, out int gridHeight))
            {
                message = $"item config not found: {configId}";
                return false;
            }

            List<LoadoutGridItemInfo> nextItems = new(component.Items);
            int remainCount = count;

            if (maxStack > 1)
            {
                for (int i = 0; i < nextItems.Count; ++i)
                {
                    LoadoutGridItemInfo existed = nextItems[i];
                    if (!LegacyItemConfigIdHelper.MatchesConfigId(existed.ConfigId, resolvedConfigId) || existed.Count >= maxStack)
                    {
                        continue;
                    }

                    int addCount = Math.Min(remainCount, maxStack - existed.Count);
                    existed.ConfigId = resolvedConfigId;
                    existed.Count += addCount;
                    nextItems[i] = existed;
                    remainCount -= addCount;
                    if (remainCount <= 0)
                    {
                        break;
                    }
                }
            }

            while (remainCount > 0)
            {
                List<GridPlacementItemInfo> validationItems = BuildValidationItems(nextItems);
                if (!LoadoutGridPlacementHelper.TryFindFirstFitAnchorSlot(
                        validationItems,
                        component.Width,
                        component.Height,
                        gridWidth,
                        gridHeight,
                        out int anchorSlotIndex))
                {
                    message = "secure is full";
                    return false;
                }

                int addCount = Math.Min(remainCount, maxStack);
                nextItems.Add(new LoadoutGridItemInfo
                {
                    ConfigId = resolvedConfigId,
                    Count = addCount,
                    AnchorSlotIndex = anchorSlotIndex,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                });
                remainCount -= addCount;
            }

            ReplaceItems(component, nextItems);
            return true;
        }

        public static int MoveItem(Unit unit, int sourceAnchorSlotIndex, int targetAnchorSlotIndex)
        {
            RuntimeSecureInventoryComponent component = Get(unit);
            if (component == null)
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            if (sourceAnchorSlotIndex < 0 || targetAnchorSlotIndex < 0)
            {
                return ErrorCode.ERR_ItemSlotInvalid;
            }

            if (sourceAnchorSlotIndex == targetAnchorSlotIndex)
            {
                return ErrorCode.ERR_Success;
            }

            if (!TryGetItem(component, sourceAnchorSlotIndex, out int sourceIndex, out LoadoutGridItemInfo sourceItem))
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            if (!TryGetItem(component, targetAnchorSlotIndex, out int targetIndex, out LoadoutGridItemInfo targetItem))
            {
                if (!CanPlaceAtAnchorSlot(component, targetAnchorSlotIndex, sourceItem.GridWidth, sourceItem.GridHeight, sourceIndex))
                {
                    return ErrorCode.ERR_ItemSlotInvalid;
                }

                sourceItem.AnchorSlotIndex = targetAnchorSlotIndex;
                component.Items[sourceIndex] = sourceItem;
                NotifyChanged(unit, component);
                return ErrorCode.ERR_Success;
            }

            if (LegacyItemConfigIdHelper.MatchesConfigId(sourceItem.ConfigId, targetItem.ConfigId) &&
                TryResolveItemMetrics(sourceItem.ConfigId, out int resolvedConfigId, out _, out int maxStack, out _, out _) &&
                maxStack > 1)
            {
                int stackCount = Math.Min(maxStack - targetItem.Count, sourceItem.Count);
                if (stackCount > 0)
                {
                    targetItem.ConfigId = resolvedConfigId;
                    sourceItem.ConfigId = resolvedConfigId;
                    targetItem.Count += stackCount;
                    sourceItem.Count -= stackCount;
                    component.Items[targetIndex] = targetItem;
                    if (sourceItem.Count > 0)
                    {
                        component.Items[sourceIndex] = sourceItem;
                    }
                    else
                    {
                        component.Items.RemoveAt(sourceIndex);
                    }

                    NotifyChanged(unit, component);
                    return ErrorCode.ERR_Success;
                }
            }

            if (!CanPlaceAtAnchorSlot(component, targetAnchorSlotIndex, sourceItem.GridWidth, sourceItem.GridHeight, sourceIndex, targetIndex) ||
                !CanPlaceAtAnchorSlot(component, sourceAnchorSlotIndex, targetItem.GridWidth, targetItem.GridHeight, sourceIndex, targetIndex))
            {
                return ErrorCode.ERR_ItemSlotInvalid;
            }

            sourceItem.AnchorSlotIndex = targetAnchorSlotIndex;
            targetItem.AnchorSlotIndex = sourceAnchorSlotIndex;
            component.Items[sourceIndex] = targetItem;
            component.Items[targetIndex] = sourceItem;
            NotifyChanged(unit, component);
            return ErrorCode.ERR_Success;
        }

        public static bool ShouldPreferSecure(int configId, int count)
        {
            if (count <= 0)
            {
                return false;
            }

            if (!TryResolveItemMetrics(configId, out _, out ItemConfig itemConfig, out _, out int gridWidth, out int gridHeight))
            {
                return false;
            }

            long area = (long)Math.Max(1, gridWidth) * Math.Max(1, gridHeight);
            long totalValue = (long)Math.Max(0, itemConfig.LoadoutBuyPrice) * count;
            long threshold = (long)GetAutoSecureMinValuePerGrid() * area;
            return totalValue > threshold;
        }

        public static int GetAutoSecureMinValuePerGrid()
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            object data = category?.Data;
            if (data == null)
            {
                return 0;
            }

            PropertyInfo property = data.GetType().GetProperty("AutoSecureMinValuePerGrid", BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetValue(data) is int propertyValue)
            {
                return Math.Max(0, propertyValue);
            }

            FieldInfo field = data.GetType().GetField("AutoSecureMinValuePerGrid", BindingFlags.Instance | BindingFlags.Public);
            if (field?.GetValue(data) is int fieldValue)
            {
                return Math.Max(0, fieldValue);
            }

            return 0;
        }

        public static void NotifyChanged(Unit unit)
        {
            NotifyChanged(unit, Get(unit));
        }

        private static void NotifyChanged(Unit unit, RuntimeSecureInventoryComponent component)
        {
            if (unit == null || unit.IsDisposed || component == null)
            {
                return;
            }

            M2C_RuntimeSecureStateChanged message = M2C_RuntimeSecureStateChanged.Create();
            message.SecureWidth = component.Width;
            message.SecureHeight = component.Height;
            FillMessage(component, message.Items);
            MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
        }

        private static void ReplaceItems(RuntimeSecureInventoryComponent component, List<LoadoutGridItemInfo> nextItems)
        {
            component.Items.Clear();
            if (nextItems == null)
            {
                return;
            }

            for (int i = 0; i < nextItems.Count; ++i)
            {
                component.Items.Add(nextItems[i]);
            }
        }

        private static List<GridPlacementItemInfo> BuildValidationItems(
            IList<LoadoutGridItemInfo> items,
            int ignoreIndex = -1,
            int ignoreIndex2 = -1)
        {
            List<GridPlacementItemInfo> result = new(items?.Count ?? 0);
            if (items == null)
            {
                return result;
            }

            for (int i = 0; i < items.Count; ++i)
            {
                if (i == ignoreIndex || i == ignoreIndex2)
                {
                    continue;
                }

                LoadoutGridItemInfo item = items[i];
                result.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
            }

            return result;
        }
    }
}
