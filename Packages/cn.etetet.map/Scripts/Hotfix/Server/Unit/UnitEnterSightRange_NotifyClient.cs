namespace ET.Server
{
    // 进入视野通知
    [Event(SceneType.Map)]
    public class UnitEnterSightRange_NotifyClient: AEvent<Scene, UnitEnterSightRange>
    {
        protected override async ETTask Run(Scene scene, UnitEnterSightRange args)
        {
            Unit a = args.A;
            Unit b = args.B;
            if (a.Id == b.Id)
            {
                return;
            }

            if (a.UnitType != UnitType.Player)
            {
                return;
            }

            ExtraUnitVisibilityComponent extraVisibility = scene.GetComponent<ExtraUnitVisibilityComponent>();
            if (extraVisibility != null && extraVisibility.ShouldConcealTargetFromViewer(a, b))
            {
                extraVisibility.MarkTargetConcealed(a, b);
                return;
            }

            if (extraVisibility != null)
            {
                if (extraVisibility.IsTargetSuppressed(a.Id, b.Id))
                {
                    return;
                }

                if (extraVisibility.HasExtraVisibility(a.Id, b.Id) && !extraVisibility.IsTargetHidden(a.Id, b.Id))
                {
                    return;
                }
            }

            MapMessageHelper.NoticeUnitAdd(a, b);
            
            await ETTask.CompletedTask;
        }
    }
}
