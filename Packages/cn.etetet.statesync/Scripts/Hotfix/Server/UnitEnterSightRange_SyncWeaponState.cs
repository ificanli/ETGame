namespace ET.Server
{
    /// <summary>
    /// 目标单位进入视野时，同步其当前武器状态，确保客户端能立即播放正确动画和显示武器模型。
    /// </summary>
    [Event(SceneType.Map)]
    public class UnitEnterSightRange_SyncWeaponState : AEvent<Scene, UnitEnterSightRange>
    {
        protected override async ETTask Run(Scene scene, UnitEnterSightRange args)
        {
            Unit viewer = args.A;
            Unit target = args.B;

            WeaponSyncHelper.SendCurrentWeaponStateToViewer(viewer, target);

            await ETTask.CompletedTask;
        }
    }
}
