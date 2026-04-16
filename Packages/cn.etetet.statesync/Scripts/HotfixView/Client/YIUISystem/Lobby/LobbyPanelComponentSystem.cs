using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    [FriendOf(typeof(HeroSelectItemComponent))]
    [FriendOf(typeof(LobbyPanelComponent))]
    [FriendOf(typeof(EquipSlotItemComponent))]
    [FriendOf(typeof(LoadoutComponent))]
    public static partial class LobbyPanelComponentSystem
    {
        private const long QuickTransferDoubleClickDelayMs = 300;

        [EntitySystem]
        private static void YIUIInitialize(this LobbyPanelComponent self)
        {
            // 初始化英雄列表 LoopScroll
            var heroLoopScroll = self.u_ComHeroList.GetComponentInChildren<LoopScrollRect>();
            self.m_HeroLoop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type, string>(
                heroLoopScroll,
                typeof(HeroSelectItemComponent),
                "u_EventSelect"
            );

            if (self.u_ComCurrentBagItemTemplate != null)
            {
                self.u_ComCurrentBagItemTemplate.gameObject.SetActive(false);
            }

            if (self.u_ComSecureItemTemplate != null)
            {
                self.u_ComSecureItemTemplate.gameObject.SetActive(false);
            }

            // 初始化装备槽位
            self.InitHeroDisplay();
            self.InitEquipSlots();
            self.BindMatchModeButtons();
            self.RefreshMatchModeSelection();
            self.InitBattleRecordUi();
            self.InitWarehouseArea();
            self.BindOwnedAreaBoard(self.u_ComCurrentBagBoardRoot, LoadoutAreaType.Bag);
            self.BindOwnedAreaBoard(self.u_ComSecureBoardRoot, LoadoutAreaType.Secure);
            self.RefreshLoadoutContentTabUi();
        }

        [EntitySystem]
        private static void Destroy(this LobbyPanelComponent self)
        {
            ReleaseViews(self.CurrentBagItemViews);
            ReleaseViews(self.SecureItemViews);
            ReleaseViews(self.WarehouseItemViews);
            ReleaseViews(self.CurrentBagGridCellViews);
            ReleaseViews(self.SecureGridCellViews);
            ReleaseViews(self.WarehouseGridCellViews);
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LobbyPanelComponent self)
        {
            self.IsMatchFlowRunning = false;
            AudioHelper.PlayBgm(self.Root(), AudioEventId.BgmLobby);
            EntityRef<LobbyPanelComponent> selfRef = self;
            bool inHome = self.IsInHomeScene();
            self.ShowPanel(self.u_ComRolePanelRectTransform);
            await self.CloseMatchWaitingViewAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            await self.RefreshHeroList();
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            self.TryRefreshLoadoutUi(true);
            if (inHome)
            {
                await self.Root().OpenHomePanelAsync();
                self = selfRef;
                if (self == null || self.IsDisposed)
                {
                    return false;
                }
            }

            return true;
        }

        #region YIUIEvent开始

        [YIUIInvoke(LobbyPanelComponent.OnEventEnterMapInvoke)]
        private static async ETTask OnEventEnterMapInvoke(this LobbyPanelComponent self)
        {
            if (self.IsMatchFlowRunning)
            {
                Log.Info("[LobbyUI] match flow is already running, ignore repeated enter map click");
                await ETTask.CompletedTask;
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            self.IsMatchFlowRunning = true;
            int gameMode = self.GetSelectedMatchGameMode();
            bool started = false;
            try
            {
                started = await self.SendMatchRequest(gameMode);
            }
            finally
            {
                LobbyPanelComponent lobbyPanel = selfRef;
                if (!started && lobbyPanel != null && !lobbyPanel.IsDisposed)
                {
                    lobbyPanel.IsMatchFlowRunning = false;
                }
            }
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventRoleToggleInvoke)]
        private static async ETTask OnEventRoleToggleInvoke(this LobbyPanelComponent self)
        {
            self.CloseBattleRecordOverlay();
            self.ShowPanel(self.u_ComRolePanelRectTransform);
            if (self.IsInHomeScene())
            {
                await self.Root().CloseHomePanelAsync(false);
            }

            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventEquipToggleInvoke)]
        private static async ETTask OnEventEquipToggleInvoke(this LobbyPanelComponent self)
        {
            self.CloseBattleRecordOverlay();
            self.ShowPanel(self.u_ComEquipPanelRectTransform);
            self.RefreshLoadoutContentTabUi();
            self.TryRefreshLoadoutUi(true);
            if (self.IsInHomeScene())
            {
                await self.Root().CloseHomePanelAsync(false);
            }

            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventMatchToggleInvoke)]
        private static async ETTask OnEventMatchToggleInvoke(this LobbyPanelComponent self)
        {
            self.ShowPanel(self.u_ComMatchPanelRectTransform);
            self.BindMatchModeButtons();
            self.RefreshMatchModeSelection();
            self.InitBattleRecordUi();
            self.CloseBattleRecordOverlay();
            if (self.IsInHomeScene())
            {
                await self.Root().CloseHomePanelAsync(false);
            }

            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventBuildToggleInvoke)]
        private static async ETTask OnEventBuildToggleInvoke(this LobbyPanelComponent self)
        {
            self.CloseBattleRecordOverlay();
            if (self.IsInHomeScene())
            {
                self.ShowPanel(self.u_ComRolePanelRectTransform);
                await self.Root().OpenHomePanelAsync();
                return;
            }

            self.ShowPanel(self.u_ComBuildPanelRectTransform);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventExploreToggleInvoke)]
        private static async ETTask OnEventExploreToggleInvoke(this LobbyPanelComponent self)
        {
            self.CloseBattleRecordOverlay();
            self.ShowPanel(self.u_ComExplorePanelRectTransform);
            if (self.IsInHomeScene())
            {
                await self.Root().CloseHomePanelAsync(false);
            }

            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventOneOneMatchButtonInvoke)]
        private static async ETTask OnEventOneOneMatchButtonInvoke(this LobbyPanelComponent self)
        {
            self.SelectMatchGameMode(GameModeType.OneVsOne);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventSouDaCeMatchButtonInvoke)]
        private static async ETTask OnEventSouDaCeMatchButtonInvoke(this LobbyPanelComponent self)
        {
            self.SelectMatchGameMode(GameModeType.Extraction);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventThreeThreeMatchButtonInvoke)]
        private static async ETTask OnEventThreeThreeMatchButtonInvoke(this LobbyPanelComponent self)
        {
            self.SelectMatchGameMode(GameModeType.ThreeVsThree);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventClickPutIntoBagInvoke)]
        private static async ETTask OnEventClickPutIntoBagInvoke(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            if (await self.TryTakeSelectedWarehouseToAreaAsync(LoadoutAreaType.Bag))
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.OpenEquipSelectView(EquipSlotType.BagContent);
        }

        
        [YIUIInvoke(LobbyPanelComponent.OnEventClickBagInvoke)]
        private static async ETTask OnEventClickBagInvoke(this LobbyPanelComponent self)
        {
            await self.ConfirmLoadoutAsync();
        }
        
        [YIUIInvoke(LobbyPanelComponent.OnEventOneKeyUnloadButtonInvoke)]
        private static async ETTask OnEventOneKeyUnloadButtonInvoke(this LobbyPanelComponent self)
        {
            G2C_LoadoutOneKeyUnload response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(C2G_LoadoutOneKeyUnload.Create()) as G2C_LoadoutOneKeyUnload;
            if (response == null || response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[LoadoutUI] OneKeyUnload failed: error={response?.Error}, message={response?.Message}");
            }
        }
        
        [YIUIInvoke(LobbyPanelComponent.OnEventWarehouseInvoke)]
        private static async ETTask OnEventWarehouseInvoke(this LobbyPanelComponent self)
        {
            self.SwitchLoadoutContentTab(true);
            await ETTask.CompletedTask;
        }
        
        [YIUIInvoke(LobbyPanelComponent.OnEventEquipInvoke)]
        private static async ETTask OnEventEquipInvoke(this LobbyPanelComponent self)
        {
            self.SwitchLoadoutContentTab(false);
            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束

        #region 页签切换逻辑

        /// <summary>
        /// 显示指定面板，隐藏其他面板
        /// </summary>
        private static void ShowPanel(this LobbyPanelComponent self, RectTransform targetPanel)
        {
            self.u_ComRolePanelRectTransform.gameObject.SetActive(self.u_ComRolePanelRectTransform == targetPanel);
            self.u_ComEquipPanelRectTransform.gameObject.SetActive(self.u_ComEquipPanelRectTransform == targetPanel);
            self.u_ComMatchPanelRectTransform.gameObject.SetActive(self.u_ComMatchPanelRectTransform == targetPanel);
            self.u_ComBuildPanelRectTransform.gameObject.SetActive(self.u_ComBuildPanelRectTransform == targetPanel);
            self.u_ComExplorePanelRectTransform.gameObject.SetActive(self.u_ComExplorePanelRectTransform == targetPanel);
            if (self.u_ComBuildPanelRectTransform == targetPanel)
            {
                self.RefreshHomeUiNow();
            }
        }

        private static bool IsInHomeScene(this LobbyPanelComponent self)
        {
            Scene currentScene = self?.Root()?.CurrentScene();
            return currentScene != null &&
                !currentScene.IsDisposed &&
                currentScene.Name.GetSceneConfigName() == "Home";
        }

        private static void SwitchLoadoutContentTab(this LobbyPanelComponent self, bool showWarehouse)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            bool changed = self.IsWarehouseTabActive != showWarehouse;
            self.IsWarehouseTabActive = showWarehouse;
            self.RefreshLoadoutContentTabUi();
            if (!changed)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            self.TryRefreshLoadoutUi(true);
        }

        private static void RefreshLoadoutContentTabUi(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.ResolveLoadoutContentTabRefs();

            bool showWarehouse = self.IsWarehouseTabActive;
            if (self.LoadoutEquipContentRoot != null)
            {
                self.LoadoutEquipContentRoot.gameObject.SetActive(!showWarehouse);
            }

            if (self.LoadoutWarehouseContentRoot != null)
            {
                self.LoadoutWarehouseContentRoot.gameObject.SetActive(showWarehouse);
            }

            if (self.u_ComCurrentBagRoot != null)
            {
                self.u_ComCurrentBagRoot.gameObject.SetActive(!showWarehouse);
            }

            if (self.u_ComSecureBagRoot != null)
            {
                self.u_ComSecureBagRoot.gameObject.SetActive(!showWarehouse);
            }

            if (self.u_ComWarehouseRoot != null)
            {
                self.u_ComWarehouseRoot.gameObject.SetActive(showWarehouse);
            }

            self.ApplyLoadoutContentTabVisual(
                self.LoadoutEquipTabButton,
                self.LoadoutEquipTabGraphic,
                self.LoadoutEquipTabText,
                !showWarehouse);
            self.ApplyLoadoutContentTabVisual(
                self.LoadoutWarehouseTabButton,
                self.LoadoutWarehouseTabGraphic,
                self.LoadoutWarehouseTabText,
                showWarehouse);
        }

        private static void ResolveLoadoutContentTabRefs(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (self.LoadoutEquipContentRoot == null && self.u_ComEquipPanelRectTransform != null)
            {
                self.LoadoutEquipContentRoot = FindDirectChildRectTransform(self.u_ComEquipPanelRectTransform, "Equip");
            }

            if (self.LoadoutWarehouseContentRoot == null && self.u_ComEquipPanelRectTransform != null)
            {
                self.LoadoutWarehouseContentRoot = FindDirectChildRectTransform(self.u_ComEquipPanelRectTransform, "Warehouse");
            }

            if (self.LoadoutContentSwitchRoot == null)
            {
                self.LoadoutContentSwitchRoot = FindDescendantRectTransform(self.u_ComEquipPanelRectTransform, "Switch");
            }

            if (self.LoadoutContentSwitchRoot == null)
            {
                return;
            }

            if (self.LoadoutEquipTabButton == null)
            {
                RectTransform equipTabRect = FindDirectChildRectTransform(self.LoadoutContentSwitchRoot, "Equit", "Equip");
                if (equipTabRect != null)
                {
                    self.LoadoutEquipTabButton = equipTabRect.GetComponent<Button>();
                    self.LoadoutEquipTabGraphic = equipTabRect.GetComponent<Graphic>();
                    self.LoadoutEquipTabText = equipTabRect.GetComponentInChildren<TMP_Text>(true);
                }
            }

            if (self.LoadoutWarehouseTabButton == null)
            {
                RectTransform warehouseTabRect = FindDirectChildRectTransform(self.LoadoutContentSwitchRoot, "Warehouse");
                if (warehouseTabRect != null)
                {
                    self.LoadoutWarehouseTabButton = warehouseTabRect.GetComponent<Button>();
                    self.LoadoutWarehouseTabGraphic = warehouseTabRect.GetComponent<Graphic>();
                    self.LoadoutWarehouseTabText = warehouseTabRect.GetComponentInChildren<TMP_Text>(true);
                }
            }
        }

        private static void ApplyLoadoutContentTabVisual(
            this LobbyPanelComponent self,
            Button button,
            Graphic graphic,
            TMP_Text text,
            bool selected)
        {
            _ = self;
            Color32 selectedGraphicColor = new Color32(255, 214, 124, 255);
            Color32 unselectedGraphicColor = new Color32(255, 255, 255, 255);
            Color32 selectedTextColor = new Color32(92, 57, 20, 255);
            Color32 unselectedTextColor = new Color32(50, 50, 50, 255);

            if (graphic != null)
            {
                graphic.color = selected ? selectedGraphicColor : unselectedGraphicColor;
            }

            if (text != null)
            {
                text.color = selected ? selectedTextColor : unselectedTextColor;
            }

            if (button != null)
            {
                button.targetGraphic = graphic;
            }
        }

        #endregion

        #region 英雄列表逻辑

        private static async ETTask RefreshHeroList(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            Canvas.ForceUpdateCanvases();
            LoadoutComponent loadout = self.Root()?.GetComponent<LoadoutComponent>();
            int requestColumnCount = self.GetWarehouseRequestColumnCount(loadout);
            Log.Info(
                $"[WarehouseUI] boardWidth={GetWarehouseRectWidth(self.GetWarehouseBoardRoot()):F2}, " +
                $"suggestedColumns={self.GetWarehouseSuggestedColumnCount()}, " +
                $"requestColumns={requestColumnCount}, " +
                $"currentColumns={(loadout?.WarehouseColumnCount ?? 0)}");
            C2G_GetHeroList request = C2G_GetHeroList.Create();
            request.WarehouseColumnCount = requestColumnCount;
            G2C_GetHeroList response = (G2C_GetHeroList)await self.Root().GetComponent<ClientSenderComponent>().Call(request);
            self = selfRef;

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"获取英雄列表失败: {response.Error}");
                return;
            }

            loadout = self.Root().GetComponent<LoadoutComponent>() ?? self.Root().AddComponent<LoadoutComponent>();
            loadout.Heroes.Clear();
            foreach (var hero in response.Heroes)
            {
                loadout.Heroes.Add(new HeroInfo
                {
                    HeroConfigId = hero.HeroConfigId,
                    Name = hero.Name,
                    UnitConfigId = hero.UnitConfigId
                });
            }

            LoadoutClientStateHelper.ApplyGetHeroList(loadout, response);
            loadout.AllowFreeSelection = false;
            if (loadout.SelectedHeroConfigId <= 0 && loadout.Heroes.Count > 0)
            {
                loadout.SelectedHeroConfigId = loadout.Heroes[0].HeroConfigId;
            }

            self.HasLoadoutSnapshot = true;
            self.LastLoadoutSnapshot = string.Empty;

            self.HeroLoop.ClearSelect();
            await self.HeroLoop.SetDataRefresh(loadout.Heroes, 0);
            self = selfRef;

            // 默认选中第一个英雄
            var loadout2 = self.Root().GetComponent<LoadoutComponent>();
            if (loadout2.Heroes.Count > 0)
            {
                if (loadout2.SelectedHeroConfigId <= 0)
                {
                    loadout2.SelectedHeroConfigId = loadout2.Heroes[0].HeroConfigId;
                }

                await self.RefreshHeroDisplay(loadout2.SelectedHeroConfigId);
                self = selfRef;
            }

            self?.TryRefreshLoadoutUi(true);
        }

        [EntitySystem]
        private static void YIUILoopRenderer(
            this LobbyPanelComponent self,
            HeroSelectItemComponent item,
            HeroInfo data,
            int index,
            bool select)
        {
            item.u_DataHeroName.SetValue(data.Name);
            item.SetSelected(select);

            UnitConfig unitConfig = UnitConfigCategory.Instance.GetOrDefault(data.UnitConfigId);
            item.SetHeroIcon(unitConfig?.HeadIcon);
        }

        [EntitySystem]
        private static void YIUILoopOnClick(
            this LobbyPanelComponent self,
            HeroSelectItemComponent item,
            HeroInfo data,
            int index,
            bool select)
        {
            item.SetSelected(select);
            if (select)
            {
                LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
                loadout.SelectedHeroConfigId = data.HeroConfigId;
                loadout.IsConfirmed = false;
                self.RefreshHeroDisplay(data.HeroConfigId).Coroutine();
                Log.Info($"选中英雄: {data.Name} (ConfigId: {data.HeroConfigId})");
            }
        }

        #endregion

        #region 匹配逻辑

        private static void SelectMatchGameMode(this LobbyPanelComponent self, int gameMode)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectedMatchGameMode = NormalizeMatchGameMode(gameMode);
            self.RefreshMatchModeSelection();
        }

        private static int GetSelectedMatchGameMode(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return GameModeType.OneVsOne;
            }

            self.SelectedMatchGameMode = NormalizeMatchGameMode(self.SelectedMatchGameMode);
            return self.SelectedMatchGameMode;
        }

        private static int NormalizeMatchGameMode(int gameMode)
        {
            return gameMode switch
            {
                GameModeType.PVE => GameModeType.PVE,
                GameModeType.OneVsOne => GameModeType.OneVsOne,
                GameModeType.ThreeVsThree => GameModeType.ThreeVsThree,
                GameModeType.Extraction => GameModeType.Extraction,
                _ => GameModeType.OneVsOne,
            };
        }

        private static void BindMatchModeButtons(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed || self.u_ComMatchPanelRectTransform == null)
            {
                return;
            }

            List<RectTransform> buttonRects = self.GetMatchModeButtonRects();
            if (buttonRects.Count == 0)
            {
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            for (int i = 0; i < buttonRects.Count; ++i)
            {
                RectTransform buttonRect = buttonRects[i];
                if (!TryResolveMatchGameMode(buttonRect, out int gameMode))
                {
                    continue;
                }

                DisableLegacyMatchModeClick(buttonRect);

                Button button = GetMatchModeButton(buttonRect);
                if (button == null)
                {
                    Log.Warning($"[MatchPanel] 地图项缺少 Button 组件: {buttonRect.name}");
                    continue;
                }

                int captureGameMode = gameMode;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    LobbyPanelComponent panel = selfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.SelectMatchGameMode(captureGameMode);
                });
            }
        }

        private static List<RectTransform> GetMatchModeButtonRects(this LobbyPanelComponent self)
        {
            List<RectTransform> result = new();
            if (self == null || self.IsDisposed || self.u_ComMatchPanelRectTransform == null)
            {
                return result;
            }

            HashSet<int> addedIds = new();
            RectTransform matchPanel = self.u_ComMatchPanelRectTransform;
            RectTransform content = FindDescendantRectTransform(matchPanel, "Content");
            if (content != null)
            {
                for (int i = 0; i < content.childCount; ++i)
                {
                    RectTransform child = content.GetChild(i) as RectTransform;
                    if (!TryResolveMatchGameMode(child, out _))
                    {
                        continue;
                    }

                    AddMatchModeButtonRect(result, addedIds, child);
                }
            }

            for (int i = 0; i < matchPanel.childCount; ++i)
            {
                RectTransform child = matchPanel.GetChild(i) as RectTransform;
                if (!TryResolveMatchGameMode(child, out _))
                {
                    continue;
                }

                AddMatchModeButtonRect(result, addedIds, child);
            }

            return result;
        }

        private static void AddMatchModeButtonRect(List<RectTransform> result, HashSet<int> addedIds, RectTransform buttonRect)
        {
            if (buttonRect == null)
            {
                return;
            }

            int instanceId = buttonRect.GetInstanceID();
            if (!addedIds.Add(instanceId))
            {
                return;
            }

            result.Add(buttonRect);
        }

        private static bool TryResolveMatchGameMode(RectTransform buttonRect, out int gameMode)
        {
            gameMode = 0;
            if (buttonRect == null)
            {
                return false;
            }

            string normalizedName = NormalizeMatchModeName(buttonRect.name);
            if (string.IsNullOrEmpty(normalizedName) ||
                normalizedName == "startbutton" ||
                normalizedName == "loopscrollverticalgroup" ||
                normalizedName == "content" ||
                normalizedName == "cache" ||
                normalizedName == "viewport")
            {
                return false;
            }

            if (normalizedName.Contains("singleplayer") || normalizedName.Contains("single") || normalizedName.Contains("pve"))
            {
                gameMode = GameModeType.PVE;
                return true;
            }

            if (normalizedName.Contains("oneone") || normalizedName.Contains("onevsone") || normalizedName.Contains("1v1"))
            {
                gameMode = GameModeType.OneVsOne;
                return true;
            }

            if (normalizedName.Contains("threethree") || normalizedName.Contains("threevsthree") || normalizedName.Contains("3v3"))
            {
                gameMode = GameModeType.ThreeVsThree;
                return true;
            }

            if (normalizedName.Contains("soudace") || normalizedName.Contains("extraction") || normalizedName.Contains("sdc"))
            {
                gameMode = GameModeType.Extraction;
                return true;
            }

            return false;
        }

        private static string NormalizeMatchModeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return name
                    .Replace(" ", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("(", string.Empty)
                    .Replace(")", string.Empty)
                    .ToLowerInvariant();
        }

        private static void DisableLegacyMatchModeClick(RectTransform buttonRect)
        {
            if (buttonRect == null)
            {
                return;
            }

            UIEventBind[] eventBinds = buttonRect.GetComponentsInChildren<UIEventBind>(true);
            for (int i = 0; i < eventBinds.Length; ++i)
            {
                UIEventBind eventBind = eventBinds[i];
                if (eventBind == null || !eventBind.enabled)
                {
                    continue;
                }

                eventBind.enabled = false;
            }
        }

        private static Button GetMatchModeButton(RectTransform buttonRect)
        {
            if (buttonRect == null)
            {
                return null;
            }

            Button button = buttonRect.GetComponent<Button>();
            return button != null ? button : buttonRect.GetComponentInChildren<Button>(true);
        }

        private static void RefreshMatchModeSelection(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            List<RectTransform> buttonRects = self.GetMatchModeButtonRects();
            if (buttonRects.Count == 0)
            {
                return;
            }

            int selectedGameMode = self.GetSelectedMatchGameMode();
            int firstAvailableGameMode = 0;
            bool hasSelectedGameMode = false;
            for (int i = 0; i < buttonRects.Count; ++i)
            {
                if (!TryResolveMatchGameMode(buttonRects[i], out int gameMode))
                {
                    continue;
                }

                if (firstAvailableGameMode == 0)
                {
                    firstAvailableGameMode = gameMode;
                }

                if (gameMode == selectedGameMode)
                {
                    hasSelectedGameMode = true;
                }
            }

            if (!hasSelectedGameMode && firstAvailableGameMode > 0)
            {
                selectedGameMode = firstAvailableGameMode;
                self.SelectedMatchGameMode = firstAvailableGameMode;
            }

            for (int i = 0; i < buttonRects.Count; ++i)
            {
                RectTransform buttonRect = buttonRects[i];
                if (!TryResolveMatchGameMode(buttonRect, out int gameMode))
                {
                    continue;
                }

                ApplyMatchModeButtonSelection(buttonRect, selectedGameMode == gameMode);
            }
        }

        private static void ApplyMatchModeButtonSelection(RectTransform buttonRect, bool selected)
        {
            if (buttonRect == null)
            {
                return;
            }

            const string selectionMarkerName = "SelectedHighlight";
            if (!selected)
            {
                Transform existingMarker = buttonRect.Find(selectionMarkerName);
                if (existingMarker != null)
                {
                    existingMarker.gameObject.SetActive(false);
                }

                return;
            }

            RectTransform marker = EnsureMatchModeSelectionMarker(buttonRect, selectionMarkerName);
            if (marker != null)
            {
                marker.gameObject.SetActive(true);
            }
        }

        private static RectTransform EnsureMatchModeSelectionMarker(RectTransform buttonRect, string markerName)
        {
            Transform existingMarker = buttonRect.Find(markerName);
            if (existingMarker is RectTransform existingRect)
            {
                return existingRect;
            }

            Sprite borderSprite = GetMatchModeSelectionBorderSprite(buttonRect);
            if (borderSprite == null)
            {
                Log.Warning($"[MatchPanel] 无法创建选中高亮，缺少可用 Sprite: {buttonRect.name}");
                return null;
            }

            GameObject markerObject = new GameObject(markerName, typeof(RectTransform));
            RectTransform markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.SetParent(buttonRect, false);
            markerRect.anchorMin = Vector2.zero;
            markerRect.anchorMax = Vector2.one;
            markerRect.offsetMin = Vector2.zero;
            markerRect.offsetMax = Vector2.zero;
            markerRect.SetAsLastSibling();

            CreateMatchModeSelectionEdge(markerRect, "Top", borderSprite, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 10f), new Vector2(0f, -5f));
            CreateMatchModeSelectionEdge(markerRect, "Bottom", borderSprite, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 5f));
            CreateMatchModeSelectionEdge(markerRect, "Left", borderSprite, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(5f, 0f));
            CreateMatchModeSelectionEdge(markerRect, "Right", borderSprite, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-5f, 0f));

            markerObject.SetActive(false);
            return markerRect;
        }

        private static void CreateMatchModeSelectionEdge(
            RectTransform parent,
            string edgeName,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            GameObject edgeObject = new GameObject(edgeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
            edgeRect.SetParent(parent, false);
            edgeRect.anchorMin = anchorMin;
            edgeRect.anchorMax = anchorMax;
            edgeRect.sizeDelta = sizeDelta;
            edgeRect.anchoredPosition = anchoredPosition;

            Image edgeImage = edgeObject.GetComponent<Image>();
            edgeImage.sprite = sprite;
            edgeImage.type = Image.Type.Sliced;
            edgeImage.raycastTarget = false;
            edgeImage.color = new Color(1f, 0.72f, 0.15f, 1f);
        }

        private static Sprite GetMatchModeSelectionBorderSprite(RectTransform buttonRect)
        {
            Image buttonImage = buttonRect.GetComponent<Image>();
            if (buttonImage != null && buttonImage.sprite != null)
            {
                return buttonImage.sprite;
            }

            Button button = GetMatchModeButton(buttonRect);
            Image targetImage = button?.targetGraphic as Image;
            return targetImage?.sprite;
        }

        /// <summary>
        /// 发送匹配请求
        /// </summary>
        private static async ETTask<bool> SendMatchRequest(this LobbyPanelComponent self, int gameMode)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = self.Root();

            bool confirmSuccess = await self.ConfirmLoadoutAsync();
            self = selfRef;
            if (self == null || self.IsDisposed || !confirmSuccess)
            {
                return false;
            }

            C2G_MatchRequest request = C2G_MatchRequest.Create();
            request.GameMode = gameMode;

            G2C_MatchRequest response = (G2C_MatchRequest)await self.Root().GetComponent<ClientSenderComponent>().Call(request);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"匹配请求失败: {response.Error}");
                return false;
            }

            Log.Info($"匹配请求成功，RequestId: {response.RequestId}, GameMode: {gameMode}，等待匹配...");

            // 打开匹配等待弹窗
            await self.UIPanel.OpenViewAsync<MatchViewComponent>();
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            self.WaitMatchEnterMapFlow(rootRef).Coroutine();
            return true;
        }

        private static async ETTask WaitMatchEnterMapFlow(this LobbyPanelComponent self, EntityRef<Scene> rootRef)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            try
            {
                // 匹配成功后先关闭等待弹窗，避免后续回 Home 时残留旧界面
                Scene matchSuccessRoot = rootRef;
                if (matchSuccessRoot == null || matchSuccessRoot.IsDisposed)
                {
                    return;
                }

                await matchSuccessRoot.GetComponent<ObjectWait>().Wait<Wait_MatchSuccess>();

                Scene closeWaitingRoot = rootRef;
                if (closeWaitingRoot == null || closeWaitingRoot.IsDisposed)
                {
                    return;
                }

                await closeWaitingRoot.CloseMatchWaitingViewAsync(false);

                // 服务端匹配成功后会自动传送玩家，客户端只需等待场景切换完成
                Scene sceneChangeRoot = rootRef;
                if (sceneChangeRoot == null || sceneChangeRoot.IsDisposed)
                {
                    return;
                }

                await sceneChangeRoot.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();

                Scene finishRoot = rootRef;
                if (finishRoot == null || finishRoot.IsDisposed)
                {
                    return;
                }

                // 发布 EnterMapFinish 事件，关闭 Loading 面板
                EventSystem.Instance.Publish(finishRoot, new EnterMapFinish());

                // 关闭 Lobby 面板（MatchView 会随面板一起关闭），改走 root 级别兜底，避免 stale UIPanel 截断进图链路。
                await finishRoot.CloseLobbyPanelAsync(false);
            }
            catch (Exception ex)
            {
                Log.Error($"[LobbyUI] wait match enter map flow failed: {ex}");
            }
            finally
            {
                LobbyPanelComponent lobbyPanel = selfRef;
                if (lobbyPanel != null && !lobbyPanel.IsDisposed)
                {
                    lobbyPanel.IsMatchFlowRunning = false;
                }
            }
        }

        private static async ETTask CloseMatchWaitingViewAsync(this LobbyPanelComponent self, bool tween = true)
        {
            await self?.Root().CloseMatchWaitingViewAsync(tween);
        }

        private static async ETTask CloseMatchWaitingViewAsync(this Scene root, bool tween = true)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            YIUIMgrComponent yiuiMgr = root.GetComponent<YIUIMgrComponent>();
            if (yiuiMgr == null || yiuiMgr.IsDisposed)
            {
                return;
            }

            LobbyPanelComponent lobbyPanel = yiuiMgr.GetPanel<LobbyPanelComponent>();
            if (lobbyPanel == null || lobbyPanel.IsDisposed || lobbyPanel.UIPanel == null)
            {
                return;
            }

            try
            {
                await lobbyPanel.UIPanel.CloseViewAsync<MatchViewComponent>(tween);
            }
            catch (Exception ex)
            {
                Log.Warning($"[LobbyUI] close match waiting view ignored exception: {ex.Message}");
            }
        }

        private static async ETTask CloseLobbyPanelAsync(this Scene root, bool tween = true)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            YIUIMgrComponent yiuiMgr = root.GetComponent<YIUIMgrComponent>();
            if (yiuiMgr == null || yiuiMgr.IsDisposed)
            {
                return;
            }

            try
            {
                await yiuiMgr.ClosePanelAsync<LobbyPanelComponent>(tween);
            }
            catch (Exception ex)
            {
                Log.Warning($"[LobbyUI] close lobby panel ignored exception: {ex.Message}");
            }
        }

        private static async ETTask<bool> ConfirmLoadoutAsync(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>() ?? self.Root().AddComponent<LoadoutComponent>();
            C2G_ConfirmLoadout confirmReq = C2G_ConfirmLoadout.Create();
            confirmReq.HeroConfigId = loadout.SelectedHeroConfigId;
            if (confirmReq.HeroConfigId <= 0 && loadout.Heroes.Count > 0)
            {
                confirmReq.HeroConfigId = loadout.Heroes[0].HeroConfigId;
            }

            if (confirmReq.HeroConfigId <= 0)
            {
                confirmReq.HeroConfigId = HeroConfigHelper.GetDefaultHeroConfigId();
            }

            confirmReq.MainWeaponConfigId = loadout.MainWeaponConfigId;
            confirmReq.SubWeaponConfigId = loadout.SubWeaponConfigId;
            confirmReq.ArmorConfigId = loadout.ArmorConfigId;
            confirmReq.BackpackConfigId = loadout.BackpackConfigId;
            confirmReq.BagWidth = loadout.BagWidth;
            confirmReq.BagHeight = loadout.BagHeight;
            confirmReq.SecureWidth = loadout.SecureWidth;
            confirmReq.SecureHeight = loadout.SecureHeight;
            FillGridItemMessage(loadout.CarriedBagItems, confirmReq.FinalBagItems);
            FillGridItemMessage(loadout.CarriedSecureItems, confirmReq.FinalSecureItems);
            Log.Info($"[LoadoutConfirm] request hero={confirmReq.HeroConfigId}, main={confirmReq.MainWeaponConfigId}, sub={confirmReq.SubWeaponConfigId}, armor={confirmReq.ArmorConfigId}");

            G2C_ConfirmLoadout confirmResp = (G2C_ConfirmLoadout)await self.Root().GetComponent<ClientSenderComponent>().Call(confirmReq);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            loadout = self.Root().GetComponent<LoadoutComponent>();
            if (confirmResp.Error != ErrorCode.ERR_Success)
            {
                if (loadout != null)
                {
                    loadout.IsConfirmed = false;
                }

                Log.Error($"[LoadoutConfirm] failed: error={confirmResp.Error}, message={confirmResp.Message}");
                return false;
            }

            if (loadout != null)
            {
                loadout.SelectedHeroConfigId = confirmReq.HeroConfigId;
                loadout.MainWeaponConfigId = confirmReq.MainWeaponConfigId;
                loadout.SubWeaponConfigId = confirmReq.SubWeaponConfigId;
                loadout.ArmorConfigId = confirmReq.ArmorConfigId;
                loadout.BackpackConfigId = confirmReq.BackpackConfigId;
                loadout.BagWidth = confirmReq.BagWidth;
                loadout.BagHeight = confirmReq.BagHeight;
                loadout.SecureWidth = confirmReq.SecureWidth;
                loadout.SecureHeight = confirmReq.SecureHeight;
                loadout.ConsumableConfigIds.Clear();
                loadout.AllowFreeSelection = false;
                loadout.IsConfirmed = true;
            }

            self.TryRefreshLoadoutUi(true);
            Log.Info($"[LoadoutConfirm] success: hero={confirmReq.HeroConfigId}, main={confirmReq.MainWeaponConfigId}, sub={confirmReq.SubWeaponConfigId}, armor={confirmReq.ArmorConfigId}");
            return true;
        }

        #endregion

        #region 装备系统逻辑

        /// <summary>
        /// 初始化装备槽位
        /// </summary>
        private static void InitEquipSlots(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            self.UIEquipSlotItemWeapon.u_DataSlotName.SetValue("武器1");
            self.UIEquipSlotItemWeapon.u_DataIsEmpty.SetValue(true);
            self.UIEquipSlotItemWeapon.SetItemIcon(string.Empty);
            BindFixedSlotButton(self.UIEquipSlotItemWeapon, selfRef, EquipSlotType.Weapon);
            self.BindFixedSlotDragInteract(self.UIEquipSlotItemWeapon, EquipSlotType.Weapon);

            self.UIEquipSlotItemWeapon2.u_DataSlotName.SetValue("武器2");
            self.UIEquipSlotItemWeapon2.u_DataIsEmpty.SetValue(true);
            self.UIEquipSlotItemWeapon2.SetItemIcon(string.Empty);
            BindFixedSlotButton(self.UIEquipSlotItemWeapon2, selfRef, EquipSlotType.Weapon2);
            self.BindFixedSlotDragInteract(self.UIEquipSlotItemWeapon2, EquipSlotType.Weapon2);

            self.UIEquipSlotItemArmor.u_DataSlotName.SetValue("防具");
            self.UIEquipSlotItemArmor.u_DataIsEmpty.SetValue(true);
            self.UIEquipSlotItemArmor.SetItemIcon(string.Empty);
            BindFixedSlotButton(self.UIEquipSlotItemArmor, selfRef, EquipSlotType.Armor);
            self.BindFixedSlotDragInteract(self.UIEquipSlotItemArmor, EquipSlotType.Armor);

            self.UIEquipSlotItemBag.u_DataSlotName.SetValue("背包");
            self.UIEquipSlotItemBag.u_DataIsEmpty.SetValue(true);
            self.UIEquipSlotItemBag.SetItemIcon(string.Empty);
            BindFixedSlotButton(self.UIEquipSlotItemBag, selfRef, EquipSlotType.Bag);
            self.BindFixedSlotDragInteract(self.UIEquipSlotItemBag, EquipSlotType.Bag);
        }

        private static void BindFixedSlotButton(EquipSlotItemComponent slotItem, EntityRef<LobbyPanelComponent> panelRef, EquipSlotType slotType)
        {
            Button button = slotItem?.UIBase?.OwnerGameObject?.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
        }

        private static async ETTask HandleFixedSlotClickAsync(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            int currentConfigId = GetCurrentFixedSlotConfigId(self.Root()?.GetComponent<LoadoutComponent>(), slotType);
            if (currentConfigId > 0)
            {
                await self.OpenItemClickedAsync(currentConfigId, false);
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            if (await self.TryEquipSelectedWarehouseIntoFixedSlotAsync(slotType))
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.OpenEquipSelectView(slotType);
        }

        private static async ETTask OpenItemClickedAsync(this LobbyPanelComponent self, int configId, bool allowEquipAction, long itemUid = 0)
        {
            if (self == null || self.IsDisposed || configId <= 0)
            {
                return;
            }

            ItemClickedComponent itemClicked = self.GetOrCreateItemClickedCommon();
            if (itemClicked == null || itemClicked.IsDisposed)
            {
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            ItemClickedOpenData openData = new()
            {
                LobbyPanelRef = self,
                ConfigId = configId,
                ItemUid = itemUid,
                AllowEquipAction = allowEquipAction,
            };

            await YIUIEventSystem.Open(itemClicked, openData);
            self = selfRef;
        }

        private static ItemClickedComponent GetOrCreateItemClickedCommon(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return null;
            }

            ItemClickedComponent existing = self.ItemClickedCommon;
            if (existing != null && !existing.IsDisposed)
            {
                return existing;
            }

            RectTransform root = self.UIBase?.OwnerRectTransform;
            if (root == null)
            {
                return null;
            }

            Transform parent = root.FindChildByName($"{ItemClickedComponent.ResName}{YIUIConstHelper.Const.UIParentName}");
            if (parent == null)
            {
                Log.Warning("[LobbyPanel] 未找到 ItemClickedParent");
                return null;
            }

            ItemClickedComponent created = YIUIFactory.Instantiate<ItemClickedComponent>(self.Scene(), self, parent) as ItemClickedComponent;
            if (created == null)
            {
                return null;
            }

            created.UIBase?.SetActive(false);
            self.ItemClickedCommon = created;
            return created;
        }

        /// <summary>
        /// 打开装备选择界面
        /// </summary>
        private static async ETTask OpenEquipSelectView(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            self.CurrentSelectingSlot = slotType;

            EquipSelectViewComponent equipSelectView = await self.UIPanel.OpenViewAsync<EquipSelectViewComponent>();
            self = selfRef;

            if (self == null || self.IsDisposed || equipSelectView == null)
            {
                Log.Error("打开装备选择界面失败");
                return;
            }

            equipSelectView.m_LobbyPanel = self;
            equipSelectView.CurrentSlotType = slotType;
            equipSelectView.CurrentItemSourceMode = self.CurrentItemSourceMode;
            await self.RefreshEquipSelectView(equipSelectView, slotType);
        }

        public static async ETTask SwitchEquipSelectSourceModeAsync(
            this LobbyPanelComponent self,
            EquipSelectViewComponent view,
            LoadoutItemSourceMode sourceMode)
        {
            if (self == null || self.IsDisposed || view == null || view.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (view.CurrentItemSourceMode == sourceMode)
            {
                await ETTask.CompletedTask;
                return;
            }

            view.CurrentItemSourceMode = sourceMode;
            await self.RefreshEquipSelectView(view, view.CurrentSlotType);
        }

        /// <summary>
        /// 刷新装备选择界面
        /// </summary>
        private static async ETTask RefreshEquipSelectView(this LobbyPanelComponent self, EquipSelectViewComponent view, EquipSlotType slotType)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<EquipSelectViewComponent> viewRef = view;

            List<LoadoutWarehouseItemViewData> equipList = self.GetEquipListBySlotType(slotType, view.CurrentItemSourceMode);

            if (view.EquipLoop == null)
            {
                Log.Error("装备选择界面的 LoopScroll 未初始化");
                return;
            }

            view.EquipLoop.ClearSelect();
            if (equipList.Count > 0)
            {
                await view.EquipLoop.SetDataRefresh(equipList, 0);
            }
            else
            {
                await view.EquipLoop.SetDataRefresh(equipList);
            }
            self = selfRef;
            view = viewRef;

            if (self == null || view == null || self.IsDisposed || view.IsDisposed)
            {
                return;
            }

            if (equipList.Count > 0)
            {
                EquipSelectViewPreviewHelper.UpdatePreview(view, equipList[0], true);
            }
            else
            {
                EquipSelectViewPreviewHelper.UpdatePreview(view, default, true);
            }
        }

        private static List<LoadoutWarehouseItemViewData> GetEquipListBySlotType(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            return self.GetEquipListBySlotType(slotType, self.CurrentItemSourceMode);
        }

        private static List<LoadoutWarehouseItemViewData> GetEquipListBySlotType(
            this LobbyPanelComponent self,
            EquipSlotType slotType,
            LoadoutItemSourceMode sourceMode)
        {
            LoadoutComponent loadout = self.Root()?.GetComponent<LoadoutComponent>();
            List<LoadoutWarehouseItemViewData> result = self.BuildLoadoutSourceItemList(loadout, sourceMode);
            if (slotType == EquipSlotType.BagContent)
            {
                return result;
            }

            result.RemoveAll(data => !CanConfigFitSlot(data.ConfigId, slotType));
            return result;
        }

        /// <summary>
        /// 装备物品到槽位
        /// </summary>
        public static async ETTask<bool> EquipItemAsync(
            this LobbyPanelComponent self,
            int itemConfigId,
            EquipSlotType slotType,
            LoadoutItemSourceMode sourceMode,
            long itemUid = 0)
        {
            if (itemUid <= 0 &&
                sourceMode == LoadoutItemSourceMode.Warehouse &&
                self.SelectedWarehouseConfigId == itemConfigId)
            {
                itemUid = self.SelectedWarehouseItemUid;
            }

            switch (slotType)
            {
                case EquipSlotType.Weapon:
                case EquipSlotType.Weapon2:
                case EquipSlotType.Armor:
                case EquipSlotType.Bag:
                    return await self.AcquireLoadoutItemAsync(
                        sourceMode,
                        itemConfigId,
                        LoadoutAreaType.FixedSlot,
                        ToFixedSlotType(slotType),
                        0,
                        itemUid);
                case EquipSlotType.BagContent:
                    if (!self.TryFindFirstFitAnchorSlot(LoadoutAreaType.Bag, itemConfigId, out int bagAnchorSlotIndex))
                    {
                        Log.Warning($"[LoadoutUI] Bag has no space for config={itemConfigId}");
                        return false;
                    }

                    return await self.AcquireLoadoutItemAsync(
                        sourceMode,
                        itemConfigId,
                        LoadoutAreaType.Bag,
                        LoadoutFixedSlotType.None,
                        bagAnchorSlotIndex,
                        itemUid);
                default:
                    return false;
            }
        }

        private static async ETTask<bool> TryUnloadSlotAsync(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            if (GetCurrentFixedSlotConfigId(self.Root().GetComponent<LoadoutComponent>(), slotType) <= 0)
            {
                return false;
            }

            await self.PutFixedSlotToWarehouseAsync(slotType);
            return true;
        }

        private static async ETTask<bool> TryEquipSelectedWarehouseIntoFixedSlotAsync(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            LoadoutItemSourceMode sourceMode = self.CurrentItemSourceMode;
            int selectedConfigId = sourceMode == LoadoutItemSourceMode.Shop
                ? self.SelectedShopConfigId
                : self.SelectedWarehouseConfigId;
            long selectedItemUid = sourceMode == LoadoutItemSourceMode.Warehouse ? self.SelectedWarehouseItemUid : 0;
            if (selectedConfigId <= 0 || !CanConfigFitSlot(selectedConfigId, slotType))
            {
                return false;
            }

            await self.AcquireLoadoutItemAsync(
                sourceMode,
                selectedConfigId,
                LoadoutAreaType.FixedSlot,
                ToFixedSlotType(slotType),
                0,
                selectedItemUid);
            return true;
        }

        private static async ETTask<bool> TryTakeSelectedWarehouseToAreaAsync(this LobbyPanelComponent self, LoadoutAreaType areaType)
        {
            LoadoutItemSourceMode sourceMode = self.CurrentItemSourceMode;
            int selectedConfigId = sourceMode == LoadoutItemSourceMode.Shop
                ? self.SelectedShopConfigId
                : self.SelectedWarehouseConfigId;
            long selectedItemUid = sourceMode == LoadoutItemSourceMode.Warehouse ? self.SelectedWarehouseItemUid : 0;
            if (selectedConfigId <= 0)
            {
                return false;
            }

            if (!self.TryFindFirstFitAnchorSlot(areaType, selectedConfigId, out int anchorSlotIndex))
            {
                Log.Warning($"[LoadoutUI] {areaType} has no space for config={selectedConfigId}");
                return true;
            }

            await self.AcquireLoadoutItemAsync(sourceMode, selectedConfigId, areaType, LoadoutFixedSlotType.None, anchorSlotIndex, selectedItemUid);
            return true;
        }

        private static async ETTask<bool> AcquireLoadoutItemAsync(
            this LobbyPanelComponent self,
            LoadoutItemSourceMode sourceMode,
            int configId,
            LoadoutAreaType targetAreaType,
            LoadoutFixedSlotType targetSlotType,
            int targetAnchorSlotIndex,
            long itemUid)
        {
            return sourceMode == LoadoutItemSourceMode.Shop
                ? await self.BuyShopItemAsync(configId, targetAreaType, targetSlotType, targetAnchorSlotIndex)
                : await self.TakeWarehouseItemAsync(configId, targetAreaType, targetSlotType, targetAnchorSlotIndex, itemUid);
        }

        private static async ETTask<bool> TakeWarehouseItemAsync(
            this LobbyPanelComponent self,
            int configId,
            LoadoutAreaType targetAreaType,
            LoadoutFixedSlotType targetSlotType,
            int targetAnchorSlotIndex,
            long itemUid)
        {
            C2G_LoadoutTakeFromWarehouse request = C2G_LoadoutTakeFromWarehouse.Create();
            request.ConfigId = configId;
            request.Count = 1;
            request.TargetAreaType = (int)targetAreaType;
            request.TargetSlotType = (int)targetSlotType;
            request.TargetAnchorSlotIndex = targetAnchorSlotIndex;
            request.ItemUid = itemUid;

            if (targetAreaType == LoadoutAreaType.FixedSlot && targetSlotType == LoadoutFixedSlotType.Backpack && TryResolveBackpackSize(configId, out int bagWidth, out int bagHeight))
            {
                request.TargetBagWidth = bagWidth;
                request.TargetBagHeight = bagHeight;
            }

            G2C_LoadoutTakeFromWarehouse response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(request) as G2C_LoadoutTakeFromWarehouse;
            if (response == null || response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[LoadoutUI] TakeFromWarehouse failed: config={configId}, area={targetAreaType}, slot={targetSlotType}, anchor={targetAnchorSlotIndex}, error={response?.Error}, message={response?.Message}");
                return false;
            }

            return true;
        }

        private static async ETTask<bool> BuyShopItemAsync(
            this LobbyPanelComponent self,
            int configId,
            LoadoutAreaType targetAreaType,
            LoadoutFixedSlotType targetSlotType,
            int targetAnchorSlotIndex)
        {
            C2G_LoadoutBuyFromShop request = C2G_LoadoutBuyFromShop.Create();
            request.ConfigId = configId;
            request.Count = 1;
            request.TargetAreaType = (int)targetAreaType;
            request.TargetSlotType = (int)targetSlotType;
            request.TargetAnchorSlotIndex = targetAnchorSlotIndex;

            if (targetAreaType == LoadoutAreaType.FixedSlot &&
                targetSlotType == LoadoutFixedSlotType.Backpack &&
                TryResolveBackpackSize(configId, out int bagWidth, out int bagHeight))
            {
                request.TargetBagWidth = bagWidth;
                request.TargetBagHeight = bagHeight;
            }

            G2C_LoadoutBuyFromShop response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(request) as G2C_LoadoutBuyFromShop;
            if (response == null || response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[LoadoutUI] BuyFromShop failed: config={configId}, area={targetAreaType}, slot={targetSlotType}, anchor={targetAnchorSlotIndex}, error={response?.Error}, message={response?.Message}");
                return false;
            }

            return true;
        }

        private static async ETTask<bool> PutFixedSlotToWarehouseAsync(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            C2G_LoadoutPutToWarehouse request = C2G_LoadoutPutToWarehouse.Create();
            request.SourceAreaType = (int)LoadoutAreaType.FixedSlot;
            request.SourceSlotType = (int)ToFixedSlotType(slotType);
            request.SourceAnchorSlotIndex = 0;
            request.Count = 1;
            request.WarehouseColumnCount = self.GetWarehouseRequestColumnCount(self.Root()?.GetComponent<LoadoutComponent>());

            G2C_LoadoutPutToWarehouse response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(request) as G2C_LoadoutPutToWarehouse;
            if (response == null || response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[LoadoutUI] PutToWarehouse failed: slot={slotType}, error={response?.Error}, message={response?.Message}");
                return false;
            }

            return true;
        }

        private static async ETTask<bool> MoveWarehouseItemAsync(this LobbyPanelComponent self, long itemUid, int targetAnchorSlotIndex)
        {
            if (itemUid <= 0 || targetAnchorSlotIndex < 0)
            {
                return false;
            }

            C2G_LoadoutMoveWarehouseItem request = C2G_LoadoutMoveWarehouseItem.Create();
            request.ItemUid = itemUid;
            request.TargetAnchorSlotIndex = targetAnchorSlotIndex;
            request.WarehouseColumnCount = self.GetWarehouseRequestColumnCount(self.Root()?.GetComponent<LoadoutComponent>());

            G2C_LoadoutMoveWarehouseItem response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(request) as G2C_LoadoutMoveWarehouseItem;
            if (response == null || response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[LoadoutUI] MoveWarehouseItem failed: itemUid={itemUid}, targetAnchor={targetAnchorSlotIndex}, error={response?.Error}, message={response?.Message}");
                return false;
            }

            return true;
        }

        private static void RefreshLoadoutView(this LobbyPanelComponent self)
        {
            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            if (loadout == null)
            {
                self.RefreshSlotView(self.UIEquipSlotItemWeapon, EquipSlotType.Weapon, 0, "武器1", true);
                self.RefreshSlotView(self.UIEquipSlotItemWeapon2, EquipSlotType.Weapon2, 0, "武器2", true);
                self.RefreshSlotView(self.UIEquipSlotItemArmor, EquipSlotType.Armor, 0, "防具");
                self.RefreshSlotView(self.UIEquipSlotItemBag, EquipSlotType.Bag, 0, "背包");
                self.RefreshLoadoutExtraUi(null);
                return;
            }

            self.RefreshSlotView(self.UIEquipSlotItemWeapon, EquipSlotType.Weapon, loadout.MainWeaponConfigId, "武器1", true);
            self.RefreshSlotView(self.UIEquipSlotItemWeapon2, EquipSlotType.Weapon2, loadout.SubWeaponConfigId, "武器2", true);
            self.RefreshSlotView(self.UIEquipSlotItemArmor, EquipSlotType.Armor, loadout.ArmorConfigId, "防具");
            self.RefreshSlotView(self.UIEquipSlotItemBag, EquipSlotType.Bag, loadout.BackpackConfigId, "背包");
            self.RefreshLoadoutExtraUi(loadout);
            self.RefreshCurrentHeroDescription();
        }

        private static void RefreshSlotView(
            this LobbyPanelComponent self,
            EquipSlotItemComponent slotItem,
            EquipSlotType slotType,
            int configId,
            string slotName,
            bool hideTextWhenEquipped = false)
        {
            if (slotItem == null)
            {
                return;
            }

            if (configId <= 0)
            {
                slotItem.u_DataSlotName.SetValue(slotName);
                slotItem.u_DataEquipName.SetValue(string.Empty);
                slotItem.u_DataIsEmpty.SetValue(true);
                slotItem.SetItemIcon(string.Empty);
                self.RefreshFixedSlotDragProxy(slotItem, slotType, 0);
                return;
            }

            ResolveDisplayInfo(configId, out string name, out string icon, out _);
            slotItem.u_DataSlotName.SetValue(hideTextWhenEquipped ? string.Empty : slotName);
            slotItem.u_DataEquipName.SetValue(hideTextWhenEquipped ? string.Empty : name);
            slotItem.u_DataIsEmpty.SetValue(false);
            slotItem.SetItemIcon(icon);
            self.RefreshFixedSlotDragProxy(slotItem, slotType, configId);
        }

        private static bool TryFindFirstFitAnchorSlot(this LobbyPanelComponent self, LoadoutAreaType areaType, int configId, out int anchorSlotIndex)
        {
            anchorSlotIndex = -1;

            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            List<LoadoutGridItemInfo> container = GetGridContainer(loadout, areaType);
            int width = GetGridContainerWidth(loadout, areaType);
            int height = GetGridContainerHeight(loadout, areaType);
            if (container == null || width <= 0 || height <= 0)
            {
                return false;
            }

            if (!TryResolveGridMetrics(configId, out int gridWidth, out int gridHeight))
            {
                return false;
            }

            List<GridPlacementItemInfo> placements = BuildPlacementItems(container);
            for (int i = 0; i < width * height; ++i)
            {
                placements.Add(new GridPlacementItemInfo
                {
                    ConfigId = configId,
                    Count = 1,
                    AnchorSlotIndex = i,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                });

                if (LoadoutGridPlacementHelper.ArePlacementsValid(placements, width, height))
                {
                    anchorSlotIndex = i;
                    return true;
                }

                placements.RemoveAt(placements.Count - 1);
            }

            return false;
        }

        private static bool TryResolveGridMetrics(int configId, out int gridWidth, out int gridHeight)
        {
            gridWidth = LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
            gridHeight = LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                gridWidth = itemConfig.GridWidth > 0 ? itemConfig.GridWidth : LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
                gridHeight = itemConfig.GridHeight > 0 ? itemConfig.GridHeight : LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;
                return true;
            }

            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            return equipConfig != null;
        }

        private static bool TryResolveBackpackSize(int configId, out int bagWidth, out int bagHeight)
        {
            bagWidth = 0;
            bagHeight = 0;

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig == null)
            {
                return false;
            }

            if (itemConfig.BackpackWidth <= 0 || itemConfig.BackpackHeight <= 0)
            {
                return false;
            }

            bagWidth = itemConfig.BackpackWidth;
            bagHeight = itemConfig.BackpackHeight;
            return true;
        }

        private static List<GridPlacementItemInfo> BuildPlacementItems(List<LoadoutGridItemInfo> container)
        {
            List<GridPlacementItemInfo> result = new();
            if (container == null)
            {
                return result;
            }

            for (int i = 0; i < container.Count; ++i)
            {
                LoadoutGridItemInfo item = container[i];
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

        private static void FillGridItemMessage(IList<LoadoutGridItemInfo> source, IList<LoadoutGridItemData> target)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutGridItemInfo item = source[i];
                LoadoutGridItemData data = LoadoutGridItemData.Create();
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                data.AnchorSlotIndex = item.AnchorSlotIndex;
                data.GridWidth = item.GridWidth;
                data.GridHeight = item.GridHeight;
                target.Add(data);
            }
        }

        private static List<LoadoutGridItemInfo> GetGridContainer(LoadoutComponent loadout, LoadoutAreaType areaType)
        {
            if (loadout == null)
            {
                return null;
            }

            return areaType switch
            {
                LoadoutAreaType.Bag when loadout.BackpackConfigId > 0 && loadout.BagWidth > 0 && loadout.BagHeight > 0 => loadout.CarriedBagItems,
                LoadoutAreaType.Secure when loadout.SecureWidth > 0 && loadout.SecureHeight > 0 => loadout.CarriedSecureItems,
                _ => null,
            };
        }

        private static int GetGridContainerWidth(LoadoutComponent loadout, LoadoutAreaType areaType)
        {
            return areaType switch
            {
                LoadoutAreaType.Bag => loadout?.BagWidth ?? 0,
                LoadoutAreaType.Secure => loadout?.SecureWidth ?? 0,
                _ => 0,
            };
        }

        private static int GetGridContainerHeight(LoadoutComponent loadout, LoadoutAreaType areaType)
        {
            return areaType switch
            {
                LoadoutAreaType.Bag => loadout?.BagHeight ?? 0,
                LoadoutAreaType.Secure => loadout?.SecureHeight ?? 0,
                _ => 0,
            };
        }

        private static int GetCurrentFixedSlotConfigId(LoadoutComponent loadout, EquipSlotType slotType)
        {
            if (loadout == null)
            {
                return 0;
            }

            return slotType switch
            {
                EquipSlotType.Weapon => loadout.MainWeaponConfigId,
                EquipSlotType.Weapon2 => loadout.SubWeaponConfigId,
                EquipSlotType.Armor => loadout.ArmorConfigId,
                EquipSlotType.Bag => loadout.BackpackConfigId,
                _ => 0,
            };
        }

        private static LoadoutFixedSlotType ToFixedSlotType(EquipSlotType slotType)
        {
            return slotType switch
            {
                EquipSlotType.Weapon => LoadoutFixedSlotType.MainWeapon,
                EquipSlotType.Weapon2 => LoadoutFixedSlotType.SubWeapon,
                EquipSlotType.Armor => LoadoutFixedSlotType.Armor,
                EquipSlotType.Bag => LoadoutFixedSlotType.Backpack,
                _ => LoadoutFixedSlotType.None,
            };
        }

        private static bool CanConfigFitSlot(int configId, EquipSlotType slotType)
        {
            if (slotType == EquipSlotType.BagContent)
            {
                return true;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            EquipmentConfig equipmentConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            return slotType switch
            {
                EquipSlotType.Weapon => (itemConfig != null && (itemConfig.CanEquipMainWeapon || itemConfig.CanEquipSubWeapon)) || equipmentConfig?.EquipSlot == (int)EquipmentSlotType.MainHand,
                EquipSlotType.Weapon2 => (itemConfig != null && (itemConfig.CanEquipMainWeapon || itemConfig.CanEquipSubWeapon)) || equipmentConfig?.EquipSlot == (int)EquipmentSlotType.MainHand,
                EquipSlotType.Armor => (itemConfig != null && itemConfig.CanEquipArmor) || equipmentConfig?.EquipSlot == (int)EquipmentSlotType.Chest,
                EquipSlotType.Bag => itemConfig != null && (itemConfig.CanEquipBackpack || itemConfig.IsBackpack || (itemConfig.BackpackWidth > 0 && itemConfig.BackpackHeight > 0)),
                _ => false,
            };
        }

        #endregion
    }
}
