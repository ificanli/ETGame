namespace ET.Client
{
    /// <summary>
    /// 武器槽位变化后，同步角色的武器模型和持枪动画状态。
    /// </summary>
    [Event(SceneType.Client)]
    public class EventWeaponSwitched_SyncWeaponView : AEvent<Scene, EventWeaponSwitched>
    {
        protected override async ETTask Run(Scene root, EventWeaponSwitched args)
        {
            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            Unit unit = unitComponent.Get(args.UnitId);
            if (unit == null)
            {
                return;
            }

            WeaponViewComponent weaponViewComponent = unit.GetComponent<WeaponViewComponent>();
            if (weaponViewComponent == null)
            {
                weaponViewComponent = unit.AddComponent<WeaponViewComponent>();
            }

            await WeaponViewComponentSystem.RefreshWeaponAsync(weaponViewComponent, root, args.WeaponId);
        }
    }
}
