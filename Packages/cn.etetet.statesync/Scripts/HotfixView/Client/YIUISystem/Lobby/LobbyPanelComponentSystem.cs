using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public static partial class LobbyPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this LobbyPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this LobbyPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LobbyPanelComponent self)
        {
            // 默认显示匹配面板
            self.ShowPanel(self.u_ComRolePanelRectTransform);
            
            await ETTask.CompletedTask;
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

            // 服务端匹配成功后会自动传送玩家，客户端只需等待场景切换完成
            await self.Root().GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
            self = selfRef;

            // 关闭 Lobby 面板
            await self.UIPanel.CloseAsync();
        }

        #endregion
    }
}