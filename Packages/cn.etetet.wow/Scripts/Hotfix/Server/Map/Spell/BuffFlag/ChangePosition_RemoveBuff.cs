namespace ET.Server
{
    [Event(SceneType.Map)]
    public class ChangePosition_RemoveBuff: AEvent<Scene, ChangePosition>
    {
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            BuffHelper.RemoveBuffFlag(unit, BuffFlags.MoveRemove);
            await ETTask.CompletedTask;
        }
    }
}