using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(MainPanelComponent))]
    public static partial class MainPanelComponentSystem
    {
        private const float FpsRefreshInterval = 0.25f;
        private const float FpsSmoothFactor = 0.35f;

        [EntitySystem]
        private static void YIUIInitialize(this MainPanelComponent self)
        {
            // 找到摇杆并注入Entity引用
            JoystickView joystickView = self.UIBase.OwnerGameObject.GetComponentInChildren<JoystickView>(true);
            if (joystickView != null)
            {
                joystickView.SetEntity(self);
            }

            // 找到摇杆范围控件并设置引用
            JoystickRangeView joystickRangeView = self.UIBase.OwnerGameObject.GetComponentInChildren<JoystickRangeView>(true);
            if (joystickRangeView != null && joystickView != null)
            {
                // 设置摇杆背景和摇杆View的引用
                if (joystickRangeView.JoystickBackground == null)
                {
                    joystickRangeView.JoystickBackground = joystickView.Background;
                }
                if (joystickRangeView.JoystickView == null)
                {
                    joystickRangeView.JoystickView = joystickView;
                }
                if (joystickRangeView.RangeRect == null)
                {
                    joystickRangeView.RangeRect = joystickRangeView.GetComponent<RectTransform>();
                }

                joystickRangeView.ResetVisualState();
            }

            if (self.u_DataSearchingButton == null)
            {
                Log.Warning("[ECAClient][MainPanel] missing data: u_DataSearchingButton");
            }

            self.LastSearchButtonShow = false;
            self.LastOpenDoorButtonShow = false;
            self.LastFocusPointId = null;
            self.LastOpenContainerPointId = null;
            self.LastFocusButtonTextId = int.MinValue;
            self.LastFocusCanInteract = false;
            self.LastOpenDoorText = null;
            self.u_DataSearchingButton?.SetValue(false, true);
            self.u_DataOpenDoorText?.SetValue(string.Empty, true);
            self.BindSearchButtonUI();
            self.BindOpenDoorButtonUI();
            self.RefreshSearchButtonVisual(0, false);
            self.RefreshOpenDoorButton(false, string.Empty, false);
            self.BindRogueLevelBar();
            self.BindFpsCounter();
            self.ResetFpsCounter();
            self.BindMinimap();
            self.RefreshRogueLevelBar(true);
            self.RefreshMinimap(true);
            self.UIWeaponBar?.RefreshCurrentPlayerWeaponBar();
        }

        [EntitySystem]
        private static void Destroy(this MainPanelComponent self)
        {
            if (self.u_EventClickSearchingButton != null && self.u_EventClickSearchingButtonHandle != null)
            {
                self.u_EventClickSearchingButton.Remove(self.u_EventClickSearchingButtonHandle);
            }

            self.u_EventClickSearchingButtonHandle = null;
            self.u_EventClickSearchingButton = null;
            if (self.u_EventOpenedDoor != null && self.u_EventOpenedDoorHandle != null)
            {
                self.u_EventOpenedDoor.Remove(self.u_EventOpenedDoorHandle);
            }

            self.u_EventOpenedDoorHandle = null;
            self.u_EventOpenedDoor = null;
            if (self.u_EventClickBagButton != null && self.u_EventClickBagButtonHandle != null)
            {
                self.u_EventClickBagButton.Remove(self.u_EventClickBagButtonHandle);
            }

            self.u_EventClickBagButtonHandle = null;
            self.u_EventClickBagButton = null;
            if (self.u_EventClickOpenMap != null && self.u_EventClickOpenMapHandle != null)
            {
                self.u_EventClickOpenMap.Remove(self.u_EventClickOpenMapHandle);
            }

            self.u_EventClickOpenMapHandle = null;
            self.u_EventClickOpenMap = null;
            self.u_DataSearchingButton = null;
            self.u_DataOpenDoorText = null;
            self.SearchButton = null;
            self.SearchButtonText = null;
            self.OpenDoorButton = null;
            self.FpsCounterText = null;
            self.ClearMinimapMarkers();
            if (self.MinimapMarkerSprite != null)
            {
                UnityEngine.Object.Destroy(self.MinimapMarkerSprite);
            }

            if (self.MinimapFogTexture != null)
            {
                UnityEngine.Object.Destroy(self.MinimapFogTexture);
            }

            self.MinimapRoot = null;
            self.MinimapBackground = null;
            self.MinimapMask = null;
            self.MinimapTexture = null;
            self.MinimapFogOverlay = null;
            self.MinimapFogTexture = null;
            self.MinimapArrow = null;
            self.MinimapNameText = null;
            self.MinimapMarkerLayer = null;
            self.MinimapMarkerSprite = null;
            self.RogueLevelSlider = null;
            self.RogueLevelText = null;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this MainPanelComponent self)
        {
            self.LastSearchButtonShow = false;
            self.LastOpenDoorButtonShow = false;
            self.LastFocusPointId = null;
            self.LastOpenContainerPointId = null;
            self.LastFocusButtonTextId = int.MinValue;
            self.LastFocusCanInteract = false;
            self.LastOpenDoorText = null;
            self.DebugProbeFrame = 0;
            self.u_DataSearchingButton?.SetValue(false, true);
            self.u_DataOpenDoorText?.SetValue(string.Empty, true);
            self.BindSearchButtonUI();
            self.BindOpenDoorButtonUI();
            self.RefreshSearchButtonVisual(0, false);
            self.RefreshOpenDoorButton(false, string.Empty, false);
            self.BindFpsCounter();
            self.ResetFpsCounter();
            self.RefreshRogueLevelBar(true);
            self.BindMinimap();
            self.RefreshMinimap(true);
            self.UIWeaponBar?.RefreshCurrentPlayerWeaponBar();

            Scene root = self.Root();
            RogueClientComponent rogueRuntime = root?.GetComponent<RogueClientComponent>();
            if (rogueRuntime != null && rogueRuntime.ChoicePopupPending && rogueRuntime.ChoiceSerial > 0 && rogueRuntime.ChoiceOptions.Count > 0)
            {
                EventSystem.Instance.Publish(root, new EventRogueChoicePopup());
            }

            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void LateUpdate(this MainPanelComponent self)
        {
            self.UpdateFpsCounter();

            Scene root = self.Root();
            RogueClientComponent rogueRuntime = root?.GetComponent<RogueClientComponent>();
            if (rogueRuntime != null &&
                rogueRuntime.ChoicePopupPending &&
                !rogueRuntime.ChoicePopupOpening &&
                rogueRuntime.ChoiceSerial > 0 &&
                rogueRuntime.ChoiceOptions.Count > 0 &&
                root.YIUIMgr()?.GetPanel<RoguePanelComponent>() == null)
            {
                EventSystem.Instance.Publish(root, new EventRogueChoicePopup());
            }

            ECAInteractClientComponent runtime = root?.GetComponent<ECAInteractClientComponent>();
            string focusPointId = runtime?.FocusPointId;
            string openContainerPointId = runtime?.OpenContainerPointId;
            int buttonTextId = 0;
            bool canInteract = false;
            bool isDoorPoint = false;
            string openDoorText = string.Empty;

            if (runtime != null && string.IsNullOrWhiteSpace(focusPointId) && runtime.InRangePointIds.Count > 0)
            {
                foreach (string pointId in runtime.InRangePointIds)
                {
                    focusPointId = pointId;
                    runtime.FocusPointId = pointId;
                    break;
                }
            }

            if (runtime != null &&
                !string.IsNullOrWhiteSpace(openContainerPointId) &&
                !runtime.InRangePointIds.Contains(openContainerPointId))
            {
                Log.Info(
                    $"[ECAClient][MainPanel] clear stale open point: {openContainerPointId}, inRangeCount={runtime.InRangePointIds.Count}");
                runtime.OpenContainerPointId = null;
                runtime.OpenContainerUiKey = null;
                runtime.ContainerItems.Clear();
                openContainerPointId = null;
            }

            bool show = runtime != null &&
                runtime.InRangePointIds.Count > 0 &&
                string.IsNullOrWhiteSpace(openContainerPointId);

            if (show && !string.IsNullOrWhiteSpace(focusPointId))
            {
                runtime.PointButtonTextIds.TryGetValue(focusPointId, out buttonTextId);
                runtime.PointCanInteract.TryGetValue(focusPointId, out canInteract);
                isDoorPoint = TryGetFocusPointType(root, focusPointId, out int pointType) &&
                              (pointType == ECAPointType.Door || pointType == ECAPointType.KeyDoor);
                if (isDoorPoint)
                {
                    openDoorText = ResolveDoorText(runtime, focusPointId, pointType, canInteract, buttonTextId);
                }
            }

            bool showSearchButton = show && !isDoorPoint;
            bool showOpenDoorButton = show && isDoorPoint;

            if (self.LastSearchButtonShow != showSearchButton ||
                self.LastOpenDoorButtonShow != showOpenDoorButton ||
                self.LastFocusPointId != focusPointId ||
                self.LastOpenContainerPointId != openContainerPointId ||
                self.LastFocusButtonTextId != buttonTextId ||
                self.LastFocusCanInteract != canInteract ||
                self.LastOpenDoorText != openDoorText)
            {
                self.LastSearchButtonShow = showSearchButton;
                self.LastOpenDoorButtonShow = showOpenDoorButton;
                self.LastFocusPointId = focusPointId;
                self.LastOpenContainerPointId = openContainerPointId;
                self.LastFocusButtonTextId = buttonTextId;
                self.LastFocusCanInteract = canInteract;
                self.LastOpenDoorText = openDoorText;
            }

            if (self.u_DataSearchingButton != null && self.u_DataSearchingButton.GetValue() != showSearchButton)
            {
                self.u_DataSearchingButton.SetValue(showSearchButton);
            }

            self.RefreshSearchButtonVisual(buttonTextId, canInteract && showSearchButton);
            self.RefreshOpenDoorButton(showOpenDoorButton, openDoorText, canInteract);

            self.RefreshRogueLevelBar();
            self.RefreshMinimap();
        }

        #region YIUIEvent开始
        [YIUIInvoke(MainPanelComponent.OnEventClickSearchingButtonInvoke)]
        private static async ETTask OnEventClickSearchingButtonInvoke(this MainPanelComponent self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                ECAInteractClientComponent runtime = root.GetComponent<ECAInteractClientComponent>();
                if (runtime != null &&
                    !string.IsNullOrWhiteSpace(runtime.FocusPointId) &&
                    runtime.PointCanInteract.TryGetValue(runtime.FocusPointId, out bool canInteract) &&
                    !canInteract)
                {
                    await ETTask.CompletedTask;
                    return;
                }

                Log.Info("[ECAClient][MainPanel] click search button -> TryInteractFocus");
                ECAInteractHelper.TryInteractFocus(root).Coroutine();
            }

            await ETTask.CompletedTask;
        }

        [YIUIInvoke(MainPanelComponent.OnEventClickBagButtonInvoke)]
        private static async ETTask OnEventClickBagButtonInvoke(this MainPanelComponent self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                Log.Info("[ECAClient][MainPanel] click bag button -> OpenBagPanel");
                OpenBagPanel(root).Coroutine();
            }

            await ETTask.CompletedTask;
        }

        private static async ETTask OpenBagPanel(Scene root)
        {
            EntityRef<Scene> rootRef = root;
            await SyncBagData(root);
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            SearchPanelOpenContextComponent openContext = SearchPanelOpenContextHelper.GetOrAdd(root);
            openContext?.PrepareBackpackOpen();
            await root.YIUIRoot().OpenPanelAsync<SearchPanelComponent>();
        }

        private static async ETTask SyncBagData(Scene root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            C2M_SyncBagData request = C2M_SyncBagData.Create();
            EntityRef<Scene> rootRef = root;
            M2C_SyncBagData response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_SyncBagData;
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                Log.Warning("[ECAClient][Bag] sync bag failed: null response");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ECAClient][Bag] sync bag failed: error={response.Error}, msg={response.Message}");
                return;
            }

            ItemComponent itemComponent = root.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                Log.Warning("[ECAClient][Bag] sync bag failed: ItemComponent missing");
                return;
            }

            itemComponent.Clear();
            if (response.Width > 0 && response.Height > 0)
            {
                itemComponent.SetSize(response.Width, response.Height);
            }
            else
            {
                itemComponent.SetCapacity(response.Capacity);
            }
            foreach (ItemData itemData in response.Items)
            {
                itemComponent.UpdateItem(itemData.ItemId, itemData.SlotIndex, itemData.ConfigId, itemData.Count, itemData.GridWidth, itemData.GridHeight);
            }

            Log.Info($"[ECAClient][Bag] sync bag success: size={itemComponent.Width}x{itemComponent.Height}, itemCount={response.Items.Count}");
        }

        private static void BindRogueLevelBar(this MainPanelComponent self)
        {
            if (self.RogueLevelSlider != null && self.RogueLevelText != null)
            {
                return;
            }

            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return;
            }

            Transform levelBarTransform = rootTransform.Find("LevelBar");
            if (levelBarTransform == null)
            {
                return;
            }

            self.RogueLevelSlider = levelBarTransform.Find("Slider")?.GetComponent<Slider>();
            self.RogueLevelText = levelBarTransform.Find("Text (TMP)")?.GetComponent<TMP_Text>();
            if (self.RogueLevelText == null || !self.RogueLevelText.enabled)
            {
                TMP_Text[] levelTexts = levelBarTransform.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text levelText in levelTexts)
                {
                    if (levelText != null && levelText.enabled)
                    {
                        self.RogueLevelText = levelText;
                        break;
                    }
                }

                if (self.RogueLevelText == null && levelTexts.Length > 0)
                {
                    self.RogueLevelText = levelTexts[0];
                }
            }
        }

        private static void BindSearchButtonUI(this MainPanelComponent self)
        {
            if (self.SearchButton != null && self.SearchButtonText != null)
            {
                return;
            }

            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return;
            }

            Transform searchButtonTransform = rootTransform.Find("SearchButton");
            if (searchButtonTransform == null)
            {
                return;
            }

            self.SearchButton = searchButtonTransform.GetComponent<Button>();
            self.SearchButtonText = searchButtonTransform.GetComponentInChildren<TMP_Text>(true);
        }

        private static void BindOpenDoorButtonUI(this MainPanelComponent self)
        {
            if (self.OpenDoorButton != null)
            {
                return;
            }

            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return;
            }

            Transform openDoorButtonTransform = rootTransform.Find("OpenDoorButton");
            if (openDoorButtonTransform == null)
            {
                return;
            }

            self.OpenDoorButton = openDoorButtonTransform.GetComponent<Button>();
        }

        private static void RefreshSearchButtonVisual(this MainPanelComponent self, int buttonTextId, bool canInteract)
        {
            self.BindSearchButtonUI();
            if (self.SearchButton != null)
            {
                self.SearchButton.interactable = canInteract;
            }

            if (self.SearchButtonText != null)
            {
                self.SearchButtonText.text = ResolveText(buttonTextId);
            }
        }

        private static void RefreshOpenDoorButton(this MainPanelComponent self, bool show, string text, bool canInteract)
        {
            self.BindOpenDoorButtonUI();
            if (self.OpenDoorButton != null && self.OpenDoorButton.gameObject.activeSelf != show)
            {
                self.OpenDoorButton.gameObject.SetActive(show);
            }

            if (self.OpenDoorButton != null)
            {
                self.OpenDoorButton.interactable = canInteract;
            }

            self.u_DataOpenDoorText?.SetValue(show ? text ?? string.Empty : string.Empty);
        }

        private static string ResolveText(int textId)
        {
            if (textId <= 0)
            {
                return string.Empty;
            }

            TextConfig config = TextConfigCategory.Instance.GetOrDefault(textId);
            return config?.CN ?? string.Empty;
        }

        private static string ResolveDoorText(
            ECAInteractClientComponent runtime,
            string pointId,
            int pointType,
            bool canInteract,
            int buttonTextId)
        {
            string configuredText = ResolveText(buttonTextId);
            if (!string.IsNullOrWhiteSpace(configuredText))
            {
                return configuredText;
            }

            if (runtime == null || string.IsNullOrWhiteSpace(pointId))
            {
                return string.Empty;
            }

            runtime.PointStates.TryGetValue(pointId, out int state);
            if (state == ECADoorState.Opened)
            {
                return "关闭";
            }

            if (pointType == ECAPointType.KeyDoor && state == ECADoorState.Locked && !canInteract)
            {
                return "获得钥匙打开";
            }

            return "打开";
        }

        private static bool TryGetFocusPointType(Scene root, string pointId, out int pointType)
        {
            pointType = 0;
            if (root == null || root.IsDisposed || string.IsNullOrWhiteSpace(pointId))
            {
                return false;
            }

            Scene currentScene = root.CurrentScene();
            ECAPointViewRuntimeComponent runtime = currentScene?.GetComponent<ECAPointViewRuntimeComponent>();
            if (runtime == null || !runtime.PointViewMarkers.TryGetValue(pointId, out ECAPointViewMarker marker) || marker == null)
            {
                return false;
            }

            ECAPointMarker pointMarker = marker.GetComponent<ECAPointMarker>();
            if (pointMarker == null)
            {
                pointMarker = marker.GetComponentInParent<ECAPointMarker>();
            }

            if (pointMarker == null)
            {
                return false;
            }

            pointType = pointMarker.Type;
            return true;
        }

        private static void BindMinimap(this MainPanelComponent self)
        {
            if (self.MinimapRoot != null &&
                self.MinimapMask != null &&
                self.MinimapTexture != null &&
                self.MinimapFogOverlay != null &&
                self.MinimapArrow != null &&
                self.MinimapMarkerLayer != null)
            {
                return;
            }

            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return;
            }

            self.MinimapRoot = rootTransform.Find("Minimap") as RectTransform;
            if (self.MinimapRoot == null)
            {
                return;
            }

            self.MinimapBackground = self.MinimapRoot.Find("Minimap Background") as RectTransform;
            self.MinimapMask = self.MinimapRoot.Find("Minimap Background/Minimap Mask") as RectTransform;
            self.MinimapTexture = self.MinimapRoot.Find("Minimap Background/Minimap Mask/Minimap Texture")?.GetComponent<RawImage>();
            self.MinimapFogOverlay = self.MinimapRoot.Find("Minimap Background/Minimap Mask/Minimap Fog")?.GetComponent<RawImage>();
            self.MinimapArrow = self.MinimapRoot.Find("Minimap Background/Minimap Arrow")?.GetComponent<Image>();
            self.MinimapNameText = self.MinimapRoot.Find("Minimap Nameplate/Minimap Name Label")?.GetComponent<TMP_Text>();
            if (self.MinimapMask == null)
            {
                return;
            }

            if (self.MinimapFogOverlay == null)
            {
                GameObject fogObject = new GameObject("Minimap Fog", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                RectTransform fogRect = fogObject.GetComponent<RectTransform>();
                fogRect.SetParent(self.MinimapMask, false);
                fogRect.anchorMin = Vector2.zero;
                fogRect.anchorMax = Vector2.one;
                fogRect.offsetMin = Vector2.zero;
                fogRect.offsetMax = Vector2.zero;
                if (self.MinimapTexture != null)
                {
                    fogRect.SetSiblingIndex(self.MinimapTexture.rectTransform.GetSiblingIndex() + 1);
                }

                RawImage fogImage = fogObject.GetComponent<RawImage>();
                fogImage.raycastTarget = false;
                self.MinimapFogOverlay = fogImage;
            }

            Transform markerLayerTransform = self.MinimapMask.Find("MarkerLayer");
            if (markerLayerTransform == null)
            {
                GameObject markerLayerObject = new GameObject("MarkerLayer", typeof(RectTransform));
                RectTransform markerLayer = markerLayerObject.GetComponent<RectTransform>();
                markerLayer.SetParent(self.MinimapMask, false);
                markerLayer.anchorMin = Vector2.zero;
                markerLayer.anchorMax = Vector2.one;
                markerLayer.offsetMin = Vector2.zero;
                markerLayer.offsetMax = Vector2.zero;
                markerLayer.SetSiblingIndex(self.MinimapMask.childCount - 1);
                self.MinimapMarkerLayer = markerLayer;
            }
            else
            {
                self.MinimapMarkerLayer = markerLayerTransform as RectTransform;
            }
        }

        private static void RefreshMinimap(this MainPanelComponent self, bool force = false)
        {
            self.BindMinimap();
            if (self.MinimapRoot == null || self.MinimapMask == null || self.MinimapMarkerLayer == null)
            {
                return;
            }

            MinimapRuntimeComponent runtime = self.Root()?.CurrentScene()?.GetComponent<MinimapRuntimeComponent>();
            if (runtime == null)
            {
                self.ClearMinimapMarkers();
                return;
            }

            if (self.MinimapNameText != null && (force || self.MinimapNameText.text != runtime.MapName))
            {
                self.MinimapNameText.text = runtime.MapName;
            }

            if (!runtime.TryGetMyPosition(out float3 myPosition))
            {
                self.ClearMinimapMarkers();
                return;
            }

            self.RefreshMinimapArrow(runtime);
            self.RefreshMinimapTexture(runtime, myPosition);
            runtime.RefreshLocalFog();
            self.RefreshMinimapFog(runtime);
            self.RefreshMinimapMarkers(runtime, myPosition);
        }

        private static void RefreshMinimapArrow(this MainPanelComponent self, MinimapRuntimeComponent runtime)
        {
            if (self.MinimapArrow == null)
            {
                return;
            }

            string colorText = global::ET.MinimapConstConfigHelper.GetString(global::ET.MinimapConstKey.MarkerColorSelf, "#FFFFFF");
            if (ColorUtility.TryParseHtmlString(colorText, out Color arrowColor))
            {
                self.MinimapArrow.color = arrowColor;
            }

            Unit myUnit = runtime.GetMyUnit();
            if (myUnit == null || myUnit.IsDisposed)
            {
                return;
            }

            Vector3 forward = myUnit.Forward;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            self.MinimapArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        private static void RefreshMinimapTexture(this MainPanelComponent self, MinimapRuntimeComponent runtime, Vector3 myPosition)
        {
            if (self.MinimapTexture == null)
            {
                return;
            }

            float width = runtime.WorldMaxX - runtime.WorldMinX;
            float height = runtime.WorldMaxZ - runtime.WorldMinZ;
            if (width <= 0f || height <= 0f)
            {
                self.MinimapTexture.uvRect = new Rect(0f, 0f, 1f, 1f);
                if (self.MinimapFogOverlay != null)
                {
                    self.MinimapFogOverlay.uvRect = self.MinimapTexture.uvRect;
                }
                return;
            }

            Vector2 center = runtime.WorldToNormalizedPosition(myPosition);
            float uvWidth = Mathf.Clamp01((runtime.CompactRange * 2f) / width);
            float uvHeight = Mathf.Clamp01((runtime.CompactRange * 2f) / height);
            float x = Mathf.Clamp01(center.x - uvWidth * 0.5f);
            float y = Mathf.Clamp01(center.y - uvHeight * 0.5f);

            if (x + uvWidth > 1f)
            {
                x = 1f - uvWidth;
            }

            if (y + uvHeight > 1f)
            {
                y = 1f - uvHeight;
            }

            self.MinimapTexture.uvRect = new Rect(x, y, uvWidth, uvHeight);
            if (self.MinimapFogOverlay != null)
            {
                self.MinimapFogOverlay.uvRect = self.MinimapTexture.uvRect;
            }
        }

        private static void RefreshMinimapFog(this MainPanelComponent self, MinimapRuntimeComponent runtime)
        {
            if (self.MinimapFogOverlay == null)
            {
                return;
            }

            if (!runtime.TryGetFogGridSize(out int gridWidth, out int gridHeight))
            {
                self.MinimapFogOverlay.texture = null;
                self.MinimapFogOverlay.gameObject.SetActive(false);
                return;
            }

            self.EnsureMinimapFogTexture(gridWidth, gridHeight);
            if (self.MinimapFogTexture == null)
            {
                self.MinimapFogOverlay.texture = null;
                self.MinimapFogOverlay.gameObject.SetActive(false);
                return;
            }

            Color32 visibleColor = self.ResolveMinimapFogColor(global::ET.MinimapConstKey.FogColorVisible, "#00000000");
            Color32 exploredColor = self.ResolveMinimapFogColor(global::ET.MinimapConstKey.FogColorExplored, "#09131E88");
            Color32 unexploredColor = self.ResolveMinimapFogColor(global::ET.MinimapConstKey.FogColorUnexplored, "#09131EE8");
            Color32[] colors = new Color32[gridWidth * gridHeight];

            for (int i = 0; i < colors.Length; ++i)
            {
                if (runtime.CurrentVisibleCells.Contains(i))
                {
                    colors[i] = visibleColor;
                    continue;
                }

                colors[i] = runtime.ExploredCells.Contains(i) ? exploredColor : unexploredColor;
            }

            self.MinimapFogTexture.SetPixels32(colors);
            self.MinimapFogTexture.Apply(false, false);
            self.MinimapFogOverlay.texture = self.MinimapFogTexture;
            self.MinimapFogOverlay.color = Color.white;
            self.MinimapFogOverlay.gameObject.SetActive(true);
        }

        private static void EnsureMinimapFogTexture(this MainPanelComponent self, int width, int height)
        {
            if (self.MinimapFogTexture != null &&
                self.MinimapFogTexture.width == width &&
                self.MinimapFogTexture.height == height)
            {
                return;
            }

            if (self.MinimapFogTexture != null)
            {
                UnityEngine.Object.Destroy(self.MinimapFogTexture);
            }

            self.MinimapFogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        private static void RefreshMinimapMarkers(this MainPanelComponent self, MinimapRuntimeComponent runtime, Vector3 myPosition)
        {
            HashSet<long> activeMarkerIds = new HashSet<long>();
            float markerSize = Mathf.Max(global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.MarkerSize, 10f), 4f);
            float radius = Mathf.Min(self.MinimapMask.rect.width, self.MinimapMask.rect.height) * 0.5f - markerSize;

            foreach (KeyValuePair<long, MinimapMarkerRuntime> pair in runtime.GetMarkers())
            {
                MinimapMarkerRuntime marker = pair.Value;
                if (marker.UnitId == runtime.MyUnitId)
                {
                    continue;
                }

                if (!MinimapRuntimeMarkerHelper.TryWorldToCompactLocalPosition(runtime, myPosition, marker.Position, out float2 localPosition))
                {
                    self.HideMinimapMarker(marker.UnitId);
                    continue;
                }

                RectTransform markerRect = self.GetOrCreateMinimapMarker(marker.UnitId);
                if (markerRect == null)
                {
                    continue;
                }

                activeMarkerIds.Add(marker.UnitId);
                markerRect.gameObject.SetActive(true);
                markerRect.sizeDelta = new Vector2(markerSize, markerSize);
                markerRect.anchoredPosition = new Vector2(localPosition.x * radius, localPosition.y * radius);

                if (self.MinimapMarkerImages.TryGetValue(marker.UnitId, out Image markerImage) && markerImage != null)
                {
                    markerImage.color = self.ResolveMinimapMarkerColor(runtime, marker);
                }
            }

            foreach (KeyValuePair<long, RectTransform> pair in self.MinimapMarkerRects)
            {
                if (!activeMarkerIds.Contains(pair.Key) && pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }
        }

        private static RectTransform GetOrCreateMinimapMarker(this MainPanelComponent self, long unitId)
        {
            if (self.MinimapMarkerRects.TryGetValue(unitId, out RectTransform markerRect) && markerRect != null)
            {
                return markerRect;
            }

            if (self.MinimapMarkerLayer == null)
            {
                return null;
            }

            GameObject markerObject = new GameObject($"Marker_{unitId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.SetParent(self.MinimapMarkerLayer, false);
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);

            Image markerImage = markerObject.GetComponent<Image>();
            markerImage.raycastTarget = false;
            markerImage.sprite = self.GetMinimapMarkerSprite();
            markerImage.type = Image.Type.Simple;

            self.MinimapMarkerRects[unitId] = markerRect;
            self.MinimapMarkerImages[unitId] = markerImage;
            return markerRect;
        }

        private static void HideMinimapMarker(this MainPanelComponent self, long unitId)
        {
            if (self.MinimapMarkerRects.TryGetValue(unitId, out RectTransform markerRect) && markerRect != null)
            {
                markerRect.gameObject.SetActive(false);
            }
        }

        private static void ClearMinimapMarkers(this MainPanelComponent self)
        {
            foreach (KeyValuePair<long, RectTransform> pair in self.MinimapMarkerRects)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.gameObject);
                }
            }

            self.MinimapMarkerRects.Clear();
            self.MinimapMarkerImages.Clear();
        }

        private static Sprite GetMinimapMarkerSprite(this MainPanelComponent self)
        {
            if (self.MinimapMarkerSprite != null)
            {
                return self.MinimapMarkerSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            self.MinimapMarkerSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            return self.MinimapMarkerSprite;
        }

        private static Color ResolveMinimapMarkerColor(this MainPanelComponent self, MinimapRuntimeComponent runtime, MinimapMarkerRuntime marker)
        {
            string key = global::ET.MinimapConstKey.MarkerColorOther;
            switch (marker.UnitType)
            {
                case UnitType.Player:
                    key = self.ResolvePlayerMarkerColorKey(runtime, marker);
                    break;
                case UnitType.Monster:
                    key = global::ET.MinimapConstKey.MarkerColorMonster;
                    break;
            }

            string colorText = global::ET.MinimapConstConfigHelper.GetString(key, "#FFFFFF");
            if (ColorUtility.TryParseHtmlString(colorText, out Color color))
            {
                return color;
            }

            return Color.white;
        }

        private static string ResolvePlayerMarkerColorKey(this MainPanelComponent self, MinimapRuntimeComponent runtime, MinimapMarkerRuntime marker)
        {
            Unit myUnit = runtime?.GetMyUnit();
            if (myUnit == null || myUnit.IsDisposed)
            {
                return global::ET.MinimapConstKey.MarkerColorPlayer;
            }

            Scene scene = runtime.GetParent<Scene>();
            Unit targetUnit = scene?.GetComponent<UnitComponent>()?.Get(marker.UnitId);
            if (targetUnit == null || targetUnit.IsDisposed)
            {
                return global::ET.MinimapConstKey.MarkerColorPlayer;
            }

            CampRelation relation = CampHelper.GetRelation(myUnit, targetUnit);
            switch (relation)
            {
                case CampRelation.Friendly:
                    return global::ET.MinimapConstKey.MarkerColorFriendlyPlayer;
                case CampRelation.Enemy:
                    return global::ET.MinimapConstKey.MarkerColorEnemyPlayer;
                default:
                    return global::ET.MinimapConstKey.MarkerColorPlayer;
            }
        }

        private static Color32 ResolveMinimapFogColor(this MainPanelComponent self, string key, string defaultColor)
        {
            string colorText = global::ET.MinimapConstConfigHelper.GetString(key, defaultColor);
            if (ColorUtility.TryParseHtmlString(colorText, out Color color))
            {
                return color;
            }

            return Color.clear;
        }

        private static void RefreshRogueLevelBar(this MainPanelComponent self, bool force = false)
        {
            self.BindRogueLevelBar();

            RogueClientComponent runtime = self.Root()?.GetComponent<RogueClientComponent>();
            if (runtime == null)
            {
                return;
            }

            if (!force &&
                self.LastRogueLevel == runtime.Level &&
                self.LastRogueCurrentExp == runtime.CurrentExp &&
                self.LastRogueNeedExp == runtime.NeedExp)
            {
                return;
            }

            self.LastRogueLevel = runtime.Level;
            self.LastRogueCurrentExp = runtime.CurrentExp;
            self.LastRogueNeedExp = runtime.NeedExp;
            int safeNeedExp = Mathf.Max(runtime.NeedExp, 1);
            float normalizedExp = Mathf.Clamp01(runtime.CurrentExp / (float)safeNeedExp);

            if (self.RogueLevelSlider != null)
            {
                // 预制体里 Slider 通过 u_DataCurExp(0~1) 绑定，保持同一语义避免数值冲突。
                self.RogueLevelSlider.minValue = 0f;
                self.RogueLevelSlider.maxValue = 1f;
                self.RogueLevelSlider.value = normalizedExp;
            }

            if (self.RogueLevelText != null)
            {
                self.RogueLevelText.text = runtime.Level.ToString();
            }

            // YIUI 数据绑定兜底：部分预制体文本/进度条可能只监听 DataValue
            self.u_DataTxtLevel?.SetValue(runtime.Level.ToString(), true);
            self.u_DataCurExp?.SetValue(normalizedExp, true);
        }

        private static void BindFpsCounter(this MainPanelComponent self)
        {
            if (self.FpsCounterText != null)
            {
                return;
            }

            GameObject ownerGameObject = self.UIBase?.OwnerGameObject;
            if (ownerGameObject == null)
            {
                return;
            }

            foreach (TMP_Text text in ownerGameObject.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text.gameObject.name == "FPS Counter")
                {
                    self.FpsCounterText = text;
                    break;
                }
            }

            if (self.FpsCounterText == null)
            {
                Log.Warning("[MainPanel] missing fps counter text node: FPS Counter");
            }
        }

        private static void ResetFpsCounter(this MainPanelComponent self)
        {
            self.FpsAccumulatedTime = 0f;
            self.FpsAccumulatedFrames = 0;
            self.SmoothedFps = 0f;
            self.LastDisplayedFps = int.MinValue;

            if (self.FpsCounterText != null)
            {
                self.FpsCounterText.text = "FPS: --";
            }
        }

        private static void UpdateFpsCounter(this MainPanelComponent self)
        {
            if (self.FpsCounterText == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            self.FpsAccumulatedTime += deltaTime;
            self.FpsAccumulatedFrames += 1;
            if (self.FpsAccumulatedTime < FpsRefreshInterval)
            {
                return;
            }

            float intervalFps = self.FpsAccumulatedFrames / self.FpsAccumulatedTime;
            if (self.SmoothedFps <= 0f)
            {
                self.SmoothedFps = intervalFps;
            }
            else
            {
                self.SmoothedFps = Mathf.Lerp(self.SmoothedFps, intervalFps, FpsSmoothFactor);
            }

            int displayFps = Mathf.Max(0, Mathf.RoundToInt(self.SmoothedFps));
            if (displayFps != self.LastDisplayedFps)
            {
                self.FpsCounterText.text = $"FPS: {displayFps}";
                self.LastDisplayedFps = displayFps;
            }

            self.FpsAccumulatedTime = 0f;
            self.FpsAccumulatedFrames = 0;
        }
        
        [YIUIInvoke(MainPanelComponent.OnEventClickOpenMapInvoke)]
        private static async ETTask OnEventClickOpenMapInvoke(this MainPanelComponent self)
        {
            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            await root.YIUIRoot().OpenPanelAsync<MapWorldPanelComponent>();
        }
        
        [YIUIInvoke(MainPanelComponent.OnEventOpenedDoorInvoke)]
        private static async ETTask OnEventOpenedDoorInvoke(this MainPanelComponent self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                ECAInteractClientComponent runtime = root.GetComponent<ECAInteractClientComponent>();
                string focusPointId = runtime?.FocusPointId;
                int pointState = 0;
                if (runtime != null && !string.IsNullOrWhiteSpace(focusPointId))
                {
                    runtime.PointStates.TryGetValue(focusPointId, out pointState);
                }

                Log.Info($"[ECAClient][DoorButton] click focus={focusPointId ?? "null"}, state={pointState}");
                if (runtime != null &&
                    !string.IsNullOrWhiteSpace(runtime.FocusPointId) &&
                    runtime.PointCanInteract.TryGetValue(runtime.FocusPointId, out bool canInteract) &&
                    !canInteract)
                {
                    Log.Info($"[ECAClient][DoorButton] click blocked: focus={runtime.FocusPointId}, canInteract=false, state={pointState}");
                    await ETTask.CompletedTask;
                    return;
                }

                Log.Info("[ECAClient][MainPanel] click open door button -> TryInteractFocus");
                ECAInteractHelper.TryInteractFocus(root).Coroutine();
            }

            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束
    }
}
