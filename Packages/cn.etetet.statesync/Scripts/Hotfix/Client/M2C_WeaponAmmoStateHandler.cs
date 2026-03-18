namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponAmmoStateHandler : MessageHandler<Scene, M2C_WeaponAmmoState>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponAmmoState message)
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

            pending.CacheAmmo(message);
            if (!pending.TryApply(root, message.UnitId))
            {
                Log.Info($"[WeaponInitTrace][ClientAmmo] pending unitId={message.UnitId}, slot1Ammo={message.Slot1Ammo}, slot2Ammo={message.Slot2Ammo}");
            }

            await ETTask.CompletedTask;
        }
    }
}
