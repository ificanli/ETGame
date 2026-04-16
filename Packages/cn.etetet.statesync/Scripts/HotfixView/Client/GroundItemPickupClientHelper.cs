namespace ET.Client
{
    public static class GroundItemPickupClientHelper
    {
        /// <summary>
        /// 一键拾取地面物品
        /// </summary>
        public static async ETTask RequestPickupGroundItem(Scene root, string pointId)
        {
            if (root == null || root.IsDisposed || string.IsNullOrWhiteSpace(pointId))
            {
                return;
            }

            C2M_PickupGroundItem request = C2M_PickupGroundItem.Create();
            request.PointId = pointId;

            M2C_PickupGroundItem response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_PickupGroundItem;
            if (response == null)
            {
                Log.Warning("[GroundPickup] No response");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[GroundPickup] Failed: error={response.Error}, msg={response.Message}");
                return;
            }

            Log.Info($"[GroundPickup] Success: pointId={pointId}, result={response.PickupResult}");
        }
    }
}
