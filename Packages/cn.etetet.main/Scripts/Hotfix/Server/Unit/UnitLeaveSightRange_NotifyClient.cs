namespace ET.Server
{
    // 离开视野
    [Event(SceneType.Map)]
    public class UnitLeaveSightRange_NotifyClient: AEvent<Scene, UnitLeaveSightRange>
    {
        protected override async ETTask Run(Scene scene, UnitLeaveSightRange args)
        {
            Unit a = args.A;
            Unit b = args.B;
            if (a.UnitType != UnitType.Player)
            {
                return;
            }

            MapMessageHelper.NoticeUnitRemove(a, b);
            
            await ETTask.CompletedTask;
        }
    }
}