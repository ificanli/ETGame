namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_SwitchWeaponHandler : MessageHandler<Scene, M2C_SwitchWeapon>
    {
        protected override async ETTask Run(Scene root, M2C_SwitchWeapon message)
        {
            Scene currentScene = root.CurrentScene();
            if (currentScene == null)
            {
                return;
            }

            PendingWeaponSyncComponent pending = currentScene.GetComponent<PendingWeaponSyncComponent>();
            if (pending == null)
            {
                pending = currentScene.AddComponent<PendingWeaponSyncComponent>();
            }

            pending.CacheSwitch(message);
            if (!pending.TryApply(root, message.UnitId))
            {
                Log.Info($"[WeaponInitTrace][ClientSwitch] pending unitId={message.UnitId}, slot={message.SlotIndex}, weaponId={message.WeaponId}");
            }

            await ETTask.CompletedTask;
        }
    }
}
