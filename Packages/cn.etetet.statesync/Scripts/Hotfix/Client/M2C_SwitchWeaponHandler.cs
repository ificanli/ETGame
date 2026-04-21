namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_SwitchWeaponHandler : MessageHandler<Scene, M2C_SwitchWeapon>
    {
        protected override async ETTask Run(Scene root, M2C_SwitchWeapon message)
        {
            long clientNow = TimeInfo.Instance.ClientNow();
            Scene currentScene = root.CurrentScene();
            if (currentScene == null)
            {
                Log.Warning(
                    $"[WeaponSwitchTrace][ClientRecv] clientNow={clientNow}, unitId={message.UnitId}, slot={message.SlotIndex}, weaponId={message.WeaponId}, result=current_scene_missing");
                return;
            }

            PendingWeaponSyncComponent pending = currentScene.GetComponent<PendingWeaponSyncComponent>();
            if (pending == null)
            {
                pending = currentScene.AddComponent<PendingWeaponSyncComponent>();
            }

            pending.CacheSwitch(message);
            bool applied = pending.TryApply(root, message.UnitId);
            Log.Info(
                $"[WeaponSwitchTrace][ClientRecv] clientNow={clientNow}, serverNow={TimeInfo.Instance.ServerNow()}, unitId={message.UnitId}, slot={message.SlotIndex}, weaponId={message.WeaponId}, applied={applied}");
            if (!applied)
            {
                Log.Info($"[WeaponInitTrace][ClientSwitch] pending unitId={message.UnitId}, slot={message.SlotIndex}, weaponId={message.WeaponId}");
            }

            await ETTask.CompletedTask;
        }
    }
}
