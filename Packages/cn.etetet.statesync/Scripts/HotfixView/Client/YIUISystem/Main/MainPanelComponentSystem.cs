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
        private const string PickupHintRootName = "PickupHintRoot";
        private const string GroundDropPointPrefix = "ground_drop_";
        private const float PickupHintCardWidth = 328f;
        private const float PickupHintCardHeight = 72f;
        private const float PickupHintScreenYOffset = 44f;
        private const float PickupHintClampPadding = 10f;
        private const float PickupHintOverlapThresholdX = 220f;
        private const float PickupHintOverlapThresholdY = 90f;
        private const float PickupHintOverlapOffsetX = 28f;
        private const float PickupHintOverlapOffsetY = 52f;
        private const int PickupHintMaxOverlapLevel = 8;

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
            self.BindExitButtonUI();
            self.ResetQuickExitButtonState(true);
            self.RefreshSearchButtonVisual(0, false);
            self.RefreshOpenDoorButton(false, string.Empty, false);
            self.BindRogueLevelBar();
            self.BindRogueEffectUI();
            self.HideRogueEffectDesc(true);
            self.RefreshRogueEffectPanel(true);
            self.BindFpsCounter();
            self.ResetFpsCounter();
            BindRunTimeLimitUi(self, true);
            self.BindMinimap();
            self.BindHitDirectionUI();
            self.RefreshRogueLevelBar(true);
            self.RefreshMinimap(true);
            self.UIWeaponBar?.RefreshCurrentPlayerWeaponBar();
            self.ReleaseAllPickupHintCommons();
            self.LastEvacuateTipsVisible = false;
            self.LastEvacuationPointId = null;
            self.LastEvacuateRemainSeconds = int.MinValue;
            self.IsEvacuateTipsOpening = false;
            self.LastRunTimeLimitVisible = false;
            self.LastRunTimeLimitRemainSeconds = long.MinValue;
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
            if (self.ExitButton != null)
            {
                self.ExitButton.onClick.RemoveAllListeners();
            }

            self.SearchButton = null;
            self.SearchButtonText = null;
            self.OpenDoorButton = null;
            self.ExitButton = null;
            self.IsQuickExitRequesting = false;
            self.FpsCounterText = null;
            self.ReleaseAllRogueEffectButtonSprites();
            self.ReleaseAllMinimapMarkerSprites();
            self.ReleaseAllMinimapPoiSprites();
            self.ClearRogueEffectButtons();
            self.ClearMinimapMarkers();
            self.ClearMinimapPois();
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
            self.MinimapPoiLayer = null;
            self.MinimapTrackedPoiLayer = null;
            self.MinimapMarkerSprite = null;
            self.MinimapTrackedPoiRect = null;
            self.MinimapTrackedPoiImage = null;
            self.MinimapTrackedPoiDesiredSpriteName = null;
            self.ClearHitDirectionIndicators();
            if (self.HitDirectionRoot != null)
            {
                UnityEngine.Object.Destroy(self.HitDirectionRoot.gameObject);
            }

            if (self.HitDirectionSprite != null)
            {
                UnityEngine.Object.Destroy(self.HitDirectionSprite.texture);
                UnityEngine.Object.Destroy(self.HitDirectionSprite);
            }

            self.HitDirectionRoot = null;
            self.HitDirectionSprite = null;
            self.RogueLevelSlider = null;
            self.RogueLevelText = null;
            self.RogueEffectRoot = null;
            self.RogueEffectTextRect = null;
            self.RogueEffectText = null;
            self.LastRogueEffectSignature = int.MinValue;
            self.RogueEffectPreviewIndex = -1;
            self.ReleaseAllPickupHintCommons();
            self.EvacuateTipsViewRef = default;
            self.IsEvacuateTipsOpening = false;
            self.LastEvacuateTipsVisible = false;
            self.LastEvacuationPointId = null;
            self.LastEvacuateRemainSeconds = int.MinValue;
            self.LastRunTimeLimitVisible = false;
            self.LastRunTimeLimitRemainSeconds = long.MinValue;
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
            self.BindExitButtonUI();
            self.ResetQuickExitButtonState(true);
            self.RefreshSearchButtonVisual(0, false);
            self.RefreshOpenDoorButton(false, string.Empty, false);
            self.BindRogueEffectUI();
            self.HideRogueEffectDesc(true);
            self.RefreshRogueEffectPanel(true);
            self.BindFpsCounter();
            self.ResetFpsCounter();
            BindRunTimeLimitUi(self, true);
            self.RefreshRogueLevelBar(true);
            self.BindMinimap();
            self.BindHitDirectionUI();
            self.RefreshMinimap(true);
            self.UIWeaponBar?.RefreshCurrentPlayerWeaponBar();
            self.ReleaseAllPickupHintCommons();
            self.LastEvacuateTipsVisible = false;
            self.LastEvacuationPointId = null;
            self.LastEvacuateRemainSeconds = int.MinValue;
            self.IsEvacuateTipsOpening = false;
            self.LastRunTimeLimitVisible = false;
            self.LastRunTimeLimitRemainSeconds = long.MinValue;

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
            self.RefreshHitDirectionIndicators();

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
            bool isGroundDropPoint = false;
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
                isGroundDropPoint = focusPointId.StartsWith("ground_drop_", StringComparison.Ordinal);
                isDoorPoint = TryGetFocusPointType(root, focusPointId, out int pointType) &&
                              (pointType == ECAPointType.Door || pointType == ECAPointType.KeyDoor);
                if (isDoorPoint)
                {
                    openDoorText = ResolveDoorText(runtime, focusPointId, pointType, canInteract, buttonTextId);
                }
            }

            bool showSearchButton = show && !isDoorPoint && !isGroundDropPoint;
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
            self.RefreshPickupHintCommons(runtime);

            self.RefreshRogueEffectPanel();
            self.TryCloseRogueEffectDescOnOutsideClick();
            self.RefreshMinimap();
            self.RefreshEvacuateTips(runtime);
            self.RefreshRunTimeLimit(root?.GetComponent<RunTimeLimitClientComponent>());
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

                if (RogueMissionTaskPopupComponentSystem.TryOpenFocusPoint(root))
                {
                    await ETTask.CompletedTask;
                    return;
                }

                // GroundDrop 物品 → 一键拾取（不打开SearchPanel）
                string focusPointId = runtime?.FocusPointId;
                if (!string.IsNullOrWhiteSpace(focusPointId) && focusPointId.StartsWith("ground_drop_"))
                {
                    Log.Info($"[ECAClient][MainPanel] click search button -> PickupGroundItem: {focusPointId}");
                    GroundItemPickupClientHelper.RequestPickupGroundItem(root, focusPointId).Coroutine();
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

        private static async ETTask OnClickExitButton(EntityRef<MainPanelComponent> selfRef)
        {
            MainPanelComponent self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (self.IsQuickExitRequesting)
            {
                return;
            }

            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self.IsQuickExitRequesting = true;
            self.ResetQuickExitButtonState(false);
            Log.Info("[ECAClient][MainPanel] send quick exit extraction request");

            try
            {
                M2C_QuickExitExtraction response =
                        await root.GetComponent<ClientSenderComponent>().Call(C2M_QuickExitExtraction.Create()) as M2C_QuickExitExtraction;
                self = selfRef;
                if (self == null || self.IsDisposed)
                {
                    return;
                }

                if (response == null)
                {
                    Log.Warning("[ECAClient][MainPanel] quick exit extraction failed: null response");
                    self.ResetQuickExitButtonState(true);
                    return;
                }

                if (response.Error != ErrorCode.ERR_Success)
                {
                    Log.Warning(
                        $"[ECAClient][MainPanel] quick exit extraction failed: error={response.Error}, msg={response.Message}");
                    self.ResetQuickExitButtonState(true);
                    return;
                }

                Log.Info("[ECAClient][MainPanel] quick exit extraction request accepted");
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null && !self.IsDisposed)
                {
                    self.ResetQuickExitButtonState(true);
                }

                Log.Error(e);
            }
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

        private static void BindExitButtonUI(this MainPanelComponent self)
        {
            if (self.ExitButton != null)
            {
                return;
            }

            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return;
            }

            Transform exitButtonTransform = rootTransform.Find("ExitButton");
            if (exitButtonTransform == null)
            {
                Log.Warning("[ECAClient][MainPanel] missing ExitButton");
                return;
            }

            self.ExitButton = exitButtonTransform.GetComponent<Button>();
            if (self.ExitButton == null)
            {
                Log.Warning("[ECAClient][MainPanel] ExitButton missing Button component");
                return;
            }

            EntityRef<MainPanelComponent> selfRef = self;
            self.ExitButton.onClick.AddListener(() => OnClickExitButton(selfRef).Coroutine());
        }

        private static void ResetQuickExitButtonState(this MainPanelComponent self, bool resetRequesting)
        {
            if (resetRequesting)
            {
                self.IsQuickExitRequesting = false;
            }

            if (self.ExitButton != null)
            {
                self.ExitButton.interactable = !self.IsQuickExitRequesting;
            }
        }

        private static void BindRogueEffectUI(this MainPanelComponent self)
        {
            self.RogueEffectRoot ??= self.u_ComRogueEffectRectTransform;
            self.RogueEffectTextRect ??= self.u_ComRogueEffectTextRectTransform;
            if (self.RogueEffectTextRect != null && self.RogueEffectText == null)
            {
                self.RogueEffectText = self.RogueEffectTextRect.GetComponent<TMP_Text>();
                self.RogueEffectText ??= self.RogueEffectTextRect.GetComponentInChildren<TMP_Text>(true);
            }

            if (self.RogueEffectRoot == null || self.RogueEffectButtons.Count > 0)
            {
                return;
            }

            List<Button> buttons = new();
            self.TryAddRogueEffectButton(buttons, self.u_ComEffectButton1);
            self.TryAddRogueEffectButton(buttons, self.u_ComEffectButton2);
            self.TryAddRogueEffectButton(buttons, self.u_ComEffectButton3);
            self.TryAddRogueEffectButton(buttons, self.u_ComEffectButton4);

            if (buttons.Count == 0)
            {
                foreach (Button button in self.RogueEffectRoot.GetComponentsInChildren<Button>(true))
                {
                    if (button != null && button.transform.parent == self.RogueEffectRoot)
                    {
                        buttons.Add(button);
                    }
                }
            }

            buttons.Sort((left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            foreach (Button button in buttons)
            {
                self.RegisterRogueEffectButton(button);
            }
        }

        private static void TryAddRogueEffectButton(this MainPanelComponent self, List<Button> buttons, RectTransform buttonRect)
        {
            Button button = buttonRect?.GetComponent<Button>();
            if (button != null && !buttons.Contains(button))
            {
                buttons.Add(button);
            }
        }

        private static void RefreshRogueEffectPanel(this MainPanelComponent self, bool force = false)
        {
            self.BindRogueEffectUI();
            if (self.RogueEffectRoot == null)
            {
                return;
            }

            RogueClientComponent runtime = self.Root()?.GetComponent<RogueClientComponent>();
            int selectedCount = runtime?.SelectedOptions?.Count ?? 0;
            bool hasSelectedOptions = selectedCount > 0;
            if (self.RogueEffectRoot.gameObject.activeSelf != hasSelectedOptions)
            {
                self.RogueEffectRoot.gameObject.SetActive(hasSelectedOptions);
            }

            if (!hasSelectedOptions)
            {
                self.LastRogueEffectSignature = 0;
                self.HideRogueEffectDesc(true);
                self.DisableUnusedRogueEffectButtons(0);
                return;
            }

            int signature = ComputeRogueEffectSignature(runtime.SelectedOptions);
            if (!force && self.LastRogueEffectSignature == signature)
            {
                return;
            }

            self.LastRogueEffectSignature = signature;
            self.HideRogueEffectDesc(true);
            self.EnsureRogueEffectButtonPool(selectedCount);
            self.RebindRogueEffectButtonListeners();

            int bindCount = Mathf.Min(selectedCount, self.RogueEffectButtons.Count);
            for (int i = 0; i < bindCount; ++i)
            {
                Button button = self.RogueEffectButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(true);
                button.interactable = true;
                Image buttonImage = self.GetRogueEffectButtonImage(i);
                self.PrepareRogueEffectButtonVisual(button, buttonImage);
                self.SetRogueEffectButtonSprite(i, runtime.SelectedOptions[i].ImagePath ?? string.Empty).Coroutine();
            }

            self.DisableUnusedRogueEffectButtons(bindCount);
        }

        private static void EnsureRogueEffectButtonPool(this MainPanelComponent self, int count)
        {
            self.BindRogueEffectUI();
            if (self.RogueEffectRoot == null || count <= 0)
            {
                return;
            }

            while (self.RogueEffectButtons.Count < count)
            {
                Button templateButton = self.RogueEffectButtons.Count > 0 ? self.RogueEffectButtons[0] : null;
                if (templateButton == null)
                {
                    Log.Warning("[MainPanel] missing rogue effect button template");
                    return;
                }

                GameObject cloneObject = UnityEngine.Object.Instantiate(templateButton.gameObject, self.RogueEffectRoot);
                cloneObject.name = $"RogueEffectButton_{self.RogueEffectButtons.Count}";
                cloneObject.SetActive(false);
                cloneObject.transform.SetSiblingIndex(self.RogueEffectButtons.Count);
                self.RogueEffectDynamicButtons.Add(cloneObject);
                self.RegisterRogueEffectButton(cloneObject.GetComponent<Button>());
            }
        }

        private static void RegisterRogueEffectButton(this MainPanelComponent self, Button button)
        {
            if (button == null || self.RogueEffectButtons.Contains(button))
            {
                return;
            }

            Image buttonImage = ResolveRogueEffectButtonImage(button);
            self.RogueEffectButtons.Add(button);
            self.RogueEffectButtonImages.Add(buttonImage);
            self.RogueEffectButtonSprites.Add(null);
            self.RogueEffectButtonSpriteNames.Add(string.Empty);
            self.RogueEffectButtonDesiredSpriteNames.Add(string.Empty);
            self.PrepareRogueEffectButtonVisual(button, buttonImage);
        }

        private static void PrepareRogueEffectButtonVisual(this MainPanelComponent self, Button button, Image buttonImage)
        {
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = button != null && ReferenceEquals(buttonImage, button.targetGraphic);
                buttonImage.preserveAspect = true;
                buttonImage.color = Color.white;
            }

            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = string.Empty;
                text.gameObject.SetActive(false);
            }
        }

        private static void RebindRogueEffectButtonListeners(this MainPanelComponent self)
        {
            EntityRef<MainPanelComponent> selfRef = self;
            for (int i = 0; i < self.RogueEffectButtons.Count; ++i)
            {
                Button button = self.RogueEffectButtons[i];
                if (button == null)
                {
                    continue;
                }

                int buttonIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => HandleRogueEffectButtonClick(selfRef, buttonIndex));
            }
        }

        private static void HandleRogueEffectButtonClick(EntityRef<MainPanelComponent> selfRef, int buttonIndex)
        {
            MainPanelComponent self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.OnRogueEffectButtonClicked(buttonIndex);
        }

        private static void OnRogueEffectButtonClicked(this MainPanelComponent self, int buttonIndex)
        {
            RogueClientComponent runtime = self.Root()?.GetComponent<RogueClientComponent>();
            if (runtime == null || buttonIndex < 0 || buttonIndex >= runtime.SelectedOptions.Count)
            {
                self.HideRogueEffectDesc(true);
                return;
            }

            self.BindRogueEffectUI();
            if (self.RogueEffectTextRect == null || self.RogueEffectText == null)
            {
                return;
            }

            self.RogueEffectPreviewIndex = buttonIndex;
            self.RogueEffectText.text = ResolveRogueEffectDesc(runtime.SelectedOptions[buttonIndex]);
            if (!self.RogueEffectTextRect.gameObject.activeSelf)
            {
                self.RogueEffectTextRect.gameObject.SetActive(true);
            }
        }

        private static void TryCloseRogueEffectDescOnOutsideClick(this MainPanelComponent self)
        {
            if (self.RogueEffectPreviewIndex < 0 || self.RogueEffectTextRect == null || !self.RogueEffectTextRect.gameObject.activeInHierarchy)
            {
                return;
            }

            if (!TryGetPointerDownScreenPosition(out Vector2 screenPosition))
            {
                return;
            }

            Camera uiCamera = self.ResolveUICamera();
            if (RectTransformUtility.RectangleContainsScreenPoint(self.RogueEffectTextRect, screenPosition, uiCamera))
            {
                return;
            }

            foreach (Button button in self.RogueEffectButtons)
            {
                if (button == null || !button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                RectTransform buttonRect = button.transform as RectTransform;
                if (buttonRect != null && RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPosition, uiCamera))
                {
                    return;
                }
            }

            self.HideRogueEffectDesc(true);
        }

        private static void HideRogueEffectDesc(this MainPanelComponent self, bool clearText)
        {
            self.RogueEffectPreviewIndex = -1;
            if (clearText && self.RogueEffectText != null)
            {
                self.RogueEffectText.text = string.Empty;
            }

            if (self.RogueEffectTextRect != null && self.RogueEffectTextRect.gameObject.activeSelf)
            {
                self.RogueEffectTextRect.gameObject.SetActive(false);
            }
        }

        private static async ETTask SetRogueEffectButtonSprite(this MainPanelComponent self, int buttonIndex, string imagePath)
        {
            if (self == null || self.IsDisposed || buttonIndex < 0 || buttonIndex >= self.RogueEffectButtons.Count)
            {
                return;
            }

            self.RogueEffectButtonDesiredSpriteNames[buttonIndex] = imagePath ?? string.Empty;
            Image buttonImage = self.GetRogueEffectButtonImage(buttonIndex);
            if (buttonImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                self.ReleaseRogueEffectButtonSprite(buttonIndex);
                buttonImage.sprite = null;
                buttonImage.enabled = false;
                return;
            }

            if (self.RogueEffectButtonSpriteNames[buttonIndex] == imagePath && self.RogueEffectButtonSprites[buttonIndex] != null)
            {
                buttonImage.sprite = self.RogueEffectButtonSprites[buttonIndex];
                buttonImage.enabled = true;
                buttonImage.color = Color.white;
                return;
            }

            buttonImage.sprite = null;
            buttonImage.enabled = false;

            EntityRef<MainPanelComponent> selfRef = self;
            int lockHash = unchecked((self.GetHashCode() * 397) ^ (buttonIndex + 4099));
            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;
            if (self == null || self.IsDisposed || buttonIndex < 0 || buttonIndex >= self.RogueEffectButtons.Count)
            {
                return;
            }

            if (!string.Equals(self.RogueEffectButtonDesiredSpriteNames[buttonIndex], imagePath, StringComparison.Ordinal))
            {
                return;
            }

            if (self.RogueEffectButtonSpriteNames[buttonIndex] == imagePath && self.RogueEffectButtonSprites[buttonIndex] != null)
            {
                buttonImage = self.GetRogueEffectButtonImage(buttonIndex);
                if (buttonImage != null)
                {
                    buttonImage.sprite = self.RogueEffectButtonSprites[buttonIndex];
                    buttonImage.enabled = true;
                    buttonImage.color = Color.white;
                }

                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = imagePath });

            self = selfRef;
            if (self == null || self.IsDisposed || buttonIndex < 0 || buttonIndex >= self.RogueEffectButtons.Count)
            {
                if (sprite != null)
                {
                    EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                        YIUISingletonHelper.YIUIMgr,
                        new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                }

                return;
            }

            if (!string.Equals(self.RogueEffectButtonDesiredSpriteNames[buttonIndex], imagePath, StringComparison.Ordinal))
            {
                if (sprite != null)
                {
                    EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                        YIUISingletonHelper.YIUIMgr,
                        new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                }

                return;
            }

            buttonImage = self.GetRogueEffectButtonImage(buttonIndex);
            if (sprite == null || buttonImage == null)
            {
                self.ReleaseRogueEffectButtonSprite(buttonIndex);
                if (buttonImage != null)
                {
                    buttonImage.sprite = null;
                    buttonImage.enabled = false;
                }

                return;
            }

            self.ReleaseRogueEffectButtonSprite(buttonIndex);
            self.RogueEffectButtonSprites[buttonIndex] = sprite;
            self.RogueEffectButtonSpriteNames[buttonIndex] = imagePath;
            buttonImage.sprite = sprite;
            buttonImage.enabled = true;
            buttonImage.color = Color.white;
            buttonImage.preserveAspect = true;
        }

        private static void ReleaseRogueEffectButtonSprite(this MainPanelComponent self, int buttonIndex)
        {
            if (buttonIndex < 0 || buttonIndex >= self.RogueEffectButtonSprites.Count)
            {
                return;
            }

            Sprite sprite = self.RogueEffectButtonSprites[buttonIndex];
            if (sprite == null)
            {
                self.RogueEffectButtonSpriteNames[buttonIndex] = string.Empty;
                return;
            }

            EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_ReleaseSprite { obj = sprite });

            Image buttonImage = self.GetRogueEffectButtonImage(buttonIndex);
            if (buttonImage != null && buttonImage.sprite == sprite)
            {
                buttonImage.sprite = null;
            }

            self.RogueEffectButtonSprites[buttonIndex] = null;
            self.RogueEffectButtonSpriteNames[buttonIndex] = string.Empty;
        }

        private static void ReleaseAllRogueEffectButtonSprites(this MainPanelComponent self)
        {
            for (int i = 0; i < self.RogueEffectButtonSprites.Count; ++i)
            {
                self.RogueEffectButtonDesiredSpriteNames[i] = string.Empty;
                self.ReleaseRogueEffectButtonSprite(i);
            }
        }

        private static void DisableUnusedRogueEffectButtons(this MainPanelComponent self, int startIndex)
        {
            for (int i = startIndex; i < self.RogueEffectButtons.Count; ++i)
            {
                Button button = self.RogueEffectButtons[i];
                if (button != null && button.gameObject.activeSelf)
                {
                    button.gameObject.SetActive(false);
                }

                if (i < self.RogueEffectButtonDesiredSpriteNames.Count)
                {
                    self.RogueEffectButtonDesiredSpriteNames[i] = string.Empty;
                }

                self.ReleaseRogueEffectButtonSprite(i);
                Image buttonImage = self.GetRogueEffectButtonImage(i);
                if (buttonImage != null)
                {
                    buttonImage.sprite = null;
                    buttonImage.enabled = false;
                }
            }
        }

        private static void ClearRogueEffectButtons(this MainPanelComponent self)
        {
            foreach (Button button in self.RogueEffectButtons)
            {
                button?.onClick.RemoveAllListeners();
            }

            foreach (GameObject dynamicButton in self.RogueEffectDynamicButtons)
            {
                if (dynamicButton != null)
                {
                    UnityEngine.Object.Destroy(dynamicButton);
                }
            }

            self.RogueEffectButtons.Clear();
            self.RogueEffectButtonImages.Clear();
            self.RogueEffectButtonSprites.Clear();
            self.RogueEffectButtonSpriteNames.Clear();
            self.RogueEffectButtonDesiredSpriteNames.Clear();
            self.RogueEffectDynamicButtons.Clear();
        }

        private static Image ResolveRogueEffectButtonImage(Button button)
        {
            if (button == null)
            {
                return null;
            }

            Image targetGraphic = button.targetGraphic as Image ?? button.GetComponent<Image>();
            Transform iconTransform = button.transform.Find("Icon");
            Image iconImage = iconTransform?.GetComponent<Image>();
            if (iconImage != null)
            {
                return iconImage;
            }

            foreach (Image image in button.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image == targetGraphic || image.transform == button.transform)
                {
                    continue;
                }

                if (string.Equals(image.gameObject.name, "Icon", StringComparison.OrdinalIgnoreCase))
                {
                    return image;
                }
            }

            foreach (Image image in button.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image == targetGraphic || image.transform == button.transform)
                {
                    continue;
                }

                return image;
            }

            return targetGraphic;
        }

        private static Image GetRogueEffectButtonImage(this MainPanelComponent self, int buttonIndex)
        {
            if (buttonIndex < 0 || buttonIndex >= self.RogueEffectButtons.Count)
            {
                return null;
            }

            if (buttonIndex >= self.RogueEffectButtonImages.Count)
            {
                return null;
            }

            Image buttonImage = self.RogueEffectButtonImages[buttonIndex];
            if (buttonImage == null)
            {
                Button button = self.RogueEffectButtons[buttonIndex];
                buttonImage = ResolveRogueEffectButtonImage(button);
                self.RogueEffectButtonImages[buttonIndex] = buttonImage;
            }

            return buttonImage;
        }

        private static int ComputeRogueEffectSignature(List<RogueClientOptionData> selectedOptions)
        {
            if (selectedOptions == null || selectedOptions.Count == 0)
            {
                return 0;
            }

            int signature = 17;
            unchecked
            {
                signature = signature * 31 + selectedOptions.Count;
                foreach (RogueClientOptionData option in selectedOptions)
                {
                    signature = signature * 31 + option.OptionId;
                    signature = signature * 31 + option.BuffConfigId;
                }
            }

            return signature;
        }

        private static string ResolveRogueEffectDesc(RogueClientOptionData optionData)
        {
            if (!string.IsNullOrWhiteSpace(optionData.Desc))
            {
                return optionData.Desc;
            }

            return optionData.Name ?? string.Empty;
        }

        private static Camera ResolveUICamera(this MainPanelComponent self)
        {
            Canvas canvas = self.UIBase?.OwnerGameObject?.GetComponentInParent<Canvas>();
            return canvas?.worldCamera;
        }

        private static bool TryGetPointerDownScreenPosition(out Vector2 screenPosition)
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; ++i)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began)
                    {
                        screenPosition = touch.position;
                        return true;
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = default;
            return false;
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
                self.SearchButtonText.text = ResolveText(buttonTextId, "交互");
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

        private static void RefreshPickupHintCommons(this MainPanelComponent self, ECAInteractClientComponent runtime)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (runtime == null || !string.IsNullOrWhiteSpace(runtime.OpenContainerPointId) || runtime.InRangePointIds.Count == 0)
            {
                self.HideAllPickupHintCommons();
                return;
            }

            RectTransform pickupHintRoot = self.EnsurePickupHintRoot();
            if (pickupHintRoot == null)
            {
                self.HideAllPickupHintCommons();
                return;
            }

            using ListComponent<string> sortedPointIds = ListComponent<string>.Create();
            using HashSetComponent<string> visiblePointIds = HashSetComponent<string>.Create();
            using ListComponent<Vector2> occupiedPositions = ListComponent<Vector2>.Create();

            foreach (string pointId in runtime.InRangePointIds)
            {
                if (ShouldShowPickupHint(runtime, pointId))
                {
                    sortedPointIds.Add(pointId);
                }
            }

            if (sortedPointIds.Count == 0)
            {
                self.HideAllPickupHintCommons();
                return;
            }

            string focusPointId = runtime.FocusPointId;
            sortedPointIds.Sort((left, right) => ComparePickupHintPointIds(left, right, focusPointId));

            foreach (string pointId in sortedPointIds)
            {
                if (!self.TryResolveGroundDropLocalPosition(pointId, out Vector2 basePosition))
                {
                    continue;
                }

                Vector2 resolvedPosition = self.ResolvePickupHintOverlapPosition(basePosition, occupiedPositions);
                PickupHintPanelComponent view = self.GetOrCreatePickupHintCommon(pointId);
                if (view == null || view.IsDisposed)
                {
                    continue;
                }

                visiblePointIds.Add(pointId);
                occupiedPositions.Add(resolvedPosition);
                view.Show(pointId, 0, resolvedPosition);
            }

            self.HideInactivePickupHintCommons(visiblePointIds);

            if (!string.IsNullOrWhiteSpace(focusPointId) &&
                self.PickupHintViewRefs.TryGetValue(focusPointId, out EntityRef<PickupHintPanelComponent> focusViewRef))
            {
                PickupHintPanelComponent focusView = focusViewRef;
                focusView?.UIBase?.OwnerRectTransform?.SetAsLastSibling();
            }
        }

        private static RectTransform EnsurePickupHintRoot(this MainPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return null;
            }

            RectTransform pickupHintRoot = self.PickupHintRoot;
            if (pickupHintRoot != null)
            {
                pickupHintRoot.SetAsLastSibling();
                return pickupHintRoot;
            }

            RectTransform ownerRoot = self.UIBase?.OwnerRectTransform;
            if (ownerRoot == null)
            {
                return null;
            }

            RectTransform existingRoot = ownerRoot.Find(PickupHintRootName) as RectTransform;
            if (existingRoot != null)
            {
                existingRoot.SetAsLastSibling();
                self.PickupHintRoot = existingRoot;
                return existingRoot;
            }

            GameObject rootObject = new GameObject(PickupHintRootName, typeof(RectTransform));
            rootObject.layer = ownerRoot.gameObject.layer;
            pickupHintRoot = rootObject.GetComponent<RectTransform>();
            pickupHintRoot.SetParent(ownerRoot, false);
            pickupHintRoot.anchorMin = Vector2.zero;
            pickupHintRoot.anchorMax = Vector2.one;
            pickupHintRoot.pivot = new Vector2(0.5f, 0.5f);
            pickupHintRoot.offsetMin = Vector2.zero;
            pickupHintRoot.offsetMax = Vector2.zero;
            pickupHintRoot.anchoredPosition = Vector2.zero;
            pickupHintRoot.SetAsLastSibling();
            self.PickupHintRoot = pickupHintRoot;
            return pickupHintRoot;
        }

        private static PickupHintPanelComponent GetOrCreatePickupHintCommon(this MainPanelComponent self, string pointId)
        {
            if (self == null || self.IsDisposed || string.IsNullOrWhiteSpace(pointId))
            {
                return null;
            }

            if (self.PickupHintViewRefs.TryGetValue(pointId, out EntityRef<PickupHintPanelComponent> viewRef))
            {
                PickupHintPanelComponent existingView = viewRef;
                if (existingView != null && !existingView.IsDisposed)
                {
                    return existingView;
                }

                self.PickupHintViewRefs.Remove(pointId);
            }

            RectTransform pickupHintRoot = self.EnsurePickupHintRoot();
            if (pickupHintRoot == null)
            {
                return null;
            }

            PickupHintPanelComponent created =
                    YIUIFactory.Instantiate<PickupHintPanelComponent>(self.Scene(), self, pickupHintRoot) as PickupHintPanelComponent;
            if (created == null)
            {
                return null;
            }

            created.Hide();
            self.PickupHintViewRefs[pointId] = created;
            return created;
        }

        private static void HideAllPickupHintCommons(this MainPanelComponent self)
        {
            if (self == null || self.IsDisposed || self.PickupHintViewRefs.Count == 0)
            {
                return;
            }

            using ListComponent<string> invalidPointIds = ListComponent<string>.Create();
            foreach (KeyValuePair<string, EntityRef<PickupHintPanelComponent>> pair in self.PickupHintViewRefs)
            {
                PickupHintPanelComponent view = pair.Value;
                if (view == null || view.IsDisposed)
                {
                    invalidPointIds.Add(pair.Key);
                    continue;
                }

                view.Hide();
            }

            RemovePickupHintKeys(self, invalidPointIds);
        }

        private static void HideInactivePickupHintCommons(this MainPanelComponent self, HashSet<string> visiblePointIds)
        {
            if (self == null || self.IsDisposed || self.PickupHintViewRefs.Count == 0)
            {
                return;
            }

            using ListComponent<string> invalidPointIds = ListComponent<string>.Create();
            foreach (KeyValuePair<string, EntityRef<PickupHintPanelComponent>> pair in self.PickupHintViewRefs)
            {
                PickupHintPanelComponent view = pair.Value;
                if (view == null || view.IsDisposed)
                {
                    invalidPointIds.Add(pair.Key);
                    continue;
                }

                if (!visiblePointIds.Contains(pair.Key))
                {
                    view.Hide();
                }
            }

            RemovePickupHintKeys(self, invalidPointIds);
        }

        private static void ReleaseAllPickupHintCommons(this MainPanelComponent self)
        {
            if (self == null)
            {
                return;
            }

            foreach (KeyValuePair<string, EntityRef<PickupHintPanelComponent>> pair in self.PickupHintViewRefs)
            {
                PickupHintPanelComponent view = pair.Value;
                if (view == null || view.IsDisposed)
                {
                    continue;
                }

                view.UIBase?.Dispose();
            }

            self.PickupHintViewRefs.Clear();
            if (self.PickupHintRoot != null)
            {
                UnityEngine.Object.Destroy(self.PickupHintRoot.gameObject);
                self.PickupHintRoot = null;
            }
        }

        private static bool TryResolveGroundDropLocalPosition(this MainPanelComponent self, string pointId, out Vector2 localPosition)
        {
            localPosition = default;
            if (self == null || self.IsDisposed || string.IsNullOrWhiteSpace(pointId))
            {
                return false;
            }

            if (!TryParseGroundDropPointUnitId(pointId, out long pointUnitId))
            {
                return false;
            }

            Scene root = self.Root();
            Unit pointUnit = root?.CurrentScene()?.GetComponent<UnitComponent>()?.Get(pointUnitId);
            if (pointUnit == null || pointUnit.IsDisposed)
            {
                return false;
            }

            Camera worldCamera = Camera.main;
            RectTransform pickupHintRoot = self.EnsurePickupHintRoot();
            if (worldCamera == null || pickupHintRoot == null)
            {
                return false;
            }

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(pointUnit.Position);
            if (screenPosition.z <= 0f)
            {
                return false;
            }

            Vector2 screenPoint = new Vector2(screenPosition.x, screenPosition.y + PickupHintScreenYOffset);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    pickupHintRoot,
                    screenPoint,
                    self.ResolveUICamera(),
                    out Vector2 anchoredPosition))
            {
                return false;
            }

            localPosition = self.ClampPickupHintPosition(anchoredPosition);
            return true;
        }

        private static Vector2 ResolvePickupHintOverlapPosition(
            this MainPanelComponent self,
            Vector2 basePosition,
            List<Vector2> occupiedPositions)
        {
            Vector2 clampedBasePosition = self.ClampPickupHintPosition(basePosition);
            if (!IsPickupHintOverlappingAny(clampedBasePosition, occupiedPositions))
            {
                return clampedBasePosition;
            }

            for (int level = 1; level <= PickupHintMaxOverlapLevel; ++level)
            {
                Vector2 rightOffset = new Vector2(PickupHintOverlapOffsetX * level, PickupHintOverlapOffsetY * level);
                Vector2 rightCandidate = self.ClampPickupHintPosition(basePosition + rightOffset);
                if (!IsPickupHintOverlappingAny(rightCandidate, occupiedPositions))
                {
                    return rightCandidate;
                }

                Vector2 leftOffset = new Vector2(-PickupHintOverlapOffsetX * level, PickupHintOverlapOffsetY * level);
                Vector2 leftCandidate = self.ClampPickupHintPosition(basePosition + leftOffset);
                if (!IsPickupHintOverlappingAny(leftCandidate, occupiedPositions))
                {
                    return leftCandidate;
                }
            }

            return clampedBasePosition;
        }

        private static Vector2 ClampPickupHintPosition(this MainPanelComponent self, Vector2 anchoredPosition)
        {
            RectTransform pickupHintRoot = self.PickupHintRoot;
            if (pickupHintRoot == null)
            {
                return anchoredPosition;
            }

            Rect rootRect = pickupHintRoot.rect;
            if (rootRect.width <= 0f || rootRect.height <= 0f)
            {
                return anchoredPosition;
            }

            float halfWidth = PickupHintCardWidth * 0.5f;
            float halfHeight = PickupHintCardHeight * 0.5f;
            float minX = rootRect.xMin + halfWidth + PickupHintClampPadding;
            float maxX = rootRect.xMax - halfWidth - PickupHintClampPadding;
            float minY = rootRect.yMin + halfHeight + PickupHintClampPadding;
            float maxY = rootRect.yMax - halfHeight - PickupHintClampPadding;

            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);
            return anchoredPosition;
        }

        private static bool TryParseGroundDropPointUnitId(string pointId, out long pointUnitId)
        {
            pointUnitId = 0;
            if (string.IsNullOrWhiteSpace(pointId) ||
                !pointId.StartsWith(GroundDropPointPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            int lastSeparatorIndex = pointId.LastIndexOf('_');
            if (lastSeparatorIndex < 0 || lastSeparatorIndex >= pointId.Length - 1)
            {
                return false;
            }

            return long.TryParse(pointId.Substring(lastSeparatorIndex + 1), out pointUnitId);
        }

        private static bool ShouldShowPickupHint(ECAInteractClientComponent runtime, string pointId)
        {
            return runtime != null &&
                !string.IsNullOrWhiteSpace(pointId) &&
                pointId.StartsWith(GroundDropPointPrefix, StringComparison.Ordinal) &&
                (!runtime.PointCanInteract.TryGetValue(pointId, out bool canInteract) || canInteract);
        }

        private static bool IsPickupHintOverlappingAny(Vector2 candidate, List<Vector2> occupiedPositions)
        {
            if (occupiedPositions == null)
            {
                return false;
            }

            for (int i = 0; i < occupiedPositions.Count; ++i)
            {
                Vector2 occupiedPosition = occupiedPositions[i];
                if (Mathf.Abs(candidate.x - occupiedPosition.x) < PickupHintOverlapThresholdX &&
                    Mathf.Abs(candidate.y - occupiedPosition.y) < PickupHintOverlapThresholdY)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ComparePickupHintPointIds(string left, string right, string focusPointId)
        {
            bool leftFocused = string.Equals(left, focusPointId, StringComparison.Ordinal);
            bool rightFocused = string.Equals(right, focusPointId, StringComparison.Ordinal);
            if (leftFocused != rightFocused)
            {
                return leftFocused ? -1 : 1;
            }

            return string.Compare(left, right, StringComparison.Ordinal);
        }

        private static void RemovePickupHintKeys(MainPanelComponent self, List<string> invalidPointIds)
        {
            if (self == null || invalidPointIds == null || invalidPointIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < invalidPointIds.Count; ++i)
            {
                self.PickupHintViewRefs.Remove(invalidPointIds[i]);
            }
        }

        private static void RefreshEvacuateTips(this MainPanelComponent self, ECAInteractClientComponent runtime)
        {
            bool show = runtime != null &&
                runtime.EvacuationState == ECAEvacuationState.Running &&
                !string.IsNullOrWhiteSpace(runtime.EvacuationPointId);

            string pointId = show ? runtime.EvacuationPointId : null;
            long remainMs = 0;
            int remainSeconds = 0;
            if (show)
            {
                remainMs = Math.Max(runtime.EvacuationEndTimeMs - TimeInfo.Instance.ClientNow(), 0);
                runtime.EvacuationRemainMs = remainMs;
                remainSeconds = remainMs <= 0 ? 0 : (int)Math.Ceiling(remainMs / 1000d);
            }

            bool needOpen = show &&
                !self.IsEvacuateTipsOpening &&
                (!self.LastEvacuateTipsVisible ||
                 self.LastEvacuationPointId != pointId ||
                 self.EvacuateTipsView == null ||
                 self.EvacuateTipsView.IsDisposed);

            if (needOpen)
            {
                self.EnsureEvacuateTipsViewOpenAsync(remainSeconds).Coroutine();
            }
            else if (!show && (self.LastEvacuateTipsVisible || self.IsEvacuateTipsOpening || (self.EvacuateTipsView != null && !self.EvacuateTipsView.IsDisposed)))
            {
                self.HideEvacuateTips();
            }

            EvacuateTipsComponent tipsView = self.EvacuateTipsView;
            if (show && tipsView != null && !tipsView.IsDisposed && self.LastEvacuateRemainSeconds != remainSeconds)
            {
                tipsView.SetRemainSeconds(remainSeconds);
            }

            self.LastEvacuateTipsVisible = show;
            self.LastEvacuationPointId = pointId;
            self.LastEvacuateRemainSeconds = remainSeconds;
        }

        private static async ETTask EnsureEvacuateTipsViewOpenAsync(this MainPanelComponent self, int remainSeconds)
        {
            if (self == null || self.IsDisposed || self.UIPanel == null)
            {
                return;
            }

            if (self.IsEvacuateTipsOpening)
            {
                return;
            }

            EvacuateTipsComponent currentView = self.EvacuateTipsView;
            if (currentView != null && !currentView.IsDisposed)
            {
                currentView.SetRemainSeconds(remainSeconds);
                return;
            }

            self.IsEvacuateTipsOpening = true;

            self.EnsureViewParentRegistered(EvacuateTipsComponent.ResName, $"{EvacuateTipsComponent.ResName}ViewParent");

            EntityRef<MainPanelComponent> selfRef = self;
            EvacuateTipsComponent view;
            try
            {
                view = await self.UIPanel.OpenViewAsync<EvacuateTipsComponent>();
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null && !self.IsDisposed)
                {
                    self.IsEvacuateTipsOpening = false;
                }

                Log.Error($"[MainPanel] open evacuate tips failed: {e}");
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.IsEvacuateTipsOpening = false;
            if (view == null)
            {
                return;
            }

            if (!self.LastEvacuateTipsVisible)
            {
                self.UIPanel.CloseView<EvacuateTipsComponent>();
                return;
            }

            self.EvacuateTipsViewRef = view;
            view.SetRemainSeconds(remainSeconds);
        }

        private static void HideEvacuateTips(this MainPanelComponent self)
        {
            if (self == null || self.IsDisposed || self.UIPanel == null)
            {
                return;
            }

            self.IsEvacuateTipsOpening = false;
            self.UIPanel.CloseView<EvacuateTipsComponent>();
            self.EvacuateTipsViewRef = default;
        }

        private static void EnsureViewParentRegistered(this MainPanelComponent self, string viewName, string explicitParentName = null)
        {
            if (self == null || self.IsDisposed || self.UIPanel == null || string.IsNullOrEmpty(viewName))
            {
                return;
            }

            if (self.UIPanel.m_ViewParent.ContainsKey(viewName))
            {
                return;
            }

            RectTransform root = self.UIBase?.OwnerRectTransform;
            if (root == null)
            {
                return;
            }

            RectTransform viewParent = FindViewParent(root, explicitParentName, $"{viewName}{YIUIConstHelper.Const.UIParentName}");
            if (viewParent == null)
            {
                Log.Warning($"[MainPanel] 未找到View父节点: view={viewName}, explicitParent={explicitParentName ?? "null"}");
                return;
            }

            self.UIPanel.m_ViewParent.Add(viewName, viewParent);
        }

        private static RectTransform FindViewParent(RectTransform root, string explicitParentName, string fallbackParentName)
        {
            Transform popupRoot = root.FindChildByName(YIUIConstHelper.Const.UIAllPopupViewParentName);
            if (!string.IsNullOrEmpty(explicitParentName))
            {
                RectTransform explicitRect = popupRoot?.FindChildByName(explicitParentName) as RectTransform;
                if (explicitRect != null)
                {
                    return explicitRect;
                }

                explicitRect = root.FindChildByName(explicitParentName) as RectTransform;
                if (explicitRect != null)
                {
                    return explicitRect;
                }
            }

            RectTransform fallbackRect = popupRoot?.FindChildByName(fallbackParentName) as RectTransform;
            if (fallbackRect != null)
            {
                return fallbackRect;
            }

            return root.FindChildByName(fallbackParentName) as RectTransform;
        }

        private static string ResolveText(int textId, string defaultText = "")
        {
            if (textId <= 0)
            {
                return defaultText;
            }

            TextConfig config = TextConfigCategory.Instance.GetOrDefault(textId);
            return string.IsNullOrWhiteSpace(config?.CN) ? defaultText : config.CN;
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
                self.MinimapMarkerLayer != null &&
                self.MinimapPoiLayer != null &&
                self.MinimapTrackedPoiLayer != null)
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

            MinimapDisplayHelper.StretchToFillParent(self.MinimapTexture?.rectTransform);
            MinimapDisplayHelper.StretchToFillParent(self.MinimapFogOverlay?.rectTransform);

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

            Transform poiLayerTransform = self.MinimapMask.Find("PoiLayer");
            if (poiLayerTransform == null)
            {
                GameObject poiLayerObject = new GameObject("PoiLayer", typeof(RectTransform));
                RectTransform poiLayer = poiLayerObject.GetComponent<RectTransform>();
                poiLayer.SetParent(self.MinimapMask, false);
                poiLayer.anchorMin = Vector2.zero;
                poiLayer.anchorMax = Vector2.one;
                poiLayer.offsetMin = Vector2.zero;
                poiLayer.offsetMax = Vector2.zero;
                poiLayer.SetSiblingIndex(self.MinimapMarkerLayer != null ? self.MinimapMarkerLayer.GetSiblingIndex() + 1 : self.MinimapMask.childCount - 1);
                self.MinimapPoiLayer = poiLayer;
            }
            else
            {
                self.MinimapPoiLayer = poiLayerTransform as RectTransform;
            }

            Transform trackedPoiLayerTransform = self.MinimapMask.Find("TrackedPoiLayer");
            if (trackedPoiLayerTransform == null)
            {
                GameObject trackedPoiLayerObject = new GameObject("TrackedPoiLayer", typeof(RectTransform));
                RectTransform trackedPoiLayer = trackedPoiLayerObject.GetComponent<RectTransform>();
                trackedPoiLayer.SetParent(self.MinimapMask, false);
                trackedPoiLayer.anchorMin = Vector2.zero;
                trackedPoiLayer.anchorMax = Vector2.one;
                trackedPoiLayer.offsetMin = Vector2.zero;
                trackedPoiLayer.offsetMax = Vector2.zero;
                trackedPoiLayer.SetSiblingIndex(self.MinimapPoiLayer != null ? self.MinimapPoiLayer.GetSiblingIndex() + 1 : self.MinimapMask.childCount - 1);
                self.MinimapTrackedPoiLayer = trackedPoiLayer;
            }
            else
            {
                self.MinimapTrackedPoiLayer = trackedPoiLayerTransform as RectTransform;
            }

            MinimapDisplayHelper.StretchToFillParent(self.MinimapMarkerLayer);
            MinimapDisplayHelper.StretchToFillParent(self.MinimapPoiLayer);
            MinimapDisplayHelper.StretchToFillParent(self.MinimapTrackedPoiLayer);
        }

        private static void RefreshMinimap(this MainPanelComponent self, bool force = false)
        {
            self.BindMinimap();
            if (self.MinimapRoot == null ||
                self.MinimapMask == null ||
                self.MinimapMarkerLayer == null ||
                self.MinimapPoiLayer == null ||
                self.MinimapTrackedPoiLayer == null)
            {
                return;
            }

            Scene currentScene = self.Root()?.CurrentScene();
            MinimapRuntimeComponent runtime = currentScene?.GetComponent<MinimapRuntimeComponent>();
            MapPoiRuntimeComponent poiRuntime = currentScene?.GetComponent<MapPoiRuntimeComponent>();
            if (runtime == null)
            {
                self.ClearMinimapMarkers();
                self.ClearMinimapPois();
                return;
            }

            if (self.MinimapNameText != null && (force || self.MinimapNameText.text != runtime.MapName))
            {
                self.MinimapNameText.text = runtime.MapName;
            }

            if (!runtime.TryGetMyPosition(out float3 myPosition))
            {
                self.ClearMinimapMarkers();
                self.ClearMinimapPois();
                return;
            }

            float3 viewCenter = MinimapDisplayHelper.GetCompactViewCenter(runtime, myPosition);
            self.RefreshMinimapArrow(runtime, myPosition, viewCenter);
            self.RefreshMinimapTexture(runtime, myPosition);
            self.RefreshMinimapFog(runtime);
            self.RefreshMinimapMarkers(runtime, myPosition, viewCenter);
            self.RefreshMinimapPois(poiRuntime, runtime, viewCenter);
            self.RefreshTrackedPoiIndicator(poiRuntime, runtime, viewCenter);
        }

        private static void RefreshMinimapArrow(this MainPanelComponent self, MinimapRuntimeComponent runtime, float3 myPosition, float3 viewCenter)
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

            // 根据钳制后的视野中心计算箭头偏移位置（与迷雾显示一致）
            float range = runtime.CompactRange;
            if (range > 0f && self.MinimapMask != null)
            {
                float offsetX = (myPosition.x - viewCenter.x) / range;
                float offsetZ = (myPosition.z - viewCenter.z) / range;
                float radius = Mathf.Min(self.MinimapMask.rect.width, self.MinimapMask.rect.height) * 0.5f;
                self.MinimapArrow.rectTransform.anchoredPosition = new Vector2(offsetX * radius, offsetZ * radius);
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

            if (runtime == null)
            {
                self.MinimapTexture.uvRect = new Rect(0f, 0f, 1f, 1f);
                if (self.MinimapFogOverlay != null)
                {
                    self.MinimapFogOverlay.uvRect = self.MinimapTexture.uvRect;
                }
                return;
            }

            self.MinimapTexture.uvRect = MinimapDisplayHelper.GetCompactBaseUvRect(runtime, self.MinimapTexture.texture, myPosition);
            if (self.MinimapFogOverlay != null)
            {
                self.MinimapFogOverlay.uvRect = MinimapDisplayHelper.GetCompactFogUvRect(runtime, myPosition);
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

        private static void RefreshMinimapMarkers(this MainPanelComponent self, MinimapRuntimeComponent runtime, float3 myPosition, float3 viewCenter)
        {
            HashSet<long> activeMarkerIds = new HashSet<long>();
            float markerSize = Mathf.Max(global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.MarkerSize, 10f), 4f);
            float radius = Mathf.Min(self.MinimapMask.rect.width, self.MinimapMask.rect.height) * 0.5f - markerSize;

            foreach (KeyValuePair<long, MinimapMarkerRuntime> pair in runtime.GetMarkers())
            {
                MinimapMarkerRuntime marker = pair.Value;
                if (marker.UnitId == runtime.MyUnitId || !MinimapRuntimeMarkerHelper.ShouldDisplayMarker(runtime, marker))
                {
                    continue;
                }

                if (!MinimapRuntimeMarkerHelper.TryWorldToCompactLocalPosition(runtime, viewCenter, marker.Position, out float2 localPosition))
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
                    self.RefreshMinimapMarkerVisual(runtime, marker, markerImage);
                }
            }

            foreach (KeyValuePair<long, RectTransform> pair in self.MinimapMarkerRects)
            {
                if (!activeMarkerIds.Contains(pair.Key) && pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                    self.MinimapMarkerDesiredSpriteNames.Remove(pair.Key);
                }
            }
        }

        private static void RefreshMinimapPois(
            this MainPanelComponent self,
            MapPoiRuntimeComponent poiRuntime,
            MinimapRuntimeComponent runtime,
            float3 viewCenter)
        {
            if (self.MinimapMask == null)
            {
                return;
            }

            if (poiRuntime == null || poiRuntime.IsDisposed)
            {
                self.ClearMinimapPois();
                return;
            }

            poiRuntime.EnsureConfigLoaded();

            HashSet<string> activePoiIds = new HashSet<string>();
            float defaultPoiSize = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.PoiSize, 25f);
            float poiSize = Mathf.Max(global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.CompactPoiSize, defaultPoiSize), 4f);
            float radius = Mathf.Max(Mathf.Min(self.MinimapMask.rect.width, self.MinimapMask.rect.height) * 0.5f - poiSize, 0f);

            foreach (KeyValuePair<string, MapPoiRuntimeData> pair in poiRuntime.GetPois())
            {
                MapPoiRuntimeData poi = pair.Value;
                if (!poiRuntime.ShouldDisplayOnMinimap(poi))
                {
                    self.HideMinimapPoi(poi.PoiId);
                    continue;
                }

                if (!MinimapRuntimeMarkerHelper.TryWorldToCompactLocalPosition(runtime, viewCenter, poi.Position, out float2 localPosition))
                {
                    self.HideMinimapPoi(poi.PoiId);
                    continue;
                }

                RectTransform poiRect = self.GetOrCreateMinimapPoi(poi.PoiId);
                if (poiRect == null)
                {
                    continue;
                }

                activePoiIds.Add(poi.PoiId);
                poiRect.gameObject.SetActive(true);
                poiRect.sizeDelta = new Vector2(poiSize, poiSize);
                poiRect.anchoredPosition = new Vector2(localPosition.x * radius, localPosition.y * radius);

                if (self.MinimapPoiImages.TryGetValue(poi.PoiId, out Image poiImage) && poiImage != null)
                {
                    self.RefreshMinimapPoiVisual(poi, poiImage);
                }
            }

            foreach (KeyValuePair<string, RectTransform> pair in self.MinimapPoiRects)
            {
                if (!activePoiIds.Contains(pair.Key) && pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                    self.MinimapPoiDesiredSpriteNames.Remove(pair.Key);
                }
            }
        }

        private static void RefreshTrackedPoiIndicator(
            this MainPanelComponent self,
            MapPoiRuntimeComponent poiRuntime,
            MinimapRuntimeComponent runtime,
            float3 viewCenter)
        {
            if (self.MinimapMask == null)
            {
                return;
            }

            if (poiRuntime == null || poiRuntime.IsDisposed || !poiRuntime.TryGetSelectedPoi(out MapPoiRuntimeData poi) || !poiRuntime.ShouldDisplayOnMinimap(poi))
            {
                self.HideTrackedPoiIndicator();
                return;
            }

            RectTransform trackedRect = self.EnsureTrackedPoiIndicator();
            if (trackedRect == null)
            {
                return;
            }

            float defaultPoiSize = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.PoiSize, 25f);
            float poiSize = Mathf.Max(global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.CompactPoiSize, defaultPoiSize), 4f);
            float radius = Mathf.Max(Mathf.Min(self.MinimapMask.rect.width, self.MinimapMask.rect.height) * 0.5f - poiSize, 0f);
            Vector2 anchoredPosition;

            if (MinimapRuntimeMarkerHelper.TryWorldToCompactLocalPosition(runtime, viewCenter, poi.Position, out float2 localPosition))
            {
                anchoredPosition = new Vector2(localPosition.x * radius, localPosition.y * radius);
            }
            else
            {
                float2 edgeLocalPosition = MinimapRuntimeMarkerHelper.WorldToCompactLocalPosition(runtime, viewCenter, poi.Position);
                Vector2 direction = new Vector2(edgeLocalPosition.x, edgeLocalPosition.y);
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    self.HideTrackedPoiIndicator();
                    return;
                }

                anchoredPosition = direction.normalized * radius;
            }

            trackedRect.gameObject.SetActive(true);
            trackedRect.sizeDelta = new Vector2(poiSize, poiSize);
            trackedRect.anchoredPosition = anchoredPosition;
            if (self.MinimapTrackedPoiImage != null)
            {
                self.RefreshTrackedPoiVisual(poi, self.MinimapTrackedPoiImage);
            }
        }

        private static RectTransform GetOrCreateMinimapPoi(this MainPanelComponent self, string poiId)
        {
            if (self.MinimapPoiRects.TryGetValue(poiId, out RectTransform poiRect) && poiRect != null)
            {
                return poiRect;
            }

            if (self.MinimapPoiLayer == null)
            {
                return null;
            }

            GameObject poiObject = new GameObject($"Poi_{poiId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            poiRect = poiObject.GetComponent<RectTransform>();
            poiRect.SetParent(self.MinimapPoiLayer, false);
            poiRect.anchorMin = new Vector2(0.5f, 0.5f);
            poiRect.anchorMax = new Vector2(0.5f, 0.5f);
            poiRect.pivot = new Vector2(0.5f, 0.5f);

            Image poiImage = poiObject.GetComponent<Image>();
            poiImage.raycastTarget = false;
            poiImage.sprite = self.GetMinimapMarkerSprite();
            poiImage.type = Image.Type.Simple;
            poiImage.preserveAspect = false;

            self.MinimapPoiRects[poiId] = poiRect;
            self.MinimapPoiImages[poiId] = poiImage;
            return poiRect;
        }

        private static RectTransform EnsureTrackedPoiIndicator(this MainPanelComponent self)
        {
            if (self.MinimapTrackedPoiRect != null)
            {
                return self.MinimapTrackedPoiRect;
            }

            if (self.MinimapTrackedPoiLayer == null)
            {
                return null;
            }

            GameObject poiObject = new GameObject("TrackedPoi", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform poiRect = poiObject.GetComponent<RectTransform>();
            poiRect.SetParent(self.MinimapTrackedPoiLayer, false);
            poiRect.anchorMin = new Vector2(0.5f, 0.5f);
            poiRect.anchorMax = new Vector2(0.5f, 0.5f);
            poiRect.pivot = new Vector2(0.5f, 0.5f);

            Image poiImage = poiObject.GetComponent<Image>();
            poiImage.raycastTarget = false;
            poiImage.sprite = self.GetMinimapMarkerSprite();
            poiImage.type = Image.Type.Simple;
            poiImage.preserveAspect = false;

            self.MinimapTrackedPoiRect = poiRect;
            self.MinimapTrackedPoiImage = poiImage;
            return poiRect;
        }

        private static void HideMinimapPoi(this MainPanelComponent self, string poiId)
        {
            if (self.MinimapPoiRects.TryGetValue(poiId, out RectTransform poiRect) && poiRect != null)
            {
                poiRect.gameObject.SetActive(false);
            }
        }

        private static void HideTrackedPoiIndicator(this MainPanelComponent self)
        {
            if (self.MinimapTrackedPoiRect != null)
            {
                self.MinimapTrackedPoiRect.gameObject.SetActive(false);
            }

            self.MinimapTrackedPoiDesiredSpriteName = string.Empty;
        }

        private static void ClearMinimapPois(this MainPanelComponent self)
        {
            foreach (KeyValuePair<string, RectTransform> pair in self.MinimapPoiRects)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.gameObject);
                }
            }

            self.MinimapPoiRects.Clear();
            self.MinimapPoiImages.Clear();
            self.MinimapPoiDesiredSpriteNames.Clear();
            if (self.MinimapTrackedPoiRect != null)
            {
                UnityEngine.Object.Destroy(self.MinimapTrackedPoiRect.gameObject);
            }

            self.MinimapTrackedPoiRect = null;
            self.MinimapTrackedPoiImage = null;
            self.MinimapTrackedPoiDesiredSpriteName = string.Empty;
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
            markerImage.preserveAspect = false;

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
            self.MinimapMarkerDesiredSpriteNames.Clear();
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

        private static void RefreshMinimapMarkerVisual(this MainPanelComponent self, MinimapRuntimeComponent runtime, MinimapMarkerRuntime marker, Image markerImage)
        {
            if (markerImage == null)
            {
                return;
            }

            string iconName = MinimapMarkerIconHelper.ResolveIconName(marker);
            self.MinimapMarkerDesiredSpriteNames[marker.UnitId] = iconName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(iconName))
            {
                if (self.MinimapMarkerLoadedSprites.TryGetValue(iconName, out Sprite customSprite) && customSprite != null)
                {
                    markerImage.sprite = customSprite;
                    markerImage.color = Color.white;
                    markerImage.preserveAspect = true;
                    return;
                }

                markerImage.sprite = self.GetMinimapMarkerSprite();
                markerImage.color = self.ResolveMinimapMarkerColor(runtime, marker);
                markerImage.preserveAspect = false;
                self.RequestMinimapMarkerSprite(iconName);
                return;
            }

            markerImage.sprite = self.GetMinimapMarkerSprite();
            markerImage.color = self.ResolveMinimapMarkerColor(runtime, marker);
            markerImage.preserveAspect = false;
        }

        private static void RequestMinimapMarkerSprite(this MainPanelComponent self, string spriteName)
        {
            if (self == null || self.IsDisposed || string.IsNullOrWhiteSpace(spriteName))
            {
                return;
            }

            if (self.MinimapMarkerLoadedSprites.ContainsKey(spriteName) || !self.MinimapMarkerLoadingSpriteNames.Add(spriteName))
            {
                return;
            }

            self.LoadMinimapMarkerSpriteAsync(spriteName).Coroutine();
        }

        private static async ETTask LoadMinimapMarkerSpriteAsync(this MainPanelComponent self, string spriteName)
        {
            EntityRef<MainPanelComponent> selfRef = self;
            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = spriteName });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                if (sprite != null)
                {
                    EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                        YIUISingletonHelper.YIUIMgr,
                        new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                }

                return;
            }

            self.MinimapMarkerLoadingSpriteNames.Remove(spriteName);
            if (sprite == null)
            {
                return;
            }

            if (self.MinimapMarkerLoadedSprites.TryGetValue(spriteName, out Sprite cachedSprite) && cachedSprite != null)
            {
                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                return;
            }

            self.MinimapMarkerLoadedSprites[spriteName] = sprite;
            foreach (KeyValuePair<long, Image> pair in self.MinimapMarkerImages)
            {
                if (!self.MinimapMarkerDesiredSpriteNames.TryGetValue(pair.Key, out string desiredSpriteName) ||
                    desiredSpriteName != spriteName ||
                    pair.Value == null)
                {
                    continue;
                }

                pair.Value.sprite = sprite;
                pair.Value.color = Color.white;
                pair.Value.preserveAspect = true;
            }
        }

        private static void ReleaseAllMinimapMarkerSprites(this MainPanelComponent self)
        {
            foreach (Sprite sprite in self.MinimapMarkerLoadedSprites.Values)
            {
                if (sprite == null)
                {
                    continue;
                }

                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
            }

            self.MinimapMarkerLoadedSprites.Clear();
            self.MinimapMarkerLoadingSpriteNames.Clear();
            self.MinimapMarkerDesiredSpriteNames.Clear();
        }

        private static void RefreshMinimapPoiVisual(this MainPanelComponent self, MapPoiRuntimeData poi, Image poiImage)
        {
            if (poiImage == null)
            {
                return;
            }

            string iconName = MinimapPoiIconHelper.ResolveIconName(poi);
            self.MinimapPoiDesiredSpriteNames[poi.PoiId] = iconName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(iconName))
            {
                if (self.MinimapPoiLoadedSprites.TryGetValue(iconName, out Sprite customSprite) && customSprite != null)
                {
                    poiImage.sprite = customSprite;
                    poiImage.color = Color.white;
                    poiImage.preserveAspect = true;
                    return;
                }

                poiImage.sprite = self.GetMinimapMarkerSprite();
                poiImage.color = self.ResolveMinimapPoiColor(poi);
                poiImage.preserveAspect = false;
                self.RequestMinimapPoiSprite(iconName);
                return;
            }

            poiImage.sprite = self.GetMinimapMarkerSprite();
            poiImage.color = self.ResolveMinimapPoiColor(poi);
            poiImage.preserveAspect = false;
        }

        private static void RefreshTrackedPoiVisual(this MainPanelComponent self, MapPoiRuntimeData poi, Image poiImage)
        {
            if (poiImage == null)
            {
                return;
            }

            string iconName = MinimapPoiIconHelper.ResolveTrackedIconName(poi);
            self.MinimapTrackedPoiDesiredSpriteName = iconName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(iconName))
            {
                if (self.MinimapPoiLoadedSprites.TryGetValue(iconName, out Sprite customSprite) && customSprite != null)
                {
                    poiImage.sprite = customSprite;
                    poiImage.color = Color.white;
                    poiImage.preserveAspect = true;
                    return;
                }

                poiImage.sprite = self.GetMinimapMarkerSprite();
                poiImage.color = self.ResolveMinimapPoiColor(poi);
                poiImage.preserveAspect = false;
                self.RequestMinimapPoiSprite(iconName);
                return;
            }

            poiImage.sprite = self.GetMinimapMarkerSprite();
            poiImage.color = self.ResolveMinimapPoiColor(poi);
            poiImage.preserveAspect = false;
        }

        private static Color ResolveMinimapPoiColor(this MainPanelComponent self, MapPoiRuntimeData poi)
        {
            string key = global::ET.MinimapConstKey.MarkerColorOther;
            switch (poi.PoiType)
            {
                case MapPoiType.Evacuation:
                    key = global::ET.MinimapConstKey.MarkerColorSelf;
                    break;
                case MapPoiType.HighContainer:
                    key = global::ET.MinimapConstKey.MarkerColorOther;
                    break;
                case MapPoiType.BossSpawn:
                    key = global::ET.MinimapConstKey.MarkerColorMonster;
                    break;
                case MapPoiType.MissionTask:
                    key = global::ET.MinimapConstKey.MarkerColorMissionTask;
                    break;
            }

            string colorText = global::ET.MinimapConstConfigHelper.GetString(key, "#FFFFFF");
            if (ColorUtility.TryParseHtmlString(colorText, out Color color))
            {
                return color;
            }

            return Color.white;
        }

        private static void RequestMinimapPoiSprite(this MainPanelComponent self, string spriteName)
        {
            if (self == null || self.IsDisposed || string.IsNullOrWhiteSpace(spriteName))
            {
                return;
            }

            if (self.MinimapPoiLoadedSprites.ContainsKey(spriteName) || !self.MinimapPoiLoadingSpriteNames.Add(spriteName))
            {
                return;
            }

            self.LoadMinimapPoiSpriteAsync(spriteName).Coroutine();
        }

        private static async ETTask LoadMinimapPoiSpriteAsync(this MainPanelComponent self, string spriteName)
        {
            EntityRef<MainPanelComponent> selfRef = self;
            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = spriteName });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                if (sprite != null)
                {
                    EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                        YIUISingletonHelper.YIUIMgr,
                        new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                }

                return;
            }

            self.MinimapPoiLoadingSpriteNames.Remove(spriteName);
            if (sprite == null)
            {
                return;
            }

            if (self.MinimapPoiLoadedSprites.TryGetValue(spriteName, out Sprite cachedSprite) && cachedSprite != null)
            {
                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                return;
            }

            self.MinimapPoiLoadedSprites[spriteName] = sprite;
            foreach (KeyValuePair<string, Image> pair in self.MinimapPoiImages)
            {
                if (!self.MinimapPoiDesiredSpriteNames.TryGetValue(pair.Key, out string desiredSpriteName) ||
                    desiredSpriteName != spriteName ||
                    pair.Value == null)
                {
                    continue;
                }

                pair.Value.sprite = sprite;
                pair.Value.color = Color.white;
                pair.Value.preserveAspect = true;
            }

            if (self.MinimapTrackedPoiImage != null && self.MinimapTrackedPoiDesiredSpriteName == spriteName)
            {
                self.MinimapTrackedPoiImage.sprite = sprite;
                self.MinimapTrackedPoiImage.color = Color.white;
                self.MinimapTrackedPoiImage.preserveAspect = true;
            }
        }

        private static void ReleaseAllMinimapPoiSprites(this MainPanelComponent self)
        {
            foreach (Sprite sprite in self.MinimapPoiLoadedSprites.Values)
            {
                if (sprite == null)
                {
                    continue;
                }

                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
            }

            self.MinimapPoiLoadedSprites.Clear();
            self.MinimapPoiLoadingSpriteNames.Clear();
            self.MinimapPoiDesiredSpriteNames.Clear();
            self.MinimapTrackedPoiDesiredSpriteName = string.Empty;
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

        public static void RefreshRogueLevelBar(this MainPanelComponent self, bool force = false)
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
            long pingMs = GetCurrentPing(self);
            if (displayFps != self.LastDisplayedFps || pingMs != self.LastDisplayedPing)
            {
                self.FpsCounterText.text = $"FPS: {displayFps} | Ping: {pingMs}ms";
                self.LastDisplayedFps = displayFps;
                self.LastDisplayedPing = pingMs;
            }

            self.FpsAccumulatedTime = 0f;
            self.FpsAccumulatedFrames = 0;
        }

        private static long GetCurrentPing(MainPanelComponent self)
        {
            Session session = self.Root()?.GetComponent<SessionComponent>()?.Session;
            PingComponent pingComponent = session?.GetComponent<PingComponent>();
            return pingComponent?.Ping ?? -1;
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
