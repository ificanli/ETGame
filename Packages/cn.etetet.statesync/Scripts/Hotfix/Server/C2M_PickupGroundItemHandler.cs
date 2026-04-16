namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_PickupGroundItemHandler : MessageLocationHandler<Unit, C2M_PickupGroundItem, M2C_PickupGroundItem>
    {
        protected override async ETTask Run(Unit unit, C2M_PickupGroundItem request, M2C_PickupGroundItem response)
        {
            (int error, int pickupResult) = await WeaponPickupHelper.TryPickupGroundItemAsync(unit, request.PointId);
            response.Error = error;
            response.PickupResult = pickupResult;
        }
    }
}
