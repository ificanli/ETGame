namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_DiscardWeaponHandler : MessageLocationHandler<Unit, C2M_DiscardWeapon, M2C_DiscardWeapon>
    {
        protected override async ETTask Run(Unit unit, C2M_DiscardWeapon request, M2C_DiscardWeapon response)
        {
            response.Error = await WeaponDiscardHelper.TryDiscardWeaponAsync(unit, request.SlotIndex);
        }
    }
}
