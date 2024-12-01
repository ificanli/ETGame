namespace ET.Server
{
    [Event(SceneType.Map)]
    public class ChangePosition_RemoveBuff: AEvent<Scene, ChangePosition>
    {
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            unit.GetComponent<BuffComponent>()?.RemoveBuffFlag(BuffFlags.MoveRemove);
            await ETTask.CompletedTask;
        }
    }
}