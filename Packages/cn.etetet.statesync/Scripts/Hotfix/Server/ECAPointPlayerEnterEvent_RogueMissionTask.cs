namespace ET.Server
{
    [Event(SceneType.Map)]
    public class ECAPointPlayerEnterEvent_RogueMissionTask : AEvent<Scene, ECAPointPlayerEnterEvent>
    {
        protected override async ETTask Run(Scene scene, ECAPointPlayerEnterEvent args)
        {
            if (RogueMissionTaskPointHelper.HasTaskConfig(args.Point))
            {
                RogueMissionTaskECAActionRegistrar.EnsureRegistered();
            }

            await ETTask.CompletedTask;
        }
    }
}
