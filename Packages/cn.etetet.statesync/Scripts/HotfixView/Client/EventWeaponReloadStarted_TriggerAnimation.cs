namespace ET.Client
{
    /// <summary>
    /// 武器换弹状态变化时，同步 Animator 的 IsReloading / IsFiring Bool。
    /// - 开始换弹：IsReloading=true, IsFiring=false（停止射击循环）
    /// - 结束换弹：IsReloading=false
    /// </summary>
    [Event(SceneType.Client)]
    public class EventWeaponReloadStateChanged_SyncAnimation : AEvent<Scene, EventWeaponReloadStateChanged>
    {
        protected override async ETTask Run(Scene root, EventWeaponReloadStateChanged args)
        {
            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            Unit unit = unitComponent?.Get(args.UnitId);
            if (unit == null)
            {
                return;
            }

            AnimatorComponent animatorComponent = unit.GetComponent<AnimatorComponent>();
            WeaponViewComponent weaponViewComponent = unit.GetComponent<WeaponViewComponent>();
            animatorComponent?.SetBool("IsReloading", args.IsReloading);

            // 开始换弹时，结束连发射击动画
            if (args.IsReloading)
            {
                weaponViewComponent?.StopRapidFireAnimation();
            }

            await ETTask.CompletedTask;
        }
    }
}
