using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.13
    /// Desc
    /// </summary>
    [FriendOf(typeof(SettlementPanelComponent))]
    public static partial class SettlementPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SettlementPanelComponent self)
        {
            self.RefreshView();
        }

        [EntitySystem]
        private static void Destroy(this SettlementPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SettlementPanelComponent self)
        {
            self.RefreshView();
            await ETTask.CompletedTask;
            return true;
        }

        public static void RefreshView(this SettlementPanelComponent self)
        {
            SettlementClientComponent runtime = self.Root().GetComponent<SettlementClientComponent>();
            bool isSuccess = runtime != null && runtime.HasSettlement && runtime.IsSuccess;
            long totalWealth = runtime?.TotalWealth ?? 0;
            int killNum = runtime?.KillNum ?? 0;

            self.u_DataExtractResult?.SetValue(isSuccess, true);
            self.u_DataGoldSettletment?.SetValue(totalWealth.ToString(), true);
            self.u_DataKillNum?.SetValue(killNum.ToString(), true);
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(SettlementPanelComponent.OnEventClickExitPanelInvoke)]
        private static async ETTask OnEventClickExitPanelInvoke(this SettlementPanelComponent self)
        {
            self.Root().GetComponent<SettlementClientComponent>()?.ResetRuntime();
            await self.UIPanel.CloseAsync();
        }
        #endregion YIUIEvent结束
    }
}
