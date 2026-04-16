namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponDiscardedHandler : MessageHandler<Scene, M2C_WeaponDiscarded>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponDiscarded message)
        {
            Scene currentScene = root.CurrentScene();
            if (currentScene == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            Unit unit = unitComponent?.Get(message.UnitId);
            if (unit == null || unit.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            WeaponComponent weaponComp = unit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                weaponComp = unit.AddComponent<WeaponComponent, int, int>(message.Slot1WeaponId, message.Slot2WeaponId);
            }
            else
            {
                weaponComp.Slot1WeaponId = message.Slot1WeaponId;
                weaponComp.Slot2WeaponId = message.Slot2WeaponId;
                weaponComp.CurrentSlot = message.CurrentSlot;

                // 清空被移除槽位的弹药
                if (message.Slot1WeaponId == 0)
                {
                    weaponComp.Slot1Ammo = 0;
                    weaponComp.Slot1Reloading = false;
                }

                if (message.Slot2WeaponId == 0)
                {
                    weaponComp.Slot2Ammo = 0;
                    weaponComp.Slot2Reloading = false;
                }
            }

            EventSystem.Instance.Publish(root, new EventWeaponDiscarded
            {
                Scene = root,
                UnitId = message.UnitId,
            });

            await ETTask.CompletedTask;
        }
    }
}
