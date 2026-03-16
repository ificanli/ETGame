using System;
using UnityEngine;
using UnityEngine.UI;
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

            // 初始化装备背包 LoopScroll
            var bagLoopScroll = self.u_ComEquipBagScroll.GetComponentInChildren<LoopScrollRect>();
            self.m_EquipBagLoop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type, string>(
                bagLoopScroll,
                typeof(EquipSelectItemComponent),
                "u_EventSelect"
            );

            // 初始化装备槽位
            self.InitHeroDisplay();
            self.InitEquipSlots();
        }

        [EntitySystem]
        private static void Destroy(this LobbyPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            self.ShowPanel(self.u_ComRolePanelRectTransform);
            await self.RefreshHeroList();
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            self.RefreshLoadoutView();
            return true;
        }

        #region YIUIEvent开始

        [YIUIInvoke(LobbyPanelComponent.OnEventEnterMapInvoke)]
        private static async ETTask OnEventEnterMapInvoke(this LobbyPanelComponent self)
        {
            // 复用匹配链路，PVE 按钮与 1v1 相同流程（匹配成功 -> 服务端传送）
            await self.SendMatchRequest(1);
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventRoleToggleInvoke)]
        private static async ETTask OnEventRoleToggleInvoke(this LobbyPanelComponent self)
        {
            self.ShowPanel(self.u_ComRolePanelRectTransform);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventEquipToggleInvoke)]
        private static async ETTask OnEventEquipToggleInvoke(this LobbyPanelComponent self)
        {
            self.ShowPanel(self.u_ComEquipPanelRectTransform);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventMatchToggleInvoke)]
        private static async ETTask OnEventMatchToggleInvoke(this LobbyPanelComponent self)
        {
            self.ShowPanel(self.u_ComMatchPanelRectTransform);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventBuildToggleInvoke)]
        private static async ETTask OnEventBuildToggleInvoke(this LobbyPanelComponent self)
        {
            self.ShowPanel(self.u_ComBuildPanelRectTransform);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventExploreToggleInvoke)]
        private static async ETTask OnEventExploreToggleInvoke(this LobbyPanelComponent self)
        {
            self.ShowPanel(self.u_ComExplorePanelRectTransform);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventOneOneMatchButtonInvoke)]
        private static async ETTask OnEventOneOneMatchButtonInvoke(this LobbyPanelComponent self)
        {
            await self.SendMatchRequest(2); // OneVsOne
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventSouDaCeMatchButtonInvoke)]
        private static async ETTask OnEventSouDaCeMatchButtonInvoke(this LobbyPanelComponent self)
        {
            await self.SendMatchRequest(4); // Extraction
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventThreeThreeMatchButtonInvoke)]
        private static async ETTask OnEventThreeThreeMatchButtonInvoke(this LobbyPanelComponent self)
        {
            await self.SendMatchRequest(3); // ThreeVsThree
        }

        [YIUIInvoke(LobbyPanelComponent.OnEventClickPutIntoBagInvoke)]
        private static async ETTask OnEventClickPutIntoBagInvoke(this LobbyPanelComponent self)
        {
            // 打开装备选择界面，选择药品放入背包
            await self.OpenEquipSelectView(EquipSlotType.Bag);
        }

        
        [YIUIInvoke(LobbyPanelComponent.OnEventClickBagInvoke)]
        private static async ETTask OnEventClickBagInvoke(this LobbyPanelComponent self)
        {
            await self.ConfirmLoadoutAsync();
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
        }

        #endregion

        #region 英雄列表逻辑

        private static async ETTask RefreshHeroList(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            C2G_GetHeroList request = C2G_GetHeroList.Create();
            G2C_GetHeroList response = (G2C_GetHeroList)await self.Root().GetComponent<ClientSenderComponent>().Call(request);
            self = selfRef;

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"获取英雄列表失败: {response.Error}");
                return;
            }

            var loadout = self.Root().GetComponent<LoadoutComponent>();
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

            loadout.StorageItemCounts.Clear();
            int storagePairCount = Math.Min(response.StorageConfigIds.Count, response.StorageCounts.Count);
            for (int i = 0; i < storagePairCount; ++i)
            {
                int count = response.StorageCounts[i];
                if (count > 0)
                {
                    loadout.StorageItemCounts[response.StorageConfigIds[i]] = count;
                }
            }

            loadout.TotalWealth = response.TotalWealth;
            loadout.SelectedHeroConfigId = response.CurrentHeroConfigId > 0 ? response.CurrentHeroConfigId : loadout.SelectedHeroConfigId;
            loadout.MainWeaponConfigId = response.CurrentMainWeaponConfigId;
            loadout.SubWeaponConfigId = response.CurrentSubWeaponConfigId;
            loadout.ArmorConfigId = response.CurrentArmorConfigId;
            loadout.ConsumableConfigIds.Clear();
            if (response.CurrentConsumableConfigIds != null)
            {
                loadout.ConsumableConfigIds.AddRange(response.CurrentConsumableConfigIds);
            }
            loadout.AllowFreeSelection = loadout.StorageItemCounts.Count == 0 &&
                    loadout.MainWeaponConfigId == 0 &&
                    loadout.SubWeaponConfigId == 0 &&
                    loadout.ArmorConfigId == 0 &&
                    loadout.ConsumableConfigIds.Count == 0;
            self.BagEquipIds.Clear();
            self.BagEquipIds.AddRange(loadout.ConsumableConfigIds);
            self.HasLoadoutSnapshot = true;

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

            self?.RefreshLoadoutView();
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
            item.u_DataSelect.SetValue(select);

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
            item.u_DataSelect.SetValue(select);
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

        /// <summary>
        /// 发送匹配请求
        /// </summary>
        private static async ETTask SendMatchRequest(this LobbyPanelComponent self, int gameMode)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            bool confirmSuccess = await self.ConfirmLoadoutAsync();
            self = selfRef;
            if (!confirmSuccess)
            {
                return;
            }

            C2G_MatchRequest request = C2G_MatchRequest.Create();
            request.GameMode = gameMode;

            G2C_MatchRequest response = (G2C_MatchRequest)await self.Root().GetComponent<ClientSenderComponent>().Call(request);
            self = selfRef;

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"匹配请求失败: {response.Error}");
                return;
            }

            Log.Info($"匹配请求成功，RequestId: {response.RequestId}, GameMode: {gameMode}，等待匹配...");

            // 打开匹配等待弹窗
            await self.UIPanel.OpenViewAsync<MatchViewComponent>();
            self = selfRef;

            // 服务端匹配成功后会自动传送玩家，客户端只需等待场景切换完成
            await self.Root().GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
            self = selfRef;

            // 发布 EnterMapFinish 事件，关闭 Loading 面板
            EventSystem.Instance.Publish(self.Root(), new EnterMapFinish());

            // 关闭 Lobby 面板（MatchView 会随面板一起关闭）
            await self.UIPanel.CloseAsync();
        }

        private static async ETTask<bool> ConfirmLoadoutAsync(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
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
            confirmReq.ConsumableConfigIds.AddRange(loadout.ConsumableConfigIds);
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
                loadout.ConsumableConfigIds.Clear();
                loadout.ConsumableConfigIds.AddRange(confirmReq.ConsumableConfigIds);
                self.BagEquipIds.Clear();
                self.BagEquipIds.AddRange(confirmReq.ConsumableConfigIds);
                loadout.AllowFreeSelection = false;
                loadout.IsConfirmed = true;
            }

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

            // 武器1
            self.UIEquipSlotItemWeapon.u_DataSlotName.SetValue("武器1");
            self.UIEquipSlotItemWeapon.u_DataIsEmpty.SetValue(true);
            self.UIEquipSlotItemWeapon.SetItemIcon(string.Empty);
            var weaponBtn = self.UIEquipSlotItemWeapon.UIBase.OwnerGameObject.GetComponent<Button>();
            if (weaponBtn != null)
            {
                weaponBtn.onClick.AddListener(() =>
                {
                    var panel = selfRef.Entity;
                    if (panel != null)
                    {
                        if (!panel.TryUnloadSlot(EquipSlotType.Weapon))
                        {
                            panel.OpenEquipSelectView(EquipSlotType.Weapon).Coroutine();
                        }
                    }
                });
            }

            // 武器2
            self.UIEquipSlotItemWeapon2.u_DataSlotName.SetValue("武器2");
            self.UIEquipSlotItemWeapon2.u_DataIsEmpty.SetValue(true);
            self.UIEquipSlotItemWeapon2.SetItemIcon(string.Empty);
            var weapon2Btn = self.UIEquipSlotItemWeapon2.UIBase.OwnerGameObject.GetComponent<Button>();
            if (weapon2Btn != null)
            {
                weapon2Btn.onClick.AddListener(() =>
                {
                    var panel = selfRef.Entity;
                    if (panel != null)
                    {
                        if (!panel.TryUnloadSlot(EquipSlotType.Weapon2))
                        {
                            panel.OpenEquipSelectView(EquipSlotType.Weapon2).Coroutine();
                        }
                    }
                });
            }

            // 防具
            self.UIEquipSlotItemArmor.u_DataSlotName.SetValue("防具");
            self.UIEquipSlotItemArmor.u_DataIsEmpty.SetValue(true);
            self.UIEquipSlotItemArmor.SetItemIcon(string.Empty);
            var armorBtn = self.UIEquipSlotItemArmor.UIBase.OwnerGameObject.GetComponent<Button>();
            if (armorBtn != null)
            {
                armorBtn.onClick.AddListener(() =>
                {
                    var panel = selfRef.Entity;
                    if (panel != null)
                    {
                        if (!panel.TryUnloadSlot(EquipSlotType.Armor))
                        {
                            panel.OpenEquipSelectView(EquipSlotType.Armor).Coroutine();
                        }
                    }
                });
            }

            // 背包
            self.UIEquipSlotItemBag.u_DataSlotName.SetValue("背包");
            self.UIEquipSlotItemBag.u_DataIsEmpty.SetValue(true);
            self.UIEquipSlotItemBag.SetItemIcon(string.Empty);
            var bagBtn = self.UIEquipSlotItemBag.UIBase.OwnerGameObject.GetComponent<Button>();
            if (bagBtn != null)
            {
                bagBtn.onClick.AddListener(() =>
                {
                    var panel = selfRef.Entity;
                    if (panel != null)
                    {
                        panel.OpenEquipSelectView(EquipSlotType.Bag).Coroutine();
                    }
                });
            }
        }

        /// <summary>
        /// 打开装备选择界面
        /// </summary>
        private static async ETTask OpenEquipSelectView(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            self.CurrentSelectingSlot = slotType;

            // 打开装备选择界面
            var equipSelectView = await self.UIPanel.OpenViewAsync<EquipSelectViewComponent>();
            self = selfRef;

            if (equipSelectView == null)
            {
                Log.Error("打开装备选择界面失败");
                return;
            }

            // 设置引用和槽位类型
            equipSelectView.m_LobbyPanel = self;
            equipSelectView.CurrentSlotType = slotType;

            // 刷新装备列表
            await self.RefreshEquipSelectView(equipSelectView, slotType);
        }

        /// <summary>
        /// 刷新装备选择界面
        /// </summary>
        private static async ETTask RefreshEquipSelectView(this LobbyPanelComponent self, EquipSelectViewComponent view, EquipSlotType slotType)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<EquipSelectViewComponent> viewRef = view;

            // 根据槽位类型获取可选装备列表
            List<ItemConfig> equipList = self.GetEquipListBySlotType(slotType);

            if (view.EquipLoop == null)
            {
                Log.Error("装备选择界面的 LoopScroll 未初始化");
                return;
            }

            // 刷新列表
            await view.EquipLoop.SetDataRefresh(equipList, 0);
            self = selfRef;
            view = viewRef;

            if (self == null || view == null || self.IsDisposed || view.IsDisposed)
            {
                return;
            }

            if (equipList.Count > 0)
            {
                ItemConfig firstItem = equipList[0];
                view.PendingItemConfigId = firstItem.Id;
                view.u_DataGunName?.SetValue(self.BuildEquipPreviewText(firstItem, slotType));
            }
            else
            {
                view.PendingItemConfigId = 0;
                view.u_DataGunName?.SetValue(string.Empty);
            }
        }

        private static string BuildEquipPreviewText(this LobbyPanelComponent self, ItemConfig itemConfig, EquipSlotType slotType)
        {
            if (itemConfig == null)
            {
                return string.Empty;
            }

            string desc = null;
            if (slotType == EquipSlotType.Weapon || slotType == EquipSlotType.Weapon2)
            {
                WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(itemConfig.Id);
                desc = weaponConfig?.Desc;
            }

            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = itemConfig.Desc;
            }

            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = itemConfig.Name;
            }

            return desc;
        }

        /// <summary>
        /// 根据槽位类型获取装备列表
        /// </summary>
        private static List<ItemConfig> GetEquipListBySlotType(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            ItemConfigCategory itemCategory = ItemConfigCategory.Instance;
            List<ItemConfig> result = new();

            foreach (var item in itemCategory.GetAll().Values)
            {
                switch (slotType)
                {
                    case EquipSlotType.Weapon:
                    case EquipSlotType.Weapon2:
                        // Type == 1 表示武器
                        if (item.Type == 1)
                        {
                            result.Add(item);
                        }

                        break;
                    case EquipSlotType.Armor:
                        // Type == 2 表示防具
                        if (item.Type == 2)
                        {
                            result.Add(item);
                        }

                        break;
                    case EquipSlotType.Bag:
                        // Type == 3 表示药品
                        if (item.Type == 3)
                        {
                            result.Add(item);
                        }

                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// 装备物品到槽位
        /// </summary>
        public static void EquipItem(this LobbyPanelComponent self, int itemConfigId, EquipSlotType slotType)
        {
            var loadout = self.Root().GetComponent<LoadoutComponent>();
            ItemConfig itemConfig = ItemConfigCategory.Instance.Get(itemConfigId);

            switch (slotType)
            {
                case EquipSlotType.Weapon:
                    if (!self.TryAcquireStorageItem(itemConfigId))
                    {
                        return;
                    }
                    self.ReturnStorageItem(loadout.MainWeaponConfigId);
                    loadout.MainWeaponConfigId = itemConfigId;
                    loadout.IsConfirmed = false;
                    self.RefreshSlotView(self.UIEquipSlotItemWeapon, loadout.MainWeaponConfigId, "武器1", true);
                    self.RefreshCurrentHeroDescription();
                    Log.Info($"装备武器1: {itemConfig.Name}");
                    break;
                case EquipSlotType.Weapon2:
                    if (!self.TryAcquireStorageItem(itemConfigId))
                    {
                        return;
                    }
                    self.ReturnStorageItem(loadout.SubWeaponConfigId);
                    loadout.SubWeaponConfigId = itemConfigId;
                    loadout.IsConfirmed = false;
                    self.RefreshSlotView(self.UIEquipSlotItemWeapon2, loadout.SubWeaponConfigId, "武器2", true);
                    self.RefreshCurrentHeroDescription();
                    Log.Info($"装备武器2: {itemConfig.Name}");
                    break;
                case EquipSlotType.Armor:
                    if (!self.TryAcquireStorageItem(itemConfigId))
                    {
                        return;
                    }
                    self.ReturnStorageItem(loadout.ArmorConfigId);
                    loadout.ArmorConfigId = itemConfigId;
                    loadout.IsConfirmed = false;
                    self.RefreshSlotView(self.UIEquipSlotItemArmor, loadout.ArmorConfigId, "防具");
                    self.RefreshCurrentHeroDescription();
                    Log.Info($"装备防具: {itemConfig.Name}");
                    break;
                case EquipSlotType.Bag:
                    if (!self.TryAcquireStorageItem(itemConfigId))
                    {
                        return;
                    }
                    // 添加到背包列表
                    self.BagEquipIds.Add(itemConfigId);
                    loadout.ConsumableConfigIds.Clear();
                    loadout.ConsumableConfigIds.AddRange(self.BagEquipIds);
                    loadout.IsConfirmed = false;
                    self.RefreshBagScroll().Coroutine();
                    Log.Info($"添加到背包: {itemConfig.Name}");
                    break;
            }
        }

        /// <summary>
        /// 刷新背包 LoopScroll
        /// </summary>
        private static async ETTask RefreshBagScroll(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            // 获取背包中的装备配置列表
            List<ItemConfig> bagItems = new();
            ItemConfigCategory itemCategory = ItemConfigCategory.Instance;
            foreach (var itemId in self.BagEquipIds)
            {
                var itemConfig = itemCategory.Get(itemId);
                if (itemConfig != null)
                {
                    bagItems.Add(itemConfig);
                }
            }

            // 刷新背包 LoopScroll
            await self.EquipBagLoop.SetDataRefresh(bagItems, 0);
            self = selfRef;

            // 更新背包槽位显示
            self.UIEquipSlotItemBag.u_DataIsEmpty.SetValue(self.BagEquipIds.Count == 0);
            self.UIEquipSlotItemBag.SetItemIcon(bagItems.Count > 0 ? bagItems[0].Icon : string.Empty);
        }

        /// <summary>
        /// 背包物品绑定回调
        /// </summary>
        [EntitySystem]
        private static void YIUILoopRenderer(
            this LobbyPanelComponent self,
            EquipSelectItemComponent item,
            ItemConfig data,
            int index,
            bool select)
        {
            item.u_DataEquipName.SetValue(data.Name);
            item.SetSelected(false);
            item.SetItemIcon(data.Icon);
        }

        /// <summary>
        /// 背包物品点击回调（可以实现移除功能）
        /// </summary>
        [EntitySystem]
        private static void YIUILoopOnClick(
            this LobbyPanelComponent self,
            EquipSelectItemComponent item,
            ItemConfig data,
            int index,
            bool select)
        {
            if (index < 0 || index >= self.BagEquipIds.Count)
            {
                return;
            }

            self.ReturnStorageItem(self.BagEquipIds[index]);
            self.BagEquipIds.RemoveAt(index);
            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            loadout.ConsumableConfigIds.Clear();
            loadout.ConsumableConfigIds.AddRange(self.BagEquipIds);
            loadout.IsConfirmed = false;
            self.RefreshBagScroll().Coroutine();
            Log.Info($"卸下背包物品: {data.Name}");
        }

        private static void RefreshLoadoutView(this LobbyPanelComponent self)
        {
            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            if (loadout == null)
            {
                return;
            }

            self.RefreshSlotView(self.UIEquipSlotItemWeapon, loadout.MainWeaponConfigId, "武器1", true);
            self.RefreshSlotView(self.UIEquipSlotItemWeapon2, loadout.SubWeaponConfigId, "武器2", true);
            self.RefreshSlotView(self.UIEquipSlotItemArmor, loadout.ArmorConfigId, "防具");

            self.BagEquipIds.Clear();
            self.BagEquipIds.AddRange(loadout.ConsumableConfigIds);
            self.RefreshBagScroll().Coroutine();
            self.RefreshCurrentHeroDescription();
        }

        private static void RefreshSlotView(this LobbyPanelComponent self, EquipSlotItemComponent slotItem, int configId, string slotName, bool hideTextWhenEquipped = false)
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
                return;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            slotItem.u_DataSlotName.SetValue(hideTextWhenEquipped ? string.Empty : slotName);
            slotItem.u_DataEquipName.SetValue(hideTextWhenEquipped ? string.Empty : itemConfig?.Name ?? string.Empty);
            slotItem.u_DataIsEmpty.SetValue(itemConfig == null);
            slotItem.SetItemIcon(itemConfig?.Icon ?? string.Empty);
        }

        private static bool TryUnloadSlot(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            if (loadout == null)
            {
                return false;
            }

            int configId = slotType switch
            {
                EquipSlotType.Weapon => loadout.MainWeaponConfigId,
                EquipSlotType.Weapon2 => loadout.SubWeaponConfigId,
                EquipSlotType.Armor => loadout.ArmorConfigId,
                _ => 0,
            };

            if (configId <= 0)
            {
                return false;
            }

            self.ReturnStorageItem(configId);
            switch (slotType)
            {
                case EquipSlotType.Weapon:
                    loadout.MainWeaponConfigId = 0;
                    break;
                case EquipSlotType.Weapon2:
                    loadout.SubWeaponConfigId = 0;
                    break;
                case EquipSlotType.Armor:
                    loadout.ArmorConfigId = 0;
                    break;
            }

            loadout.IsConfirmed = false;
            self.RefreshLoadoutView();
            return true;
        }

        private static bool TryAcquireStorageItem(this LobbyPanelComponent self, int configId)
        {
            if (configId <= 0)
            {
                return false;
            }

            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            if (loadout == null)
            {
                return false;
            }

            if (loadout.StorageItemCounts.TryGetValue(configId, out int count) && count > 0)
            {
                if (count == 1)
                {
                    loadout.StorageItemCounts.Remove(configId);
                }
                else
                {
                    loadout.StorageItemCounts[configId] = count - 1;
                }

                return true;
            }

            return loadout.AllowFreeSelection;
        }

        private static void ReturnStorageItem(this LobbyPanelComponent self, int configId)
        {
            if (configId <= 0)
            {
                return;
            }

            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            if (loadout == null)
            {
                return;
            }

            if (loadout.StorageItemCounts.TryGetValue(configId, out int count))
            {
                loadout.StorageItemCounts[configId] = count + 1;
            }
            else
            {
                loadout.StorageItemCounts[configId] = 1;
            }
        }

        #endregion
    }
}
