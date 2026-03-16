namespace ET.Client
{
    /// <summary>
    /// 监听武器弹药状态变化事件，刷新武器栏显示
    /// </summary>
    [Event(SceneType.Client)]
    public class EventWeaponAmmoChanged_RefreshWeaponBar : AEvent<Scene, EventWeaponAmmoChanged>
    {
        protected override async ETTask Run(Scene scene, EventWeaponAmmoChanged args)
        {
            Scene root = scene.Root();
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            MainPanelComponent mainPanel = root.YIUIMgr()?.GetPanel<MainPanelComponent>();
            if (mainPanel?.UIWeaponBar == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            Scene currentScene = root.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null || myUnit.IsDisposed || myUnit.Id != args.UnitId)
            {
                await ETTask.CompletedTask;
                return;
            }

            WeaponComponent weaponComponent = myUnit.GetComponent<WeaponComponent>();
            if (weaponComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            mainPanel.UIWeaponBar.RefreshWeaponBar(weaponComponent);
            await ETTask.CompletedTask;
        }
    }
}
