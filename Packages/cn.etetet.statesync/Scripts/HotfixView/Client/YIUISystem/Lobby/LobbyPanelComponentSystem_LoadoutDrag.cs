using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

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
            EnsureCanvasGroup(view);
            EventTrigger trigger = view.GetComponent<EventTrigger>() ?? view.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            AddLoadoutTrigger(trigger, EventTriggerType.InitializePotentialDrag, OnLoadoutInitializePotentialDragEvent);
            AddLoadoutTrigger(trigger, EventTriggerType.PointerDown, OnLoadoutPointerDownEvent);
            AddLoadoutTrigger(trigger, EventTriggerType.PointerUp, OnLoadoutPointerUpEvent);
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
            EnsureCanvasGroup(view);
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

        private static void BindWarehouseBoardInteract(this LobbyPanelComponent self, RectTransform boardRoot)
        {
            if (self == null || self.IsDisposed || boardRoot == null)
            {
                return;
            }

            EnsureRaycastGraphic(boardRoot);
            EventTrigger trigger = boardRoot.GetComponent<EventTrigger>() ?? boardRoot.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            EntityRef<LobbyPanelComponent> selfRef = self;
            AddWarehouseBoardTrigger(
                trigger,
                EventTriggerType.InitializePotentialDrag,
                selfRef,
                (panel, eventData) => panel.ForwardWarehouseInitializePotentialDrag(eventData));
            AddWarehouseBoardTrigger(
                trigger,
                EventTriggerType.PointerDown,
                selfRef,
                (panel, _) =>
                {
                    panel.EndWarehouseScrollForwarding(null);
                    panel.ClearWarehousePressState();
                });
            AddWarehouseBoardTrigger(
                trigger,
                EventTriggerType.PointerUp,
                selfRef,
                (panel, eventData) =>
                {
                    panel.EndWarehouseScrollForwarding(eventData);
                    panel.ClearWarehousePressState();
                });
            AddWarehouseBoardTrigger(
                trigger,
                EventTriggerType.BeginDrag,
                selfRef,
                (panel, eventData) => panel.BeginWarehouseScrollForwarding(eventData));
            AddWarehouseBoardTrigger(
                trigger,
                EventTriggerType.Drag,
                selfRef,
                (panel, eventData) => panel.ForwardWarehouseDrag(eventData));
            AddWarehouseBoardTrigger(
                trigger,
                EventTriggerType.EndDrag,
                selfRef,
                (panel, eventData) =>
                {
                    panel.EndWarehouseScrollForwarding(eventData);
                    panel.ClearWarehousePressState();
                });
        }

        private static void AddWarehouseBoardTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            EntityRef<LobbyPanelComponent> selfRef,
            Action<LobbyPanelComponent, PointerEventData> handler)
        {
            if (trigger == null || handler == null)
            {
                return;
            }

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = eventType,
            };
            entry.callback.AddListener(data =>
            {
                LobbyPanelComponent self = selfRef;
                PointerEventData eventData = data as PointerEventData;
                if (self == null || self.IsDisposed || eventData == null)
                {
                    return;
                }

                handler(self, eventData);
            });
            trigger.triggers.Add(entry);
        }

        private static void OnLoadoutInitializePotentialDragEvent(BaseEventData data)
        {
            if (!TryGetLoadoutDragContext(data, out LobbyPanelComponent self, out _, out LoadoutGridItemViewProxy proxy, out PointerEventData eventData))
            {
                return;
            }

            if (proxy.IsWarehouse)
            {
                self.ForwardWarehouseInitializePotentialDrag(eventData);
            }
        }

        private static void OnLoadoutPointerDownEvent(BaseEventData data)
        {
            if (!TryGetLoadoutDragContext(data, out LobbyPanelComponent self, out RectTransform view, out LoadoutGridItemViewProxy proxy, out PointerEventData eventData))
            {
                return;
            }

            if (proxy.IsWarehouse)
            {
                self.RecordWarehousePressState(view, proxy, eventData);
            }
        }

        private static void OnLoadoutPointerUpEvent(BaseEventData data)
        {
            if (!TryGetLoadoutDragContext(data, out LobbyPanelComponent self, out RectTransform view, out LoadoutGridItemViewProxy proxy, out PointerEventData eventData))
            {
                return;
            }

            if (!proxy.IsWarehouse)
            {
                return;
            }

            if (!self.IsDragging || self.DraggingView != view)
            {
                self.EndWarehouseScrollForwarding(eventData);
                self.ClearWarehousePressState();
            }
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

            GameObject go = eventData.pointerDrag ??
                            eventData.pointerPress ??
                            eventData.pointerPressRaycast.gameObject ??
                            eventData.pointerCurrentRaycast.gameObject;
            if (go == null)
            {
                return false;
            }

            proxy = go.GetComponent<LoadoutGridItemViewProxy>() ?? go.GetComponentInParent<LoadoutGridItemViewProxy>();
            if (proxy == null)
            {
                return false;
            }

            self = proxy.PanelRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            view = proxy.GetComponent<RectTransform>();
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

            if (proxy.IsWarehouse)
            {
                if (!self.ShouldBeginWarehouseItemDrag(view, proxy))
                {
                    self.BeginWarehouseScrollForwarding(eventData);
                    return;
                }

                self.StopWarehouseScrollForItemDrag();
                self.ClearWarehousePressState();
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

            CanvasGroup canvasGroup = EnsureCanvasGroup(view);
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
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

            if (proxy.IsWarehouse && self.WarehouseScrollForwarding && (!self.IsDragging || self.DraggingView != view))
            {
                self.ForwardWarehouseDrag(eventData);
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

            if (proxy.IsWarehouse && self.WarehouseScrollForwarding && (!self.IsDragging || self.DraggingView != view))
            {
                self.EndWarehouseScrollForwarding(eventData);
                self.ClearWarehousePressState();
                return;
            }

            if (!self.IsDragging || self.DraggingView != view)
            {
                if (proxy.IsWarehouse)
                {
                    self.ClearWarehousePressState();
                }

                return;
            }

            bool hasValidTarget = self.TryGetLoadoutDropTarget(eventData, out bool targetIsWarehouse, out LoadoutAreaType targetAreaType, out LoadoutFixedSlotType targetSlotType, out int targetAnchorSlotIndex);
            self.ClearLoadoutDragState();
            self.ClearWarehousePressState();

            if (hasValidTarget)
            {
                if (proxy.IsWarehouse)
                {
                    if (targetIsWarehouse)
                    {
                        if (targetAnchorSlotIndex >= 0 && targetAnchorSlotIndex != proxy.AnchorSlotIndex)
                        {
                            self.MoveWarehouseItemAsync(proxy.ItemUid, targetAnchorSlotIndex).Coroutine();
                        }
                    }
                    else
                    {
                        self.DragTakeWarehouseItemAsync(proxy.ConfigId, proxy.ItemUid, targetAreaType, targetSlotType, targetAnchorSlotIndex).Coroutine();
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
                self.TryResolveWarehouseSlot(eventData.position, eventCamera, out targetAnchorSlotIndex);
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
            long itemUid,
            LoadoutAreaType targetAreaType,
            LoadoutFixedSlotType targetSlotType,
            int targetAnchorSlotIndex)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.TakeWarehouseItemAsync(configId, targetAreaType, targetSlotType, targetAnchorSlotIndex, itemUid);
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
            bool draggedWarehouse = self.DraggingIsWarehouse;
            self.IsDragging = false;
            self.DraggingIsWarehouse = false;
            self.DraggingItemUid = 0;
            self.DraggingConfigId = 0;
            self.DraggingAnchorSlotIndex = 0;
            self.DraggingAreaType = 0;
            self.DraggingFixedSlotType = 0;
            self.DraggingView = null;
            self.DragWorldOffset = Vector3.zero;

            if (draggedWarehouse)
            {
                self.RestoreWarehouseScrollInteraction();
            }
        }

        private static CanvasGroup EnsureCanvasGroup(RectTransform view)
        {
            if (view == null)
            {
                return null;
            }

            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
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

        private static void RecordWarehousePressState(this LobbyPanelComponent self, RectTransform view, LoadoutGridItemViewProxy proxy, PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null || proxy == null || eventData == null)
            {
                return;
            }

            self.WarehousePressView = view;
            self.WarehousePressItemUid = proxy.ItemUid;
            self.WarehousePressPointerId = eventData.pointerId;
            self.WarehousePressPosition = eventData.position;
            self.WarehousePressStartedAt = Time.unscaledTime;
            self.WarehouseScrollForwarding = false;
        }

        private static void ClearWarehousePressState(this LobbyPanelComponent self)
        {
            self.WarehousePressView = null;
            self.WarehousePressItemUid = 0;
            self.WarehousePressPointerId = -1;
            self.WarehousePressPosition = Vector2.zero;
            self.WarehousePressStartedAt = 0f;
        }

        private static bool ShouldBeginWarehouseItemDrag(this LobbyPanelComponent self, RectTransform view, LoadoutGridItemViewProxy proxy)
        {
            if (self == null || self.IsDisposed || view == null || proxy == null || !proxy.IsWarehouse)
            {
                return false;
            }

            if (self.WarehousePressView != view || self.WarehousePressItemUid != proxy.ItemUid || self.WarehousePressStartedAt <= 0f)
            {
                return false;
            }

            return Time.unscaledTime - self.WarehousePressStartedAt >= GetWarehouseHoldDuration();
        }

        private static void ForwardWarehouseInitializePotentialDrag(this LobbyPanelComponent self, PointerEventData eventData)
        {
            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect != null && eventData != null)
            {
                scrollRect.OnInitializePotentialDrag(eventData);
            }
        }

        private static void BeginWarehouseScrollForwarding(this LobbyPanelComponent self, PointerEventData eventData)
        {
            if (self.WarehouseScrollForwarding)
            {
                return;
            }

            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect == null || eventData == null)
            {
                return;
            }

            scrollRect.vertical = true;
            scrollRect.OnBeginDrag(eventData);
            self.WarehouseScrollForwarding = true;
        }

        private static void ForwardWarehouseDrag(this LobbyPanelComponent self, PointerEventData eventData)
        {
            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect == null || eventData == null)
            {
                return;
            }

            scrollRect.OnDrag(eventData);
        }

        private static void EndWarehouseScrollForwarding(this LobbyPanelComponent self, PointerEventData eventData)
        {
            if (!self.WarehouseScrollForwarding)
            {
                return;
            }

            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect != null && eventData != null)
            {
                scrollRect.OnEndDrag(eventData);
                scrollRect.vertical = true;
            }

            self.WarehouseScrollForwarding = false;
        }

        private static void StopWarehouseScrollForItemDrag(this LobbyPanelComponent self)
        {
            self.WarehouseScrollForwarding = false;
            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect == null)
            {
                return;
            }

            scrollRect.StopMovement();
            scrollRect.vertical = false;
        }

        private static void RestoreWarehouseScrollInteraction(this LobbyPanelComponent self)
        {
            self.WarehouseScrollForwarding = false;
            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect != null)
            {
                scrollRect.vertical = true;
            }
        }

        private static float GetWarehouseHoldDuration()
        {
            const float fallbackHoldDuration = 0.4f;

            Type inputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            if (inputSystemType == null)
            {
                return fallbackHoldDuration;
            }

            PropertyInfo settingsProperty = inputSystemType.GetProperty("settings", BindingFlags.Public | BindingFlags.Static);
            object settings = settingsProperty?.GetValue(null);
            if (settings == null)
            {
                return fallbackHoldDuration;
            }

            PropertyInfo holdTimeProperty = settings.GetType().GetProperty("defaultHoldTime", BindingFlags.Public | BindingFlags.Instance);
            if (holdTimeProperty?.GetValue(settings) is float holdTime && holdTime > 0f)
            {
                return holdTime;
            }

            return fallbackHoldDuration;
        }
    }
}
