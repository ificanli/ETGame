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
            // 鍒濆鍖栬嫳闆勫垪琛?LoopScroll
            var heroLoopScroll = self.u_ComHeroList.GetComponentInChildren<LoopScrollRect>();
            self.m_HeroLoop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type, string>(
                heroLoopScroll,
                typeof(HeroSelectItemComponent),
                "u_EventSelect"
            );

            // 鍒濆鍖栬澶囪儗鍖?LoopScroll
            var bagLoopScroll = self.u_ComEquipBagScroll.GetComponentInChildren<LoopScrollRect>();
            self.m_EquipBagLoop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type, string>(
                bagLoopScroll,
                typeof(EquipSelectItemComponent),
                "u_EventSelect"
            );

            // 鍒濆鍖栬澶囨Ы浣?

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
            self.ShowPanel(self.u_ComRolePanelRectTransform);
            await self.RefreshHeroList();
            return true;
        }

        #region YIUIEvent寮€濮?

        [YIUIInvoke(LobbyPanelComponent.OnEventEnterMapInvoke)]
        private static async ETTask OnEventEnterMapInvoke(this LobbyPanelComponent self)
        {
            // 澶嶇敤鍖归厤閾捐矾锛歅VE 鎸夐挳璧颁笌 1v1 鐩稿悓娴佺▼锛堝尮閰嶆垚鍔?-> 鏈嶅姟绔紶閫侊級
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
            // 鎵撳紑瑁呭閫夋嫨鐣岄潰锛岄€夋嫨鑽搧鏀惧叆鑳屽寘
            await self.OpenEquipSelectView(EquipSlotType.Bag);
        }
        #endregion YIUIEvent缁撴潫

        #region 椤电鍒囨崲閫昏緫

        /// <summary>
        /// 鏄剧ず鎸囧畾闈㈡澘锛岄殣钘忓叾浠栭潰鏉?
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

        #region 鑻遍泟鍒楄〃閫昏緫

        private static async ETTask RefreshHeroList(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            C2G_GetHeroList request = C2G_GetHeroList.Create();
            G2C_GetHeroList response = (G2C_GetHeroList)await self.Root().GetComponent<ClientSenderComponent>().Call(request);
            self = selfRef;

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"鑾峰彇鑻遍泟鍒楄〃澶辫触: {response.Error}");
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

            self.HeroLoop.ClearSelect();
            await self.HeroLoop.SetDataRefresh(loadout.Heroes, 0);
            self = selfRef;

            // 榛樿閫変腑绗竴涓嫳闆?
            var loadout2 = self.Root().GetComponent<LoadoutComponent>();
            if (loadout2.Heroes.Count > 0)
            {
                loadout2.SelectedHeroConfigId = loadout2.Heroes[0].HeroConfigId;
                await self.RefreshHeroDisplay(loadout2.Heroes[0].HeroConfigId);
                self = selfRef;
            }
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
                self.Root().GetComponent<LoadoutComponent>().SelectedHeroConfigId = data.HeroConfigId;
                self.RefreshHeroDisplay(data.HeroConfigId).Coroutine();
                Log.Info($"閫変腑鑻遍泟: {data.Name} (ConfigId: {data.HeroConfigId})");
            }
        }

        #endregion

        #region 鍖归厤閫昏緫

        /// <summary>
        /// 鍙戦€佸尮閰嶈姹?
        /// </summary>
        private static async ETTask SendMatchRequest(this LobbyPanelComponent self, int gameMode)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            // 鍏堢‘璁よ捣瑁咃紝鎶婇€夊ソ鐨勮嫳闆勫拰瑁呭鎻愪氦缁欐湇鍔＄
            var loadout = self.Root().GetComponent<LoadoutComponent>();
            C2G_ConfirmLoadout confirmReq = C2G_ConfirmLoadout.Create();
            confirmReq.HeroConfigId = loadout.SelectedHeroConfigId > 0 ? loadout.SelectedHeroConfigId : 1001;
            confirmReq.MainWeaponConfigId = loadout.MainWeaponConfigId;
            confirmReq.SubWeaponConfigId = loadout.SubWeaponConfigId;
            confirmReq.ArmorConfigId = loadout.ArmorConfigId;
            Log.Info($"鍙戦€佺‘璁よ捣瑁? HeroConfigId={confirmReq.HeroConfigId}, MainWeapon={confirmReq.MainWeaponConfigId}, SubWeapon={confirmReq.SubWeaponConfigId}");
            G2C_ConfirmLoadout confirmResp = (G2C_ConfirmLoadout)await self.Root().GetComponent<ClientSenderComponent>().Call(confirmReq);
            self = selfRef;
            if (confirmResp.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"纭璧疯澶辫触: {confirmResp.Error} {confirmResp.Message}");
                return;
            }
            Log.Info($"纭璧疯鎴愬姛锛岃嫳闆?{confirmReq.HeroConfigId}, 姝﹀櫒1={confirmReq.MainWeaponConfigId}, 姝﹀櫒2={confirmReq.SubWeaponConfigId}");

            C2G_MatchRequest request = C2G_MatchRequest.Create();
            request.GameMode = gameMode;

            G2C_MatchRequest response = (G2C_MatchRequest)await self.Root().GetComponent<ClientSenderComponent>().Call(request);

            self = selfRef;

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"鍖归厤璇锋眰澶辫触: {response.Error}");
                return;
            }

            Log.Info($"鍖归厤璇锋眰鎴愬姛锛孯equestId: {response.RequestId}, GameMode: {gameMode}锛岀瓑寰呭尮閰?..");

            // 鎵撳紑鍖归厤绛夊緟寮圭獥
            await self.UIPanel.OpenViewAsync<MatchViewComponent>();
            self = selfRef;

            // 鏈嶅姟绔尮閰嶆垚鍔熷悗浼氳嚜鍔ㄤ紶閫佺帺瀹讹紝瀹㈡埛绔彧闇€绛夊緟鍦烘櫙鍒囨崲瀹屾垚
            await self.Root().GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
            self = selfRef;

            // 鍙戝竷 EnterMapFinish 浜嬩欢锛屽叧闂?Loading 闈㈡澘
            EventSystem.Instance.Publish(self.Root(), new EnterMapFinish());

            // 鍏抽棴 Lobby 闈㈡澘锛圡atchView 浼氶殢闈㈡澘涓€璧峰叧闂級
            await self.UIPanel.CloseAsync();
        }

        #endregion

        #region 瑁呭绯荤粺閫昏緫

        /// <summary>
        /// 鍒濆鍖栬澶囨Ы浣?
        /// </summary>
        private static void InitEquipSlots(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            // 姝﹀櫒1
            self.UIEquipSlotItemWeapon.u_DataSlotName.SetValue("姝﹀櫒1");
            self.UIEquipSlotItemWeapon.u_DataIsEmpty.SetValue(true);
            var weaponBtn = self.UIEquipSlotItemWeapon.UIBase.OwnerGameObject.GetComponent<Button>();
            if (weaponBtn != null)
            {
                weaponBtn.onClick.AddListener(() =>
                {
                    var panel = selfRef.Entity;
                    if (panel != null)
                    {
                        panel.OpenEquipSelectView(EquipSlotType.Weapon).Coroutine();
                    }
                });
            }

            // 姝﹀櫒2
            self.UIEquipSlotItemWeapon2.u_DataSlotName.SetValue("姝﹀櫒2");
            self.UIEquipSlotItemWeapon2.u_DataIsEmpty.SetValue(true);
            var weapon2Btn = self.UIEquipSlotItemWeapon2.UIBase.OwnerGameObject.GetComponent<Button>();
            if (weapon2Btn != null)
            {
                weapon2Btn.onClick.AddListener(() =>
                {
                    var panel = selfRef.Entity;
                    if (panel != null)
                    {
                        panel.OpenEquipSelectView(EquipSlotType.Weapon2).Coroutine();
                    }
                });
            }

            // 闃插叿
            self.UIEquipSlotItemArmor.u_DataSlotName.SetValue("闃插叿");
            self.UIEquipSlotItemArmor.u_DataIsEmpty.SetValue(true);
            var armorBtn = self.UIEquipSlotItemArmor.UIBase.OwnerGameObject.GetComponent<Button>();
            if (armorBtn != null)
            {
                armorBtn.onClick.AddListener(() =>
                {
                    var panel = selfRef.Entity;
                    if (panel != null)
                    {
                        panel.OpenEquipSelectView(EquipSlotType.Armor).Coroutine();
                    }
                });
            }

            // 鑳屽寘
            self.UIEquipSlotItemBag.u_DataSlotName.SetValue("鑳屽寘");
            self.UIEquipSlotItemBag.u_DataIsEmpty.SetValue(true);
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
        /// 鎵撳紑瑁呭閫夋嫨鐣岄潰
        /// </summary>
        private static async ETTask OpenEquipSelectView(this LobbyPanelComponent self, EquipSlotType slotType)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            self.CurrentSelectingSlot = slotType;

            // 鎵撳紑瑁呭閫夋嫨鐣岄潰
            var equipSelectView = await self.UIPanel.OpenViewAsync<EquipSelectViewComponent>();
            self = selfRef;

            if (equipSelectView == null)
            {
                Log.Error("鎵撳紑瑁呭閫夋嫨鐣岄潰澶辫触");
                return;
            }

            // 璁剧疆寮曠敤鍜屾Ы浣嶇被鍨?
            equipSelectView.m_LobbyPanel = self;
            equipSelectView.CurrentSlotType = slotType;

            // 鍒锋柊瑁呭鍒楄〃
            await self.RefreshEquipSelectView(equipSelectView, slotType);
        }

        /// <summary>
        /// 鍒锋柊瑁呭閫夋嫨鐣岄潰
        /// </summary>
        private static async ETTask RefreshEquipSelectView(this LobbyPanelComponent self, EquipSelectViewComponent view, EquipSlotType slotType)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<EquipSelectViewComponent> viewRef = view;

            // 鏍规嵁妲戒綅绫诲瀷鑾峰彇鍙€夎澶囧垪琛?
            List<ItemConfig> equipList = self.GetEquipListBySlotType(slotType);

            if (view.EquipLoop == null)
            {
                Log.Error("瑁呭閫夋嫨鐣岄潰鐨凩oopScroll鏈垵濮嬪寲");
                return;
            }

            // 鍒锋柊鍒楄〃
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
        /// 鏍规嵁妲戒綅绫诲瀷鑾峰彇瑁呭鍒楄〃
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
                        // Type == 1 琛ㄧず姝﹀櫒
                        if (item.Type == 1)
                        {
                            result.Add(item);
                        }
                        break;
                    case EquipSlotType.Armor:
                        // Type == 2 琛ㄧず闃插叿
                        if (item.Type == 2)
                        {
                            result.Add(item);
                        }
                        break;
                    case EquipSlotType.Bag:
                        // Type == 3 琛ㄧず鑽搧
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
        /// 瑁呭鐗╁搧鍒版Ы浣?
        /// </summary>
        public static void EquipItem(this LobbyPanelComponent self, int itemConfigId, EquipSlotType slotType)
        {
            var loadout = self.Root().GetComponent<LoadoutComponent>();
            ItemConfig itemConfig = ItemConfigCategory.Instance.Get(itemConfigId);

            switch (slotType)
            {
                case EquipSlotType.Weapon:
                    loadout.MainWeaponConfigId = itemConfigId;
                    self.UIEquipSlotItemWeapon.u_DataEquipName.SetValue(itemConfig.Name);
                    self.UIEquipSlotItemWeapon.u_DataIsEmpty.SetValue(false);
                    Log.Info($"瑁呭姝﹀櫒1: {itemConfig.Name}");
                    break;
                case EquipSlotType.Weapon2:
                    loadout.SubWeaponConfigId = itemConfigId;
                    self.UIEquipSlotItemWeapon2.u_DataEquipName.SetValue(itemConfig.Name);
                    self.UIEquipSlotItemWeapon2.u_DataIsEmpty.SetValue(false);
                    Log.Info($"瑁呭姝﹀櫒2: {itemConfig.Name}");
                    break;
                case EquipSlotType.Armor:
                    loadout.ArmorConfigId = itemConfigId;
                    self.UIEquipSlotItemArmor.u_DataEquipName.SetValue(itemConfig.Name);
                    self.UIEquipSlotItemArmor.u_DataIsEmpty.SetValue(false);
                    Log.Info($"瑁呭闃插叿: {itemConfig.Name}");
                    break;
                case EquipSlotType.Bag:
                    // 娣诲姞鍒拌儗鍖呭垪琛?
                    self.BagEquipIds.Add(itemConfigId);
                    self.RefreshBagScroll().Coroutine();
                    Log.Info($"娣诲姞鍒拌儗鍖? {itemConfig.Name}");
                    break;
            }
        }

        /// <summary>
        /// 鍒锋柊鑳屽寘 LoopScroll
        /// </summary>
        private static async ETTask RefreshBagScroll(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            // 鑾峰彇鑳屽寘涓殑瑁呭閰嶇疆鍒楄〃
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

            // 鍒锋柊鑳屽寘 LoopScroll
            await self.EquipBagLoop.SetDataRefresh(bagItems, 0);
            self = selfRef;

            // 鏇存柊鑳屽寘妲戒綅鏄剧ず
            self.UIEquipSlotItemBag.u_DataIsEmpty.SetValue(self.BagEquipIds.Count == 0);
        }

        /// <summary>
        /// 鑳屽寘鐗╁搧缁戝畾鍥炶皟
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
            item.u_DataSelect.SetValue(false);
        }

        /// <summary>
        /// 鑳屽寘鐗╁搧鐐瑰嚮鍥炶皟锛堝彲浠ュ疄鐜扮Щ闄ゅ姛鑳斤級
        /// </summary>
        [EntitySystem]
        private static void YIUILoopOnClick(
            this LobbyPanelComponent self,
            EquipSelectItemComponent item,
            ItemConfig data,
            int index,
            bool select)
        {
            Log.Info($"鐐瑰嚮鑳屽寘鐗╁搧: {data.Name}");
            // TODO: 鍙互鍦ㄨ繖閲屽疄鐜扮Щ闄よ儗鍖呯墿鍝佺殑鍔熻兘
        }

        #endregion
    }
}
