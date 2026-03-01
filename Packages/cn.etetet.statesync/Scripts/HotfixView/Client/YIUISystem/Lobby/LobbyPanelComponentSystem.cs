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

        #region YIUIEvent开始

        [YIUIInvoke(LobbyPanelComponent.OnEventEnterMapInvoke)]
        private static async ETTask OnEventEnterMapInvoke(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            // 玩家已在Home地图，使用C2M_TransferMap进行地图间传送
            await self.Root().GetComponent<ClientSenderComponent>().Call(C2M_TransferMap.Create());
            self = selfRef;
            await self.Root().GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
            self = selfRef;
            await self.UIPanel.CloseAsync();
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

            self.HeroLoop.ClearSelect();
            await self.HeroLoop.SetDataRefresh(loadout.Heroes, 0);
            self = selfRef;

            // 默认选中第一个英雄
            var loadout2 = self.Root().GetComponent<LoadoutComponent>();
            if (loadout2.Heroes.Count > 0)
            {
                loadout2.SelectedHeroConfigId = loadout2.Heroes[0].HeroConfigId;
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

            // 关闭 Lobby 面板（MatchView 会随面板一起关闭）
            await self.UIPanel.CloseAsync();
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

            // 武器2
            self.UIEquipSlotItemWeapon2.u_DataSlotName.SetValue("武器2");
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

            // 防具
            self.UIEquipSlotItemArmor.u_DataSlotName.SetValue("防具");
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

            // 背包
            self.UIEquipSlotItemBag.u_DataSlotName.SetValue("背包");
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

            // 根据槽位类型获取可选装备列表
            List<ItemConfig> equipList = self.GetEquipListBySlotType(slotType);

            // 刷新列表
            await view.EquipLoop.SetDataRefresh(equipList, 0);
            self = selfRef;
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
                    loadout.MainWeaponConfigId = itemConfigId;
                    self.UIEquipSlotItemWeapon.u_DataEquipName.SetValue(itemConfig.Name);
                    self.UIEquipSlotItemWeapon.u_DataIsEmpty.SetValue(false);
                    Log.Info($"装备武器1: {itemConfig.Name}");
                    break;
                case EquipSlotType.Weapon2:
                    loadout.SubWeaponConfigId = itemConfigId;
                    self.UIEquipSlotItemWeapon2.u_DataEquipName.SetValue(itemConfig.Name);
                    self.UIEquipSlotItemWeapon2.u_DataIsEmpty.SetValue(false);
                    Log.Info($"装备武器2: {itemConfig.Name}");
                    break;
                case EquipSlotType.Armor:
                    loadout.ArmorConfigId = itemConfigId;
                    self.UIEquipSlotItemArmor.u_DataEquipName.SetValue(itemConfig.Name);
                    self.UIEquipSlotItemArmor.u_DataIsEmpty.SetValue(false);
                    Log.Info($"装备防具: {itemConfig.Name}");
                    break;
                case EquipSlotType.Bag:
                    // 添加到背包列表
                    self.BagEquipIds.Add(itemConfigId);
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
            item.u_DataSelect.SetValue(false);
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
            Log.Info($"点击背包物品: {data.Name}");
            // TODO: 可以在这里实现移除背包物品的功能
        }

        #endregion
    }
}