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
    }
}
