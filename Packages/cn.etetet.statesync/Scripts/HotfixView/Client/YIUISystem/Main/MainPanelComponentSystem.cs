using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    [FriendOf(typeof(MainPanelComponent))]
    public static partial class MainPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this MainPanelComponent self)
        {
            UnityEngine.Debug.LogWarning("[ECAProbe][MainPanel] YIUIInitialize");
            // 找到摇杆并注入Entity引用
            JoystickView joystickView = self.UIBase.OwnerGameObject.GetComponentInChildren<JoystickView>();
            if (joystickView != null)
            {
                joystickView.SetEntity(self);
            }

            // 找到摇杆范围控件并设置引用
            JoystickRangeView joystickRangeView = self.UIBase.OwnerGameObject.GetComponentInChildren<JoystickRangeView>();
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
            }

            self.u_DataSearchingButton = self.UIBase.DataTable.FindDataValue<UIDataValueBool>("u_DataSearchingButton");
            self.u_EventClickSearchingButton = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickSearchingButton");
            if (self.u_EventClickSearchingButton != null)
            {
                self.u_EventClickSearchingButtonHandle = self.u_EventClickSearchingButton.Add(
                    self,
                    MainPanelComponent.OnEventClickSearchingButtonInvoke);
            }
            else
            {
                Log.Warning("[ECAClient][MainPanel] missing event: u_EventClickSearchingButton");
            }
            self.u_EventClickBagButton = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickBagButton");
            if (self.u_EventClickBagButton != null)
            {
                self.u_EventClickBagButtonHandle = self.u_EventClickBagButton.Add(
                    self,
                    MainPanelComponent.OnEventClickBagButtonInvoke);
            }
            else
            {
                Log.Warning("[ECAClient][MainPanel] missing event: u_EventClickBagButton");
            }

            if (self.u_DataSearchingButton == null)
            {
                Log.Warning("[ECAClient][MainPanel] missing data: u_DataSearchingButton");
            }

            self.LastSearchButtonShow = false;
            self.LastFocusPointId = null;
            self.LastOpenContainerPointId = null;
            self.u_DataSearchingButton?.SetValue(false, true);
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
            if (self.u_EventClickBagButton != null && self.u_EventClickBagButtonHandle != null)
            {
                self.u_EventClickBagButton.Remove(self.u_EventClickBagButtonHandle);
            }

            self.u_EventClickBagButtonHandle = null;
            self.u_EventClickBagButton = null;
            self.u_DataSearchingButton = null;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this MainPanelComponent self)
        {
            self.LastSearchButtonShow = false;
            self.LastFocusPointId = null;
            self.LastOpenContainerPointId = null;
            self.DebugProbeFrame = 0;
            self.u_DataSearchingButton?.SetValue(false, true);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void LateUpdate(this MainPanelComponent self)
        {
            Scene root = self.Root();
            ECAInteractClientComponent runtime = root?.GetComponent<ECAInteractClientComponent>();
            string focusPointId = runtime?.FocusPointId;
            string openContainerPointId = runtime?.OpenContainerPointId;

            self.DebugProbeFrame++;
            if (self.DebugProbeFrame >= 60)
            {
                self.DebugProbeFrame = 0;
                UnityEngine.Debug.LogWarning(
                    $"[ECAProbe][Tick] runtime={(runtime != null)}, inRangeCount={runtime?.InRangePointIds.Count ?? 0}, focus={focusPointId ?? "null"}, open={openContainerPointId ?? "null"}");
            }

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

            if (self.LastSearchButtonShow != show ||
                self.LastFocusPointId != focusPointId ||
                self.LastOpenContainerPointId != openContainerPointId)
            {
                UnityEngine.Debug.LogWarning(
                    $"[ECAProbe][MainPanel] show={show}, focus={focusPointId ?? "null"}, open={openContainerPointId ?? "null"}, inRangeCount={runtime?.InRangePointIds.Count ?? 0}");
                Log.Info(
                    $"[ECAClient][MainPanel] searchBtnShow={show}, focus={focusPointId ?? "null"}, open={openContainerPointId ?? "null"}, runtime={(runtime != null)}");
                self.LastSearchButtonShow = show;
                self.LastFocusPointId = focusPointId;
                self.LastOpenContainerPointId = openContainerPointId;
            }

            if (self.u_DataSearchingButton != null && self.u_DataSearchingButton.GetValue() != show)
            {
                self.u_DataSearchingButton.SetValue(show);
            }
        }

        #region YIUIEvent开始
        [YIUIInvoke(MainPanelComponent.OnEventClickSearchingButtonInvoke)]
        private static async ETTask OnEventClickSearchingButtonInvoke(this MainPanelComponent self)
        {
            UnityEngine.Debug.LogWarning("[ECAProbe][MainPanel] click search button");
            Scene root = self.Root();
            if (root != null)
            {
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
            itemComponent.SetCapacity(response.Capacity);
            foreach (ItemData itemData in response.Items)
            {
                itemComponent.UpdateItem(itemData.ItemId, itemData.SlotIndex, itemData.ConfigId, itemData.Count);
            }

            Log.Info($"[ECAClient][Bag] sync bag success: capacity={response.Capacity}, itemCount={response.Items.Count}");
        }
        #endregion YIUIEvent结束
    }
}
