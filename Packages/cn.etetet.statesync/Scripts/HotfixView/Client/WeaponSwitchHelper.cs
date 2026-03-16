namespace ET.Client
{
    /// <summary>
    /// 武器切换辅助类（客户端）
    /// </summary>
    public static class WeaponSwitchHelper
    {
        /// <summary>
        /// 请求切换武器槽位
        /// </summary>
        public static void SwitchWeapon(Scene root, int slotIndex)
        {
            Scene currentScene = root.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null)
            {
                Log.Error("WeaponSwitchHelper: Cannot find my unit");
                return;
            }

            C2M_SwitchWeapon message = C2M_SwitchWeapon.Create();
            message.SlotIndex = slotIndex;

            root.GetComponent<ClientSenderComponent>().Send(message);

            Log.Debug($"WeaponSwitchHelper: Requested switch to slot {slotIndex}");
        }
    }
}
