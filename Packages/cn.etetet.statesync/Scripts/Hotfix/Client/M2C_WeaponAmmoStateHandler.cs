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

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null)
            {
                weaponComponent = unit.AddComponent<WeaponComponent, int, int>(0, 0);
            }

            weaponComponent.Slot1Ammo = message.Slot1Ammo;
            weaponComponent.Slot2Ammo = message.Slot2Ammo;
            weaponComponent.Slot1Reloading = message.Slot1Reloading;
            weaponComponent.Slot2Reloading = message.Slot2Reloading;
            weaponComponent.Slot1EffectiveMagazineSize = message.Slot1MagazineSize;
            weaponComponent.Slot2EffectiveMagazineSize = message.Slot2MagazineSize;
            weaponComponent.Slot1EffectiveAttackRange = message.Slot1AttackRange;
            weaponComponent.Slot2EffectiveAttackRange = message.Slot2AttackRange;

            Log.Debug($"Unit {message.UnitId} ammo state: Slot1={message.Slot1Ammo}, Slot2={message.Slot2Ammo}");
            EventSystem.Instance.Publish(root, new EventWeaponAmmoChanged
            {
                Scene = root,
                UnitId = message.UnitId,
            });

            await ETTask.CompletedTask;
        }
    }
}
