using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(LobbyPanelComponent))]
    public static partial class LobbyPanelComponentSystem
    {
        private static void InitBattleRecordUi(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (self.u_ComBattleRecordButton != null)
            {
                DisableLegacyBattleRecordClickTask(self.u_ComBattleRecordButton.transform);
                EntityRef<LobbyPanelComponent> selfRef = self;
                self.u_ComBattleRecordButton.onClick.RemoveAllListeners();
                self.u_ComBattleRecordButton.onClick.AddListener(() => OnBattleRecordEntryClicked(selfRef));
            }

            if (self.u_ComBattleRecordOverlayRoot != null)
            {
                self.u_ComBattleRecordOverlayRoot.SetAsLastSibling();
            }

            self.CloseBattleRecordOverlay();
        }

        private static async ETTask OpenBattleRecordOverlayAsync(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (self.u_ComBattleRecordOverlayRoot == null)
            {
                Log.Warning("[BattleRecordUI] overlay root not found");
                return;
            }

            self.u_ComBattleRecordOverlayRoot.gameObject.SetActive(true);
            BattleRecordPanelComponent battleRecordPanel = self.GetOrCreateBattleRecordPanel();
            if (battleRecordPanel == null || battleRecordPanel.IsDisposed)
            {
                Log.Warning("[BattleRecordUI] battle record panel create failed");
                self.u_ComBattleRecordOverlayRoot.gameObject.SetActive(false);
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            BattleRecordPanelOpenData openData = new()
            {
                LobbyPanelRef = self,
            };
            await YIUIEventSystem.Open(battleRecordPanel, openData);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }
        }

        private static BattleRecordPanelComponent GetOrCreateBattleRecordPanel(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed || self.u_ComBattleRecordOverlayRoot == null)
            {
                return null;
            }

            BattleRecordPanelComponent existing = self.BattleRecordPanel;
            if (existing != null && !existing.IsDisposed)
            {
                return existing;
            }

            BattleRecordPanelComponent created =
                    YIUIFactory.Instantiate<BattleRecordPanelComponent>(self.Scene(), self, self.u_ComBattleRecordOverlayRoot) as BattleRecordPanelComponent;
            if (created == null)
            {
                return null;
            }

            created.UIBase?.SetActive(false);
            self.BattleRecordPanel = created;
            return created;
        }

        public static void CloseBattleRecordOverlay(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (self.u_ComBattleRecordOverlayRoot != null)
            {
                self.u_ComBattleRecordOverlayRoot.gameObject.SetActive(false);
            }

            BattleRecordPanelComponent battleRecordPanel = self.BattleRecordPanel;
            if (battleRecordPanel != null && !battleRecordPanel.IsDisposed)
            {
                battleRecordPanel.UIBase?.SetActive(false);
            }
        }

        private static void OnBattleRecordEntryClicked(EntityRef<LobbyPanelComponent> selfRef)
        {
            LobbyPanelComponent self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.OpenBattleRecordOverlayAsync().Coroutine();
        }

        private static void DisableLegacyBattleRecordClickTask(Transform buttonTransform)
        {
            if (buttonTransform == null)
            {
                return;
            }

            Transform legacyClickTask = buttonTransform.Find("ClickTask");
            if (legacyClickTask == null)
            {
                return;
            }

            legacyClickTask.gameObject.SetActive(false);
        }
    }
}
