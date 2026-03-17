using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace ET.Client
{
    [FriendOf(typeof(LobbyPanelComponent))]
    [FriendOf(typeof(LoadoutGridItemViewProxy))]
    public static partial class LobbyPanelComponentSystem
    {
        private static void BindLoadoutGridItemInteract(this LobbyPanelComponent self, RectTransform view, LoadoutGridItemViewProxy proxy)
        {
            if (view == null || proxy == null)
            {
                return;
            }

            EnsureRaycastGraphic(view);
            EventTrigger trigger = view.GetComponent<EventTrigger>() ?? view.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            AddLoadoutTrigger(trigger, EventTriggerType.BeginDrag, OnLoadoutBeginDragEvent);
            AddLoadoutTrigger(trigger, EventTriggerType.Drag, OnLoadoutDragEvent);
            AddLoadoutTrigger(trigger, EventTriggerType.EndDrag, OnLoadoutEndDragEvent);
            AddLoadoutTrigger(trigger, EventTriggerType.PointerClick, OnLoadoutClickEvent);
        }

        private static void BindFixedSlotDragInteract(this LobbyPanelComponent self, EquipSlotItemComponent slotItem, EquipSlotType slotType)
        {
            RectTransform view = GetFixedSlotRectTransform(slotItem);
            if (view == null)
            {
                return;
            }

            EnsureRaycastGraphic(view);
            LoadoutGridItemViewProxy proxy = view.GetComponent<LoadoutGridItemViewProxy>() ?? view.gameObject.AddComponent<LoadoutGridItemViewProxy>();
            proxy.PanelRef = self;
            proxy.IsWarehouse = false;
            proxy.ItemUid = 0;
            proxy.AreaType = (int)LoadoutAreaType.FixedSlot;
            proxy.FixedSlotType = (int)ToFixedSlotType(slotType);
            proxy.AnchorSlotIndex = 0;

            EventTrigger trigger = view.GetComponent<EventTrigger>() ?? view.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            AddLoadoutTrigger(trigger, EventTriggerType.BeginDrag, OnLoadoutBeginDragEvent);
            AddLoadoutTrigger(trigger, EventTriggerType.Drag, OnLoadoutDragEvent);
            AddLoadoutTrigger(trigger, EventTriggerType.EndDrag, OnLoadoutEndDragEvent);
        }

        private static void RefreshFixedSlotDragProxy(this LobbyPanelComponent self, EquipSlotItemComponent slotItem, EquipSlotType slotType, int configId)
        {
            RectTransform view = GetFixedSlotRectTransform(slotItem);
            if (view == null)
            {
                return;
            }

            LoadoutGridItemViewProxy proxy = view.GetComponent<LoadoutGridItemViewProxy>() ?? view.gameObject.AddComponent<LoadoutGridItemViewProxy>();
            proxy.PanelRef = self;
            proxy.IsWarehouse = false;
            proxy.ItemUid = 0;
            proxy.AreaType = (int)LoadoutAreaType.FixedSlot;
            proxy.FixedSlotType = (int)ToFixedSlotType(slotType);
            proxy.AnchorSlotIndex = 0;
            proxy.ConfigId = configId;
        }

        private static void AddLoadoutTrigger(EventTrigger trigger, EventTriggerType eventType, UnityAction<BaseEventData> handler)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = eventType,
            };
            entry.callback.AddListener(handler);
            trigger.triggers.Add(entry);
        }

        private static void OnLoadoutBeginDragEvent(BaseEventData data)
        {
            if (!TryGetLoadoutDragContext(data, out LobbyPanelComponent self, out RectTransform view, out LoadoutGridItemViewProxy proxy, out PointerEventData eventData))
            {
                return;
            }

            OnLoadoutItemBeginDrag(self, view, proxy, eventData);
        }

        private static void OnLoadoutDragEvent(BaseEventData data)
        {
            if (!TryGetLoadoutDragContext(data, out LobbyPanelComponent self, out RectTransform view, out LoadoutGridItemViewProxy proxy, out PointerEventData eventData))
            {
                return;
            }

            OnLoadoutItemDrag(self, view, proxy, eventData);
        }

        private static void OnLoadoutEndDragEvent(BaseEventData data)
        {
            if (!TryGetLoadoutDragContext(data, out LobbyPanelComponent self, out RectTransform view, out LoadoutGridItemViewProxy proxy, out PointerEventData eventData))
            {
                return;
            }

            OnLoadoutItemEndDrag(self, view, proxy, eventData);
        }

        private static void OnLoadoutClickEvent(BaseEventData data)
        {
            if (!TryGetLoadoutDragContext(data, out LobbyPanelComponent self, out RectTransform view, out LoadoutGridItemViewProxy proxy, out PointerEventData eventData))
            {
                return;
            }

            OnLoadoutItemClick(self, view, proxy, eventData);
        }

        private static bool TryGetLoadoutDragContext(
            BaseEventData data,
            out LobbyPanelComponent self,
            out RectTransform view,
            out LoadoutGridItemViewProxy proxy,
            out PointerEventData eventData)
        {
            self = null;
            view = null;
            proxy = null;
            eventData = data as PointerEventData;
            if (eventData == null)
            {
                return false;
            }

            GameObject go = eventData.pointerDrag != null ? eventData.pointerDrag : eventData.pointerPress;
            if (go == null)
            {
                return false;
            }

            proxy = go.GetComponent<LoadoutGridItemViewProxy>();
            if (proxy == null)
            {
                return false;
            }

            self = proxy.PanelRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            view = go.GetComponent<RectTransform>();
            return view != null;
        }

        private static void OnLoadoutItemBeginDrag(
            LobbyPanelComponent self,
            RectTransform view,
            LoadoutGridItemViewProxy proxy,
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null || proxy == null || eventData == null || proxy.ConfigId <= 0)
            {
                return;
            }

            RectTransform sourceLayer = self.GetLoadoutDragSourceLayer(view, proxy);
            if (sourceLayer == null)
            {
                return;
            }

            self.IsDragging = true;
            self.DraggingIsWarehouse = proxy.IsWarehouse;
            self.DraggingItemUid = proxy.ItemUid;
            self.DraggingConfigId = proxy.ConfigId;
            self.DraggingAnchorSlotIndex = proxy.AnchorSlotIndex;
            self.DraggingAreaType = proxy.AreaType;
            self.DraggingFixedSlotType = proxy.FixedSlotType;
            self.DraggingView = view;
            view.SetAsLastSibling();

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(sourceLayer, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                self.DragWorldOffset = view.position - worldPoint;
            }
            else
            {
                self.DragWorldOffset = Vector3.zero;
            }

            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>() ?? view.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
        }

        private static void OnLoadoutItemDrag(
            LobbyPanelComponent self,
            RectTransform view,
            LoadoutGridItemViewProxy proxy,
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null || proxy == null || eventData == null)
            {
                return;
            }

            if (!self.IsDragging || self.DraggingView != view)
            {
                return;
            }

            RectTransform sourceLayer = self.GetLoadoutDragSourceLayer(view, proxy);
            if (sourceLayer == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(sourceLayer, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                view.position = worldPoint + self.DragWorldOffset;
            }
        }

        private static void OnLoadoutItemEndDrag(
            LobbyPanelComponent self,
            RectTransform view,
            LoadoutGridItemViewProxy proxy,
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null || proxy == null)
            {
                return;
            }

            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            if (!self.IsDragging || self.DraggingView != view)
            {
                return;
            }

            bool hasValidTarget = self.TryGetLoadoutDropTarget(eventData, out bool targetIsWarehouse, out LoadoutAreaType targetAreaType, out LoadoutFixedSlotType targetSlotType, out int targetAnchorSlotIndex);
            self.ClearLoadoutDragState();

            if (hasValidTarget)
            {
                if (proxy.IsWarehouse)
                {
                    if (!targetIsWarehouse)
                    {
                        self.DragTakeWarehouseItemAsync(proxy.ConfigId, targetAreaType, targetSlotType, targetAnchorSlotIndex).Coroutine();
                    }
                }
                else
                {
                    LoadoutAreaType sourceAreaType = (LoadoutAreaType)proxy.AreaType;
                    LoadoutFixedSlotType sourceSlotType = (LoadoutFixedSlotType)proxy.FixedSlotType;
                    if (sourceAreaType == LoadoutAreaType.FixedSlot)
                    {
                        if (targetIsWarehouse)
                        {
                            self.PutFixedSlotToWarehouseAsync(ToFixedEquipSlotType(sourceSlotType)).Coroutine();
                        }
                        else if (targetAreaType != LoadoutAreaType.None &&
                                 !(targetAreaType == LoadoutAreaType.FixedSlot && targetSlotType == sourceSlotType))
                        {
                            self.MoveOwnedItemAsync(
                                sourceAreaType,
                                sourceSlotType,
                                0,
                                targetAreaType,
                                targetSlotType,
                                targetAnchorSlotIndex).Coroutine();
                        }
                    }
                    else if (targetIsWarehouse)
                    {
                        self.HandleOwnedGridItemClickAsync(sourceAreaType, proxy.AnchorSlotIndex).Coroutine();
                    }
                    else if (targetAreaType == LoadoutAreaType.FixedSlot ||
                             ((targetAreaType == LoadoutAreaType.Bag || targetAreaType == LoadoutAreaType.Secure) &&
                              (targetAreaType != sourceAreaType || targetAnchorSlotIndex != proxy.AnchorSlotIndex)))
                    {
                        self.MoveOwnedItemAsync(
                            sourceAreaType,
                            LoadoutFixedSlotType.None,
                            proxy.AnchorSlotIndex,
                            targetAreaType,
                            targetSlotType,
                            targetAnchorSlotIndex).Coroutine();
                    }
                }
            }

            self.RefreshLoadoutExtraUi(self.Root()?.GetComponent<LoadoutComponent>());
        }

        private static void OnLoadoutItemClick(
            LobbyPanelComponent self,
            RectTransform view,
            LoadoutGridItemViewProxy proxy,
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null || proxy == null || eventData == null || eventData.dragging)
            {
                return;
            }

            if (proxy.IsWarehouse)
            {
                self.OnWarehouseGridItemClicked(proxy.ItemUid, proxy.ConfigId);
                return;
            }

            if ((LoadoutAreaType)proxy.AreaType == LoadoutAreaType.FixedSlot)
            {
                return;
            }

            self.HandleOwnedGridItemClickAsync((LoadoutAreaType)proxy.AreaType, proxy.AnchorSlotIndex).Coroutine();
        }

        private static bool TryGetLoadoutDropTarget(
            this LobbyPanelComponent self,
            PointerEventData eventData,
            out bool targetIsWarehouse,
            out LoadoutAreaType targetAreaType,
            out LoadoutFixedSlotType targetSlotType,
            out int targetAnchorSlotIndex)
        {
            targetIsWarehouse = false;
            targetAreaType = LoadoutAreaType.None;
            targetSlotType = LoadoutFixedSlotType.None;
            targetAnchorSlotIndex = -1;
            if (self == null || self.IsDisposed || eventData == null)
            {
                return false;
            }

            Camera eventCamera = eventData.pressEventCamera;
            if (self.TryResolveFixedSlotDropTarget(eventData.position, eventCamera, out targetSlotType))
            {
                targetAreaType = LoadoutAreaType.FixedSlot;
                targetAnchorSlotIndex = 0;
                return true;
            }

            if (self.TryResolveOwnedAreaSlot(LoadoutAreaType.Bag, eventData.position, eventCamera, out targetAnchorSlotIndex))
            {
                targetAreaType = LoadoutAreaType.Bag;
                return true;
            }

            if (self.TryResolveOwnedAreaSlot(LoadoutAreaType.Secure, eventData.position, eventCamera, out targetAnchorSlotIndex))
            {
                targetAreaType = LoadoutAreaType.Secure;
                return true;
            }

            RectTransform warehouseBoard = self.GetWarehouseBoardRoot();
            if (warehouseBoard != null && RectTransformUtility.RectangleContainsScreenPoint(warehouseBoard, eventData.position, eventCamera))
            {
                targetIsWarehouse = true;
                return true;
            }

            return false;
        }

        private static bool TryResolveFixedSlotDropTarget(
            this LobbyPanelComponent self,
            Vector2 screenPosition,
            Camera eventCamera,
            out LoadoutFixedSlotType slotType)
        {
            slotType = LoadoutFixedSlotType.None;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            if (IsScreenPointInFixedSlot(self.UIEquipSlotItemWeapon, screenPosition, eventCamera))
            {
                slotType = LoadoutFixedSlotType.MainWeapon;
                return true;
            }

            if (IsScreenPointInFixedSlot(self.UIEquipSlotItemWeapon2, screenPosition, eventCamera))
            {
                slotType = LoadoutFixedSlotType.SubWeapon;
                return true;
            }

            if (IsScreenPointInFixedSlot(self.UIEquipSlotItemArmor, screenPosition, eventCamera))
            {
                slotType = LoadoutFixedSlotType.Armor;
                return true;
            }

            if (IsScreenPointInFixedSlot(self.UIEquipSlotItemBag, screenPosition, eventCamera))
            {
                slotType = LoadoutFixedSlotType.Backpack;
                return true;
            }

            return false;
        }

        private static bool TryResolveOwnedAreaSlot(
            this LobbyPanelComponent self,
            LoadoutAreaType areaType,
            Vector2 screenPosition,
            Camera eventCamera,
            out int slotIndex)
        {
            slotIndex = -1;
            RectTransform boardRoot = self.GetOwnedAreaBoardRoot(areaType);
            LoadoutComponent loadout = self.Root()?.GetComponent<LoadoutComponent>();
            int cols = GetGridContainerWidth(loadout, areaType);
            int rows = GetGridContainerHeight(loadout, areaType);
            if (boardRoot == null || cols <= 0 || rows <= 0)
            {
                return false;
            }

            Vector2 cellSize = CalcGridCellSize(boardRoot, cols, rows, self.GridSpacing, self.GridPadding);
            return TryResolveLoadoutBoardSlot(
                boardRoot,
                cols,
                rows,
                cellSize,
                self.GridSpacing,
                self.GridPadding,
                screenPosition,
                eventCamera,
                out slotIndex);
        }

        private static RectTransform GetOwnedAreaBoardRoot(this LobbyPanelComponent self, LoadoutAreaType areaType)
        {
            return areaType switch
            {
                LoadoutAreaType.Bag => self.u_ComCurrentBagBoardRoot,
                LoadoutAreaType.Secure => self.u_ComSecureBoardRoot,
                _ => null,
            };
        }

        private static RectTransform GetOwnedAreaItemsLayer(this LobbyPanelComponent self, LoadoutAreaType areaType)
        {
            return areaType switch
            {
                LoadoutAreaType.Bag => self.u_ComCurrentBagItemsLayer,
                LoadoutAreaType.Secure => self.u_ComSecureItemsLayer,
                _ => null,
            };
        }

        private static RectTransform GetLoadoutDragSourceLayer(this LobbyPanelComponent self, RectTransform view, LoadoutGridItemViewProxy proxy)
        {
            if (self == null || self.IsDisposed || view == null || proxy == null)
            {
                return null;
            }

            if (proxy.IsWarehouse)
            {
                return self.WarehouseItemsLayer;
            }

            if ((LoadoutAreaType)proxy.AreaType == LoadoutAreaType.FixedSlot)
            {
                return view.parent as RectTransform ?? view;
            }

            return self.GetOwnedAreaItemsLayer((LoadoutAreaType)proxy.AreaType);
        }

        private static bool TryResolveLoadoutBoardSlot(
            RectTransform boardRoot,
            int cols,
            int rows,
            Vector2 cellSize,
            Vector2 spacing,
            Vector2 padding,
            Vector2 screenPosition,
            Camera eventCamera,
            out int slotIndex)
        {
            slotIndex = -1;
            if (boardRoot == null || cols <= 0 || rows <= 0)
            {
                return false;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(boardRoot, screenPosition, eventCamera))
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPosition, eventCamera, out Vector2 localPoint))
            {
                return false;
            }

            Vector2 rectSize = boardRoot.rect.size;
            float left = localPoint.x + rectSize.x * 0.5f;
            float top = rectSize.y * 0.5f - localPoint.y;
            float strideX = cellSize.x + spacing.x;
            float strideY = cellSize.y + spacing.y;
            int x = Mathf.FloorToInt((left - padding.x) / Mathf.Max(1f, strideX));
            int y = Mathf.FloorToInt((top - padding.y) / Mathf.Max(1f, strideY));
            x = Mathf.Clamp(x, 0, Mathf.Max(0, cols - 1));
            y = Mathf.Clamp(y, 0, Mathf.Max(0, rows - 1));
            slotIndex = y * cols + x;
            return true;
        }

        private static async ETTask DragTakeWarehouseItemAsync(
            this LobbyPanelComponent self,
            int configId,
            LoadoutAreaType targetAreaType,
            LoadoutFixedSlotType targetSlotType,
            int targetAnchorSlotIndex)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.TakeWarehouseItemAsync(configId, targetAreaType, targetSlotType, targetAnchorSlotIndex);
        }

        private static async ETTask MoveOwnedItemAsync(
            this LobbyPanelComponent self,
            LoadoutAreaType sourceAreaType,
            LoadoutFixedSlotType sourceSlotType,
            int sourceAnchorSlotIndex,
            LoadoutAreaType targetAreaType,
            LoadoutFixedSlotType targetSlotType,
            int targetAnchorSlotIndex)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            C2G_LoadoutMoveOwnedItem request = C2G_LoadoutMoveOwnedItem.Create();
            request.SourceAreaType = (int)sourceAreaType;
            request.SourceSlotType = (int)sourceSlotType;
            request.SourceAnchorSlotIndex = sourceAnchorSlotIndex;
            request.TargetAreaType = (int)targetAreaType;
            request.TargetSlotType = (int)targetSlotType;
            request.TargetAnchorSlotIndex = targetAnchorSlotIndex;

            G2C_LoadoutMoveOwnedItem response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(request) as G2C_LoadoutMoveOwnedItem;
            if (response == null || response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[LoadoutUI] MoveOwnedItem failed: source={sourceAreaType}/{sourceSlotType}:{sourceAnchorSlotIndex}, target={targetAreaType}/{targetSlotType}:{targetAnchorSlotIndex}, error={response?.Error}, message={response?.Message}");
            }
        }

        private static void ClearLoadoutDragState(this LobbyPanelComponent self)
        {
            self.IsDragging = false;
            self.DraggingIsWarehouse = false;
            self.DraggingItemUid = 0;
            self.DraggingConfigId = 0;
            self.DraggingAnchorSlotIndex = 0;
            self.DraggingAreaType = 0;
            self.DraggingFixedSlotType = 0;
            self.DraggingView = null;
            self.DragWorldOffset = Vector3.zero;
        }

        private static bool IsScreenPointInFixedSlot(EquipSlotItemComponent slotItem, Vector2 screenPosition, Camera eventCamera)
        {
            RectTransform rectTransform = GetFixedSlotRectTransform(slotItem);
            return rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
        }

        private static RectTransform GetFixedSlotRectTransform(EquipSlotItemComponent slotItem)
        {
            return slotItem?.UIBase?.OwnerGameObject?.GetComponent<RectTransform>();
        }

        private static EquipSlotType ToFixedEquipSlotType(LoadoutFixedSlotType slotType)
        {
            return slotType switch
            {
                LoadoutFixedSlotType.MainWeapon => EquipSlotType.Weapon,
                LoadoutFixedSlotType.SubWeapon => EquipSlotType.Weapon2,
                LoadoutFixedSlotType.Armor => EquipSlotType.Armor,
                LoadoutFixedSlotType.Backpack => EquipSlotType.Bag,
                _ => EquipSlotType.BagContent,
            };
        }
    }
}
