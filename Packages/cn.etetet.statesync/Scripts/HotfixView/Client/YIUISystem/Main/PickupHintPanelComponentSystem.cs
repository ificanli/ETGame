using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 拾取提示面板逻辑
    /// </summary>
    [FriendOf(typeof(PickupHintPanelComponent))]
    public static partial class PickupHintPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this PickupHintPanelComponent self)
        {
            if (self.u_ComSubTitle != null)
            {
                self.u_ComSubTitle.gameObject.SetActive(false);
            }

            SetRaycastTarget(self.u_ComBackground, false);
            SetRaycastTarget(self.u_ComAccent, false);
            self.Hide();
        }

        [EntitySystem]
        private static void Destroy(this PickupHintPanelComponent self)
        {
            self.FocusPointId = null;
            self.ItemConfigId = 0;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this PickupHintPanelComponent self)
        {
            self.RefreshDisplay();
            await ETTask.CompletedTask;
            return true;
        }

        /// <summary>
        /// 拾取按钮点击
        /// </summary>
        [YIUIInvoke(PickupHintPanelComponent.OnEventPickupInvoke)]
        private static async ETTask OnEventPickupInvoke(this PickupHintPanelComponent self)
        {
            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            string pointId = self.FocusPointId;
            if (string.IsNullOrWhiteSpace(pointId))
            {
                // 回退到现有ECA交互流程
                await ECAInteractHelper.TryInteractFocus(root);
                return;
            }

            // 一键拾取
            await GroundItemPickupClientHelper.RequestPickupGroundItem(root, pointId);
        }

        public static void SetPickupTarget(this PickupHintPanelComponent self, string pointId, int itemConfigId)
        {
            self.FocusPointId = pointId;
            self.ItemConfigId = itemConfigId;
            self.RefreshDisplay();
        }

        public static void Show(this PickupHintPanelComponent self, string pointId, int itemConfigId, Vector2 anchoredPosition)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SetPickupTarget(pointId, itemConfigId);
            self.SetAnchoredPosition(anchoredPosition);
            self.UIBase?.SetActive(true);
        }

        public static void Hide(this PickupHintPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.FocusPointId = null;
            self.ItemConfigId = 0;
            self.UIBase?.SetActive(false);
        }

        public static void SetAnchoredPosition(this PickupHintPanelComponent self, Vector2 anchoredPosition)
        {
            RectTransform rectTransform = self?.UIBase?.OwnerRectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void RefreshDisplay(this PickupHintPanelComponent self)
        {
            if (self.ItemConfigId <= 0)
            {
                self.u_DataItemName?.SetValue("拾取", true);
                return;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(self.ItemConfigId);
            string itemName = itemConfig?.Name ?? "物品";
            self.u_DataItemName?.SetValue(itemName, true);
        }

        private static void SetRaycastTarget(Component component, bool raycastTarget)
        {
            Graphic graphic = component != null ? component.GetComponent<Graphic>() : null;
            if (graphic != null)
            {
                graphic.raycastTarget = raycastTarget;
            }
        }
    }
}
