namespace ET.Client
{
    /// <summary>
    /// 监听武器丢弃事件，更新WeaponBar UI
    /// </summary>
    [Event(SceneType.Client)]
    public class EventWeaponDiscarded_RefreshWeaponBar : AEvent<Scene, EventWeaponDiscarded>
    {
        protected override async ETTask Run(Scene scene, EventWeaponDiscarded args)
        {
            Scene root = scene.Root();
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            MainPanelComponent mainPanel = root.YIUIMgr()?.GetPanel<MainPanelComponent>();
            if (mainPanel == null)
            {
                return;
            }

            WeaponBarComponent weaponBar = mainPanel.UIWeaponBar;
            if (weaponBar == null)
            {
                return;
            }

            Scene currentScene = root.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null)
            {
                return;
            }

            WeaponComponent weaponComp = myUnit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                return;
            }

            weaponBar.RefreshWeaponBar(weaponComp);

            await ETTask.CompletedTask;
        }
    }
}
