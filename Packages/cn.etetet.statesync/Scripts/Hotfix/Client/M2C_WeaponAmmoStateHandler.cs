namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponAmmoStateHandler : MessageHandler<Scene, M2C_WeaponAmmoState>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponAmmoState message)
        {
            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            Unit unit = unitComponent.Get(message.UnitId);
            if (unit == null)
            {
                return;
            }

            // TODO: 更新 UI 显示弹药状态
            // 例如：
            // HUDPanel.UpdateAmmoDisplay(message.Slot1Ammo, message.Slot2Ammo);
            // HUDPanel.UpdateReloadingState(message.Slot1Reloading, message.Slot2Reloading);

            Log.Debug($"Unit {message.UnitId} ammo state: Slot1={message.Slot1Ammo}, Slot2={message.Slot2Ammo}");

            await ETTask.CompletedTask;
        }
    }
}
