namespace ET.Client
{
    public static class WeaponDiscardClientHelper
    {
        public static async ETTask RequestDiscardWeapon(Scene root, int slotIndex)
        {
            Scene currentScene = root.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null)
            {
                Log.Error("WeaponDiscardClientHelper: Cannot find my unit");
                return;
            }

            WeaponComponent weaponComp = myUnit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                return;
            }

            int weaponId = weaponComp.GetWeaponId(slotIndex);
            if (weaponId <= 0)
            {
                return;
            }

            C2M_DiscardWeapon request = C2M_DiscardWeapon.Create();
            request.SlotIndex = slotIndex;

            M2C_DiscardWeapon response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_DiscardWeapon;
            if (response == null)
            {
                Log.Warning("WeaponDiscardClientHelper: No response");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"WeaponDiscardClientHelper: Discard failed, error={response.Error}");
            }
        }
    }
}
